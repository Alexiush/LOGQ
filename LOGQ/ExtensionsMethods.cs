using System;
using System.Collections.Generic;
using System.Linq;

namespace LOGQ.Extensions
{
    public static class ExtensionsMethods
    {
        public static LogicalAction Not(List<Predicate<Dictionary<BindKey, string>>> actionsToTry)
        {
            return new LogicalAction(actionsToTry
                .Select<Predicate<Dictionary<BindKey, string>>, Predicate<Dictionary<BindKey, string>>>
                (predicate => context => !predicate(context)).ToList());
        }

        public static LogicalAction Not(Predicate<Dictionary<BindKey, string>> actionToTry) 
            => Not(new List<Predicate<Dictionary<BindKey, string>>> { actionToTry });

        public static LogicalAction Not<T>(BoundFact<T> fact, KnowledgeBase knowledgeBase) where T : new()
            => Not(knowledgeBase.CheckForFacts(fact));

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
    }
}
