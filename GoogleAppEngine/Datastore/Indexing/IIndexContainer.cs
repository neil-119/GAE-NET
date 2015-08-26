using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GoogleAppEngine.Datastore.Indexing;

namespace GoogleAppEngine.Datastore.LINQ
{
    public interface IIndexContainer
    {
        List<Index> GetIndexList();
    }
}
