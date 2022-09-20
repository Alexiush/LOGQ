﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        private static string GetHashCodeOverload(List<Property> properties)
        {
            var sb = new StringBuilder();

            // ToString has no specification, so GetHashCode is better
            sb.Append(@"
        public override int GetHashCode()
        {
            ")
                .Append(@"List<int> propertyCodes = new List<int>
            {
                ");
                // Get each property hashcode and create a hashcode out of it
            foreach (var property in properties)
            {
                sb.AppendLine($"{property.PropertyName}.GetHashCode(),");
            }

            sb.Append(@"
            };")
                .Append(@"
            int hash = 19;            

            unchecked
            {
                foreach (var code in propertyCodes)
                {
                    hash = hash * 31 + code;
                }
            }
            
            return hash;
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

        private static string IndexedStorageGetter(string getterName, string storageInterface, string storageType)
        {
            var sb = new StringBuilder();

            sb.Append(@"
        public override " + storageInterface + " " + getterName + @"()
        {
            return ").Append($"new {storageType}();").Append(@"
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

            // GetHashCode overload
            sb.Append(GetHashCodeOverload(dataToGenerate.Properties));

            // Get Type
            sb.Append(TypeGetterOverload("FactType", dataToGenerate.OriginName));

            // Get IndexedStorage
            sb.Append(IndexedStorageGetter("IndexedFactsStorage", "LOGQ.IIndexedFactsStorage", $"Indexed{className}Storage"));

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

            // Get IndexedStorage
            sb.Append(IndexedStorageGetter("IndexedFactsStorage", "LOGQ.IIndexedFactsStorage",
                $"Indexed{"Fact" + dataToGenerate.Name.Replace('.', '_')}Storage"));

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

            // Get IndexedStorage
            sb.Append(IndexedStorageGetter("IndexedRulesStorage", "LOGQ.IIndexedRulesStorage", $"Indexed{className}Storage"));

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

            // Get IndexedStorage
            sb.Append(IndexedStorageGetter("IndexedRulesStorage", "LOGQ.IIndexedRulesStorage",
                $"Indexed{"Rule" + dataToGenerate.Name.Replace('.', '_')}Storage"));

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

        private static string GenerateIndexedFactsStorage(GenerationData data)
        {
            string className = "Fact" + data.Name.Replace('.', '_');
            string storageName = $"Indexed{className}Storage";

            List<Property> hashableProoerties = data.Properties.Where(p => p.CanBeHashed).ToList();

            var sb = new StringBuilder();

            // Header IndexedFact(Name)Storage
            sb.Append(WriteHeader(storageName, "LOGQ.IIndexedFactsStorage"));

            // List of (Name)
            sb.Append(@"
        ")
            .Append($"List<LOGQ.Fact> facts = new List<LOGQ.Fact>();")
            // HashSet of (Name)
            .Append(@"
        ")
            .Append($"HashSet<{className}> factSet = new HashSet<{className}>();")
            .Append(@"
        
        ");

            // Dictionary<int, Cluster<Fact>> for each property
            foreach (Property property in hashableProoerties)
            {
                sb.Append($"Dictionary<int, Cluster<Fact>> {property.PropertyName} = new Dictionary<int, Cluster<Fact>>();").Append(@"
        ");
            }

            // Add overload
            sb.Append(@"
        public void Add(LOGQ.Fact fact)
        {
            ")
                .Append($"{className} factCasted = ({className})fact;")
                .Append(@"

            facts.Add(factCasted);")
                .Append(@"
            factSet.Add(factCasted);
            
            ");
            
            // Maybe add those as functions and later call them from here
            foreach(Property property in hashableProoerties)
            {
                sb.Append($"int {property.PropertyName}Hash = factCasted.{property.PropertyName}.Value.GetHashCode();")
                    .Append(@"
            ").Append($"if (!{property.PropertyName}.ContainsKey({property.PropertyName}Hash))").Append(@"
            {
                ")
                    .Append($"{property.PropertyName}.Add({property.PropertyName}Hash, new Cluster<Fact>());").Append(@"
            }").Append(@" 
            ")
                    .Append($"{property.PropertyName}[{property.PropertyName}Hash].Add(fact);").Append(@"
            
            ");
            }

            sb.Append(@"
        }
");

            // Get overload 
            sb.Append(@"
        public List<LOGQ.Fact> FilteredBySample(LOGQ.BoundFact sample)
        {
            ")
                .Append($"Bound{className} sampleCasted = (Bound{className})sample;")
                // Aggregate list of tuples (cluster, size)
                .Append(@"
            List<(Cluster<Fact> cluster, int size)> clusters = new List<(Cluster<Fact> cluster, int size)>();
            ");
                
            foreach (Property property in hashableProoerties)
            {
                sb.Append(@"
            ").Append($"if (sampleCasted.{property.PropertyName}.IsBound())").Append(@"
            {
                ").Append($"int code = sampleCasted.{property.PropertyName}.Value.GetHashCode();").Append(@"
                    
                ")
                // Maybe add else for empty clusters to return them - 0 facts
                .Append($"if ({property.PropertyName}.ContainsKey(code))").Append(@"
                {
                    ").Append($"Cluster<Fact> cluster = {property.PropertyName}[code];").Append(@"
                    ").Append($"clusters.Add((cluster, cluster.Size));").Append(@"
                }
                else
                {
                    clusters.Add((new Cluster<Fact>(), 0));
                }
            }
            
            ");
            }

            sb.Append($"" +
                $"if (clusters.Count == {data.Properties.Count})").Append(@"
            {
                ")
            .Append($"{className} factCopy = new {className}(").Append(@"
                    ")
            .Append(string.Join("\n, ", data.Properties.Select(property => $"sampleCasted.{property.PropertyName}.Value"))).Append(@"
                );
                
                if (factSet.Contains(factCopy))
                {
                    return new List<LOGQ.Fact> { (LOGQ.Fact)(factCopy) };
                }
            ")
            // Add each property value
            .Append(@"
            }")
            .Append(@"

            if (clusters.Count == 0)
            {
                return facts.Where(fact => fact.Equals(sample)).ToList();
            }
            
            ")
                
                .Append(@"return clusters
                .OrderBy(cluster => cluster.size)
                .First()
                .cluster
                .GetValues();")
                
            .Append(@"
        }
        ");

            // End
            sb.Append(@"
    }
");

            return sb.ToString();
        }

        private static string GenerateSimpleFactsStorage(GenerationData data)
        {
            string className = "Fact" + data.Name.Replace('.', '_');
            string storageName = $"Indexed{className}Storage";

            var sb = new StringBuilder();

            // Header IndexedFact(Name)Storage
            sb.Append(WriteHeader(storageName, "LOGQ.IIndexedFactsStorage"));

            // List of (Name)
            sb.AppendLine(@"
        List<LOGQ.Fact> facts = new List<LOGQ.Fact>();       
    ");

            // Add overload
            sb.Append(@"
        public void Add(LOGQ.Fact fact)
        {
            ")
                .Append("facts.Add(fact);").Append(@"
        }
        
        ");

            // Get overload 
            sb.Append(@"public List<LOGQ.Fact> FilteredBySample(LOGQ.BoundFact sample)
        {
            ")
                .Append("return facts.Where(fact => fact.Equals(sample)).ToList();").Append(@"
        }");

            // End
            sb.Append(@"
    }
");

            return sb.ToString();
        }

        private static string GenerateIndexedRulesStorage(GenerationData data)
        {
            string className = "Rule" + data.Name.Replace('.', '_');
            string storageName = $"Indexed{className}Storage";

            var sb = new StringBuilder();

            // Header IndexedFact(Name)Storage
            sb.Append(WriteHeader(storageName, "LOGQ.IIndexedRulesStorage"));

            // List of (Name)
            sb.AppendLine(@"
        List<LOGQ.RuleWithBody> rules = new List<LOGQ.RuleWithBody>();       
    ");

            // Add overload
            sb.Append(@"
        public void Add(LOGQ.RuleWithBody rule)
        {
            ")
                .Append("rules.Add(rule);").Append(@"
        }
        
        ");

            // Get overload 
            sb.Append(@"public List<LOGQ.RuleWithBody> FilteredByPattern(LOGQ.BoundRule pattern)
        {
            ")
                .Append("return rules.Where(rule => rule.Head.Equals(pattern)).ToList();").Append(@"
        }");

            // End
            sb.Append(@"
    }
");

            return sb.ToString();
        }

        private static string GenerateSimpleRulesStorage(GenerationData data)
        {
            string className = "Rule" + data.Name.Replace('.', '_');
            string storageName = $"Indexed{className}Storage";

            var sb = new StringBuilder();

            // Header IndexedFact(Name)Storage
            sb.Append(WriteHeader(storageName, "LOGQ.IIndexedRulesStorage"));

            // List of (Name)
            sb.AppendLine(@"
        List<LOGQ.RuleWithBody> rules = new List<LOGQ.RuleWithBody>();       
    ");

            // Add overload
            sb.Append(@"
        public void Add(LOGQ.RuleWithBody rule)
        {
            ")
                .Append("rules.Add(rule);").Append(@"
        }
        
        ");

            // Get overload 
            sb.Append(@"public List<LOGQ.RuleWithBody> FilteredByPattern(LOGQ.BoundRule pattern)
        {
            ")
                .Append("return rules.Where(rule => rule.Head.Equals(pattern)).ToList();").Append(@"
        }");

            // End
            sb.Append(@"
    }
");

            return sb.ToString();
        }

        public static string GenerateExtensionClass(List<GenerationData> classesToGenerate)
        {
            var sb = new StringBuilder();

            sb.Append(@"using LOGQ;
using System;
using System.Collections.Generic;
using System.Linq;

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

                if (data.CanBeIndexed)
                {
                    sb.Append(GenerateIndexedFactsStorage(data))
                    .Append(GenerateIndexedRulesStorage(data));
                }
                else
                {
                    sb.Append(GenerateSimpleFactsStorage(data))
                    .Append(GenerateSimpleRulesStorage(data));
                }
                
            }

            // Add extension class that generates conversions to Fact classes
            sb.Append(GenerateFactExtensions(classesToGenerate));

            sb.Append(@"
}");

            return sb.ToString();
        }
    }
}
