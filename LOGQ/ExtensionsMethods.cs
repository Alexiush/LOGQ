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

        public static LogicalAction Not<T>(BoundFact fact, KnowledgeBase knowledgeBase) where T : new()
            => Not(knowledgeBase.CheckForFacts<T>(fact));

        /*
        public static FactVariable AnyFact()
        {
            FactVariable anyValue = new FactVariable("");
            anyValue.MakeIgnorant();

            return anyValue;
        }

        public static RuleVariable AnyRule()
        {
            RuleVariable anyValue = new RuleVariable("");
            anyValue.MakeIgnorant();

            return anyValue;
        }
        */
    }
}
