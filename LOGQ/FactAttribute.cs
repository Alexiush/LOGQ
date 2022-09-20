using System;
using System.Collections.Generic;
using System.Text;

namespace LOGQ
{
    /// <summary>
    /// Enum with mapping modes that define which class members will be used when mapping to fact
    /// </summary>
    public enum MappingMode
    {
        PublicProperties,
        AllProperties,
        PublicFields,
        AllFields,
        PublicPropertiesAndFields,
        AllPropertiesAndFields,
        MarkedData
    }

    /// <summary>
    /// Marker attribute for class members that will be used in mapping to fact
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class FactMemberAttribute : System.Attribute {}

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
        public FactAttribute(string factName, MappingMode mappingMode = MappingMode.PublicProperties)
        {
            FactName = factName;
            MappingMode = mappingMode;

            if (!System.CodeDom.Compiler.CodeGenerator.IsValidLanguageIndependentIdentifier(factName))
            {
                throw new System.ArgumentException("Not a valid class name");
            }
        }

        public string FactName { get; }
        public MappingMode MappingMode { get; }
    }
}
