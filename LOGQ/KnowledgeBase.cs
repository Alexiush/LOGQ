using System;
using System.Collections.Generic;
using System.Linq;

namespace LOGQ
{
    public class KnowledgeBase
    {
        // List potentially can be replaced with some kind of sorted table of values
        // But that must not be an overkill as program can't check (now at least) if only suitable result is sought
        private Dictionary<Type, List<FactTemplate>> facts = new Dictionary<Type, List<FactTemplate>>();
        private Dictionary<Type, List<LogicalQuery>> rules = new Dictionary<Type, List<LogicalQuery>>();

        // TODO: Generate one-action query for checking of fact existence by value
        // Better to generate LAction from the start, as it is one operation check

        // TODO: Option to add a rule-query for checking of fact existence by rule
        // Rule-based queries must get own iteration LAction and build in as LAction on some values somehow

        // Check for facts must remember about it comparing fact variables, but setting bind keys
        private void BindFact<T>(BoundFact<T> sampleFact,
            Dictionary<BindKey, string> copyStorage, FactTemplate fact = null) where T : new()
        {
            // Binds fact variables to samples bounds

            List<BindKey> bounds = sampleFact.Bounds;
            List<FactVariable> values = fact is null ? sampleFact.Values.Values.ToList() : fact.Values.Values.ToList();

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
                        BindFact(sampleFact, context, fact);
                        return (sampleFact == fact);
                    });
                }
            }

            if (rules.ContainsKey(factType))
            {
                foreach (LogicalQuery rule in rules[factType])
                {
                    factCheckPredicates.Add(context =>
                    {
                        bool executionResult = rule.Execute();
                        BindFact(sampleFact, context);
                        return executionResult;
                    });
                }
            }

            return factCheckPredicates;
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
        public void AddRule<T>(LogicalQuery rule) where T: FactTemplate
        {
            Type factType = typeof(T);

            if (!facts.ContainsKey(factType))
            {
                rules.Add(factType, new List<LogicalQuery>());
            }

            rules[factType].Add(rule);
        }

        /*
        public List<T> Get<T>(T example) where T : FactTemplate, new()
        {
            Type factType = typeof(Fact<T>);
            List<T> fittingFacts = new List<T>();

            if (facts.ContainsKey(factType))
            {
                facts[factType].ForEach(fact =>
                {
                    T typedFact = (T)fact;
                    if (typedFact.EqualsFact(typedFact))
                    {
                        fittingFacts.Add(typedFact);
                    }
                });
            }

            return fittingFacts;
        }
        */
    }
}
