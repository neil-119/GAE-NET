using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GoogleAppEngine.Datastore;

namespace GoogleAppEngine.Datastore.LINQ
{
    public static class QueryHelper
    {
        public static PropertyInfo GetIdProperty<T>()
        {
            return typeof(T).GetProperties().FirstOrDefault(x => x.Name.ToLower() == "id"
                   || x.GetCustomAttributes(typeof(DatastoreKeyAttribute), false).Any());
        }

        public static string NormalizeDatetime(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd'T'HH:mm:ssZ");
        }
    }
}
