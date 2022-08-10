﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace LOGQ.Extensions
{
    public static class ExtensionsMethods
    {
        public static LAction Not(List<Predicate<Dictionary<BindKey, string>>> actionsToTry)
        {
            return new LAction(actionsToTry
                .Select<Predicate<Dictionary<BindKey, string>>, Predicate<Dictionary<BindKey, string>>>
                (predicate => context => !predicate(context)).ToList());
        }

        public static LAction Not(Predicate<Dictionary<BindKey, string>> actionToTry) 
            => Not(new List<Predicate<Dictionary<BindKey, string>>> { actionToTry });

        public static LAction Not<T>(BoundFact<T> fact, KnowledgeBase knowledgeBase) where T : new()
            => Not(knowledgeBase.CheckForFacts(fact));

        public static FactVariable Any()
        {
            FactVariable anyValue = new FactVariable("");
            anyValue.MakeIgnorant();

            return anyValue;
        }
    }
}