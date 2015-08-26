using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using GoogleAppEngine.Datastore.LINQ;
using GoogleAppEngine.Shared;

namespace GoogleAppEngine.Tests
{
    public abstract class DatastoreTestProvider : IQueryProvider
    {
        protected DatastoreTestProvider()
        {
        }

        IQueryable<TS> IQueryProvider.CreateQuery<TS>(Expression expression)
        {
            return new DatastoreTestQueryable<TS>(this, expression);
        }

        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            Type elementType = TypeSystem.GetElementType(expression.Type);
            try
            {
                return (IQueryable)Activator.CreateInstance(typeof(DatastoreQueryable<>).MakeGenericType(elementType), new object[] { this, expression });
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        TS IQueryProvider.Execute<TS>(Expression expression)
        {
            return (TS)this.Execute(expression);
        }

        object IQueryProvider.Execute(Expression expression)
        {
            return this.Execute(expression);
        }

        public abstract string GetQueryText(Expression expression);
        public abstract object Execute(Expression expression);
        public abstract CloudAuthenticator GetAuthenticator();
    }
}
