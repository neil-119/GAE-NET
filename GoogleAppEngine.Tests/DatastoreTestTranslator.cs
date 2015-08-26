using System;
using System.Linq;
using System.Linq.Expressions;
using GoogleAppEngine.Datastore.LINQ;

namespace GoogleAppEngine.Tests
{
    public class DatastoreTestTranslator<T> : DatastoreTestProvider
        where T : new()
    {
        private string _query;

        public string GetQueryText()
        {
            return _query;
        }

        public override string GetQueryText(Expression expression)
        {
            Translate(expression);
            return _query;
        }

        private State Translate(Expression expression)
        {
            while (expression.CanReduce)
                expression = expression.ReduceAndCheck();

            var state = new DatastoreExpressionVisitor<T>().Translate(expression);
            var query = state.QueryBuilder.ToString();

            _query = state.Parameters.Aggregate(query, (current, p) =>
                current.Replace(p.ParameterName,
                    p.TypeCode == TypeCode.DateTime
                        ? QueryHelper.NormalizeDatetime((DateTime)p.Value)
                        : Convert.ToString(p.Value)));

            return state;
        }

        public override object Execute(Expression expression)
        {
            var s = Translate(expression);

            if (s.QueryState.HasFlag(QueryState.IsAny))
                return true;

            return null;
        }

        public override CloudAuthenticator GetAuthenticator()
        {
            throw new NotImplementedException();
        }
    }
}