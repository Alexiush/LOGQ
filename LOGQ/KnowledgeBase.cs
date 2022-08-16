using System;
using System.Collections.Generic;
using System.Linq;

namespace LOGQ
{
    public abstract class Rule { }

    // must be reworked as rule also can be marked with it's pattern

    public class TypedRule<T> : Rule where T: new()
    {
        public delegate LogicalQuery RuleBody(BoundRule<T> body);
        private RuleBody body;

        public TypedRule(RuleBody body)
        {
            this.body = body;
        }

        public LogicalQuery Body(BoundRule<T> body)
        {
            return this.body(body);
        }
    }

    public class KnowledgeBase
    {
        // List potentially can be replaced with some kind of sorted table of values
        // But that must not be an overkill as program can't check (now at least) if only suitable result is sought
        private Dictionary<Type, List<FactTemplate>> facts = new Dictionary<Type, List<FactTemplate>>();
        private Dictionary<Type, List<Rule>> rules = new Dictionary<Type, List<Rule>>();

        // TODO: Option to add a rule-query for checking of fact existence by rule
        // Rule-based queries must get own iteration LAction and build in as LAction on some values somehow

        // Check for facts must remember about it comparing fact variables, but setting bind keys
        private void BindFact<T>(BoundFact<T> sampleFact, FactTemplate fact, Dictionary<BindKey, string> copyStorage) where T : new()
        {
            // Binds fact variables to samples bounds

            List<BindKey> bounds = sampleFact.Bounds;
            List<FactVariable> values = fact.Values.Values.ToList();

            foreach (var pair in bounds.Zip(values, (first, second) => (first, second)))
            {
                pair.first.UpdateValue(copyStorage, pair.second.value);
            }
        }

        private void BindRule<T>(BoundRule<T> rule, Dictionary<BindKey, string> copyStorage) where T : new()
        {
            List<BindKey> bounds = rule.Bounds;
            List<RuleVariable> values = rule.Values.Values.ToList();

            foreach (var pair in bounds.Zip(values, (first, second) => (first, second)))
            {
                pair.first.UpdateValue(copyStorage, pair.second.value);
            }
        }

        public List<Predicate<Dictionary<BindKey, string>>> CheckForFacts<T>(BoundFact<T> sampleFact) where T: new()
        {
            List<Predicate<Dictionary<BindKey, string>>> factCheckPredicates =
                new List<Predicate<Dictionary<BindKey, string>>>();

            Type factType = typeof(Fact<T>);

            if (facts.ContainsKey(factType))
            {
                foreach (FactTemplate fact in facts[factType])
                {
                    factCheckPredicates.Add(context =>
                    {
                        BindFact(sampleFact, fact, context);
                        return (sampleFact == fact);
                    });
                }
            }

            return factCheckPredicates;
        }

        public List<Predicate<Dictionary<BindKey, string>>> CheckForRules<T>(BoundRule<T> ruleHead) where T : new()
        {
            List<Predicate<Dictionary<BindKey, string>>> ruleCheckPredicates =
                new List<Predicate<Dictionary<BindKey, string>>>();

            Type domainType = typeof(T);

            if (rules.ContainsKey(domainType))
            {
                foreach (Rule rule in rules[domainType])
                {
                    ruleCheckPredicates.Add(context =>
                    {
                        TypedRule<T> typedRule = (TypedRule<T>)rule;
                        bool executionResult = typedRule.Body(ruleHead).Execute();
                        BindRule(ruleHead, context);
                        return executionResult;
                    });
                }
            }

            return ruleCheckPredicates;
        }

        public void AddFact<T>(T fact) where T: new()
        {
            Type factType = typeof(Fact<T>);
            
            if (!facts.ContainsKey(factType))
            {
                facts.Add(factType, new List<FactTemplate>());
            }

            facts[factType].Add(new Fact<T>(fact));
        }

        // Rule must specify what kind of condition is needed to conclude fact existence
        // Rules may be defined as query that succeeds only if fact exists
        // As an initial parameters it will recieve fact variables for fact it will try to conclude
        public void AddRule<T>(TypedRule<T> rule) where T: new()
        {
            Type factType = typeof(T);

            if (!rules.ContainsKey(factType))
            {
                rules.Add(factType, new List<Rule>());
            }

            rules[factType].Add(rule);
        }
    }
}
