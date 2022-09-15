using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace LOGQ.Extensions
{
    public static class ExtensionsMethods
    {
        /// <summary>
        /// Negates logical action created from list of predicates
        /// </summary>
        /// <param name="actionsToTry">List of available actions</param>
        /// <returns>Negated logical action (returns true only if all actions return false)</returns>
        public static LogicalAction Not(ICollection<Predicate<List<IBound>>> actionsToTry)
        {
            return new LogicalAction(actionsToTry
                .Select<Predicate<List<IBound>>, Predicate<List<IBound>>>
                (predicate => context => !predicate(context)).ToList());
        }

        /// <summary>
        /// Negates logical action created from predicate
        /// </summary>
        /// <param name="actionToTry">Predicate that defines available action</param>
        /// <returns>Negated logical action (returns true only if all actions return false)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LogicalAction Not(Predicate<List<IBound>> actionToTry) 
            => Not(new List<Predicate<List<IBound>>> { actionToTry });

        /// <summary>
        /// Negates logical action created from backtrack iterator
        /// </summary>
        /// <param name="iterator">Underlying backtrack iterator</param>
        /// <returns>Negated logical action (returns true only if all actions return false)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LogicalAction Not(BacktrackIterator iterator)
        {
            return new LogicalAction(iterator.Negate());
        }

        /// <summary>
        /// Negates logical action created by fact-checking
        /// </summary>
        /// <param name="fact">Fact pattern being searched</param>
        /// <param name="knowledgeBase">Knowledge base searched for fact pattern</param>
        /// <returns>Negated logical action (returns true only if all actions return false)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LogicalAction Not(BoundFact fact, KnowledgeBase knowledgeBase)
            => Not(knowledgeBase.CheckForFacts(fact));

        /// <summary>
        /// Negates logical action created by rule-checking
        /// </summary>
        /// <param name="rule">Rule pattern being searched</param>
        /// <param name="knowledgeBase">Knowledge base searched for rule pattern</param>
        /// <returns>Negated logical action (returns true only if all actions return false)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LogicalAction Not(BoundRule rule, KnowledgeBase knowledgeBase)
            => Not(knowledgeBase.CheckForRules(rule));
    }
}
