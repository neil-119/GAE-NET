using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace GoogleAppEngine.Tests
{
    public class DatastoreTestQueryable<T> : IOrderedQueryable<T>
    {
        private DatastoreTestProvider _provider;
        private Expression _expression;

        public DatastoreTestQueryable(DatastoreTestProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            this._provider = provider;
            this._expression = Expression.Constant(this);
        }

        public DatastoreTestQueryable(DatastoreTestProvider provider, Expression expression)
            : this(provider)
        {
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

        public IEnumerator<T> GetEnumerator()
        {
            _provider.Execute(this._expression);
            return ((IEnumerable<T>)(new List<T>())).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            this._provider.Execute(this._expression);
            return ((IEnumerable)(new List<T>())).GetEnumerator();
        }

        public override string ToString()
        {
            return this._provider.GetQueryText(this._expression);
        }
    }
}
