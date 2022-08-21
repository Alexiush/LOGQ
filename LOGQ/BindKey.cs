using System.Collections.Generic;
using static LOGQ.Extensions.ExtensionsMethods;

namespace LOGQ
{
    public class Bound
    {

    }

    /*
    // Create Generic based BindKey
    
    public class BindKey
    {
        public readonly string name;
        public string Value { get; private set; }

        protected BindKey() { }

        public BindKey(string name)
        {
            this.name = name;
        }

        public BindKey(string name, string value)
        {
            this.name = name;
            Value = value;
        }

        public void UpdateValue(Dictionary<BindKey, string> copyStorage, string value)
        {
            if (!copyStorage.ContainsKey(this))
            {
                copyStorage[this] = this.Value;
            }

            Value = value;
        }

        public virtual FactVariable AsFactVariable()
        {
            return new FactVariable(Value);
        }

        public virtual RuleVariable AsRuleVariable()
        {
            return new RuleVariable(Value);
        }
    }

    public class DummyBound : BindKey
    {
        public DummyBound() { }

        public override FactVariable AsFactVariable()
        {
            return AnyFact();
        }

        public override RuleVariable AsRuleVariable()
        {
            return AnyRule();
        }
    }

    // Must be used in duck-typing for pattern matching
    // Must turn into bound after getting some value
    public class Unbound : BindKey
    {
        public Unbound() { }

        public override FactVariable AsFactVariable()
        {
            return AnyFact();
        }

        public override RuleVariable AsRuleVariable()
        {
            return AnyRule();
        }
    }
    */
}
