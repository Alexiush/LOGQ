using System;
using System.Collections.Generic;
using System.Linq;

namespace LOGQ
{
    public class Fact 
    {
        public List<IVariable> Values;

        public virtual Type FactType()
        {
            return GetType();
        }
    }

    public class BoundFact : Fact 
    {
        public List<IBound> Bounds;

        // Check for facts must remember about it comparing fact variables, but setting bind keys
        public void Bind(Fact fact, List<IBound> copyStorage)
        {
            // Binds fact variables to samples bounds

            /*
            List<IBound> bounds = Bounds;
            List<IVariable> values = fact.Values;

            foreach (var pair in bounds.Zip(values, (first, second) => (first, second)))
            {
                pair.first.UpdateValue(copyStorage, pair.second.value);
            }
            */
        }
    }

    public class Rule 
    {
        public List<IVariable> Values;

        public virtual Type RuleType() 
        { 
            return GetType(); 
        }
    }

    public class BoundRule : Rule
    {
        public List<IBound> Bounds;

        public void Bind(List<IBound> copyStorage)
        {
            /*
            List<IBound> bounds = Bounds;
            List<IVariable> values = Values;

            foreach (var pair in bounds.Zip(values, (first, second) => (first, second)))
            {
                pair.first.UpdateValue(copyStorage, pair.second.value);
            }
            */
        }
    }

    public class KnowledgeBase
    {
        // List potentially can be replaced with some kind of sorted table of values
        // But that must not be an overkill as program can't check (now at least) if only suitable result is sought
        private Dictionary<Type, List<Fact>> facts = new Dictionary<Type, List<Fact>>();
        private Dictionary<Type, List<Rule>> rules = new Dictionary<Type, List<Rule>>();

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
                        sampleFact.Bind(fact, context);
                        return (sampleFact == fact);
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

            Type domainType = ruleHead.RuleType();

            if (rules.ContainsKey(domainType))
            {
                foreach (Rule rule in rules[domainType])
                {
                    ruleCheckPredicates.Add(context =>
                    {
                        /*
                        // rule must have attached query, here must be search by head
                        var typedRule = rule.GetTyped();
                        bool executionResult = typedRule.Body(ruleHead).Execute();
                        ruleHead.Bind(context);
                        return executionResult;
                        */

                        return true;
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
        public void AddRule(Rule rule)
        {
            Type ruleType = rule.RuleType(); 

            if (!rules.ContainsKey(ruleType))
            {
                rules.Add(ruleType, new List<Rule>());
            }

            rules[ruleType].Add(rule);
        }
    }
}
