// Projector by Matt Warren - MSFT
// See http://blogs.msdn.com/b/mattwar/archive/2007/08/02/linq-building-an-iqueryable-provider-part-iv.aspx

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GoogleAppEngine.Shared;

namespace GoogleAppEngine.Datastore.LINQ
{
    internal class ColumnProjection
    {
        internal string Columns;
        internal Expression Selector;
    }

    internal class ColumnProjector : ExpressionVisitor
    {
        StringBuilder _sb;
        ParameterExpression _row;
        static MethodInfo _miGetValue;

        internal ColumnProjector()
        {
            if (_miGetValue == null)
                _miGetValue = typeof(ProjectionRow).GetMethod("GetValue");
        }

        internal ColumnProjection ProjectColumns(Expression expression, ParameterExpression parameterExpression)
        {
            _sb = new StringBuilder();
            _row = parameterExpression;
            var selector = this.Visit(expression);
            return new ColumnProjection { Columns = this._sb.ToString(), Selector = selector };
        }

        protected override Expression VisitMember(MemberExpression m)
        {
            if (m.Expression != null && m.Expression.NodeType == ExpressionType.Parameter)
            {
                if (_sb.Length > 0)
                    _sb.Append(", ");

                _sb.Append((m.Member.Name.ToLower() == "id" 
                    || m.Member.CustomAttributes.Any(x => x.AttributeType == typeof(DatastoreKeyAttribute)))
                    ? "__key__" : m.Member.Name);
                var typeCode = ((PropertyInfo)m.Member).PropertyType.GetTypeCode();
                return Expression.Convert(Expression.Call(this._row, _miGetValue, Expression.Constant(m.Member.Name), Expression.Constant(typeCode)), m.Type);
            }

            return base.VisitMember(m);
        }
    }
}