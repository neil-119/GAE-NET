using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleAppEngine.Datastore.LINQ
{
    public enum QueryComponentType
    {
        Constant,
        EqualsParameter,
        Parameter,
        MemberName,
        QueryPartWhere,
        QueryPartAnd,
        QueryPartSelect,
        QueryPartSelectProjection,
        QueryPartFrom,
        QueryPartKind,
        OperatorEquals,
        OperatorNotEqualTo,
        OperatorLessThan,
        OperatorGreaterThan,
        OperatorGreaterThanOrEqual,
        OperatorLessThanOrEqual,
        QueryPartTake,
        QueryPartSkip,
        QueryPartOrderBy,
        QueryPartOrderByDesc,
        QueryPartOrderByPart,
    }

    public class QueryComponent
    {
        public QueryComponentType ComponentType { get; set; }
        public string Component { get; set; }
    }
}
