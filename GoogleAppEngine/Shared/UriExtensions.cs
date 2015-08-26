using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleAppEngine.Shared
{
    public static class UriExtensions
    {
        public static bool IsAbsoluteUri(string url)
        {
            Uri result;
            return Uri.TryCreate(url, UriKind.Absolute, out result);
        }

        public static string GetAbsoluteUri(string path)
        {
            return IsAbsoluteUri(path) ? path :
                new Uri(Path.Combine(System.IO.Directory.GetCurrentDirectory(), path)).LocalPath;

        }
    }
}
