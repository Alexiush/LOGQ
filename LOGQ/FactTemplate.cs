using System;
using System.Collections.Generic;
using System.Linq;

// Must contain only Variable class from now

// Looks like no binding-factvar conversion is provided 
// But with copy storage mechanism it can be saved if knowledge base would create bounds through get facts
namespace LOGQ
{
    /*
    // Must be renamed into variable
    // Both rules and facts will utilize variable and bound variable classes (with generics)
    public struct FactVariable
    {
        public string value;
        private bool isIgnorant;

        public FactVariable(string value)
        {
            this.value = value;
            isIgnorant = false;
        }

        public void MakeIgnorant()
        {
            isIgnorant = true;
        }

        public static bool operator ==(FactVariable fact, FactVariable otherFact)
        {
            return fact.isIgnorant || otherFact.isIgnorant || fact.value == otherFact.value;
        }

        public static bool operator !=(FactVariable fact, FactVariable otherFact)
        {
            return !(fact == otherFact);
        }
    }

    // Must be treated as duck-typed object even with underlying Fact<T>
    public abstract class FactTemplate
    {
        // private readonly string name; 
        protected Dictionary<string, FactVariable> values;

        public Dictionary<string, FactVariable> Values { get { return values; } }

        public static bool operator ==(FactTemplate fact, FactTemplate otherFact)
        {
            foreach (KeyValuePair<string, FactVariable> pair in fact.values)
            {
                if (otherFact.values[pair.Key] != pair.Value)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool operator !=(FactTemplate fact, FactTemplate otherFact)
        {
            return !(fact == otherFact);
        }
    }

    public class Fact<T>: FactTemplate where T: new()
    {
        private static int standardValuesCount = typeof(T).GetProperties().Count();
        private static List<string> standardValueKeys =
            JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(new T()))
            .Keys.ToList();

        public Fact(T origin)
        {
            Dictionary<string, string> stringizedValues = 
                JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(origin));

            values = stringizedValues.ToDictionary(pair => pair.Key, pair => new FactVariable(pair.Value));
        }

        public Fact(params FactVariable[] variables)
        {
            // need to know count of object public properties 
            // (as it's necessary for Json convertion too, maybe better will be to have templates generated from empty object deserialization)
            if (variables.Length != standardValuesCount)
            {
                throw new ArgumentException("Wrong count of fact parameters");
            }

            values = new Dictionary<string, FactVariable>();
            for (int index = 0; index < variables.Length; ++index)
            {
                values[standardValueKeys[index]] = variables[index];
            }
        }
    }

    public class BoundFact<T>: FactTemplate where T: new()
    {
        // So bound fact needs to operate usual fact variables when comparing,
        // But use assigned to it bound keys when binding

        private static int standardValuesCount = typeof(T).GetProperties().Count();
        private static List<string> standardValueKeys =
            JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(new T()))
            .Keys.ToList();

        private List<BindKey> bounds;
        public List<BindKey> Bounds { get { return bounds; } }

        // Need some way to add ignorable values
        public BoundFact(params BindKey[] bounds)
        {
            if (bounds.Length != standardValuesCount)
            {
                throw new ArgumentException("Wrong count of fact parameters");
            }

            this.bounds = bounds.ToList();

            values = new Dictionary<string, FactVariable>();
            for (int index = 0; index < bounds.Length; ++index)
            {
                values[standardValueKeys[index]] = bounds[index].AsFactVariable();
            }
        }
    }
    */
}
