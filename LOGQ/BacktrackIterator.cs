using System;
using System.Collections.Generic;

namespace LOGQ
{
    public class BacktrackIterator
    {
        private Func<Predicate<List<IBound>>> _generator;
        private Action _reset;

        public BacktrackIterator(Func<Predicate<List<IBound>>> generator, Action reset)
        {
            this._generator = generator;
            this._reset = reset;
        }

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

        public Predicate<List<IBound>> GetNext()
        {
            return _generator.Invoke();
        }

        public void Reset()
        {
            _reset.Invoke();
        }
    }
}
