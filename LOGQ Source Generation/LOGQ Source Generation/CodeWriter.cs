using System;
using System.Collections.Generic;
using System.Text;

namespace LOGQ_Source_Generation
{
    public static class SourceGenerationHelper
    {
        // From properties naming conventions to field naming conventions
        private static string PropertyToField(string property)
        {
            return char.ToLower(property[0]) + property.Remove(0, 1);
        }

        private static string WriteHeader(string className, string inheritedClass)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(@"
    public class ")
            .Append($"{className} : {inheritedClass}")
            .Append(@"
    {");

            return sb.ToString();
        }

        private static string WriteProperties(string propertyPrefix, List<Property> properties)
        {
            StringBuilder sb = new StringBuilder();

            foreach (Property property in properties)
            {
                sb.Append(@"
        public " + propertyPrefix + "<")
                    .Append($"{property.PropertyType}> {property.PropertyName};");
            }

            sb.Append("\n");

            return sb.ToString();
        }

        private static string WriteConstructor(string className, string propertyPrefix, List<Property> properties)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(@"
        public ")
.Append($"{className}(");

            if (properties.Count > 0)
            {
                for (int index = 0; index < properties.Count; index++)
                {
                    Property property = properties[index];

                    if (index > 0)
                    {
                        sb.Append(", ");
                    }
                    sb.Append($"{propertyPrefix}<{property.PropertyType}> {PropertyToField(property.PropertyName)}");
                }
            }

            sb.Append(@")
        {");

            foreach (Property property in properties)
            {
                sb.Append(@"
            this.")
                .Append($"{property.PropertyName} = {PropertyToField(property.PropertyName)};");
            }
            sb.Append(@"
        }
");

            return sb.ToString();
        }

        private static string EqualityOperatorsOverload(string type, string otherType, List<Property> properties)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(@"
        public static bool operator ==(")
                .Append($"{type} fact, {otherType}")
                .Append(@" otherFact)
        {
            return ");
            if (properties.Count > 0)
            {
                for (int index = 0; index < properties.Count; index++)
                {
                    Property property = properties[index];

                    if (index > 0)
                    {
                        sb.Append(" && ");
                    }
                    sb.Append($"fact.{property.PropertyName}.Equals(otherFact.{property.PropertyName})");
                }

                sb.Append(";");
            }
            else
            {
                sb.Append("return true;");
            }

            sb.Append(@"
        }
");

            sb.Append(@"
        public static bool operator !=(")
                .Append($"{type} fact, {otherType}")
                .Append(@" otherFact)
        {
            return !(fact == otherFact);
        }
");

            return sb.ToString();
        }

        private static string EqualsOverload(string type, string otherType)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(@"
        public override bool Equals(object? obj)
        {
            ")
            .Append($"{type} bound = obj as {type}")
            .Append(@";
            ")
            .Append($"{otherType} common = obj as {otherType}")
            .Append(@";
            
            ")
            .Append(@"if (!(bound is null))
            {
                return this == bound;
            }
            
            if (!(common is null))
            {
                return this == common;
            }
            
            return false;
        }
");

            return sb.ToString();
        }

        private static string TypeGetterOverload(string getterName, string type)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(@"
        public override Type " + getterName + @"()
        {
            return ").Append($"typeof({type});").Append(@"
        }
");

            return sb.ToString();
        }

        private static string GenerateFact(GenerationData dataToGenerate)
        {
            var sb = new StringBuilder();

            string className = "Fact" + dataToGenerate.Name.Replace('.', '_');

            // Header
            sb.Append(WriteHeader(className, "LOGQ.Fact"));

            // Properties
            sb.Append(WriteProperties("Variable", dataToGenerate.Properties));

            // Constructor
            sb.Append(WriteConstructor(className, "Variable", dataToGenerate.Properties));

            // == and != overload
            sb.Append(EqualityOperatorsOverload(className, className, dataToGenerate.Properties));

            // Get Type
            sb.Append(TypeGetterOverload("FactType", dataToGenerate.OriginName));

            // End
            sb.Append(@"
    }
");

            return sb.ToString();
        }

        private static string GenerateBoundFact(GenerationData dataToGenerate)
        {
            var sb = new StringBuilder();

            string className = "BoundFact" + dataToGenerate.Name.Replace('.', '_');

            // Header
            sb.Append(WriteHeader(className, "LOGQ.BoundFact"));

            // Properties
            sb.Append(WriteProperties("BoundVariable", dataToGenerate.Properties));

            // Constructor
            sb.Append(WriteConstructor(className, "BoundVariable", dataToGenerate.Properties));

            // == and != overload
            sb.Append(EqualityOperatorsOverload(className, className, dataToGenerate.Properties));

            // == and != with common Fact
            sb.Append(EqualityOperatorsOverload(className, "Fact" + dataToGenerate.Name.Replace('.', '_'), dataToGenerate.Properties));

            // Equals
            sb.Append(EqualsOverload(className, $"Fact{dataToGenerate.Name.Replace('.', '_')}"));

            // Get Type
            sb.Append(TypeGetterOverload("FactType", dataToGenerate.OriginName));

            // Bind
            sb.Append(@"
        public override void Bind(Fact fact, List<IBound> copyStorage)
        {
            if (fact.FactType() != FactType())
            {
                throw new ArgumentException(");
                sb.Append("\"Can't compare facts based on different types\"")
                .Append(@");
            }
            ")
            .Append($"Fact{dataToGenerate.Name.Replace('.', '_')} typedFact = (Fact{dataToGenerate.Name.Replace('.', '_')})fact;");

            foreach (Property property in dataToGenerate.Properties)
            {
                sb.Append(@"
            this.")
                .Append($"{property.PropertyName}.UpdateValue(copyStorage, typedFact.{property.PropertyName}.Value);");
            }

            sb.Append(@"
        }");

            // End

            sb.Append(@"
    }
");

            return sb.ToString();
        }

        private static string GenerateRule(GenerationData dataToGenerate)
        {
            var sb = new StringBuilder();

            string className = "Rule" + dataToGenerate.Name.Replace('.', '_');

            // Header
            sb.Append(WriteHeader(className, "LOGQ.Rule"));

            // Properties
            sb.Append(WriteProperties("Variable", dataToGenerate.Properties));

            // Constructor
            sb.Append(WriteConstructor(className, "Variable", dataToGenerate.Properties));

            // == and != overload
            sb.Append(EqualityOperatorsOverload(className, className, dataToGenerate.Properties));

            // == and != with bound fact
            sb.Append(EqualityOperatorsOverload(className, "Bound" + className, dataToGenerate.Properties));

            // Equals
            sb.Append(EqualsOverload($"Bound{className}", className));

            // Get Type
            sb.Append(TypeGetterOverload("RuleType", dataToGenerate.OriginName));

            // End

            sb.Append(@"
    }
");

            return sb.ToString();
        }

        private static string GenerateBoundRule(GenerationData dataToGenerate)
        {
            var sb = new StringBuilder();

            string className = "BoundRule" + dataToGenerate.Name.Replace('.', '_');

            // Header
            sb.Append(WriteHeader(className, "LOGQ.BoundRule"));

            // Properties
            sb.Append(WriteProperties("BoundVariable", dataToGenerate.Properties));

            // Constructor
            sb.Append(WriteConstructor(className, "BoundVariable", dataToGenerate.Properties));

            // == and != overload
            sb.Append(EqualityOperatorsOverload(className, className, dataToGenerate.Properties));

            // == and != for common rule
            sb.Append(EqualityOperatorsOverload(className, "Rule" + dataToGenerate.Name.Replace('.', '_'), dataToGenerate.Properties));

            // Equals
            sb.Append(EqualsOverload(className, $"Rule{dataToGenerate.Name.Replace('.', '_')}"));

            // Get Type
            sb.Append(TypeGetterOverload("RuleType", dataToGenerate.OriginName));

            // End

            sb.Append(@"
    }
");

            return sb.ToString();
        }

        private static string GenerateExtensionFunction(GenerationData generatedClass, string classPrefix, string variablePrefix)
        {
            var sb = new StringBuilder();

            string className = classPrefix + generatedClass.Name.Replace('.', '_');

            sb.Append(@"
        public static ")
            .Append(className)
            .Append($" As{classPrefix}(")
            .Append($"this {generatedClass.OriginName} origin");

            sb.Append(@")
        {
            ")
            .Append($"return new {className}(");

            if (generatedClass.Properties.Count > 0)
            {
                for (int index = 0; index < generatedClass.Properties.Count; index++)
                {
                    Property property = generatedClass.Properties[index];

                    if (index > 0)
                    {
                        sb.Append(", ");
                    }
                    sb.Append($"origin.{property.PropertyName}");
                }
            }

            sb.Append(@");
        }
");

            return sb.ToString();
        }

        private static string GenerateFactExtensions(List<GenerationData> generatedClasses)
        {
            var sb = new StringBuilder();

            // Header

            sb.Append(@"
    public static class ")
            .Append("FactExtensions")
            .Append(@"
    {");

            foreach (GenerationData generatedClass in generatedClasses)
            {
                sb.Append(GenerateExtensionFunction(generatedClass, "Fact", ""))
                    .Append(GenerateExtensionFunction(generatedClass, "BoundFact", "Bound"))
                    .Append(GenerateExtensionFunction(generatedClass, "Rule", "Rule"))
                    .Append(GenerateExtensionFunction(generatedClass, "BoundRule", "Bound"));
            }

            // End

            sb.Append(@"
    }");

            return sb.ToString();
        }

        public static string GenerateExtensionClass(List<GenerationData> classesToGenerate)
        {
            var sb = new StringBuilder();

            sb.Append(@"using LOGQ;
using System;
using System.Collections.Generic;

namespace LOGQ.Generation
{");

            foreach (GenerationData data in classesToGenerate)
            {
                // Add Fact and BoundFact classes
                // Add Rule and BoundRule classes

                sb.Append(GenerateFact(data))
                .Append(GenerateBoundFact(data))
                .Append(GenerateRule(data))
                .Append(GenerateBoundRule(data));
            }

            // Add extension class that generates conversions to Fact classes
            sb.Append(GenerateFactExtensions(classesToGenerate));

            sb.Append(@"
}");

            return sb.ToString();
        }
    }
}
