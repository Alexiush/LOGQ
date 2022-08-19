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
        public BoundVariable() { }

        public BoundVariable(T value) : base(value)
        {
            copies.Push(value);
        }

        // Considered unbound if stack is empty
        private Stack<T> copies = new Stack<T>();

        public bool IsBound() => copies.Count > 0;

        public void UpdateValue(List<BoundVariable<T>> copyStorage, T value)
        {
            if (!copyStorage.Contains(this))
            {
                copyStorage.Add(this);
            }

            copies.Push(value);
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

    class AnyValue<T> : RuleVariable<T>
    {
        public static bool operator ==(AnyValue<T> fact, BoundVariable<T> otherFact)
        {
            return true;
        }

        public static bool operator !=(AnyValue<T> fact, BoundVariable<T> otherFact)
        {
            return false;
        }
    }

    class SameValue<T> : RuleVariable<T>
    {
        public static bool operator ==(SameValue<T> fact, BoundVariable<T> otherFact)
        {
            // make value seen somehow
            return fact.value == otherFact.value;
        }

        public static bool operator !=(SameValue<T> fact, BoundVariable<T> otherFact)
        {
            return !(fact == otherFact);
        }
    }

    class UnboundValue<T> : RuleVariable<T>
    {
        public static bool operator ==(UnboundValue<T> fact, BoundVariable<T> otherFact)
        {
            return !otherFact.IsBound();
        }

        public static bool operator !=(UnboundValue<T> fact, BoundVariable<T> otherFact)
        {
            return !(fact == otherFact);
        }
    }
}
