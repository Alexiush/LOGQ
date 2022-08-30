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

namespace LOGQ_Source_Generation
{
    public struct Property
    {
        public string PropertyName;
        public string PropertyType;

        public Property(string propertyName, string propertyType)
        {
            PropertyName = propertyName;
            PropertyType = propertyType;
        }
    }

    public class GenerationData
    {
        public readonly string OriginName;
        public readonly string Name;
        public readonly List<Property> Properties;

        public GenerationData(string originName, string name, List<Property> properties)
        {
            OriginName = originName;
            Name = name;
            Properties = properties;
        }
    }

    [Generator]
    public class FactsGenerator : IIncrementalGenerator
    {
        static List<GenerationData> GetTypesToGenerate(Compilation compilation, IEnumerable<ClassDeclarationSyntax> classes, CancellationToken ct)
        {
            // Create a list to hold output
            var classesToGenerate = new List<GenerationData>();
            // Get the semantic representation of marker attribute 
            INamedTypeSymbol? classAttribute = compilation.GetTypeByMetadataName("LOGQ.FactAttribute");

            if (classAttribute == null)
            {
                // If this is null, the compilation couldn't find the marker attribute type
                // which suggests there's something very wrong! Bail out..
                return classesToGenerate;
            }

            foreach (ClassDeclarationSyntax classDeclarationSyntax in classes)
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
                string className = null;

                // Loop through all of the attributes on the class until we find the [LOGQ.Fact] attribute
                foreach (AttributeData attributeData in classSymbol.GetAttributes())
                {
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
                                    case "extensionClassName":
                                        className = (string)typedConstant.Value;
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

                // Get all the fields from the class, and add their name to the list
                foreach (ISymbol member in classMembers)
                {
                    if (member.Kind == SymbolKind.Property && member.DeclaredAccessibility == Accessibility.Public)
                    {
                        properties.Add(new Property(member.Name, ((IPropertySymbol)member).Type.ToDisplayString()));
                    }
                }

                // Create a GenerationData for use in the generation phase
                classesToGenerate.Add(new GenerationData(classSymbol.ToDisplayString(), className, properties));
            }

            return classesToGenerate;
        }

        static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
        {
            if (classes.IsDefaultOrEmpty)
            {
                // nothing to do
                return;
            }

            IEnumerable<ClassDeclarationSyntax> distinctEnums = classes.Distinct();

            // Convert each ClassDeclarationSyntax to a GenerationData
            List<GenerationData> classesToGenerate = GetTypesToGenerate(compilation, distinctEnums, context.CancellationToken);

            // If there were errors in the ClassDeclarationSyntax, we won't create a
            // GenerationData for it, so make sure we have something to generate
            if (classesToGenerate.Count > 0)
            {
                // generate the source code and add it to the output
                string result = SourceGenerationHelper.GenerateExtensionClass(classesToGenerate);
                context.AddSource("FactsGenerator.g.cs", SourceText.From(result, Encoding.UTF8));
            }
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Add the marker attribute to the compilation
            // Get classes (structs, records?) with one ore more attribute
            // Check which of them have our attribute

            static bool IsSyntaxTargetForGeneration(SyntaxNode node)
                => node is ClassDeclarationSyntax m && m.AttributeLists.Count > 0;

            static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
            {
                // we know the node is a ClassDeclarationSyntax thanks to IsSyntaxTargetForGeneration
                var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

                // loop through all the attributes on the method
                foreach (AttributeListSyntax attributeListSyntax in classDeclarationSyntax.AttributeLists)
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
                            return classDeclarationSyntax;
                        }
                    }
                }

                // we didn't find the attribute we were looking for
                return null;
            }

            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),             // select classes with attributes
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))      // select the class with the [EnumExtensions] attribute
            .Where(static m => m is not null)!; // filter out attributed enums that we don't care about

            // Add selected classes
            IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses
            = context.CompilationProvider.Combine(classDeclarations.Collect());

            // Go in compile
            context.RegisterSourceOutput(compilationAndClasses,
            static (spc, source) => Execute(source.Item1, source.Item2, spc));
        }
    }
}
