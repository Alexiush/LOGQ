﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace LOGQ
{
    public abstract class Fact 
    {
        abstract public Type FactType();
    }

    public abstract class BoundFact : Fact 
    {
        // Check for facts must remember about it comparing fact variables, but setting bind keys
        abstract public void Bind(Fact fact, List<IBound> copyStorage);
    }

    public abstract class Rule 
    {
        abstract public Type RuleType();
    }

    public abstract class BoundRule : Rule
    {
        abstract public void Bind(List<IBound> copyStorage);
    }

    public class BaseRule
    {
        public Rule head;
        public Func<BoundRule, LogicalQuery> body;

        public BaseRule(Rule head, Func<BoundRule, LogicalQuery> body)
        {
            this.head = head;
            this.body = body;
        }
    }

    public class KnowledgeBase
    {
        // List potentially can be replaced with some kind of sorted table of values
        // But that must not be an overkill as program can't check (now at least) if only suitable result is sought
        private Dictionary<Type, List<Fact>> facts = new Dictionary<Type, List<Fact>>();
        private Dictionary<Type, List<BaseRule>> rules = new Dictionary<Type, List<BaseRule>>();

        // TODO: Option to add a rule-query for checking of fact existence by rule
        // Rule-based queries must get own iteration LAction and build in as LAction on some values somehow

        public List<Predicate<List<IBound>>> CheckForFacts(BoundFact sampleFact)
        {
            List<Predicate<List<IBound>>> factCheckPredicates =
                new List<Predicate<List<IBound>>>();

            Type factType = sampleFact.FactType();

            if (facts.ContainsKey(factType))
            {
                foreach (Fact fact in facts[factType])
                {
                    factCheckPredicates.Add(context =>
                    {
                        bool comparisonResult = sampleFact.Equals(fact);
                        sampleFact.Bind(fact, context);
                        return comparisonResult;
                    });
                }
            }

            return factCheckPredicates;
        }

        public List<Predicate<List<IBound>>> CheckForRules(BoundRule ruleHead)
        {
            List<Predicate<List<IBound>>> ruleCheckPredicates =
                new List<Predicate<List<IBound>>>();

            // Get generated type, provide this as a method

            Type ruleType = ruleHead.RuleType();

            if (rules.ContainsKey(ruleType))
            {
                foreach (BaseRule rule in rules[ruleType])
                {
                    ruleCheckPredicates.Add(context =>
                    {
                        if (!rule.head.Equals(ruleHead))
                        {
                            return false;
                        }

                        //ruleHead.Bind(context);
                        return rule.body(ruleHead).Execute(); 
                    });
                }
            }

            return ruleCheckPredicates;
        }

        public void AddFact(Fact fact)
        {
            Type factType = fact.FactType();
            
            if (!facts.ContainsKey(factType))
            {
                facts.Add(factType, new List<Fact>());
            }

            facts[factType].Add(fact);
        }

        // Rule must specify what kind of condition is needed to conclude fact existence
        // Rules may be defined as query that succeeds only if fact exists
        // As an initial parameters it will recieve fact variables for fact it will try to conclude
        public void AddRule(BaseRule rule)
        {
            Type ruleType = rule.head.RuleType();

            if (!rules.ContainsKey(ruleType))
            {
                rules.Add(ruleType, new List<BaseRule>());
            }

            rules[ruleType].Add(rule);
        }
    }
}
