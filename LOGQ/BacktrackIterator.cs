using System;
using System.Collections.Generic;

namespace LOGQ
{
    public class BacktrackIterator
    {
        private Func<Predicate<List<IBound>>> generator;
        private Action reset;

        public BacktrackIterator(Func<Predicate<List<IBound>>> generator, Action reset)
        {
            this.generator = generator;
            this.reset = reset;
        }

        internal BacktrackIterator Negate()
        {
            return new BacktrackIterator(() =>
            {
                Predicate<List<IBound>> result = generator();
                return result is null ? null : context => !result(context);
            },
            reset);
        }

        public Predicate<List<IBound>> GetNext()
        {
            return generator.Invoke();
        }

        public void Reset()
        {
            reset.Invoke();
        }
    }
}
