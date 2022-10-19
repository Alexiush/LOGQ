using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LOGQ
{
    public interface IFact
    {
        /// <summary>
        /// Returns type mapped to the fact
        /// </summary>
        /// <returns>Type mapped to the fact</returns>
        abstract public Type FactType();
    }

    public abstract class StorageableFact
    {
        public static IIndexedFactsStorage Storage() => throw new InvalidOperationException("Can't call storage on base type");

        /// <summary>
        /// Returns IndexedFactsCollection generated specifically for this type
        /// </summary>
        /// <returns>IndexedFactsCollection for this type</returns>
        abstract public IIndexedFactsStorage IndexedFactsStorage();
    }

    /// <summary>
    /// Abstract class that marks facts
    /// </summary>
    public abstract class Fact: StorageableFact, IFact
    {
        abstract public Type FactType();
        abstract public override IIndexedFactsStorage IndexedFactsStorage();
    }

    /// <summary>
    /// Fact with bound variables, used in queries, can bind to matching facts
    /// </summary>
    public abstract class BoundFact : IFact 
    {
        abstract public Type FactType();

        /// <summary>
        /// Binds fact values to the bound fact bound variables
        /// </summary>
        /// <param name="fact">Fact which values are now set to bound values</param>
        /// <param name="copyStorage">Copy storage</param>
        abstract public void Bind(Fact fact, List<IBound> copyStorage);
    }

    public interface IRule
    {
        /// <summary>
        /// Returns type mapped to the rule
        /// </summary>
        /// <returns>Type mapped to the rule</returns>
        abstract public Type RuleType();
    }

    public abstract class StorageableRule
    {
        public static IIndexedRulesStorage Storage() => throw new InvalidOperationException("Can't call storage on base type");

        /// <summary>
        /// Returns IndexedRulesCollection generated specifically for this type
        /// </summary>
        /// <returns>IndexedRulesCollection for this type</returns>
        abstract public IIndexedRulesStorage IndexedRulesStorage();
    }

    /// <summary>
    /// Abstract class that marks rules
    /// </summary>
    public abstract class Rule: StorageableRule, IRule
    {
        abstract public Type RuleType();
        abstract public override IIndexedRulesStorage IndexedRulesStorage();
    }

    /// <summary>
    /// Ryle with bound variables, used in queries 
    /// </summary>
    public abstract class BoundRule : IRule 
    {
        abstract public Type RuleType();
    }

    public abstract class RuleTemplate
    {
        public Rule Head { get; protected set; }
        public Func<BoundRule, LogicalQuery> Body { get; protected set; }
    }

    public sealed class RuleWithBody<T> : RuleTemplate where T: BoundRule
    {
        public RuleWithBody(Rule head, Func<T, LogicalQuery> body)
        {
            this.Head = head;
            this.Body = bound => body((T)bound);
        }
    }

    /// <summary>
    /// Class that groups facts and rules.
    /// Knowledge base can be searched for specific facts and rules.
    /// </summary>
    public sealed class KnowledgeBase
    {
        private Dictionary<Type, IIndexedFactsStorage> _facts = new Dictionary<Type, IIndexedFactsStorage>();
        private Dictionary<Type, IIndexedRulesStorage> _rules = new Dictionary<Type, IIndexedRulesStorage>();

        /// <summary>
        /// Returns backtrack iterator for fact-checking in this knowledge base
        /// </summary>
        /// <param name="sampleFact">Bound fact that must match facts</param>
        /// <returns>List of predicates that check each fact of the same underlying type</returns>
        /// <exception cref="ArgumentException">
        /// When there is no facts of that type in a knowledge base
        /// </exception>
        internal BacktrackIterator CheckForFacts(BoundFact sampleFact)
        {
            Type factType = sampleFact.FactType();

            if (!_facts.ContainsKey(factType))
            {
                throw new ArgumentException("No facts of that type");
            }

            bool enumeratorIsUpToDate = false;

            long version = _facts[factType].GetVersion();
            List<IFact> factsFiltered = _facts[factType].FilteredBySample(sampleFact);
            var enumerator = factsFiltered.GetEnumerator();

            return new BacktrackIterator
            (
                () => {
                    while (true)
                    {
                        if (!enumeratorIsUpToDate)
                        {
                            var currentVersion = _facts[factType].GetVersion();
                            if (version != currentVersion)
                            {
                                factsFiltered = _facts[factType].FilteredBySample(sampleFact);
                                version = currentVersion;
                            }

                            enumerator = factsFiltered.GetEnumerator();
                            enumeratorIsUpToDate = true;
                        }

                        if (!enumerator.MoveNext())
                        {
                            return null;
                        }

                        bool result = sampleFact.Equals(enumerator.Current);

                        if (!result)
                        {
                            continue;
                        }

                        return copyStorage =>
                        {
                            sampleFact.Bind((Fact)enumerator.Current, copyStorage);
                            return result;
                        };
                    }
                },
                () => { enumeratorIsUpToDate = false; }
            );
        }

        /// <summary>
        /// Returns backtrack iterator for rule-checking in this knowledge base
        /// </summary>
        /// <param name="ruleHead">Bound rule that must match rules</param>
        /// <returns>Backtrack iterator that checks for each rule of the same underlying type</returns>
        /// <exception cref="ArgumentException">
        ///  When there is no rules of that type in a knowledge base
        /// </exception>
        internal BacktrackIterator CheckForRules(BoundRule ruleHead)
        {
            Type ruleType = ruleHead.RuleType();

            if (!_rules.ContainsKey(ruleType))
            {
                throw new ArgumentException("No rules of that type");
            }

            LogicalQuery innerQuery = null;
            bool enumeratorIsUpToDate = false;

            long version = _rules[ruleType].GetVersion();
            List<RuleTemplate> rulesFiltered = _rules[ruleType].FilteredByPattern(ruleHead);
            var enumerator = rulesFiltered.GetEnumerator();

            return new BacktrackIterator
            (
                () => {
                    while (true)
                    {
                        if (!enumeratorIsUpToDate)
                        {
                            var currentVersion = _rules[ruleType].GetVersion();
                            if (version != currentVersion)
                            {
                                rulesFiltered = _rules[ruleType].FilteredByPattern(ruleHead);
                                version = currentVersion;
                            }

                            enumerator = rulesFiltered.GetEnumerator();
                            enumeratorIsUpToDate = true;
                        }

                        if (innerQuery is not null)
                        {
                            innerQuery.Reset();
                        }

                        if (!enumerator.MoveNext())
                        {
                            return null;
                        }

                        if (innerQuery is null)
                        {
                            innerQuery = enumerator.Current.Body(ruleHead);
                        }

                        bool result = innerQuery.Execute();

                        if (!result)
                        {
                            innerQuery.Reset();
                            innerQuery = null;
                            continue;
                        }

                        return copyStorage => result;
                    }
                },
                () => { enumeratorIsUpToDate = false; }
            );
        }

        /// <summary>
        /// Puts fact into the knowledge base 
        /// </summary>
        /// <param name="fact">Fact to be put</param>
        public void DeclareFact(Fact fact)
        {
            Type factType = fact.FactType();
            
            if (!_facts.ContainsKey(factType))
            {
                _facts.Add(factType, fact.IndexedFactsStorage());
            }

            _facts[factType].Add(fact);
        }

        /// <summary>
        /// Removes fact from the knowledge base
        /// </summary>
        /// <param name="fact">Fact to be removed</param>
        public void RetractFact(Fact fact)
        {
            Type factType = fact.FactType();
            _facts[factType].Retract(fact);
        }

        /// <summary>
        /// Puts rule into the knowledge base
        /// </summary>
        /// <typeparam name="T">Rule type</typeparam>
        /// <param name="rule">Rule to be put</param>
        public void DeclareRule<T>(RuleWithBody<T> rule) where T : BoundRule
        {
            Type ruleType = rule.Head.RuleType();

            if (!_rules.ContainsKey(ruleType))
            {
                _rules.Add(ruleType, rule.Head.IndexedRulesStorage());
            }

            _rules[ruleType].Add(rule);
        }

        /// <summary>
        /// Removes the rule from the knowledge base
        /// </summary>
        /// <typeparam name="T">Rule type</typeparam>
        /// <param name="rule">Rule to be removed</param>
        public void RetractRule<T>(RuleWithBody<T> rule) where T : BoundRule
        {
            Type ruleType = rule.Head.RuleType();
            _rules[ruleType].Retract(rule);
        }

        /// <summary>
        /// Adds empty storage for the facts
        /// </summary>
        /// <param name="type">Fact type</param>
        /// <param name="storage">Storage of that type</param>
        public void AddFactStorage(Type type, IIndexedFactsStorage storage)
        { 
            _facts.Add(type, storage);
        }

        /// <summary>
        /// Adds empty storage for rules
        /// </summary>
        /// <param name="type">Rules type</param>
        /// <param name="storage">Storage of that type</param>
        public void AddRuleStorage(Type type, IIndexedRulesStorage storage)
        {
            _rules.Add(type, storage);
        }
    }
}
