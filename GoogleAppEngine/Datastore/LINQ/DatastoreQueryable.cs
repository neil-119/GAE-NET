using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Google.Apis.Datastore.v1beta2.Data;
using GoogleAppEngine.Shared;

namespace GoogleAppEngine.Datastore.LINQ
{
    public class DatastoreQueryable<T> : IOrderedQueryable<T>
    {
        private DatastoreProvider _provider;
        private Expression _expression;

        public DatastoreQueryable(DatastoreProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            
            this._provider = provider;
            this._expression = Expression.Constant(this);
        }

        public DatastoreQueryable(DatastoreProvider provider, Expression expression)
            : this(provider)
        {
            if (!typeof(IQueryable<T>).GetTypeInfo().IsAssignableFrom(expression.Type.GetTypeInfo()))
                throw new ArgumentOutOfRangeException(nameof(expression));

            this._expression = expression;
        }

        Expression IQueryable.Expression
        {
            get { return this._expression; }
        }

        Type IQueryable.ElementType
        {
            get { return typeof(T); }
        }

        IQueryProvider IQueryable.Provider
        {
            get { return this._provider; }
        }

        private object GetExecuteResult()
        {
            var res = _provider.Execute(_expression);
            if (res == null)
                throw new NullReferenceException("Cannot enumerate over a null result.");
            return res;
        }

        public IEnumerator<T> GetEnumerator()
        {
            
            return ((IEnumerable<T>)GetExecuteResult()).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)GetExecuteResult()).GetEnumerator();
        }

        public override string ToString()
        {
            return this._provider.GetQueryText(this._expression);
        }
    }
}
