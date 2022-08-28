using System;
using System.Collections.Generic;
using System.Text;

namespace LOGQ
{
    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class FactAttribute : System.Attribute
    {
        public FactAttribute(string factName)
        {
            FactName = factName;
        }

        public string FactName { get; }
    }
}
