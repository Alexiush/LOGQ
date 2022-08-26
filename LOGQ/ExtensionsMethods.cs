using System;
using System.Collections.Generic;
using System.Linq;

namespace LOGQ.Extensions
{
    public static class ExtensionsMethods
    {
        public static LogicalAction Not(List<Predicate<List<IBound>>> actionsToTry)
        {
            return new LogicalAction(actionsToTry
                .Select<Predicate<List<IBound>>, Predicate<List<IBound>>>
                (predicate => context => !predicate(context)).ToList());
        }

        public static LogicalAction Not(Predicate<List<IBound>> actionToTry) 
            => Not(new List<Predicate<List<IBound>>> { actionToTry });

        public static LogicalAction Not(BacktrackIterator iterator)
        {
            return new LogicalAction(iterator.Negate());
        }

        public static LogicalAction Not(BoundFact fact, KnowledgeBase knowledgeBase)
            => Not(knowledgeBase.CheckForFacts(fact));

        public static LogicalAction Not(BoundRule rule, KnowledgeBase knowledgeBase)
            => Not(knowledgeBase.CheckForRules(rule));
    }
}
