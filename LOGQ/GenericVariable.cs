using Functional.Option;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LOGQ
{
    /// <summary>
    /// Interface that marks variable types
    /// </summary>
    public interface IVariable { }

    /// <summary>
    /// Interface that marks bound types
    /// </summary>
    public interface IBound { public void Rollback(); }

    /// <summary>
    /// Common variable, used to declare facts
    /// </summary>
    /// <typeparam name="T">Underlying type</typeparam>
    public class Variable<T> : IVariable
    {
        public T Value { get; protected set; }

        private protected Variable() {}

        /// <summary>
        /// Variable of a fact
        /// </summary>
        /// <param name="value">Variable value</param>
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

            if (variable is null)
            {
                return false;
            }

            return this == variable;
        }
    }

    /// <summary>
    /// Bound variable represents variables used in queries.
    /// They are bound to values given to them through actions.
    /// </summary>
    /// <typeparam name="T">Underlying type</typeparam>
    public class BoundVariable<T> : Variable<T>, IBound
    {
        private protected BoundVariable() { }

        public BoundVariable(T value) : base(value)
        {
            copies.Push(value);
        }

        public static implicit operator BoundVariable<T>(T value)
            => new BoundVariable<T>(value);

        // Considered unbound if stack is empty
        private Stack<T> copies = new Stack<T>();

        /// <summary>
        /// Checks if bound variable is actually bound to some value
        /// </summary>
        /// <returns>True if variable has a value else false</returns>
        public bool IsBound() => copies.Count > 0;

        /// <summary>
        /// Updates variable value and adds record about change made to the copy storage
        /// </summary>
        /// <param name="copyStorage">Copy storage</param>
        /// <param name="value">New variable value</param>
        public void UpdateValue(List<IBound> copyStorage, T value)
        {
            copyStorage.Add(this);
            copies.Push(value);
            Value = value;
        }

        /// <summary>
        /// Updates variable value and adds record about change made to the copy storage
        /// </summary>
        /// <param name="copyStorage">Copy storage</param>
        /// <param name="value">New variable value</param>
        public void UpdateValue(List<IBound> copyStorage, Variable<T> variable)
        {
            copyStorage.Add(this);

            var value = variable.Value;
            copies.Push(value);
            Value = value;
        }

        /// <summary>
        /// Returns variable to a state one change ago
        /// </summary>
        /// <exception cref="ArgumentException">
        /// When there is no copies left
        /// </exception>
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

    /// <summary>
    /// Unbound variable - one to get a value, works as any value when comparing
    /// </summary>
    /// <typeparam name="T">Underlying type</typeparam>
    public sealed class UnboundVariable<T> : BoundVariable<T>
    {
        public UnboundVariable() {}

        public static bool operator ==(UnboundVariable<T> fact, Variable<T> otherFact)
        {
            return true;
        }

        public static bool operator !=(UnboundVariable<T> fact, Variable<T> otherFact)
        {
            return false;
        }

        public override bool Equals(object obj)
        {
            Variable<T> variable = obj as Variable<T>;

            if (variable is null)
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Base class for rule patterns
    /// </summary>
    /// <typeparam name="T">Underlying type</typeparam>
    public abstract class RuleVariable<T> : Variable<T>, ISpecificallyStorable<T>
    { 
        protected RuleVariable() { }

        public abstract Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> AddFilter();
        public abstract Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> GetFilter();

        public abstract Option<int> OptionHash();
        public abstract Type PatternType();
    }

    /// <summary>
    /// Interface for rule patterns that need specific approach for clustering
    /// </summary>
    /// <typeparam name="T">type</typeparam>
    public interface ISpecificallyStorable<T> 
    {
        /// <summary>
        /// Function used to get clusters to add/retract rule templates
        /// </summary>
        /// <returns>filtering function</returns>
        public Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> AddFilter();

        /// <summary>
        /// Function used to get clusters to query on rule templates
        /// </summary>
        /// <returns>filtering function</returns>
        public Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> GetFilter();
    }

    /// <summary>
    /// Accepts any value including unbound values
    /// </summary>
    /// <typeparam name="T">Underlying type</typeparam>
    public sealed class AnyValue<T> : RuleVariable<T>
    {
        public AnyValue() { }

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

            if (variable is null)
            {
                return false;
            }

            return true;
        }

        public override Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> AddFilter()
        {
            return (hash, dictionary) =>
            {
                if (!dictionary.ContainsKey(0))
                {
                    dictionary.Add(0, new Cluster<RuleTemplate>());
                }

                return new List<Cluster<RuleTemplate>>
                {
                    // all values are tossed to one cluster
                    dictionary[0]
                };
            };
        }

        public override Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> GetFilter()
        {
            return (hash, dictionary) =>
            {
                var list = new List<Cluster<RuleTemplate>>();

                if (dictionary.ContainsKey(0))
                {
                    // all values are tossed to one cluster
                    list.Add(dictionary[0]);
                };

                return list;
            };
        }

        public override Type PatternType()
        {
            return typeof(AnyValue<T>);
        }

        public override Option<int> OptionHash()
        {
            return 0;
        }
    }

    /// <summary>
    /// Accepts any value excluding unbound values
    /// </summary>
    /// <typeparam name="T">Underlying type</typeparam>
    public sealed class AnyValueBound<T> : RuleVariable<T>
    {
        public AnyValueBound() { }

        public static bool operator ==(AnyValueBound<T> fact, BoundVariable<T> otherFact)
        {
            return otherFact.IsBound();
        }

        public static bool operator !=(AnyValueBound<T> fact, BoundVariable<T> otherFact)
        {
            return !(fact == otherFact);
        }

        public override bool Equals(object obj)
        {
            BoundVariable<T> variable = obj as BoundVariable<T>;

            if (variable is null)
            {
                return false;
            }

            return variable.IsBound();
        }

        public override Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> AddFilter()
        {
            return (hash, dictionary) =>
            {
                var list = new List<Cluster<RuleTemplate>>();

                hash.Match(
                    Some: (value) => {
                        if (!dictionary.ContainsKey(0))
                        {
                            dictionary.Add(0, new Cluster<RuleTemplate>());
                        };

                        list.Add(dictionary[0]);
                    },
                    None: () => { }
                );

                return list;
            };
        }

        public override Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> GetFilter()
        {
            return (hash, dictionary) =>
            {
                var list = new List<Cluster<RuleTemplate>>();

                hash.Match(
                    Some: (value) => {
                        if (dictionary.ContainsKey(0))
                        {
                            list.Add(dictionary[0]);
                        };
                    },
                    None: () => { }
                );

                return list;
            };
        }

        public override Type PatternType()
        {
            return typeof(AnyValueBound<T>);
        }

        public override Option<int> OptionHash()
        {
            return 0;
        }
    }

    /// <summary>
    /// Accepts only values that is equal to it's value
    /// </summary>
    /// <typeparam name="T">Underlying type</typeparam>
    public sealed class Equal<T> : RuleVariable<T>
    {
        private protected Equal() { }

        public Equal(T value)
        {
            Value = value;
        }

        public static implicit operator Equal<T>(T value)
            => new Equal<T>(value);

        public static bool operator ==(Equal<T> fact, BoundVariable<T> otherFact)
        {
            return fact.Value.Equals(otherFact.Value);
        }

        public static bool operator !=(Equal<T> fact, BoundVariable<T> otherFact)
        {
            return !(fact == otherFact);
        }

        public override bool Equals(object obj)
        {
            BoundVariable<T> variable = obj as BoundVariable<T>;

            if (variable is null)
            {
                return false;
            }

            return (Variable<T>)Value == variable.Value;
        }

        public override Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> AddFilter()
        {
            return (hash, dictionary) =>
            {
                if (!dictionary.ContainsKey(hash))
                {
                    dictionary.Add(hash, new Cluster<RuleTemplate>());
                }

                return new List<Cluster<RuleTemplate>> { dictionary[hash] };
            };
        }

        public override Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> GetFilter()
        {
            return (hash, dictionary) =>
            {
                var list = new List<Cluster<RuleTemplate>>();

                if (dictionary.ContainsKey(hash))
                {
                    list.Add(dictionary[hash]);
                }

                return list;
            };
        }

        public override Type PatternType()
        {
            return typeof(Equal<T>);
        }

        public override Option<int> OptionHash()
        {
            return Value is null ? Option<int>.None : Value.GetHashCode();
        }
    }

    /// <summary>
    /// Accepts only values that is not equal to it's value
    /// </summary>
    /// <typeparam name="T">Underlying type</typeparam>
    public sealed class NotEqual<T> : RuleVariable<T>
    {
        private protected NotEqual() {}

        public NotEqual(T value)
        {
            Value = value;
        }

        public static bool operator ==(NotEqual<T> fact, BoundVariable<T> otherFact)
        {
            return !fact.Value.Equals(otherFact.Value);
        }

        public static bool operator !=(NotEqual<T> fact, BoundVariable<T> otherFact)
        {
            return !(fact == otherFact);
        }

        public override bool Equals(object obj)
        {
            BoundVariable<T> variable = obj as BoundVariable<T>;

            if (variable is null)
            {
                return false;
            }

            return (Variable<T>)Value != variable.Value;
        }

        public override Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> AddFilter()
        {
            return (hash, dictionary) =>
            {
                if (!dictionary.ContainsKey(hash))
                {
                    dictionary.Add(hash, new Cluster<RuleTemplate>());
                }

                return new List<Cluster<RuleTemplate>> { dictionary[hash] };
            };
        }

        public override Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> GetFilter()
        {
            return (hash, dictionary) =>
            {
                var filtered = dictionary
                    .Where(pair => pair.Key != hash);

                var list = new List<Cluster<RuleTemplate>>();
                
                foreach (var pair in filtered)
                {
                    list.Add(pair.Value);
                }

                return list;
            };
        }

        public override Type PatternType()
        {
            return typeof(NotEqual<T>);
        }

        public override Option<int> OptionHash()
        {
            return Value is null ? Option<int>.None : Value.GetHashCode();
        }
    }

    /// <summary>
    /// Accepts only values that is not equal to it's value and is bound
    /// </summary>
    /// <typeparam name="T">Underlying type</typeparam>
    public sealed class NotEqualBound<T> : RuleVariable<T>
    {
        private protected NotEqualBound() { }

        public NotEqualBound(T value)
        {
            Value = value;
        }

        public static bool operator ==(NotEqualBound<T> fact, BoundVariable<T> otherFact)
        {
            return !fact.Value.Equals(otherFact.Value) && otherFact.IsBound();
        }

        public static bool operator !=(NotEqualBound<T> fact, BoundVariable<T> otherFact)
        {
            return !(fact == otherFact);
        }

        public override bool Equals(object obj)
        {
            BoundVariable<T> variable = obj as BoundVariable<T>;

            if (variable is null)
            {
                return false;
            }

            return (Variable<T>)Value != variable.Value && variable.IsBound();
        }

        public override Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> AddFilter()
        {
            return (hash, dictionary) =>
            {
                var list = new List<Cluster<RuleTemplate>>();

                if (!hash.HasValue)
                {
                    return list;
                }

                if (!dictionary.ContainsKey(hash))
                {
                    dictionary.Add(hash, new Cluster<RuleTemplate>());
                }

                list.Add(dictionary[hash]);
                return list;
            };
        }

        public override Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> GetFilter()
        {
            return (hash, dictionary) =>
            {
                var list = new List<Cluster<RuleTemplate>>();

                if (!hash.HasValue)
                {
                    return list;
                }

                var filtered = dictionary
                    .Where(pair => pair.Key != hash);

                foreach (var pair in filtered)
                {
                    list.Add(pair.Value);
                }

                return list;
            };
        }

        public override Type PatternType()
        {
            return typeof(NotEqualBound<T>);
        }

        public override Option<int> OptionHash()
        {
            return Value.GetHashCode();
        }
    }

    /// <summary>
    /// Accepts only unbound variables
    /// </summary>
    /// <typeparam name="T">Underlying type</typeparam>
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

            if (variable is null)
            {
                return false;
            }

            return !variable.IsBound();
        }

        public override Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> AddFilter()
        {
            return (hash, dictionary) =>
            {
                var list = new List<Cluster<RuleTemplate>>();

                hash.Match(
                    Some: (value) => { },
                    None: () => {
                        if (!dictionary.ContainsKey(0))
                        {
                            dictionary.Add(0, new Cluster<RuleTemplate>());
                        }

                        list.Add(dictionary[0]);
                    }
                );

                return list;
            };
        }

        public override Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> GetFilter()
        {
            return (hash, dictionary) =>
            {
                var list = new List<Cluster<RuleTemplate>>();

                hash.Match(
                    Some: (value) => { },
                    None: () => {
                        if (dictionary.ContainsKey(0))
                        {
                            list.Add(dictionary[0]);
                        }
                    }
                );

                return list;
            };
        }

        public override Type PatternType()
        {
            return typeof(UnboundValue<T>);
        }

        public override Option<int> OptionHash()
        {
            return Option<int>.None;
        }
    }
}
