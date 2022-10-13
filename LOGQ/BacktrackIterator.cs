using System;
using System.Collections.Generic;
using static LOGQ.DelegateTransformer;

namespace LOGQ
{
    /// <summary>
    /// Class that iterates through actions produced by logical qyery's with action.
    /// Used as replacement to prolog predicate types as it's insensitive to available actions count
    /// </summary>
    public class BacktrackIterator
    {
        private Func<Predicate<List<IBound>>> _generator;
        private Action _reset;

        /// <summary>
        /// Constructs backtrack iterator from two functions: generator and reset
        /// </summary>
        /// <param name="generator">
        /// Generator function that returns predicate that defines available action or null where there is no more actions available
        /// </param>
        /// <param name="reset">
        /// Reset function controls state of the generator, after reset called generator must renew set of available actions
        /// </param>
        public BacktrackIterator(Func<Predicate<List<IBound>>> generator, Action reset)
        {
            this._generator = generator;
            this._reset = reset;
        }

        internal BacktrackIterator(ICollection<Predicate<List<IBound>>> initializer)
        {
            bool enumeratorIsUpToDate = false;
            var enumerator = initializer.GetEnumerator();

            _generator = () =>
            {
                if (!enumeratorIsUpToDate)
                {
                    enumerator = initializer.GetEnumerator();
                    enumeratorIsUpToDate = true;
                }

                if (!enumerator.MoveNext())
                {
                    return null;
                }

                Predicate<List<IBound>> predicate = enumerator.Current;
                return predicate;
            };

            _reset = () => enumeratorIsUpToDate = false;
        }

        internal BacktrackIterator(ICollection<Func<bool>> initializer)
        {
            bool enumeratorIsUpToDate = false;
            var enumerator = initializer.GetEnumerator();

            _generator = () =>
            {
                if (!enumeratorIsUpToDate)
                {
                    enumerator = initializer.GetEnumerator();
                    enumeratorIsUpToDate = true;
                }

                if (!enumerator.MoveNext())
                {
                    return null;
                }

                Predicate<List<IBound>> predicate = enumerator.Current.ToPredicate();
                return predicate;
            };

            _reset = () => enumeratorIsUpToDate = false;
        }

        internal BacktrackIterator(ICollection<Action<List<IBound>>> initializer)
        {
            bool enumeratorIsUpToDate = false;
            var enumerator = initializer.GetEnumerator();

            _generator = () =>
            {
                if (!enumeratorIsUpToDate)
                {
                    enumerator = initializer.GetEnumerator();
                    enumeratorIsUpToDate = true;
                }

                if (!enumerator.MoveNext())
                {
                    return null;
                }

                Predicate<List<IBound>> predicate = enumerator.Current.ToPredicate();
                return predicate;
            };

            _reset = () => enumeratorIsUpToDate = false;
        }

        internal BacktrackIterator(ICollection<Action> initializer)
        {
            bool enumeratorIsUpToDate = false;
            var enumerator = initializer.GetEnumerator();

            _generator = () =>
            {
                if (!enumeratorIsUpToDate)
                {
                    enumerator = initializer.GetEnumerator();
                    enumeratorIsUpToDate = true;
                }

                if (!enumerator.MoveNext())
                {
                    return null;
                }

                Predicate<List<IBound>> predicate = enumerator.Current.ToPredicate();
                return predicate;
            };

            _reset = () => enumeratorIsUpToDate = false;
        }

        /// <summary>
        /// Returns backtrack iterator for not function
        /// </summary>
        /// <returns>Negated backtrack iterator</returns>
        internal BacktrackIterator Negate()
        {
            bool foundAnswer = false;
            LogicalQuery innerQuery = new LogicalQuery().With(this);

            return new BacktrackIterator(() =>
            {
                if (foundAnswer)
                {
                    return null;
                }

                foundAnswer = true;
                return copyStorage => !innerQuery.Execute();
            },
            () =>
            {
                foundAnswer = false;
                innerQuery.Reset();
            });
        }

        /// <summary>
        /// Gets next available action from generator
        /// </summary>
        /// <returns>
        /// Predicate that represents one of available actions or null if there is no available action
        /// </returns>
        public Predicate<List<IBound>> GetNext()
        {
            return _generator.Invoke();
        }

        /// <summary>
        /// Resets generator's state, so it renews set of available actions
        /// </summary>
        public void Reset()
        {
            _reset.Invoke();
        }
    }
}
