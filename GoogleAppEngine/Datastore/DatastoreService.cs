using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Google.Apis.Datastore.v1beta2.Data;
using GoogleAppEngine.Datastore.LINQ;
using GoogleAppEngine.Datastore.Indexing;
using GoogleAppEngine.Datastore;
using GoogleAppEngine.Datastore.Serialization;
using GoogleAppEngine.Shared;

namespace GoogleAppEngine.Datastore
{
    public sealed class DatastoreService : GAEService, IDatastoreService
    {
        internal DatastoreConfiguration Configuration;
        internal static IIndexContainer IndexContainer;

        public DatastoreService(CloudAuthenticator authenticator) : base(authenticator)
        {
            if (authenticator == null)
                throw new ArgumentNullException(nameof(authenticator));

            if (IndexContainer == null)
                IndexContainer = new InMemoryIndexContainer();

            Configuration = new DatastoreConfiguration(); // default config
        }

        public DatastoreService(CloudAuthenticator authenticator, DatastoreConfiguration configuration) 
            : this(authenticator)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            Configuration = configuration;
        }
        
        private IDatastoreSerializer<T> GetSerializer<T>()
            where T : new()
        {
            return new DatastoreSerializer<T>();
        } 

        private DatastoreProvider GetDatastoreProvider<T>()
            where T : new()
        {
            return new DatastoreTranslatorProvider<T, DatastoreExpressionVisitor<T>, DatastoreSerializer<T>>(Authenticator, Configuration, IndexContainer);
        }

        /// <summary>
        /// Execute a LINQ query with your class objects.
        /// </summary>
        /// <typeparam name="T">Your class</typeparam>
        /// <returns>Queried objects</returns>
        public IOrderedQueryable<T> Find<T>()
            where T : new()
        {
            return new DatastoreQueryable<T>(GetDatastoreProvider<T>());
        }

        /// <summary>
        /// Executes a GQL query.
        /// </summary>
        /// <param name="gqlQuery"></param>
        public RunQueryResponse Gql(GqlQuery gqlQuery)
        {
            return (new Google.Apis.Datastore.v1beta2.DatastoreService(Authenticator.GetInitializer())).Datasets.RunQuery(new RunQueryRequest
            {
                GqlQuery = gqlQuery
            }, Authenticator.GetProjectId()).Execute();
        }

        private void UpsertToDatastore<T>(IEnumerable<T> entities)
             where T : new()
        {
            var datastore = new Google.Apis.Datastore.v1beta2.DatastoreService(Authenticator.GetInitializer());
            var transaction = datastore.Datasets.BeginTransaction(new BeginTransactionRequest(), Authenticator.GetProjectId()).Execute();
            
            var datastoreEntities = GetSerializer<T>().SerializeAndAutoKey(entities, Authenticator, Configuration.DoubleCheckGeneratedIds);

            datastore.Datasets.Commit(new CommitRequest
            {
                Mutation = new Mutation
                {
                    Upsert = datastoreEntities
                },
                Mode = "TRANSACTIONAL",
                Transaction = transaction.Transaction
            }, Authenticator.GetProjectId()).Execute();
        }

        /// <summary>
        /// Adds / updates a single entity in the datastore.
        /// </summary>
        /// <param name="entity"></param>
        public void Upsert<T>(T entity)
             where T : new()
        {
            if ((entity as IEnumerable) != null)
                throw new InvalidOperationException("Use `UpsertRange` to upsert collections.");

            UpsertToDatastore(new[] { (T)entity });
        }

        /// <summary>
        /// Adds / updates entities in the datastore.
        /// </summary>
        /// <param name="entity"></param>
        public void UpsertRange<T>(IEnumerable<T> entity)
             where T : new()
        {
            UpsertToDatastore(entity);
        }

        /// <summary>
        /// Remove entities from the datastore using LINQ.
        /// </summary>
        /// <param name="entities"></param>
        public void Delete<T>(Func<T, bool> entities)
             where T : new()
        {
            var idName = QueryHelper.GetIdProperty<T>().Name;
            var genericTParam = Expression.Parameter(typeof(T));
            var entitiesToRemove = Find<T>()
                .Where(entities)
                .Select(Expression.Lambda<Func<T, string>>(
                    Expression.Property(genericTParam, idName),
                    genericTParam
                ).Compile())
                .ToList()
                .Where(x => x != null);

            Delete(entitiesToRemove, typeof(T).Name);
        }

        public void Delete<T>(T entity)
             where T : new()
        {
            var id = (string)QueryHelper.GetIdProperty<T>().GetValue(entity, null);
            Delete(new List<string> { id }, typeof(T).Name);
        }

        /// <summary>
        /// Remove entities from the datastore.
        /// </summary>
        /// <param name="entities"></param>
        public void DeleteRange<T>(IEnumerable<T> entities)
             where T : new()
        {
            var keys = new List<string>();

            foreach (var entity in entities)
            {
                // TODO this section isn't DRY (see SerializeAndAutoKey())

                var idProperty = QueryHelper.GetIdProperty<T>();

                if (idProperty == null)
                    throw new MissingMemberException($"Could not delete because `{typeof(T).Name}` does not contain an Id property nor any other property with the DatastoreKey attribute.");

                if (idProperty.PropertyType.GetTypeCode() != TypeCode.String)
                    throw new NotSupportedException($"Id type `{idProperty.PropertyType.GetTypeCode()}` is not supported. Id type must be a string.");

                var id = (string)idProperty.GetValue(entity);

                if (string.IsNullOrWhiteSpace(id))
                    throw new Exception("Entity does not contain a valid id.");

                keys.Add(id);
            }

            Delete(keys, typeof (T).Name);
        }

        private void Delete(IEnumerable<string> keys, string kind)
        {
            var datastore = new Google.Apis.Datastore.v1beta2.DatastoreService(Authenticator.GetInitializer());
            var transaction = datastore.Datasets.BeginTransaction(new BeginTransactionRequest(), Authenticator.GetProjectId()).Execute();

            datastore.Datasets.Commit(new CommitRequest
            {
                Mutation = new Mutation
                {
                    Delete = keys.Select(x => new Key { Path = new []{ new KeyPathElement { Kind = kind, Name = x } } }).ToList()
                },
                Mode = "TRANSACTIONAL",
                Transaction = transaction.Transaction
            }, Authenticator.GetProjectId()).Execute();
        }
    }
}
