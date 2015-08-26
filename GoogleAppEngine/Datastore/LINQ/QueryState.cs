using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using GoogleAppEngine.Datastore.Indexing;

namespace GoogleAppEngine.Datastore.LINQ
{
    [Flags]
    public enum QueryState
    {
        None = 0,
        HasInequality = 1,
        IsUnaryNot = 1 << 1,
        HasMember = 1 << 2,
        HasConstantBoolean = 1 << 3,
        //IsBinaryMethod = 1 << 4,
        IsTakeMethod = 1 << 5,
        IsOrderByMethod = 1 << 6,
        IsSkipMethod = 1 << 7,
        IsFirst = 1 << 8,
        IsSingle = 1 << 9,
        AllowFirstSingleOrDefault = 1 << 10,
        IsAny = 1 << 11,
    }

    public class State
    {
        public QueryState QueryState { get; set; } = QueryState.None;
        public string InequalityProperty { get; set; }
        public string InequalityType { get; set; }
        public int ParameterCount { get; set; } = 0;
        public List<Parameter> Parameters { get; } = new List<Parameter>();
        public QueryBuilder QueryBuilder { get; } = new QueryBuilder();
        internal ParameterExpression ParameterExpression { get; set; }
        internal ColumnProjection Projection { get; set; }
        internal LambdaExpression Projector { get; set; }
        public PropertyInfo IdField { get; set; }
        public List<KeyValuePair<string, Index.OrderingType>> PropertyOrdering { get; set; }
            = new List<KeyValuePair<string, Index.OrderingType>>();

        public int BinaryNestingLevel = 0;

        public State()
        {
            ParameterExpression = Expression.Parameter(typeof(ProjectionRow), "row");
        }

        public Parameter GetParameter()
        {
            return new Parameter
            {
                ParameterName = "p" + (++ParameterCount)
            };
        }
    }
}
