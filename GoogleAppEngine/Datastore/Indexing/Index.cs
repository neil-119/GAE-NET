using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleAppEngine.Datastore.Indexing
{
    public struct Index
    {
        public enum OrderingType
        {
            NotSpecified,
            Ascending,
            Descending
        }

        public class IndexProperty
        {
            public string PropertyName { get; set; }
            public OrderingType OrderingType { get; set; }
        }


        public string Kind { get; set; }
        public List<IndexProperty> Properties { get; set; }
        public bool IsAncestor { get; set; }
    }
}
