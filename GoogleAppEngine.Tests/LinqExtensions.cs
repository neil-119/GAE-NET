using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleAppEngine.Tests
{
    public static class LinqExtensions
    {
        // ReSharper disable ReturnValueOfPureMethodIsNotUsed

        public static void Translate<T>(this IQueryable<T> query)
        {
            query.ToList();
        }

        public static void Translate<T>(this IEnumerable<T> query)
        {
            query.ToList();
        }
    }
}
