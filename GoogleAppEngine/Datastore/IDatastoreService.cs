using System;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Datastore.v1beta2.Data;

namespace GoogleAppEngine.Datastore
{
    public interface IDatastoreService
    {
        void Upsert<T>(T entity) where T : new();
        void UpsertRange<T>(IEnumerable<T> entity) where T : new();
        void Delete<T>(Func<T, bool> entities) where T : new();
        void Delete<T>(T entity) where T : new();
        void DeleteRange<T>(IEnumerable<T> entities) where T : new();
        IOrderedQueryable<T> Find<T>() where T : new();
        RunQueryResponse Gql(GqlQuery gqlQuery);
    }
}
