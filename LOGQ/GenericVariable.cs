using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LOGQ
{
    public interface IVariable { }

    public interface IBound 
    {
        public void Rollback();
    }

    public class Variable<T> : IVariable
    {
        public T value;

        internal Variable() {}

        public Variable(T value)
        {
            this.value = value;
        }

        public static implicit operator Variable<T>(T value)
            => new Variable<T>(value);

        public static bool operator ==(Variable<T> fact, Variable<T> otherFact)
            => fact.value.Equals(otherFact.value);
        

        public static bool operator !=(Variable<T> fact, Variable<T> otherFact)
            => !(fact == otherFact);
        

        public override bool Equals(object obj)
        {
            Variable<T> variable = obj as Variable<T>;

            if (obj is null)
            {
                return false;
            }

            return this == variable;
        }
    }

    public class BoundVariable<T> : Variable<T>, IBound
    {
        public BoundVariable() { }

        public BoundVariable(T value) : base(value)
        {
            copies.Push(value);
        }

        public static implicit operator BoundVariable<T>(T value)
            => new BoundVariable<T>(value);

        // Considered unbound if stack is empty
        private Stack<T> copies = new Stack<T>();

        public bool IsBound() => copies.Count > 0;

        public void UpdateValue(List<IBound> copyStorage, T value)
        {
            if (!copyStorage.Contains(this))
            {
                copyStorage.Add(this);
            }

            copies.Push(value);
            this.value = value;
        }

        public void Rollback()
        {
            copies.Pop();

            if (copies.Count > 0)
            {
                value = copies.Peek();
            }
        }
    }

    public class IgnorableVariable<T> : Variable<T>
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

    public class DummyBoundVariable<T> : BoundVariable<T>
    {
        public static bool operator ==(DummyBoundVariable<T> fact, Variable<T> otherFact)
        {
            return true;
        }

        public static bool operator !=(DummyBoundVariable<T> fact, Variable<T> otherFact)
        {
            return false;
        }

        public override bool Equals(object obj)
        {
            Variable<T> variable = obj as Variable<T>;

            if (obj is null)
            {
                return false;
            }

            return true;
        }
    }

    // Base class to generate patterns to match rule head (can't be exact values)
    // Like Any, Equals, Unbound, ...

    public class RuleVariable<T> : Variable<T> { }

    public class AnyValue<T> : RuleVariable<T>
    {
        public static bool operator ==(AnyValue<T> fact, BoundVariable<T> otherFact)
        {
            return true;
        }

        public static bool operator !=(AnyValue<T> fact, BoundVariable<T> otherFact)
        {
            return false;
        }

        public override bool Equals(object obj)
        {
            BoundVariable<T> variable = obj as BoundVariable<T>;

            if (obj is null)
            {
                return false;
            }

            return true;
        }
    }

    public class SameValue<T> : RuleVariable<T>
    {
        public static bool operator ==(SameValue<T> fact, BoundVariable<T> otherFact)
        {
            // make value seen somehow
            return fact.value.Equals(otherFact.value);
        }

        public static bool operator !=(SameValue<T> fact, BoundVariable<T> otherFact)
        {
            return !(fact == otherFact);
        }

        public override bool Equals(object obj)
        {
            BoundVariable<T> variable = obj as BoundVariable<T>;

            if (obj is null)
            {
                return false;
            }

            return (Variable<T>)value == variable.value;
        }
    }

    public class UnboundValue<T> : RuleVariable<T>
    {
        public static bool operator ==(UnboundValue<T> fact, BoundVariable<T> otherFact)
        {
            return !otherFact.IsBound();
        }

        public static bool operator !=(UnboundValue<T> fact, BoundVariable<T> otherFact)
        {
            return !(fact == otherFact);
        }

        public override bool Equals(object obj)
        {
            BoundVariable<T> variable = obj as BoundVariable<T>;

            if (obj is null)
            {
                return false;
            }

            return !variable.IsBound();
        }
    }
}
