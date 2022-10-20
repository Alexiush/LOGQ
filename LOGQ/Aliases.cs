using Functional.Option;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace LOGQ
{
    /// <summary>
    /// Generic implementation of FactsStorage
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    internal class IndexedFactsStorage<T> : IIndexedFactsStorage
    {
        private HashSet<IFact> facts;
        private long version = 0;

        public void Add(Fact fact)
        {
            facts.Add(fact);
            version++;
        }

        public void Retract(Fact fact)
        {
            facts.Remove(fact);
            version++;
        }

        public List<IFact> FilteredBySample(BoundFact sample)
        {
            if (facts.Contains(sample))
            {
                BoundFactAlias<T> factCasted = (BoundFactAlias<T>)sample;
                return new List<IFact> { new FactAlias<T>(factCasted.Value) };
            }

            return new List<IFact>();
        }

        public long GetVersion()
        {
            return version;
        }
    }

    /// <summary>
    /// General implementation of RulesStorage
    /// </summary>
    internal class IndexedRulesStorage<T> : IIndexedRulesStorage
    {
        private RulesDictionary<T> rulesClustered = new RulesDictionary<T>();
        private HashSet<RuleTemplate> rules = new HashSet<RuleTemplate>();
        private long version = 0;

        public void Add(RuleTemplate rule)
        {
            if (rules.Contains(rule))
            {
                return;
            }

            var ruleCasted = (RuleWithBody<BoundRuleAlias<T>>)rule;
            rulesClustered.Add(((RuleAlias<T>)ruleCasted.Head).Value, rule);

            version++;
        }

        public void Retract(RuleTemplate rule)
        {
            if (!rules.Remove(rule))
            {
                return;
            }

            var ruleCasted = (RuleWithBody<BoundRuleAlias<T>>)rule;
            rulesClustered.Retract(((RuleAlias<T>)ruleCasted.Head).Value, rule);

            version++;
        }

        public List<RuleTemplate> FilteredByPattern(BoundRule pattern)
        {
            var patternCasted = ((BoundRuleAlias<T>)pattern).Value;

            return rulesClustered.Get(patternCasted.Value is null ? Option<int>.None : patternCasted.Value.GetHashCode())
                .GetValues()
                .Where(rule => rule.Head.Equals(pattern))
                .ToList();
        }

        public long GetVersion()
        {
            return version;
        }
    }

    /// <summary>
    /// Generic implementation of Fact
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    public class FactAlias<T>: Fact
    {
        public Variable<T> Value;

        public FactAlias(Variable<T> value)
        {
            Value = value;
        }

        public static bool operator ==(FactAlias<T> fact, FactAlias<T> otherFact)
        {
            return fact.Value.Equals(otherFact.Value);
        }

        public static bool operator !=(FactAlias<T> fact, FactAlias<T> otherFact)
        {
            return !(fact == otherFact);
        }

        public static bool operator ==(FactAlias<T> fact, BoundFactAlias<T> otherFact)
        {
            return fact.Value.Equals(otherFact.Value);
        }

        public static bool operator !=(FactAlias<T> fact, BoundFactAlias<T> otherFact)
        {
            return !(fact == otherFact);
        }

        public override bool Equals(object obj)
        {
            FactAlias<T> factAlias = obj as FactAlias<T>;

            if (factAlias is not null)
            {
                return factAlias == this;
            }

            BoundFactAlias<T> boundFactAlias = obj as BoundFactAlias<T>;

            if (boundFactAlias is not null)
            {
                return boundFactAlias == this;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override Type FactType()
        {
            return typeof(T);
        }

        public static new IIndexedFactsStorage Storage()
        {
            return new IndexedFactsStorage<T>();
        }

        public override IIndexedFactsStorage IndexedFactsStorage()
        {
            return Storage();
        }
    }

    /// <summary>
    /// Generic implementation of BoundFact
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    public class BoundFactAlias<T>: BoundFact
    {
        public BoundVariable<T> Value;

        public static bool operator ==(BoundFactAlias<T> fact, BoundFactAlias<T> otherFact)
        {
            return fact.Value.Equals(otherFact.Value);
        }

        public static bool operator !=(BoundFactAlias<T> fact, BoundFactAlias<T> otherFact)
        {
            return !(fact == otherFact);
        }

        public static bool operator ==(BoundFactAlias<T> fact, FactAlias<T> otherFact)
        {
            return fact.Value.Equals(otherFact.Value);
        }

        public static bool operator !=(BoundFactAlias<T> fact, FactAlias<T> otherFact)
        {
            return !(fact == otherFact);
        }

        public override bool Equals(object obj)
        {
            FactAlias<T> factAlias = obj as FactAlias<T>;

            if (factAlias is not null)
            {
                return factAlias == this;
            }

            BoundFactAlias<T> boundFactAlias = obj as BoundFactAlias<T>;

            if (boundFactAlias is not null)
            {
                return boundFactAlias == this;
            }

            return false;
        }

        public override void Bind(Fact fact, List<IBound> copyStorage)
        {
            Value.UpdateValue(copyStorage, ((FactAlias<T>)fact).Value.Value);
        }

        public override Type FactType()
        {
            return typeof(T);
        }

        public BoundFactAlias(BoundVariable<T> value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// Generic implementation of Rule
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    public class RuleAlias<T>: Rule
    {
        public RuleVariable<T> Value;

        public RuleAlias(RuleVariable<T> value)
        {
            Value = value;
        }

        public static bool operator ==(RuleAlias<T> fact, RuleAlias<T> otherFact)
        {
            return fact.Value.Equals(otherFact.Value);
        }

        public static bool operator !=(RuleAlias<T> fact, RuleAlias<T> otherFact)
        {
            return !(fact == otherFact);
        }

        public static bool operator ==(RuleAlias<T> fact, BoundRuleAlias<T> otherFact)
        {
            return fact.Value.Equals(otherFact.Value);
        }

        public static bool operator !=(RuleAlias<T> fact, BoundRuleAlias<T> otherFact)
        {
            return !(fact == otherFact);
        }

        public override bool Equals(object obj)
        {
            RuleAlias<T> ruleAlias = obj as RuleAlias<T>;

            if (ruleAlias is not null)
            {
                return ruleAlias == this;
            }

            BoundRuleAlias<T> boundRuleAlias = obj as BoundRuleAlias<T>;

            if (boundRuleAlias is not null)
            {
                return boundRuleAlias == this;
            }

            return false;
        }

        public override Type RuleType()
        {
            return typeof(T);
        }

        public static new IIndexedRulesStorage Storage()
        {
            return new IndexedRulesStorage<T>();
        }

        public override IIndexedRulesStorage IndexedRulesStorage()
        {
            return Storage();
        }
    }

    /// <summary>
    /// Generic implementation of BoundRule
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    public class BoundRuleAlias<T>: BoundRule
    {
        public BoundVariable<T> Value;

        public static bool operator ==(BoundRuleAlias<T> fact, BoundRuleAlias<T> otherFact)
        {
            return fact.Value.Equals(otherFact.Value);
        }

        public static bool operator !=(BoundRuleAlias<T> fact, BoundRuleAlias<T> otherFact)
        {
            return !(fact == otherFact);
        }

        public static bool operator ==(BoundRuleAlias<T> fact, RuleAlias<T> otherFact)
        {
            return fact.Value.Equals(otherFact.Value);
        }

        public static bool operator !=(BoundRuleAlias<T> fact, RuleAlias<T> otherFact)
        {
            return !(fact == otherFact);
        }

        public override bool Equals(object obj)
        {
            RuleAlias<T> ruleAlias = obj as RuleAlias<T>;

            if (ruleAlias is not null)
            {
                return ruleAlias == this;
            }

            BoundRuleAlias<T> boundRuleAlias = obj as BoundRuleAlias<T>;

            if (boundRuleAlias is not null)
            {
                return boundRuleAlias == this;
            }

            return false;
        }

        public BoundRuleAlias(BoundVariable<T> value)
        {
            Value = value;
        }

        public override Type RuleType()
        {
            return typeof(T);
        }
    }

    /// <summary>
    /// Generic FactExtensions
    /// </summary>
    public static class GenericFactsExtensions
    {
        public static BoundRuleAlias<T> AsBoundRule<T>(this BoundVariable<T> origin)
        {
            return new BoundRuleAlias<T>(origin);
        }

        public static BoundRuleAlias<T> AsBoundRule<T>(this T origin)
        {
            return new BoundRuleAlias<T>(origin);
        }

        public static RuleAlias<T> AsRule<T>(this RuleVariable<T> origin)
        {
            return new RuleAlias<T>(origin);
        }

        public static RuleAlias<T> AsRule<T>(this T origin)
        {
            return new RuleAlias<T>(new Equal<T>(origin));
        }

        public static BoundFactAlias<T> AsBoundFact<T>(this BoundVariable<T> origin)
        {
            return new BoundFactAlias<T>(origin);
        }

        public static BoundFactAlias<T> AsBoundFact<T>(this T origin)
        {
            return new BoundFactAlias<T>(origin);
        }

        public static FactAlias<T> AsFact<T>(this Variable<T> origin)
        {
            return new FactAlias<T>(origin);
        }

        public static FactAlias<T> AsFact<T>(this T origin)
        {
            return new FactAlias<T>(origin);
        }
    }
}
