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

        private static string GenerateFact(GenerationData dataToGenerate)
        {
            var sb = new StringBuilder();

            string className = "Fact" + dataToGenerate.Name.Replace('.', '_');

            // Header

            sb.Append(@"
    public class ")
            .Append($"{className} : LOGQ.Fact")
            .Append(@"
    {");
            // Properties

            foreach (Property property in dataToGenerate.Properties)
            {
                sb.Append(@"
        public Variable<")
                    .Append($"{property.PropertyType}> {property.PropertyName};");
            }

            sb.Append("\n");

            // Constructor

            sb.Append(@"
        public ")
            .Append($"{className}(");
            
            if (dataToGenerate.Properties.Count > 0)
            {
                for (int index = 0; index < dataToGenerate.Properties.Count; index++)
                {
                    Property property = dataToGenerate.Properties[index];

                    if (index > 0)
                    {
                        sb.Append(", ");
                    }
                    sb.Append($"Variable<{property.PropertyType}> {PropertyToField(property.PropertyName)}");
                }
            }

            sb.Append(@")
        {");

            foreach (Property property in dataToGenerate.Properties)
            {
                sb.Append(@"
            this.")
                .Append($"{property.PropertyName} = {PropertyToField(property.PropertyName)};");
            }
            sb.Append(@"
        }
");

            // == and != overload

            sb.Append(@"
        public static bool operator ==(")
                .Append($"{className} fact, {className}")
                .Append(@" otherFact)
        {
            return ");
            if (dataToGenerate.Properties.Count > 0)
            {
                for (int index = 0; index < dataToGenerate.Properties.Count; index++)
                {
                    Property property = dataToGenerate.Properties[index];

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
                .Append($"{className} fact, {className}")
                .Append(@" otherFact)
        {
            return !(fact == otherFact);
        }
");

            // Get Type

            sb.Append(@"
        public override Type FactType()
        {
            return ").Append($"typeof({dataToGenerate.OriginName});").Append(@"
        }
");

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

            sb.Append(@"
    public class ")
            .Append($"{className} : LOGQ.BoundFact")
            .Append(@"
    {");
            // Properties

            foreach (Property property in dataToGenerate.Properties)
            {
                sb.Append(@"
        public BoundVariable<")
                    .Append($"{property.PropertyType}> {property.PropertyName};");
            }

            sb.Append("\n");

            // Constructor

            sb.Append(@"
        public ")
            .Append($"{className}(");

            if (dataToGenerate.Properties.Count > 0)
            {
                for (int index = 0; index < dataToGenerate.Properties.Count; index++)
                {
                    Property property = dataToGenerate.Properties[index];

                    if (index > 0)
                    {
                        sb.Append(", ");
                    }
                    sb.Append($"BoundVariable<{property.PropertyType}> {PropertyToField(property.PropertyName)}");
                }
            }

            sb.Append(@")
        {");

            foreach (Property property in dataToGenerate.Properties)
            {
                sb.Append(@"
            this.")
                .Append($"{property.PropertyName} = {PropertyToField(property.PropertyName)};");
            }
            sb.Append(@"
        }
");

            // == and != overload

            sb.Append(@"
        public static bool operator ==(")
                .Append($"{className} fact, {className}")
                .Append(@" otherFact)
        {
            return ");
            if (dataToGenerate.Properties.Count > 0)
            {
                for (int index = 0; index < dataToGenerate.Properties.Count; index++)
                {
                    Property property = dataToGenerate.Properties[index];

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
                .Append($"{className} fact, {className}")
                .Append(@" otherFact)
        {
            return !(fact == otherFact);
        }
");

            // == and != with common Fact

            sb.Append(@"
        public static bool operator ==(")
                .Append($"{className} fact, Fact{dataToGenerate.Name.Replace('.', '_')}")
                .Append(@" otherFact)
        {
            return ");
            if (dataToGenerate.Properties.Count > 0)
            {
                for (int index = 0; index < dataToGenerate.Properties.Count; index++)
                {
                    Property property = dataToGenerate.Properties[index];
                    
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
                .Append($"{className} fact, Fact{dataToGenerate.Name.Replace('.', '_')}")
                .Append(@" otherFact)
        {
            return !(fact == otherFact);
        }
");

            // Equals

            sb.Append(@"
        public override bool Equals(object? obj)
        {
            ")
            .Append($"{className} bound = obj as {className}")
            .Append(@";
            ")
            .Append($"Fact{dataToGenerate.Name.Replace('.', '_')} common = obj as Fact{dataToGenerate.Name.Replace('.', '_')}")
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


            // Get Type

            sb.Append(@"
        public override Type FactType()
        {
            return ").Append($"typeof({dataToGenerate.OriginName});").Append(@"
        }
");

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

            sb.Append(@"
    public class ")
            .Append(className)
            .Append(" : LOGQ.Rule")
            .Append(@"
    {");
            // Properties

            foreach (Property property in dataToGenerate.Properties)
            {
                sb.Append(@"
        public Variable<")
                    .Append(property.PropertyType)
                    .Append("> ")
                    .Append(property.PropertyName)
                    .Append(";\n");
            }

            sb.Append("\n");

            // Constructor

            sb.Append(@"
        public ")
            .Append(className)
            .Append("(");

            if (dataToGenerate.Properties.Count > 0)
            {
                for (int index = 0; index < dataToGenerate.Properties.Count; index++)
                {
                    Property property = dataToGenerate.Properties[index];

                    if (index > 0)
                    {
                        sb.Append(", ");
                    }
                    sb.Append("Variable<")
                        .Append(property.PropertyType)
                        .Append("> ")
                        .Append(PropertyToField(property.PropertyName));
                }
            }

            sb.Append(@")
        {");

            foreach (Property property in dataToGenerate.Properties)
            {
                sb.Append(@"
            this.")
                .Append(property.PropertyName)
                .Append(" = ")
                .Append(PropertyToField(property.PropertyName))
                .Append(";");
            }
            sb.Append(@"
        }
");

            // == and != overload

            sb.Append(@"
        public static bool operator ==(")
                .Append(className)
                .Append(" fact, ")
                .Append(className)
                .Append(@" otherFact)
        {
            return ");
            if (dataToGenerate.Properties.Count > 0)
            {
                for (int index = 0; index < dataToGenerate.Properties.Count; index++)
                {
                    Property property = dataToGenerate.Properties[index];

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
                .Append(className)
                .Append(" fact, ")
                .Append(className)
                .Append(@" otherFact)
        {
            return !(fact == otherFact);
        }
");

            sb.Append(@"
        public static bool operator ==(")
                .Append(className)
                .Append(" fact, ")
                .Append($"Bound{className}")
                .Append(@" otherFact)
        {
            return ");
            if (dataToGenerate.Properties.Count > 0)
            {
                for (int index = 0; index < dataToGenerate.Properties.Count; index++)
                {
                    Property property = dataToGenerate.Properties[index];

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
                .Append(className)
                .Append(" fact, ")
                .Append($"Bound{className}")
                .Append(@" otherFact)
        {
            return !(fact == otherFact);
        }
");
            // Equals

            sb.Append(@"
        public override bool Equals(object? obj)
        {
            ")
            .Append($"{className} common = obj as {className}")
            .Append(@";
            ")
            .Append($"Bound{className} bound = obj as Bound{className}")
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



            // Get Type

            sb.Append(@"
        public override Type RuleType()
        {
            return ").Append($"typeof({dataToGenerate.OriginName});").Append(@"
        }
");

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

            sb.Append(@"
    public class ")
            .Append(className)
            .Append(" : LOGQ.BoundRule")
            .Append(@"
    {");
            // Properties

            foreach (Property property in dataToGenerate.Properties)
            {
                sb.Append(@"
        public BoundVariable<")
                    .Append(property.PropertyType)
                    .Append("> ")
                    .Append(property.PropertyName)
                    .Append(";\n");
            }

            sb.Append("\n");

            // Constructor

            sb.Append(@"
        public ")
            .Append(className)
            .Append("(");

            if (dataToGenerate.Properties.Count > 0)
            {
                for (int index = 0; index < dataToGenerate.Properties.Count; index++)
                {
                    Property property = dataToGenerate.Properties[index];

                    if (index > 0)
                    {
                        sb.Append(", ");
                    }
                    sb.Append("BoundVariable<")
                        .Append(property.PropertyType)
                        .Append("> ")
                        .Append(PropertyToField(property.PropertyName));
                }
            }

            sb.Append(@")
        {");

            foreach (Property property in dataToGenerate.Properties)
            {
                sb.Append(@"
            this.")
                .Append(property.PropertyName)
                .Append(" = ")
                .Append(PropertyToField(property.PropertyName))
                .Append(";");
            }
            sb.Append(@"
        }
");

            // == and != overload

            sb.Append(@"
        public static bool operator ==(")
                .Append(className)
                .Append(" fact, ")
                .Append(className)
                .Append(@" otherFact)
        {
            return ");
            if (dataToGenerate.Properties.Count > 0)
            {
                for (int index = 0; index < dataToGenerate.Properties.Count; index++)
                {
                    Property property = dataToGenerate.Properties[index];

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
                .Append(className)
                .Append(" fact, ")
                .Append(className)
                .Append(@" otherFact)
        {
            return !(fact == otherFact);
        }
");

            sb.Append(@"
        public static bool operator ==(")
    .Append(className)
    .Append(" fact, ")
    .Append($"Rule{dataToGenerate.Name.Replace('.', '_')}")
    .Append(@" otherFact)
        {
            return ");
            if (dataToGenerate.Properties.Count > 0)
            {
                for (int index = 0; index < dataToGenerate.Properties.Count; index++)
                {
                    Property property = dataToGenerate.Properties[index];

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
                .Append(className)
                .Append(" fact, ")
                .Append($"Rule{dataToGenerate.Name.Replace('.', '_')}")
                .Append(@" otherFact)
        {
            return !(fact == otherFact);
        }
");

            // Equals

            sb.Append(@"
        public override bool Equals(object? obj)
        {
            ")
            .Append($"{className} bound = obj as {className}")
            .Append(@";
            
            ")
            .Append($"Rule{dataToGenerate.Name.Replace('.', '_')} common = obj as Rule{dataToGenerate.Name.Replace('.', '_')}")
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



            // Get Type

            sb.Append(@"
        public override Type RuleType()
        {
            return ").Append($"typeof({dataToGenerate.OriginName});").Append(@"
        }
");

            // Bind

            sb.Append(@"
        public override void Bind(List<IBound> copyStorage)
        {");

            foreach (Property property in dataToGenerate.Properties)
            {
                sb.Append(@"
            ")
                .Append(property.PropertyName)
                .Append($".UpdateValue(copyStorage, {property.PropertyName}.Value);");
            }

            sb.Append(@"
        }");

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
