using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleAppEngine.Datastore.LINQ
{
    public class QueryBuilder : List<QueryComponent>
    {
        public void Append(QueryComponentType type, string val = null)
        {
            switch (type)
            {
                // Don't add duplicates
                case QueryComponentType.QueryPartKind:
                case QueryComponentType.QueryPartTake:
                case QueryComponentType.QueryPartSkip:
                case QueryComponentType.QueryPartOrderByPart:
                case QueryComponentType.QueryPartOrderBy:
                case QueryComponentType.QueryPartOrderByDesc:
                case QueryComponentType.QueryPartSelect:
                case QueryComponentType.QueryPartFrom:
                case QueryComponentType.QueryPartWhere:
                case QueryComponentType.QueryPartSelectProjection:
                    if (this.Any(x => x.ComponentType == type))
                        return;
                    break;
            }

            Add(new QueryComponent { Component = val, ComponentType = type });
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            // Query parts that must come at the end
            var take = false;
            var skip = false;
            var orderBy = -1;
            var orderByProperty = string.Empty;

            foreach (var queryComponent in this)
            {
                switch (queryComponent.ComponentType)
                {
                    case QueryComponentType.QueryPartSelectProjection:
                    case QueryComponentType.QueryPartKind:
                    case QueryComponentType.Parameter:
                    case QueryComponentType.MemberName:
                    case QueryComponentType.EqualsParameter:
                    case QueryComponentType.Constant:
                        if (string.IsNullOrWhiteSpace(queryComponent.Component))
                            throw new Exception("Literal or parameter cannot be null");
                        builder.Append($"{queryComponent.Component}");
                        break;

                    case QueryComponentType.OperatorNotEqualTo:
                        builder.Append(" != "); // TODO GQL won't run this -- abstract this out
                        break;

                    case QueryComponentType.OperatorEquals:
                        builder.Append(" = ");
                        break;

                    case QueryComponentType.OperatorGreaterThan:
                        builder.Append(" > ");
                        break;

                    case QueryComponentType.OperatorGreaterThanOrEqual:
                        builder.Append(" >= ");
                        break;

                    case QueryComponentType.OperatorLessThan:
                        builder.Append(" < ");
                        break;

                    case QueryComponentType.OperatorLessThanOrEqual:
                        builder.Append(" <= ");
                        break;

                    case QueryComponentType.QueryPartAnd:
                        builder.Append(" AND ");
                        break;

                    case QueryComponentType.QueryPartFrom:
                        builder.Append(" FROM ");
                        break;

                    case QueryComponentType.QueryPartSelect:
                        builder.Append(" SELECT ");
                        break;

                    case QueryComponentType.QueryPartWhere:
                        builder.Append(" WHERE ");
                        break;

                    case QueryComponentType.QueryPartTake:
                        take = true;
                        break;

                    case QueryComponentType.QueryPartSkip:
                        skip = true;
                        break;

                    case QueryComponentType.QueryPartOrderBy:
                        orderBy = 0;
                        break;

                    case QueryComponentType.QueryPartOrderByDesc:
                        orderBy = 1;
                        break;

                    case QueryComponentType.QueryPartOrderByPart:
                        orderByProperty = queryComponent.Component;
                        break;
                }
            }
            
            if (orderBy >= 0 && !string.IsNullOrWhiteSpace(orderByProperty))
                builder.Append($" ORDER BY {orderByProperty} " + (orderBy == 0 ? "ASC" : "DESC"));

            if (take)
                builder.Append($" LIMIT @{Constants.QueryPartTakeParameterName}");

            if (skip)
                builder.Append($" OFFSET @{Constants.QueryPartSkipParameterName}");

            return builder.ToString().Trim();
        }
    }
}
