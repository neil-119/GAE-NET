using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleAppEngine.Datastore.LINQ
{
    public abstract class ProjectionRow
    {
        public abstract object GetValue(string columnName, TypeCode typeCode);
    }
}
