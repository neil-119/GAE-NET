using System;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Datastore.v1beta2.Data;
using GoogleAppEngine.Datastore;
using LightMock;

namespace GoogleAppEngine.Tests
{
    public class DatastoreServiceMock : IDatastoreService
    {
        private readonly IInvocationContext<IDatastoreService> _context;

        public DatastoreServiceMock(IInvocationContext<IDatastoreService> context)
        {
            _context = context;
        }

        public void Upsert<T>(T entity) where T : new()
        {
            _context.Invoke(f => f.Upsert(entity));
        }

        public void UpsertRange<T>(IEnumerable<T> entity) where T : new()
        {
            _context.Invoke(f => f.UpsertRange(entity));
        }

        public void Delete<T>(Func<T, bool> entities) where T : new()
        {
            _context.Invoke(f => f.Delete(entities));
        }

        public void Delete<T>(T entity) where T : new()
        {
            _context.Invoke(f => f.Delete(entity));
        }

        public void DeleteRange<T>(IEnumerable<T> entities) where T : new()
        {
            _context.Invoke(f => f.DeleteRange(entities));
        }

        public IOrderedQueryable<T> Find<T>() where T : new()
        {
            return _context.Invoke(f => f.Find<T>());
        }

        public RunQueryResponse Gql(GqlQuery gqlQuery)
        {
            return _context.Invoke(f => f.Gql(gqlQuery));
        }
    }
}