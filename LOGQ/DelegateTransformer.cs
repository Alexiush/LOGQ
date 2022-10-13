using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace LOGQ
{
    public static class DelegateTransformer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Predicate<List<IBound>> ToPredicate(this Func<bool> notBoundPredicate)
        {
            return copyStorage => notBoundPredicate(); 
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Predicate<List<IBound>> ToPredicate(this Action<List<IBound>> boundAction)
        {
            return copyStorage => { boundAction(copyStorage); return true; };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Predicate<List<IBound>> ToPredicate(this Action boundAction)
        {
            return copyStorage => { boundAction(); return true; };
        }
    }
}
