using System;
using System.Collections.Generic;
using System.Text;

namespace LOGQ
{
    /// <summary>
    /// Marker attribute for mapper that creates fact and rule representations of a class
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class FactAttribute : System.Attribute
    {
        /// <summary>
        /// Creates marker attribute for mapper
        /// </summary>
        /// <param name="factName">Name that will be used by mapper (mapper will add prefixes to given name)</param>
        public FactAttribute(string factName)
        {
            FactName = factName;
        }

        public string FactName { get; }
    }
}
