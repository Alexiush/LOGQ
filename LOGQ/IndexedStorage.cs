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
            => objects.ToList();
    }

    /// <summary>
    /// Attribute that marks inability to use clustering on this fact type
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
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
    /// Storage that provides fast access to rules that possibly fit the sample
    /// </summary>
    public interface IIndexedRulesStorage
    {
        public void Add(RuleTemplate rule);

        public void Retract(RuleTemplate rule);

        public List<RuleTemplate> FilteredByPattern(BoundRule pattern);
    }
}
