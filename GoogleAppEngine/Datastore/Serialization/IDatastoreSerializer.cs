using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Datastore.v1beta2.Data;
using GoogleAppEngine.Datastore.LINQ;

namespace GoogleAppEngine.Datastore.Serialization
{
    public interface IDatastoreSerializer<T>
    {
        List<Entity> SerializeAndAutoKey(IEnumerable<T> entities, CloudAuthenticator authenticator, bool verifyThatIdIsUnused);
        T DeserializeEntity(Entity entity);
        Entity SerializeEntity(T entity);
        Entity SerializeEntity(object entity, Type entityType);
    }
}
