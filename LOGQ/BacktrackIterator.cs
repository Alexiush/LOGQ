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
