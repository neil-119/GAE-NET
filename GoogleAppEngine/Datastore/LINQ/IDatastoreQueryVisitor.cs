using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using GoogleAppEngine.Datastore.LINQ;

namespace GoogleAppEngine.Datastore.LINQ
{
    public interface IDatastoreQueryVisitor
    {
        State Translate(Expression expression);
    }
}
