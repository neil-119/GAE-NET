using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Google.Apis.Datastore.v1beta2.Data;
using GoogleAppEngine.Datastore.LINQ;
using GoogleAppEngine.Shared;

namespace GoogleAppEngine.Datastore.LINQ
{

    public class DatastoreExpressionVisitor<T> : ExpressionVisitor, IDatastoreQueryVisitor
    {
        private readonly State _state;

        public DatastoreExpressionVisitor()
        {
            _state = new State
            {
                IdField = QueryHelper.GetIdProperty<T>()
            };
        }

        public State Translate(Expression expression)
        {
            Visit(expression);

            _state.Projector = _state.Projection != null
                ? Expression.Lambda(_state.Projection.Selector, _state.ParameterExpression)
                : null;

            if (!_state.Parameters.Any() && _state.QueryState.HasFlag(QueryState.HasConstantBoolean))
            {
                // Remove the where clause
                _state.QueryBuilder.RemoveAll(x => x.ComponentType == QueryComponentType.QueryPartWhere);
            }

            return _state;
        }

        private static Expression StripQuotes(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
            {
                e = ((UnaryExpression)e).Operand;
            }
            return e;
        }

        private void VisitWhere(LambdaExpression lambda)
        {
            if (_state.QueryBuilder.All(x => x.ComponentType != QueryComponentType.QueryPartWhere))
                _state.QueryBuilder.Append(QueryComponentType.QueryPartWhere);
            else
                _state.QueryBuilder.Append(QueryComponentType.QueryPartAnd);
            
            Visit(lambda.Body);
        }
        
        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            switch (m.Method.Name)
            {
                case "Where":
                {
                    Visit(m.Arguments[0]);
                    var lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                    VisitWhere(lambda);
                    return m;
                }

                case "Select":
                {
                    var lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                    var projection = new ColumnProjector().ProjectColumns(lambda.Body, _state.ParameterExpression);
                    _state.QueryBuilder.Append(QueryComponentType.QueryPartSelect);
                    _state.QueryBuilder.Append(QueryComponentType.QueryPartSelectProjection,
                        string.IsNullOrWhiteSpace(projection.Columns) ? "*" : projection.Columns);
                    _state.QueryBuilder.Append(QueryComponentType.QueryPartFrom);
                    Visit(m.Arguments[0]);
                    _state.Projection = projection;
                    return m;
                }

                case "Take":
                {
                    if (_state.QueryBuilder.Any(x => x.ComponentType == QueryComponentType.QueryPartTake))
                        throw new InvalidOperationException("Take() was already called once.");

                    Visit(m.Arguments[0]);
                    _state.QueryBuilder.Append(QueryComponentType.QueryPartTake);
                    _state.QueryState |= QueryState.IsTakeMethod;
                    Visit(m.Arguments[1]);
                    _state.QueryState &= ~QueryState.IsTakeMethod;
                    return m;
                }

                case "First":
                case "FirstOrDefault":
                case "Single":
                case "SingleOrDefault":
                case "Any":
                {
                    if (m.Arguments[0].NodeType == ExpressionType.MemberAccess)
                        throw new NotSupportedException($"`{m.Method.Name}` cannot be nested within another LINQ query.");

                    if (_state.QueryBuilder.Any(x => x.ComponentType == QueryComponentType.QueryPartTake))
                        throw new InvalidOperationException(
                            $"{m.Method.Name}() cannot be called when Take() is called. `Take` is unnecessary " +
                            $"here since calling `{m.Method.Name}` automatically results in a LIMIT of 1.");

                    // They shouldn't be able to call twice, but provided just in case.
                    if (_state.QueryState.HasFlag(QueryState.IsFirst) || _state.QueryState.HasFlag(QueryState.IsSingle) ||
                            _state.QueryState.HasFlag(QueryState.IsAny))
                        throw new InvalidOperationException(
                            $"{m.Method.Name}() cannot be called since Any(), First(), FirstOrDefault(), Single(), or SingleOrDefault() is already called.");
                    
                    _state.QueryBuilder.Append(QueryComponentType.QueryPartTake);

                    if (m.Method.Name == "Any")
                    {
                        _state.QueryState |= QueryState.IsAny;
                        AddTake(1);
                    }
                    else if (m.Method.Name.StartsWith("First"))
                    {
                        _state.QueryState |= QueryState.IsFirst;
                        AddTake(1);
                    }
                    else
                    {
                        _state.QueryState |= QueryState.IsSingle;
                        AddTake(2); // Get more than one entity in the result set so we'll know when to throw
                    }

                    if (m.Method.Name.Contains("Default"))
                        _state.QueryState |= QueryState.AllowFirstSingleOrDefault;
                    
                    Visit(m.Arguments[0]);

                    if (m.Arguments.Count == 2)
                    {
                        var lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                        VisitWhere(lambda);
                    }

                    return m;
                }

                case "Skip":
                    if (_state.QueryBuilder.Any(x => x.ComponentType == QueryComponentType.QueryPartSkip))
                        throw new InvalidOperationException("Skip() was already called once.");

                    Visit(m.Arguments[0]);
                    _state.QueryBuilder.Append(QueryComponentType.QueryPartSkip);
                    _state.QueryState |= QueryState.IsSkipMethod;
                    Visit(m.Arguments[1]);
                    _state.QueryState &= ~QueryState.IsSkipMethod;
                    return m;

                case "OrderBy":
                case "OrderByDescending":
                {
                    if (_state.QueryBuilder.Any(x => x.ComponentType == QueryComponentType.QueryPartOrderBy 
                                                     || x.ComponentType == QueryComponentType.QueryPartOrderByDesc))
                        throw new NotSupportedException("Multiple ordering is not yet supported.");

                    Visit(m.Arguments[0]);
                    var lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                    _state.QueryBuilder.Append(m.Method.Name == "OrderBy" ? QueryComponentType.QueryPartOrderBy : QueryComponentType.QueryPartOrderByDesc);
                    _state.QueryState |= QueryState.IsOrderByMethod;
                    Visit(lambda.Body);
                    _state.QueryState &= ~QueryState.IsOrderByMethod;
                    return m;
                }
            }


            throw new NotSupportedException($"The method '{m.Method.Name}' is not supported");
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            switch (u.NodeType)
            {
                case ExpressionType.Not:
                    _state.QueryState |= QueryState.IsUnaryNot;
                    var memberName = (u.Operand as MemberExpression)?.Member?.Name;
                    if (memberName != null)
                    {
                        Visit(u.Operand);

                        var p = _state.GetParameter();
                        p.Value = false;
                        p.TypeCode = TypeCode.Boolean;
                        _state.Parameters.Add(p);

                        _state.QueryBuilder.Append(QueryComponentType.EqualsParameter, $" = @{p.ParameterName}");
                    }
                    _state.QueryState &= ~QueryState.IsUnaryNot;
                    break;

                case ExpressionType.Convert:
                    var operand = this.Visit(u.Operand);
                    if (operand != u.Operand)
                        return Expression.MakeUnary(u.NodeType, operand, u.Type, u.Method);
                    return u;

                default:
                {
                    string methodName = "";

                    if (u.Operand is MethodCallExpression)
                    {
                        var methodExpression = (MethodCallExpression) u.Operand;
                        methodName = methodExpression.Method.Name;
                    }
                    else
                        methodName = ((MemberExpression) u.Operand).Member.Name;

                    throw new NotSupportedException(
                        $"The operator '{u.NodeType}' on '{methodName}' is not supported by Datastore GQL.");
                }
            }

            return u;
        }

        private void AddTrueParameter()
        {
            var p = _state.GetParameter();
            p.Value = true;
            p.TypeCode = TypeCode.Boolean;
            _state.Parameters.Add(p);
            _state.QueryBuilder.Append(QueryComponentType.EqualsParameter, $" = @{p.ParameterName}");
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            _state.BinaryNestingLevel++;
            Visit(b.Left);

            if (b.Left.Type == typeof(bool) && b.NodeType == ExpressionType.AndAlso
                && !_state.QueryState.HasFlag(QueryState.IsUnaryNot) && !IsParameterEnding())
            {
                AddTrueParameter();
            }

            // First check whether an inequality operator has been used already
            var propertyName = (b.Left as MemberExpression)?.Member.Name ?? (b.Right as MemberExpression)?.Member.Name;

            switch (b.NodeType)
            {
                case ExpressionType.NotEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                    {
                        if (propertyName == null)
                            break;

                        if (_state.QueryState.HasFlag(QueryState.HasInequality) && _state.InequalityProperty != propertyName)
                            throw new NotSupportedException($"'{b.NodeType}' on '{propertyName}' failed. There is already an inequality " +
                                                            $"comparison '{_state.InequalityType}' on '{_state.InequalityProperty}'. " +
                                                            $"For performance reasons, Datastore limits inequality comparisons to one property (but " +
                                                            $"multiple inequality comparisons on that one property are allowed).");

                        _state.QueryState |= QueryState.HasInequality;
                        _state.InequalityProperty = propertyName;
                        _state.InequalityType = b.NodeType.ToString();
                        break;
                    }
            }

            switch (b.NodeType)
            {
                case ExpressionType.AndAlso:
                case ExpressionType.And:
                    _state.QueryBuilder.Append(QueryComponentType.QueryPartAnd);
                    break;
                case ExpressionType.OrElse:
                case ExpressionType.Or:
                    throw new NotSupportedException("The usage of the OR (|| operator) is not supported by Google Query Language. " +
                                                    "Instead, use AsEnumerable() to filter in-memory. See the GitHub project wiki for more details.");
                    break;
                case ExpressionType.Equal:
                    if (!IsParameterEnding())
                        _state.QueryBuilder.Append(QueryComponentType.OperatorEquals);
                    break;
                case ExpressionType.NotEqual:
                    _state.QueryBuilder.Append(QueryComponentType.OperatorNotEqualTo); // TODO Datastore won't run this, abstract this out
                    throw new NotSupportedException("For performance reasons, the != operator is currently not supported by Datastore. See GAE.NET's Github wiki for workarounds.");
                    break;
                case ExpressionType.LessThan:
                    _state.QueryBuilder.Append(QueryComponentType.OperatorLessThan);
                    break;
                case ExpressionType.LessThanOrEqual:
                    _state.QueryBuilder.Append(QueryComponentType.OperatorLessThanOrEqual);
                    break;
                case ExpressionType.GreaterThan:
                    _state.QueryBuilder.Append(QueryComponentType.OperatorGreaterThan);
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    _state.QueryBuilder.Append(QueryComponentType.OperatorGreaterThanOrEqual);
                    break;
                default:
                    throw new NotSupportedException($"The binary operator '{b.NodeType}' is not supported");
            }

            Visit(b.Right);

            if (b.Right.Type == typeof(bool) && b.NodeType == ExpressionType.AndAlso
                && !_state.QueryState.HasFlag(QueryState.IsUnaryNot) && !IsParameterEnding())
            {
                AddTrueParameter();
            }

            _state.BinaryNestingLevel--;
            return b;
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            return Visit(node.Body);
        }

        private bool IsParameterEnding()
        {
            var lastComponentType = _state.QueryBuilder.LastOrDefault()?.ComponentType;
            return lastComponentType == QueryComponentType.EqualsParameter ||
                   lastComponentType == QueryComponentType.Parameter;
        }

        private void AddTake(long take)
        {
            _state.Parameters.Add(new Parameter
            {
                ParameterName = Constants.QueryPartTakeParameterName,
                TypeCode = TypeCode.Int64,
                Value = take
            });
        }

        private void AddSkip(long skip)
        {
            _state.Parameters.Add(new Parameter
            {
                ParameterName = Constants.QueryPartSkipParameterName,
                TypeCode = TypeCode.Int64,
                Value = skip
            });
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            if (IsParameterEnding())
                return c;

            var q = c.Value as IQueryable;

            if (q != null)
            {
                _state.QueryBuilder.Append(QueryComponentType.QueryPartSelect);
                _state.QueryBuilder.Append(QueryComponentType.QueryPartSelectProjection, "*");
                _state.QueryBuilder.Append(QueryComponentType.QueryPartFrom);
                _state.QueryBuilder.Append(QueryComponentType.QueryPartKind, q.ElementType.Name);
                return c;
            }
            else if (_state.QueryState.HasFlag(QueryState.IsTakeMethod))
            {
                AddTake(Convert.ToInt64(c.Value));
            }
            else if (_state.QueryState.HasFlag(QueryState.IsSkipMethod))
            {
                AddSkip(Convert.ToInt64(c.Value));
            }
            else
            {
                if (c.Value == null)
                {
                    _state.QueryBuilder.Append(QueryComponentType.Constant, "NULL");
                    return c;
                }

                if (_state.QueryState.HasFlag(QueryState.HasMember))
                {
                    // If this is a constant but the previous was a query operator
                    if (_state.QueryBuilder.LastOrDefault()?.ComponentType == QueryComponentType.QueryPartAnd)
                    {
                        if ((bool)c.Value)
                            return c;

                        throw new InvalidOperationException($"Invalid constant expression `{c.Value}`");
                    }
                }
                else
                {
                    // if it's a simple (x => true) lambda, then just return.
                    if (c.Value.GetType().GetTypeCode() == TypeCode.Boolean && (bool)c.Value)
                    {
                        _state.QueryState |= QueryState.HasConstantBoolean;
                        return c;
                    }
                    throw new InvalidOperationException($"Invalid constant expression `{c.Value}`");
                }

                var p = _state.GetParameter();

                // TODO If this is a key, then we have some extra work to do
                if (_state.IdField != null &&
                    _state.QueryBuilder.LastOrDefault(x => x.ComponentType == QueryComponentType.MemberName)?.Component == "__key__")
                    p.Value = $"KEY({typeof (T).Name}, \"{c.Value}\")";
                else
                    p.Value = c.Value;

                p.TypeCode = c.Value.GetType().GetTypeCode();

                _state.Parameters.Add(p);
                _state.QueryBuilder.Append(QueryComponentType.Parameter, $"@{p.ParameterName}");
            }
            return c;
        }

        protected override Expression VisitMember(MemberExpression m)
        {
            if (m.Expression != null)
            {
                if (m.Expression.NodeType == ExpressionType.Parameter)
                {
                    if (_state.QueryState.HasFlag(QueryState.IsOrderByMethod))
                    {
                        _state.QueryBuilder.Append(QueryComponentType.QueryPartOrderByPart, m.Member.Name);
                        return m;
                    }
                    else
                    {
                        _state.QueryState |= QueryState.HasMember;

                        if (_state.IdField != null && _state.IdField.Name == m.Member.Name)
                            _state.QueryBuilder.Append(QueryComponentType.MemberName, "__key__");
                        else
                            _state.QueryBuilder.Append(QueryComponentType.MemberName, m.Member.Name);

                        if (m.Type == typeof(bool) && !_state.QueryState.HasFlag(QueryState.IsUnaryNot)
                            && _state.BinaryNestingLevel == 0 && !IsParameterEnding())
                        {
                            AddTrueParameter();
                        }

                        return m;
                    }
                }
                else if (m.Expression.NodeType == ExpressionType.Constant)
                {
                    VisitConstant(GetMemberConstant(m));
                    return m;
                }
            }
            throw new NotSupportedException($"The member '{m.Member.Name}' is not supported");
        }

        private ConstantExpression GetMemberConstant(MemberExpression node)
        {
            object value = GetFieldValue(node) ?? GetPropertyValue(node);

            if (value == null)
                throw new NotSupportedException();

            return Expression.Constant(value, node.Type);
        }
        private object GetFieldValue(MemberExpression node)
        {
            var fieldInfo = node.Member as FieldInfo;

            if (fieldInfo == null)
                return null;

            var instance = (node.Expression == null) ? null : MakeConstantExpression(node.Expression).Value;
            return fieldInfo.GetValue(instance);
        }

        private object GetPropertyValue(MemberExpression node)
        {
            var propertyInfo = node.Member as PropertyInfo;

            if (propertyInfo == null)
                return null;

            var instance = (node.Expression == null) ? null : MakeConstantExpression(node.Expression).Value;
            return propertyInfo.GetValue(instance, null);
        }

        private ConstantExpression MakeConstantExpression(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Constant)
                return (ConstantExpression)expression;

            throw new NotSupportedException();
        }
    }
}
