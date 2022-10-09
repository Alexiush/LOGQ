using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Collections.Immutable;
using System.Threading;
using System.IO;
using Microsoft.CodeAnalysis.CSharp;
using System.Text.RegularExpressions;
using System.Reflection.Metadata;

namespace LOGQ_Source_Generation
{
    /// <summary>
    /// Class that represents properties of fact being mapped
    /// </summary>
    internal struct Property
    {
        public string PropertyName;
        public string PropertyType;
        public bool CanBeHashed;

        public Property(string propertyName, string propertyType, bool canBeHashed)
        {
            PropertyName = propertyName;
            PropertyType = propertyType;
            CanBeHashed = canBeHashed;
        }
    }

    /// <summary>
    /// Data needed to generate code
    /// </summary>
    internal class GenerationData
    {
        public readonly string OriginName;
        public readonly string Name;
        public readonly string Namespace;
        public readonly List<Property> Properties;
        public readonly bool CanBeIndexed;
        public readonly List<string> Generics;
        public readonly List<string> Constraints;

        private static List<string> GetGenerics(string name)
        {
            Regex genericParameter = new Regex(@"\<(.*?)\>");
            ImmutableHashSet<string> set = genericParameter.Matches(name)
                .Cast<Match>()
                .Select(m => m.Value
                    .Substring(1, m.Length - 2)
                    .Replace(" ", string.Empty)
                    .Split(','))
                .SelectMany(s => s)
                .ToImmutableHashSet();

            return set.ToList();
        }

        public GenerationData(string originName, string name, string nameSpace, 
            List<Property> properties, bool canBeIndexed, List<string> constraints)
        {
            OriginName = originName;
            Name = name;
            Namespace = nameSpace;
            Properties = properties;
            CanBeIndexed = canBeIndexed || properties.All(property => !property.CanBeHashed);
            Generics = GetGenerics(originName);
            Constraints = constraints;
        }
    }

    /// <summary>
    /// Class that generates fact/rule representations of objects
    /// </summary>
    [Generator]
    public class FactsGenerator : IIncrementalGenerator
    {
        private static bool PropertyFilter(ISymbol member) 
            => member.Kind == SymbolKind.Property;
        private static bool FieldFilter(ISymbol member) 
            => member.Kind == SymbolKind.Field && ((IFieldSymbol)member).AssociatedSymbol is null;
        private static bool PublicityFilter(ISymbol member) 
            => member.DeclaredAccessibility == Accessibility.Public;

        private static bool MarkedFilter(ISymbol member) 
            => member.GetAttributes().Any(a => a.AttributeClass.ToDisplayString() == "LOGQ.FactMemberAttribute");

        private static ITypeSymbol PropertyTypeReciever(ISymbol member)
            => ((IPropertySymbol)member).Type;

        private static ITypeSymbol FieldTypeReciever(ISymbol member)
            => ((IFieldSymbol)member).Type;

        private static bool HashablePropertyFilter(ISymbol member)
            => !member.GetAttributes().Any(a => a.AttributeClass.ToDisplayString() == "LOGQ.NotHashComparableAttribute");

        private static ITypeSymbol UniversalTypeReciever(ISymbol member)
        {
            if (member as IPropertySymbol != null)
            {
                return PropertyTypeReciever(member);
            }
            else
            {
                return FieldTypeReciever(member);
            }
        }

        private static List<string> GetConstraints(BaseTypeDeclarationSyntax typeSyntax)
        {
            List<string> constraints = new List<string>();

            TypeDeclarationSyntax? syntax = typeSyntax as TypeDeclarationSyntax;

            while (syntax != null && IsAllowedKind(syntax.Kind()))
            {
                constraints.Add(syntax.ConstraintClauses.ToString());
                syntax = (syntax.Parent as TypeDeclarationSyntax);
            }

            return constraints
                .Where(constraint => constraint is not null && constraint.Trim() != "")
                .ToList();
        }

        static bool IsAllowedKind(SyntaxKind kind) =>
            kind == SyntaxKind.ClassDeclaration ||
            kind == SyntaxKind.StructDeclaration ||
            kind == SyntaxKind.RecordDeclaration;

        /// <summary>
        /// determine the namespace the class/enum/struct is declared in, if any
        /// </summary>
        /// <param name="syntax">Syntax element to check for namespace</param>
        /// <returns>Namespace name</returns>
        static string GetNamespace(BaseTypeDeclarationSyntax syntax)
        {
            // If we don't have a namespace at all we'll return an empty string
            // This accounts for the "default namespace" case
            string nameSpace = string.Empty;

            // Get the containing syntax node for the type declaration
            // (could be a nested type, for example)
            SyntaxNode? potentialNamespaceParent = syntax.Parent;

            // Keep moving "out" of nested classes etc until we get to a namespace
            // or until we run out of parents
            while (potentialNamespaceParent != null &&
                    potentialNamespaceParent is not NamespaceDeclarationSyntax
                    && potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
            {
                potentialNamespaceParent = potentialNamespaceParent.Parent;
            }

            // Build up the final namespace by looping until we no longer have a namespace declaration
            if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent)
            {
                // We have a namespace. Use that as the type
                nameSpace = namespaceParent.Name.ToString();

                // Keep moving "out" of the namespace declarations until we 
                // run out of nested namespace declarations
                while (true)
                {
                    if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent)
                    {
                        break;
                    }

                    // Add the outer namespace as a prefix to the final namespace
                    nameSpace = $"{namespaceParent.Name}.{nameSpace}";
                    namespaceParent = parent;
                }
            }

            // return the final namespace
            return nameSpace;
        }

        /// <summary>
        /// Gets data from class declaration syntax objects
        /// </summary>
        /// <param name="compilation">Compilation</param>
        /// <param name="typeDeclarations">Classes marked by attribute</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>List of data needed to generate facts/rules</returns>
        static List<GenerationData> GetTypesToGenerate(Compilation compilation, IEnumerable<BaseTypeDeclarationSyntax> typeDeclarations, CancellationToken ct)
        {
            // Create a list to hold output
            var classesToGenerate = new List<GenerationData>();
            // Get the semantic representation of marker attribute 
            INamedTypeSymbol? classAttribute = compilation.GetTypeByMetadataName("LOGQ.FactAttribute");
            INamedTypeSymbol? noIndexingAttribute = compilation.GetTypeByMetadataName("LOGQ.NoIndexingAttribute");

            if (classAttribute == null)
            {
                // If this is null, the compilation couldn't find the marker attribute type
                // which suggests there's something very wrong! Bail out..
                return classesToGenerate;
            }

            foreach (BaseTypeDeclarationSyntax classDeclarationSyntax in typeDeclarations)
            {
                // stop if we're asked to
                ct.ThrowIfCancellationRequested();

                // Get the semantic representation of the сlass syntax
                SemanticModel semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
                if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
                {
                    // something went wrong, bail out
                    continue;
                }

                // Get the full type name of the class 
                string className = classSymbol.ToDisplayString();
                int mappingMode = 0;
                bool canBeIndexed = true;

                // Loop through all of the attributes on the class until we find the [LOGQ.Fact] attribute
                foreach (AttributeData attributeData in classSymbol.GetAttributes())
                {
                    // if it's NoHashing Attribute - change flag

                    if (noIndexingAttribute.Equals(attributeData.AttributeClass, SymbolEqualityComparer.Default))
                    {
                        canBeIndexed = false;
                    }

                    if (!classAttribute.Equals(attributeData.AttributeClass, SymbolEqualityComparer.Default))
                    {
                        // This isn't the [LOGQ.Fact] attribute
                        continue;
                    }

                    // This is the right attribute, check the constructor arguments
                    if (!attributeData.ConstructorArguments.IsEmpty)
                    {
                        ImmutableArray<TypedConstant> args = attributeData.ConstructorArguments;

                        // make sure we don't have any errors
                        foreach (TypedConstant arg in args)
                        {
                            if (arg.Kind == TypedConstantKind.Error)
                            {
                                // have an error, so don't try and do any generation
                                return new List<GenerationData>();
                            }
                        }

                        // Use the position of the argument to infer which value is set
                        switch (args.Length)
                        {
                            case 1:
                                className = (string)args[0].Value;
                                break;
                            case 2:
                                className = (string)args[0].Value;
                                mappingMode = (int)args[1].Value;
                                break;
                        }
                    }


                    // now check for named arguments
                    if (!attributeData.NamedArguments.IsEmpty)
                    {
                        foreach (KeyValuePair<string, TypedConstant> arg in attributeData.NamedArguments)
                        {
                            TypedConstant typedConstant = arg.Value;
                            if (typedConstant.Kind == TypedConstantKind.Error)
                            {
                                // have an error, so don't try and do any generation
                                return new List<GenerationData>();
                            }
                            else
                            {
                                // Use the constructor argument or property name to infer which value is set
                                switch (arg.Key)
                                {
                                    case "factName":
                                        className = (string)typedConstant.Value;
                                        break;
                                    case "mappingMode":
                                        mappingMode = (int)typedConstant.Value;
                                        break;
                                }
                            }
                        }
                    }

                    break;
                }

                // Get all the members in the class
                ImmutableArray<ISymbol> classMembers = classSymbol.GetMembers();
                var properties = new List<Property>(classMembers.Length);

                Predicate<ISymbol> filter = member => false;
                Func<ISymbol, ITypeSymbol> typeReciever = null;

                switch (mappingMode)
                {
                    case 0:
                        filter = member => PropertyFilter(member) && PublicityFilter(member);
                        typeReciever = member => PropertyTypeReciever(member);
                        break;
                    case 1:
                        filter = member => PropertyFilter(member);
                        typeReciever = member => PropertyTypeReciever(member);
                        break;
                    case 2:
                        filter = member => FieldFilter(member) && PublicityFilter(member);
                        typeReciever = member => FieldTypeReciever(member);
                        break;
                    case 3:
                        filter = member => FieldFilter(member);
                        typeReciever = member => FieldTypeReciever(member);
                        break;
                    case 4:
                        filter = member => (PropertyFilter(member) || FieldFilter(member)) && PublicityFilter(member);
                        typeReciever = member => UniversalTypeReciever(member);
                        break;
                    case 5:
                        filter = member => PropertyFilter(member) || FieldFilter(member);
                        typeReciever = member => UniversalTypeReciever(member);
                        break;
                    case 6:
                        filter = member => MarkedFilter(member);
                        typeReciever = member => UniversalTypeReciever(member);
                        break;
                    default:
                        break;
                }

                // Get all the properties from the class, and add their name to the list
                foreach (ISymbol member in classMembers)
                {
                    if (filter(member))
                    {
                        properties.Add(new Property(member.Name, typeReciever(member).ToDisplayString(), HashablePropertyFilter(member)));
                    }
                }

                string classNamespace = GetNamespace(classDeclarationSyntax);
                // Create a GenerationData for use in the generation phase
                classesToGenerate.Add(new GenerationData(classSymbol.ToDisplayString(), 
                    className, classNamespace, properties, canBeIndexed, GetConstraints(classDeclarationSyntax)));
            }

            return classesToGenerate;
        }

        /// <summary>
        /// Generates code
        /// </summary>
        /// <param name="compilation">Compilation</param>
        /// <param name="typeDeclarations">Marked classes</param>
        /// <param name="context">Source production context</param>
        static void Execute(Compilation compilation, ImmutableArray<BaseTypeDeclarationSyntax> typeDeclarations, SourceProductionContext context)
        {
            if (typeDeclarations.IsDefaultOrEmpty)
            {
                // nothing to do
                return;
            }

            IEnumerable<BaseTypeDeclarationSyntax> distinctDeclarations = typeDeclarations.Distinct();

            // Convert each ClassDeclarationSyntax to a GenerationData
            List<GenerationData> classesToGenerate = GetTypesToGenerate(compilation, distinctDeclarations, context.CancellationToken);

            // If there were errors in the ClassDeclarationSyntax, we won't create a
            // GenerationData for it, so make sure we have something to generate
            if (classesToGenerate.Count > 0)
            {
                // generate the source code and add it to the output
                string result = SourceGenerationHelper.GenerateExtensionClass(classesToGenerate);
                context.AddSource("FactsGenerator.g.cs", SourceText.From(result, Encoding.UTF8));
            }
        }

        /// <summary>
        /// Get marked classes
        /// </summary>
        /// <param name="context">Initialization context</param>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Add the marker attribute to the compilation
            // Get classes (structs, records?) with one ore more attribute
            // Check which of them have our attribute

            static bool IsSyntaxTargetForGeneration(SyntaxNode node)
                => node is BaseTypeDeclarationSyntax m && IsAllowedKind(m.Kind()) && m.AttributeLists.Count > 0;

            static BaseTypeDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
            {
                // we know the node is a BaseTypeDeclarationSyntax thanks to IsSyntaxTargetForGeneration
                var baseDeclarationSyntax = (BaseTypeDeclarationSyntax)context.Node;

                // loop through all the attributes on the method
                foreach (AttributeListSyntax attributeListSyntax in baseDeclarationSyntax.AttributeLists)
                {
                    foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                    {
                        if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                        {
                            // weird, we couldn't get the symbol, ignore it
                            continue;
                        }

                        INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                        string fullName = attributeContainingTypeSymbol.ToDisplayString();

                        // Is the attribute the [Fact] attribute?
                        if (fullName == "LOGQ.FactAttribute")
                        {
                            // return the class
                            return baseDeclarationSyntax;
                        }
                    }
                }

                // we didn't find the attribute we were looking for
                return null;
            }

            IncrementalValuesProvider<BaseTypeDeclarationSyntax> classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),             // select classes with attributes
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))      // select the class with the [EnumExtensions] attribute
            .Where(static m => m is not null)!; // filter out attributed enums that we don't care about

            // Add selected classes
            IncrementalValueProvider<(Compilation, ImmutableArray<BaseTypeDeclarationSyntax>)> compilationAndClasses
            = context.CompilationProvider.Combine(classDeclarations.Collect());

            // Go in compile
            context.RegisterSourceOutput(compilationAndClasses,
            static (spc, source) => Execute(source.Item1, source.Item2, spc));
        }
    }
}
