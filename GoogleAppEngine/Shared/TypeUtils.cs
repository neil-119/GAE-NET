using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace GoogleAppEngine.Shared
{
    internal static class TypeUtils
    {
        public static bool AreEquivalent(Type t1, Type t2)
        {
            return t1.IsEquivalentTo(t2);
        }

        public static bool IsEquivalentTo(this Type t1, Type t2)
        {
            return t1 == t2;
        }

        public static bool AreReferenceAssignable(Type dest, Type src)
        {
            // This actually implements "Is this identity assignable and/or reference assignable?"
            if (AreEquivalent(dest, src))
            {
                return true;
            }
            if (!dest.GetTypeInfo().IsValueType && !src.GetTypeInfo().IsValueType &&
                dest.GetTypeInfo().IsAssignableFrom(src.GetTypeInfo()))
            {
                return true;
            }
            return false;
        }
    }
}
