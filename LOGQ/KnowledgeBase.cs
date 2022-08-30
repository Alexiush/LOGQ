using System;
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

    public sealed class RuleWithBody
    {
        public Rule Head { get; private set; }
        public Func<BoundRule, LogicalQuery> Body { get; private set; }

        public RuleWithBody(Rule head, Func<BoundRule, LogicalQuery> body)
        {
            this.Head = head;
            this.Body = body;
        }
    }

    public sealed class KnowledgeBase
    {
        // List potentially can be replaced with some kind of relational table of values
        private Dictionary<Type, List<Fact>> _facts = new Dictionary<Type, List<Fact>>();
        private Dictionary<Type, List<RuleWithBody>> _rules = new Dictionary<Type, List<RuleWithBody>>();

        public List<Predicate<List<IBound>>> CheckForFacts(BoundFact sampleFact)
        {
            List<Predicate<List<IBound>>> factCheckPredicates =
                new List<Predicate<List<IBound>>>();

            Type factType = sampleFact.FactType();

            if (_facts.ContainsKey(factType))
            {
                foreach (Fact fact in _facts[factType])
                {
                    factCheckPredicates.Add(copyStorage =>
                    {
                        bool comparisonResult = sampleFact.Equals(fact);
                        sampleFact.Bind(fact, copyStorage);
                        return comparisonResult;
                    });
                }
            }
            else
            {
                throw new ArgumentException("No facts of that type");
            }

            return factCheckPredicates;
        }

        public BacktrackIterator CheckForRules(BoundRule ruleHead)
        {
            Type ruleType = ruleHead.RuleType();

            if (_rules.ContainsKey(ruleType))
            {
                List<RuleWithBody> baseRules = _rules[ruleType].Where(rule => rule.Head.Equals(ruleHead)).ToList();
                LogicalQuery innerQuery = null;
                int offset = 0;

                return new BacktrackIterator
                (
                    () => {
                        while (true)
                        {
                            if (offset == baseRules.Count)
                            {
                                return null;
                            }

                            if (innerQuery is null)
                            {
                                innerQuery = baseRules[offset].Body(ruleHead);
                            }

                            bool result = innerQuery.Execute();

                            if (!result)
                            {
                                offset++;
                                innerQuery.Reset();
                                innerQuery = null;
                                continue;
                            }

                            return copyStorage => result;
                        }
                    },
                    () => { offset = 0;}
                );
            }
            else
            {
                throw new ArgumentException("No rules of that type");
            }
        }

        public void DeclareFact(Fact fact)
        {
            Type factType = fact.FactType();
            
            if (!_facts.ContainsKey(factType))
            {
                _facts.Add(factType, new List<Fact>());
            }

            _facts[factType].Add(fact);
        }

        public void DeclareRule(RuleWithBody rule)
        {
            Type ruleType = rule.Head.RuleType();

            if (!_rules.ContainsKey(ruleType))
            {
                _rules.Add(ruleType, new List<RuleWithBody>());
            }

            _rules[ruleType].Add(rule);
        }
    }
}
