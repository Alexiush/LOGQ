using System;
using System.Collections.Generic;
using System.Linq;

namespace LOGQ
{
    /*
    // Class that works almost like fact template, but must not be accepted as such an argument
    // Possibly can have room for more patterns 

    // Used in rules checking
    // Process of rules checking can be recursive and must be more complex than linear search with facts

    public struct RuleVariable
    {
        public string value;
        private bool isIgnorant;

        public RuleVariable(string value)
        {
            this.value = value;
            isIgnorant = false;
        }

        public void MakeIgnorant()
        {
            isIgnorant = true;
        }

        public static bool operator ==(RuleVariable rule, RuleVariable otherRule)
        {
            return rule.isIgnorant || otherRule.isIgnorant || rule.value == otherRule.value;
        }

        public static bool operator !=(RuleVariable rule, RuleVariable otherRule)
        {
            return !(rule == otherRule);
        }
    }

    // Must be treated as duck-typed object even with underlying Fact<T>
    public abstract class RuleTemplate
    {
        // private readonly string name; 
        protected Dictionary<string, RuleVariable> values;

        public Dictionary<string, RuleVariable> Values { get { return values; } }

        public static bool operator ==(RuleTemplate rule, RuleTemplate otherRule)
        {
            foreach (KeyValuePair<string, RuleVariable> pair in rule.values)
            {
                if (otherRule.values[pair.Key] != pair.Value)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool operator !=(RuleTemplate fact, RuleTemplate otherFact)
        {
            return !(fact == otherFact);
        }
    }

    public class RuleHead<T> : RuleTemplate where T : new()
    {
        private static int standardValuesCount = typeof(T).GetProperties().Count();
        private static List<string> standardValueKeys =
            JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(new T()))
            .Keys.ToList();

        public RuleHead(T origin)
        {
            Dictionary<string, string> stringizedValues =
                JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(origin));

            values = stringizedValues.ToDictionary(pair => pair.Key, pair => new RuleVariable(pair.Value));
        }

        public RuleHead(params RuleVariable[] variables)
        {
            // need to know count of object public properties 
            // (as it's necessary for Json convertion too, maybe better will be to have templates generated from empty object deserialization)
            if (variables.Length != standardValuesCount)
            {
                throw new ArgumentException("Wrong count of fact parameters");
            }

            values = new Dictionary<string, RuleVariable>();
            for (int index = 0; index < variables.Length; ++index)
            {
                values[standardValueKeys[index]] = variables[index];
            }
        }
    }

    public class BoundRule<T> : RuleTemplate where T : new()
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
        public BoundRule(params BindKey[] bounds)
        {
            if (bounds.Length != standardValuesCount)
            {
                throw new ArgumentException("Wrong count of fact parameters");
            }

            this.bounds = bounds.ToList();

            values = new Dictionary<string, RuleVariable>();
            for (int index = 0; index < bounds.Length; ++index)
            {
                values[standardValueKeys[index]] = bounds[index].AsRuleVariable();
            }
        }
    }
    */
}
