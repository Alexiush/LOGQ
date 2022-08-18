using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LOGQ
{
    class Variable<T>
    {
        protected T value;

        internal Variable() {}

        public Variable(T value)
        {
            this.value = value;
        }

        public static bool operator ==(Variable<T> fact, Variable<T> otherFact)
        {
            return fact.value.Equals(otherFact.value);
        }

        public static bool operator !=(Variable<T> fact, Variable<T> otherFact)
        {
            return !(fact == otherFact);
        }
    }

    class BoundVariable<T> : Variable<T>
    {
        public void UpdateValue(Dictionary<BoundVariable<T>, T> copyStorage, T value)
        {
            if (!copyStorage.ContainsKey(this))
            {
                copyStorage[this] = this.value;
            }

            this.value = value;
        }
    }

    class IgnorableVariable<T> : Variable<T>
    {
        public IgnorableVariable() {}

        public static bool operator ==(IgnorableVariable<T> fact, Variable<T> otherFact)
        {
            return true;
        }

        public static bool operator !=(IgnorableVariable<T> fact, Variable<T> otherFact)
        {
            return false;
        }
    }

    class DummyBoundVariable<T> : BoundVariable<T>
    {
        public static bool operator ==(DummyBoundVariable<T> fact, BoundVariable<T> otherFact)
        {
            return true;
        }

        public static bool operator !=(DummyBoundVariable<T> fact, BoundVariable<T> otherFact)
        {
            return false;
        }
    }

    // Base class to generate patterns to match rule head (can't be exact values)
    // Like Any, Equals, Unbound, ...

    class RuleVariable<T> : Variable<T> { }
}
