using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LOGQ
{
    internal struct Cluster
    {
        public HashSet<int> indices;
    }

    // Both indexed collections only can be generated to get properties lists

    internal class IndexedFactsCollection
    {
        private List<Fact> facts;
        private HashSet<Fact> factSet;
        
        // Dictionary of clusters for each variable
        // Dictionary<int, Cluster> 

        public void Add(Fact fact)
        {
            // Add element to list
            facts.Add(fact);
            // Add element to hashSet
            factSet.Add(fact);

            // for each fact property - get hashcode, add index to cluster
            // propertyDict[property.GetHashCode()].indices.Add(facts.Last())
        }

        public List<Fact> FromCluster(Cluster cluster)
        {
            return cluster.indices.Select(index => facts[index]).ToList();
        }

        public List<Fact> FilteredBySample(BoundFact sample)
        {
            // Check if all variables are bound
            // if so - check in a hashSet
            // otherwise - return best cluster (based on bound variables)
            throw new NotImplementedException();
        }
    }

    internal class IndexedRulesCollection
    {
        public void Add()
        {
            // Add element to list
        }

        public List<Rule> FilteredBySample()
        {
            // Just return filtered list
            throw new NotImplementedException();
        }
    }
}
