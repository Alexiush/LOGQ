using System;
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

        private static string GenericAnnotation(GenerationData data)
        {
            if (data.Generics.Count == 0)
            {
                return "";
            }

            return $"<{string.Join(",", data.Generics)}>";
        }

        private static string GenericConstraints(GenerationData data)
        {
            return string.Join("\n", data.Constraints);
        }

        private static string WriteHeader(string className, string inheritedClass, GenerationData data)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(@"
    public sealed class ")
            .Append($"{className} : {inheritedClass}")
            .Append(@"
        ").Append(GenericConstraints(data)).Append(@"
    {
        ");

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
            .Append($"{type} first = obj as {type}")
            .Append(@";
            
            if (!(first is null))
            {
                return this == first;
            }
            ")
            .Append($"{otherType} second = obj as {otherType}")
            .Append(@";
            
            if (!(second is null))
            {
                return this == second;
            }")
            .Append(@"
            
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
                sb.AppendLine($"{property.PropertyName}.Value.GetHashCode(),");
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

        private static string IndexedStorageGetter(string getterName, string storageInterface, string storageType, GenerationData data)
        {
            var sb = new StringBuilder();

            sb.Append(@"
        public static new " + storageInterface + " Storage" + @"()
        {
            return ").Append($"new {storageType}{GenericAnnotation(data)} (); ").Append(@"
        }

");

            sb.Append(@"
        public override " + storageInterface + " " + getterName + @"()
        {
            return Storage();
        }
");

            return sb.ToString();
        }

        private static string GenerateFact(GenerationData dataToGenerate)
        {
            var sb = new StringBuilder();
            string className = "Fact" + dataToGenerate.Name + GenericAnnotation(dataToGenerate);
            
            sb
            // Header
            .Append(WriteHeader(className, "LOGQ.Fact", dataToGenerate))
            // Properties
            .Append(WriteProperties("Variable", dataToGenerate.Properties))
            // Constructor
            .Append(WriteConstructor("Fact" + dataToGenerate.Name, "Variable", dataToGenerate.Properties))
            // == and != overload
            .Append(EqualityOperatorsOverload(className, className, dataToGenerate.Properties))
            // == and != overload for bound facts
            .Append(EqualityOperatorsOverload(className, "Bound" + className, dataToGenerate.Properties))
            // Equals
            .Append(EqualsOverload(className, "Bound" + className))
            // GetHashCode overload
            .Append(GetHashCodeOverload(dataToGenerate.Properties))
            // Get Type
            .Append(TypeGetterOverload("FactType", dataToGenerate.OriginName))
            // Get IndexedStorage
            .Append(IndexedStorageGetter("IndexedFactsStorage", "LOGQ.IIndexedFactsStorage", $"Indexed{"Fact" + dataToGenerate.Name}Storage", dataToGenerate))
            // End
            .Append(@"
    }
");
            return sb.ToString();
        }

        private static string GenerateBoundFact(GenerationData dataToGenerate)
        {
            var sb = new StringBuilder();
            string className = "BoundFact" + dataToGenerate.Name + GenericAnnotation(dataToGenerate);

            sb
            // Header
            .Append(WriteHeader(className, "LOGQ.BoundFact", dataToGenerate))
            // Properties
            .Append(WriteProperties("BoundVariable", dataToGenerate.Properties))
            // Constructor
            .Append(WriteConstructor("BoundFact" + dataToGenerate.Name, "BoundVariable", dataToGenerate.Properties))
            // == and != overload
            .Append(EqualityOperatorsOverload(className, className, dataToGenerate.Properties))
            // == and != with common Fact
            .Append(EqualityOperatorsOverload(className, "Fact" + dataToGenerate.Name + GenericAnnotation(dataToGenerate), dataToGenerate.Properties))
            // Equals
            .Append(EqualsOverload(className, $"Fact{dataToGenerate.Name + GenericAnnotation(dataToGenerate)}"))
            // Get Type
            .Append(TypeGetterOverload("FactType", dataToGenerate.OriginName))
            // Bind
            .Append(@"
        public override void Bind(Fact fact, List<IBound> copyStorage)
        {
            if (fact.FactType() != FactType())
            {
                throw new ArgumentException(");
                sb.Append("\"Can't compare facts based on different types\"")
                .Append(@");
            }
            ")
            .Append($"Fact{dataToGenerate.Name + GenericAnnotation(dataToGenerate)} typedFact = (Fact{dataToGenerate.Name + GenericAnnotation(dataToGenerate)})fact;");

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
            string className = "Rule" + dataToGenerate.Name + GenericAnnotation(dataToGenerate);

            sb
            // Header
            .Append(WriteHeader(className, "LOGQ.Rule", dataToGenerate))
            // Properties
            .Append(WriteProperties("RuleVariable", dataToGenerate.Properties))
            // Constructor
            .Append(WriteConstructor("Rule" + dataToGenerate.Name, "RuleVariable", dataToGenerate.Properties))
            // == and != overload
            .Append(EqualityOperatorsOverload(className, className, dataToGenerate.Properties))
            // == and != with bound fact
            .Append(EqualityOperatorsOverload(className, "Bound" + className, dataToGenerate.Properties))
            // Equals
            .Append(EqualsOverload($"Bound{className}", className))
            // Get Type
            .Append(TypeGetterOverload("RuleType", dataToGenerate.OriginName))
            // Get IndexedStorage
            .Append(IndexedStorageGetter("IndexedRulesStorage", "LOGQ.IIndexedRulesStorage", $"Indexed{"Rule" + dataToGenerate.Name}Storage", dataToGenerate))
            // End
            .Append(@"
    }
");
            return sb.ToString();
        }

        private static string GenerateBoundRule(GenerationData dataToGenerate)
        {
            var sb = new StringBuilder();
            string className = "BoundRule" + dataToGenerate.Name + GenericAnnotation(dataToGenerate);

            sb
            // Header
            .Append(WriteHeader(className, "LOGQ.BoundRule", dataToGenerate))
            // Properties
            .Append(WriteProperties("BoundVariable", dataToGenerate.Properties))
            // Constructor
            .Append(WriteConstructor("BoundRule" + dataToGenerate.Name, "BoundVariable", dataToGenerate.Properties))
            // == and != overload
            .Append(EqualityOperatorsOverload(className, className, dataToGenerate.Properties))
            // == and != for common rule
            .Append(EqualityOperatorsOverload(className, "Rule" + dataToGenerate.Name + GenericAnnotation(dataToGenerate), dataToGenerate.Properties))
            // Equals
            .Append(EqualsOverload($"Rule{dataToGenerate.Name + GenericAnnotation(dataToGenerate)}", className))
            // Get Type
            .Append(TypeGetterOverload("RuleType", dataToGenerate.OriginName))
            // End
            .Append(@"
    }
");
            return sb.ToString();
        }

        private static string GenerateExtensionFunction(GenerationData generatedClass, string classPrefix, string variablePrefix)
        {
            var sb = new StringBuilder();

            string className = classPrefix + generatedClass.Name + GenericAnnotation(generatedClass);

            sb.Append(@"
        public static ")
            .Append(className)
            .Append($" As{classPrefix}{GenericAnnotation(generatedClass)}(")
            .Append($"this {generatedClass.OriginName} origin");

            sb.Append(@")
            ").Append(GenericConstraints(generatedClass))
            .Append(@"
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

                    if (variablePrefix == "Rule")
                    {
                        sb.Append($"new Equal<{property.PropertyType}>(origin.{property.PropertyName})");
                    }
                    else
                    {
                        sb.Append($"origin.{property.PropertyName}");
                    }
                }
            }

            sb.Append(@");
        }
");

            return sb.ToString();
        }

        private static string GenerateFactExtensions(GenerationData data)
        {
            var sb = new StringBuilder();

            // Header

            sb.Append(@"
    public static partial class ")
            .Append("FactExtensions")
            .Append(@"
    {");

            sb.Append(GenerateExtensionFunction(data, "Fact", ""))
                    .Append(GenerateExtensionFunction(data, "BoundFact", "Bound"))
                    .Append(GenerateExtensionFunction(data, "Rule", "Rule"))
                    .Append(GenerateExtensionFunction(data, "BoundRule", "Bound"));

            // End

            sb.Append(@"
    }");

            return sb.ToString();
        }

        private static string GenerateIndexedFactsStorage(GenerationData data)
        {
            string className = "Fact" + data.Name + GenericAnnotation(data);
            string storageName = $"Indexed{"Fact" + data.Name}Storage" + GenericAnnotation(data);

            List<Property> hashableProperties = data.Properties.Where(p => p.CanBeHashed).ToList();

            var sb = new StringBuilder();

            // Header IndexedFact(Name)Storage
            sb.Append(WriteHeader(storageName, "LOGQ.IIndexedFactsStorage", data));

            // List of (Name)
            sb.Append(@"
        ")
            .Append($"List<LOGQ.IFact> facts = new List<LOGQ.IFact>();")
            // HashSet of (Name)
            .Append(@"
        ")
            .Append($"HashSet<{className}> factSet = new HashSet<{className}>();")
            .Append(@"
        
        ")
            .Append($"long version = 0;")
            .Append(@"
        
        ");

            // Dictionary<int, Cluster<IFact>> for each property
            foreach (Property property in hashableProperties)
            {
                sb.Append($"Dictionary<int, Cluster<IFact>> {property.PropertyName} = new Dictionary<int, Cluster<IFact>>();").Append(@"
        ");
            }

            // Add overload
            sb.Append(@"
        public void Add(LOGQ.Fact fact)
        {
            ")
                .Append($"{className} factCasted = ({className})fact;")
                .Append(@"
            if (!factSet.Add(factCasted))
            {
                return;
            }

            facts.Add(factCasted);")
                .Append(@"
            ");
            
            foreach(Property property in hashableProperties)
            {
                sb.Append($"int {property.PropertyName}Hash = factCasted.{property.PropertyName}.Value.GetHashCode();")
                    .Append(@"
            ").Append($"if (!{property.PropertyName}.ContainsKey({property.PropertyName}Hash))").Append(@"
            {
                ")
                    .Append($"{property.PropertyName}.Add({property.PropertyName}Hash, new Cluster<IFact>());").Append(@"
            }").Append(@" 
            ")
                    .Append($"{property.PropertyName}[{property.PropertyName}Hash].Add(fact);").Append(@"
            
            ");
            }

            sb.Append(@"
            version++;
        }
");

            // Retract overload
            sb.Append(@"
        public void Retract(LOGQ.Fact fact)
        {
            ")
             .Append($"{className} factCasted = ({className})fact;")
                .Append(@"

            if (!factSet.Remove(factCasted))
            {
                return;
            }

            facts.Remove(factCasted);")
                .Append(@"
            ");

            foreach (Property property in hashableProperties)
            {
                sb.Append($"int {property.PropertyName}Hash = factCasted.{property.PropertyName}.Value.GetHashCode();")
                    .Append(@"
            ")
                    .Append($"{property.PropertyName}[{property.PropertyName}Hash].Remove(fact);").Append(@"
            
            ");
            }

            sb.Append(@"
            version++;
        }
");

            // Get overload 
            sb.Append(@"
        public List<LOGQ.IFact> FilteredBySample(LOGQ.BoundFact sample)
        {
            ")
                .Append($"Bound{className} sampleCasted = (Bound{className})sample;")
                // Aggregate list of tuples (cluster, size)
                .Append(@"
            List<(Cluster<IFact> cluster, int size)> clusters = new List<(Cluster<IFact> cluster, int size)>();
            ");
                
            foreach (Property property in hashableProperties)
            {
                sb.Append(@"
            ").Append($"if (sampleCasted.{property.PropertyName}.IsBound())").Append(@"
            {
                ").Append($"int code = sampleCasted.{property.PropertyName}.Value.GetHashCode();").Append(@"
                    
                ")
                // Maybe add else for empty clusters to return them - 0 facts
                .Append($"if ({property.PropertyName}.ContainsKey(code))").Append(@"
                {
                    ").Append($"Cluster<IFact> cluster = {property.PropertyName}[code];").Append(@"
                    ").Append($"clusters.Add((cluster, cluster.Size));").Append(@"
                }
                else
                {
                    clusters.Add((new Cluster<IFact>(), 0));
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
                    return new List<LOGQ.IFact> { (LOGQ.Fact)(factCopy) };
                }
            ")
            // Add each property value
            .Append(@"
            }")
            .Append(@"

            if (clusters.Count == 0)
            {
                return facts.Where(fact => sample.Equals(fact)).ToList();
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

            // Get version
            sb.Append(@"
        public long GetVersion()
        {
            return version;
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
            string className = "Fact" + data.Name + GenericAnnotation(data);
            string storageName = $"Indexed{"Fact" + data.Name}Storage" + GenericAnnotation(data);

            var sb = new StringBuilder();

            // Header IndexedFact(Name)Storage
            sb.Append(WriteHeader(storageName, "LOGQ.IIndexedFactsStorage", data));

            // List of (Name)
            sb.AppendLine(@"
        List<LOGQ.IFact> facts = new List<LOGQ.IFact>();       
        HashSet<LOGQ.IFact> factSet = new HashSet<LOGQ.IFact>();
        long version = 0;
        
    ");

            // Add overload
            sb.Append(@"
        public void Add(LOGQ.Fact fact)
        {
            if (!factSet.Add(fact))
            {
                return;
            }

            facts.Add(fact);
            version++;
        }
        
        ");

           // Retract overload
           sb.Append(@"
        public void Retract(LOGQ.Fact fact)
        {
            if (!factSet.Remove(fact))
            {
                return;
            }
            
            facts.Remove(fact);
            version++;
        }
        
        ");

            // Get overload 
            sb.Append(@"public List<LOGQ.IFact> FilteredBySample(LOGQ.BoundFact sample)
        {
            ")
                .Append("return facts.Where(fact => sample.Equals(fact)).ToList();").Append(@"
        }");

            // Get version
            sb.Append(@"
        public long GetVersion()
        {
            return version;
        }
        ");

            // End
            sb.Append(@"
    }
");

            return sb.ToString();
        }

        private static string GenerateIndexedRulesStorage(GenerationData data)
        {
            string className = "Rule" + data.Name + GenericAnnotation(data);
            string storageName = $"Indexed{"Rule" + data.Name}Storage" + GenericAnnotation(data);

            List<Property> hashableProperties = data.Properties.Where(p => p.CanBeHashed).ToList();

            var sb = new StringBuilder();

            // Header IndexedFact(Name)Storage
            sb.Append(WriteHeader(storageName, "LOGQ.IIndexedRulesStorage", data));

            foreach (Property property in hashableProperties)
            {
                sb.AppendLine(@" 
                    " + $"RulesDictionary<{property.PropertyType}> {property.PropertyName} = new RulesDictionary<{property.PropertyType}>();" + @"      
                ");
            }

            sb.Append(@"
        HashSet<RuleTemplate> rules = new HashSet<RuleTemplate>();
        long version = 0;
    ");


            // Add overload
            sb.Append(@"
        public void Add(LOGQ.RuleTemplate rule)
        {          
            if (!rules.Add(rule))
            {
                return;
            }
            ").Append($"var ruleCasted = (RuleWithBody<{"Bound" + className}>)rule;" + @"
            ");
            
            foreach (Property property in hashableProperties)
            {
                sb.AppendLine(@" 
                    " + $"{property.PropertyName}.Add((({className})ruleCasted.Head).{property.PropertyName}, rule);" + @"      
                ");
            }

            sb.Append(@"
            version++;
        }
        
        ");

            // Retract overload
            sb.Append(@"
        public void Retract(LOGQ.RuleTemplate rule)
        {
            if (!rules.Remove(rule))
            {
                return;
            }

            ").Append($"var ruleCasted = (RuleWithBody<{"Bound" + className}>)rule;" + @"
            ");

            foreach (Property property in hashableProperties)
            {
                sb.AppendLine(@" 
                    " + $"{property.PropertyName}.Retract((({className})ruleCasted.Head).{property.PropertyName}, rule);" + @"      
                ");
            }

            sb.Append(@"
            version++;        
        }
        
        ");

            // Get overload 
            sb.Append(@"public List<LOGQ.RuleTemplate> FilteredByPattern(LOGQ.BoundRule pattern)
        {
            ").Append($"var patternCasted = ({"Bound" + className})pattern;" + @"
            ").Append(@"
            List<(Cluster<RuleTemplate> cluster, int size)> clusters = new List<(Cluster<RuleTemplate> cluster, int size)>();
            
");


            foreach (Property property in hashableProperties)
            {
                sb.Append($"var {property.PropertyName}Cluster = {property.PropertyName}.Get(!patternCasted.{property.PropertyName}.IsBound() ? Option<int>.None : patternCasted.{property.PropertyName}.Value.GetHashCode());").Append(@"
            ")
                .Append($"clusters.Add(({property.PropertyName}Cluster, {property.PropertyName}Cluster.Size));" + @"
            ");
            }

            sb.Append(@"
            ").Append(@"
                return clusters
                .OrderBy(cluster => cluster.size)
                .First()
                .cluster
                .GetValues();");

            sb.Append(@"
        }
        
        ");

            // Get version
            sb.Append(@"
        public long GetVersion()
        {
            return version;
        }
        ");

            // End
            sb.Append(@"
    }
");

            return sb.ToString();
        }

        private static string GenerateSimpleRulesStorage(GenerationData data)
        {
            string className = "Rule" + data.Name + GenericAnnotation(data);
            string storageName = $"Indexed{"Rule" + data.Name}Storage" + GenericAnnotation(data);

            var sb = new StringBuilder();

            // Header IndexedFact(Name)Storage
            sb.Append(WriteHeader(storageName, "LOGQ.IIndexedRulesStorage", data));

            // List of (Name)
            sb.AppendLine(@"
        List<LOGQ.RuleTemplate> rules = new List<LOGQ.RuleTemplate>();       
        HashSet<RuleTemplate> ruleSet = new HashSet<RuleTemplate>();
        long version = 0;
        
    ");

            // Add overload
            sb.Append(@"
        public void Add(LOGQ.RuleTemplate rule)
        {
            if (!ruleSet.Add(rule))
            {
                return;
            }

            rules.Add(rule);
            version++;
        }
        
        ");

            // Add overload
            sb.Append(@"
        public void Retract(LOGQ.RuleTemplate rule)
        {
            if (!ruleSet.Remove(rule))
            {
                return;
            }

            rules.Remove(rule);
            version++;
        }
        
        ");

            // Get overload 
            sb.Append(@"public List<LOGQ.RuleTemplate> FilteredByPattern(LOGQ.BoundRule pattern)
        {
            ")
                .Append("return rules.Where(rule => rule.Head.Equals(pattern)).ToList();").Append(@"
        }");

            // Get version
            sb.Append(@"
        public long GetVersion()
        {
            return version;
        }
        ");

            // End
            sb.Append(@"
    }
");

            return sb.ToString();
        }

        private static string GetResource(string classToPlace, string nameSpace)
        {
            var sb = new StringBuilder();

            // If we don't have a namespace, generate the code in the "default"
            // namespace, either global:: or a different <RootNamespace>
            var hasNamespace = !string.IsNullOrEmpty(nameSpace);
            if (hasNamespace)
            {
                // We could use a file-scoped namespace here which would be a little impler, 
                // but that requires C# 10, which might not be available. 
                // Depends what you want to support!
                sb
                    .Append("namespace ")
                    .Append(nameSpace)
                    .AppendLine(@"
    {");
            }

            // Write the actual target generation code here. Not shown for brevity
            sb.AppendLine(classToPlace);

            // Close the namespace, if we had one
            if (hasNamespace)
            {
                sb.Append('}').AppendLine();
            }

            return sb.ToString();
        }

        internal static string GenerateExtensionClass(List<GenerationData> classesToGenerate)
        {
            var sb = new StringBuilder();

            // TODO: Add namespace generation for each class
            sb.Append(@"using LOGQ;
using System;
using System.Collections.Generic;
using System.Linq;
using Functional.Option;

");

            foreach (GenerationData data in classesToGenerate)
            {
                // Add Fact and BoundFact classes
                // Add Rule and BoundRule classes

                var classSB = new StringBuilder();

                classSB.Append(GenerateFact(data))
                .Append(GenerateBoundFact(data))
                .Append(GenerateRule(data))
                .Append(GenerateBoundRule(data));

                if (data.CanBeIndexed)
                {
                    classSB.Append(GenerateIndexedFactsStorage(data));
                }
                else
                {
                    classSB.Append(GenerateSimpleFactsStorage(data));
                }

                if (data.CanBeIndexed && data.HighRuleCountDomain)
                {
                    classSB.Append(GenerateIndexedRulesStorage(data));
                }
                else
                {
                    classSB.Append(GenerateSimpleRulesStorage(data));
                }

                // Add extension class that generates conversions to Fact classes
                classSB.Append(GenerateFactExtensions(data));

                sb.Append(GetResource(classSB.ToString(), data.Namespace));
            }

            return sb.ToString();
        }
    }
}
