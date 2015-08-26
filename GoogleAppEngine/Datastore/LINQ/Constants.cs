using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly:InternalsVisibleTo("GoogleAppEngine.Tests")]

namespace GoogleAppEngine.Datastore.LINQ
{
    internal static class Constants
    {
        internal const string DictionaryKeyValuePairPrefix = "g/kv_";
        internal const string DictionaryKeyDivider = "_g/k_";
        internal const string QueryPartTakeParameterName = "qlim";
        internal const string QueryPartSkipParameterName = "qoffset";
    }
}
