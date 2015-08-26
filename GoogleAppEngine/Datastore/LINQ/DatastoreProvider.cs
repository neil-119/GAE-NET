using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Google.Apis.Datastore.v1beta2.Data;
using GoogleAppEngine.Datastore;
using GoogleAppEngine.Datastore.Indexing;
using GoogleAppEngine.Datastore.LINQ;
using GoogleAppEngine.Datastore.Serialization;
using GoogleAppEngine.Shared;

namespace GoogleAppEngine.Datastore.LINQ
{
    public abstract class DatastoreProvider : IQueryProvider
    {
        protected readonly CloudAuthenticator Authenticator;
        protected readonly DatastoreConfiguration Configuration;
        protected IIndexContainer IndexContainer;

        protected DatastoreProvider(CloudAuthenticator authenticator, DatastoreConfiguration configuration, IIndexContainer indexContainer)
        {
            if (authenticator == null)
                throw new NullReferenceException(nameof(authenticator));

            if (configuration == null)
                throw new NullReferenceException(nameof(configuration));

            if (indexContainer == null)
                throw new NullReferenceException(nameof(indexContainer));

            Authenticator = authenticator;
            Configuration = configuration;
            IndexContainer = indexContainer;
        }

        IQueryable<TS> IQueryProvider.CreateQuery<TS>(Expression expression)
        {
            return new DatastoreQueryable<TS>(this, expression);
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
        protected abstract void BuildIndex(State state);
    }
}
