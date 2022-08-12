using System;
using System.Collections.Generic;

namespace LOGQ
{
    public class BacktrackIterator
    {
        private Func<Predicate<Dictionary<BindKey, string>>> generator;
        private Action reset;

        public BacktrackIterator(Func<Predicate<Dictionary<BindKey, string>>> generator, Action reset)
        {
            this.generator = generator;
            this.reset = reset;
        }

        public Predicate<Dictionary<BindKey, string>> GetNext()
        {
            return generator.Invoke();
        }

        public void Reset()
        {
            reset.Invoke();
        }
    }
}
