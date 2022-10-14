using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace LOGQ
{
    public static class DelegateTransformer
    {
        /// <summary>
        /// Transforms predicate to predicate with copy storage
        /// </summary>
        /// <param name="notBoundPredicate">predicate</param>
        /// <returns>predicate with copy storage</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Predicate<List<IBound>> ToPredicate(this Func<bool> notBoundPredicate)
        {
            return copyStorage => notBoundPredicate(); 
        }

        /// <summary>
        /// Transforms action with copy storage to predicate with copy storage
        /// </summary>
        /// <param name="notBoundPredicate">action with copy storage</param>
        /// <returns>predicate with copy storage</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Predicate<List<IBound>> ToPredicate(this Action<List<IBound>> boundAction)
        {
            return copyStorage => { boundAction(copyStorage); return true; };
        }

        /// <summary>
        /// Transforms action to predicate with copy storage
        /// </summary>
        /// <param name="notBoundPredicate">action</param>
        /// <returns>predicate with copy storage</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Predicate<List<IBound>> ToPredicate(this Action boundAction)
        {
            return copyStorage => { boundAction(); return true; };
        }
    }
}
