using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleAppEngine.Datastore
{
    public class DatastoreKeyAttribute : Attribute
    {
    }

    public class DatastoreNotIndexedAttribute : Attribute
    {
    }

    public class DatastoreNotSavedAttribute : Attribute
    {
    }
}
