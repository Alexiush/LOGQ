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
            return new BacktrackIterator(() =>
            {
                Predicate<List<IBound>> result = _generator();
                return result is null ? null : context => !result(context);
            },
            _reset);
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
