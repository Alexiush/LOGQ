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

        public List<T> GetValues()
            => objects.ToList();
    }

    /// <summary>
    /// Collection that provides fast access to facts that possibly fit the sample
    /// </summary>
    public interface IIndexedFactsCollection
    {
        public void Add(Fact fact);

        public List<Fact> FilteredBySample(BoundFact sample);
    }

    /// <summary>
    /// Collection that provides fast access to rules that possibly fit the sample
    /// </summary>
    public interface IIndexedRulesCollection
    {
        public void Add(RuleWithBody rule);

        public List<RuleWithBody> FilteredByPattern(BoundRule pattern);
    }
}
