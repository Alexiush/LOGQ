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
        public T Value { get; protected set; }

        internal Variable() {}

        public Variable(T value)
        {
            this.Value = value;
        }

        public static implicit operator Variable<T>(T value)
            => new Variable<T>(value);

        public static bool operator ==(Variable<T> fact, Variable<T> otherFact)
            => fact.Value.Equals(otherFact.Value);
        

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
        internal BoundVariable() { }

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
            copyStorage.Add(this);
            copies.Push(value);
            Value = value;
        }

        public void Rollback()
        {
            if (copies.Count == 0)
            {
                throw new ArgumentException("Nothing to rollback");
            }

            copies.Pop();
            if (copies.Count > 0)
            {
                Value = copies.Peek();
            }
        }
    }

    public sealed class IgnorableVariable<T> : Variable<T>
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

    public sealed class DummyBoundVariable<T> : BoundVariable<T>
    {
        public DummyBoundVariable() {}

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

    public class RuleVariable<T> : Variable<T> 
    { 
        private protected RuleVariable() { }
    }

    public sealed class AnyValue<T> : RuleVariable<T>
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

    public sealed class NotEqual<T> : RuleVariable<T>
    {
        private protected NotEqual()
        {

        }

        public NotEqual(T value)
        {
            Value = value;
        }

        public static bool operator ==(NotEqual<T> fact, BoundVariable<T> otherFact)
        {
            // make value seen somehow
            return !fact.Value.Equals(otherFact.Value);
        }

        public static bool operator !=(NotEqual<T> fact, BoundVariable<T> otherFact)
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

            return (Variable<T>)Value != variable.Value;
        }
    }

    public sealed class UnboundValue<T> : RuleVariable<T>
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
