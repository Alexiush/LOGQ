using Functional.Option;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LOGQ
{
    /// <summary>
    /// Cluster of facts that share same hash for some value
    /// </summary>
    public struct Cluster<T>
    {
        private List<T> objects = new List<T>();

        public Cluster() { }

        public Cluster(List<T> objects)
        {
            this.objects = objects;
        }

        public int Size { get { return objects.Count; } }

        public void Add(T obj)
        {
            objects.Add(obj);
        }

        public void Remove(T obj)
        {
            objects.Remove(obj);
        }

        public List<T> GetValues()
            => objects;
    }

    /// <summary>
    /// Attribute that marks inability to use clustering on this fact type
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
    public class NoIndexingAttribute: System.Attribute
    {
        public NoIndexingAttribute() { }
    }

    /// <summary>
    /// Attribute that marks inability to use clustering on this variable type
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class NotHashComparableAttribute: System.Attribute
    {
        public NotHashComparableAttribute() { }
    }

    [System.AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
    public class HighRuleCountDomainAttribute: System.Attribute
    {
        public HighRuleCountDomainAttribute() { }
    }

    /// <summary>
    /// Storage that provides fast access to facts that possibly fit the sample
    /// </summary>
    public interface IIndexedFactsStorage
    {
        public void Add(Fact fact);

        public void Retract(Fact fact);

        public List<IFact> FilteredBySample(BoundFact sample);
    }

    /// <summary>
    /// Special data structure for fast rule check based on add/get filters specified in pattern implementation
    /// </summary>
    /// <typeparam name="T">type</typeparam>
    public class RulesDictionary<T>
    {
        public class PatternBasedIndexedCollection<T>
        {
            Dictionary<Option<int>, Cluster<RuleTemplate>> rules;

            Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> addFilter;
            Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> getFilter;

            public PatternBasedIndexedCollection(RuleVariable<T> ruleVariable) 
            { 
                this.rules = new Dictionary<Option<int>, Cluster<RuleTemplate>>();

                addFilter = ruleVariable.AddFilter();
                getFilter = ruleVariable.GetFilter();
            }

            public IEnumerable<RuleTemplate> Get(Option<int> hash)
            {
                var list = getFilter(hash, rules);

                return list.Count() == 0 ? new List<RuleTemplate>() : list
                    .Select(cluster => (IEnumerable<RuleTemplate>)cluster.GetValues())
                    .Aggregate((acc, list) => acc.Concat(list));
            }

            public int Count(Option<int> hash)
            {
                var list = getFilter(hash, rules);

                return list.Count == 0 ? 0 : list
                    .Select(cluster => cluster.Size)
                    .Aggregate((acc, size) => acc + size);
            }
            
            public void Add(Option<int> hash, RuleTemplate template)
            {
                addFilter(hash, rules).ForEach(cluster => cluster.Add(template));
            }

            public void Retract(Option<int> hash, RuleTemplate template)
            {
                addFilter(hash, rules).ForEach(cluster => cluster.Remove(template));
            }
        }

        private Dictionary<Type, PatternBasedIndexedCollection<T>> patternBasedCollections = 
            new Dictionary<Type, PatternBasedIndexedCollection<T>>();

        public int Size(Option<int> hash)
        {
            var list = patternBasedCollections.Values;
                
            return list.Count == 0 ? 0 : list
                .Select(collection => collection.Count(hash))
                .Aggregate((acc, size) => acc + size);
        }

        public Cluster<RuleTemplate> Get(Option<int> hash)
        {
            var list = patternBasedCollections.Values
                .Select(collection => collection.Get(hash));

            var values = list.Count() == 0 ? new List<RuleTemplate>() : list
                .Aggregate((acc, list) => acc.Concat(list))
                .ToList();

            return new Cluster<RuleTemplate>(values);
        }

        public void Add(RuleVariable<T> pattern, RuleTemplate template)
        {
            Option<int> hash = pattern.OptionHash();
            Type patternType = pattern.PatternType();

            if (!patternBasedCollections.ContainsKey(patternType))
            {
                patternBasedCollections.Add(patternType, new PatternBasedIndexedCollection<T>(pattern));
            }

            patternBasedCollections[patternType].Add(hash, template);
        }

        public void Retract(RuleVariable<T> pattern, RuleTemplate template)
        {
            Option<int> hash = pattern.OptionHash();
            Type patternType = pattern.PatternType();

            if (!patternBasedCollections.ContainsKey(patternType))
            {
                patternBasedCollections.Add(patternType, new PatternBasedIndexedCollection<T>(pattern));
            }

            patternBasedCollections[patternType].Retract(hash, template);
        }
    }

    /// <summary>
    /// Storage that provides fast access to rules that possibly fit the sample
    /// </summary>
    public interface IIndexedRulesStorage
    {
        public void Add(RuleTemplate rule);

        public void Retract(RuleTemplate rule);

        public List<RuleTemplate> FilteredByPattern(BoundRule pattern);
    }
}
