using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace OceanWebSystems.Audit.EntityFramework
{
    [Generator]
    public class AuditEntityGenerator : IIncrementalGenerator
    {
        private const string AuditIncludeAttribute = "Audit.EntityFramework.AuditIncludeAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Add the configuration options to the compilation.
            context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
                "AuditConfigurationOptions.g.cs",
                SourceText.From(SourceGenerationHelper.Options, Encoding.UTF8)));

            // Add the configuration attribute to the compilation.
            context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
                "AuditConfigurationAttribute.g.cs",
                SourceText.From(SourceGenerationHelper.Attribute, Encoding.UTF8)));

            // Add the interface to the compilation.
            context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
                "IAudit.g.cs",
                SourceText.From(SourceGenerationHelper.Interface, Encoding.UTF8)));

            // Add the base class to the compilation.
            context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
                "AuditBase.g.cs",
                SourceText.From(SourceGenerationHelper.BaseClass, Encoding.UTF8)));

            // Do a simple filter for classes.
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsSyntaxTargetForGeneration(s), // Select classes with attributes.
                    transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx)) // Select the class with the [AuditInclude] attribute.
                .Where(static m => m is not null)!; // Filter out attributed classes that we don't care about.

            // Combine the selected classes with the compilation.
            IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses
                = context.CompilationProvider.Combine(classDeclarations.Collect());

            // Generate the source using the compilation and classes.
            context.RegisterSourceOutput(compilationAndClasses,
                static (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
            => node is ClassDeclarationSyntax m && m.AttributeLists.Count > 0;

        private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            // We know the node is a ClassDeclarationSyntax thanks to IsSyntaxTargetForGeneration.
            var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

            // Loop through all the attributes on the class.
            foreach (AttributeListSyntax attributeListSyntax in classDeclarationSyntax.AttributeLists)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                    {
                        continue;
                    }

                    INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    string fullName = attributeContainingTypeSymbol.ToDisplayString();

                    // Is the attribute the [AuditInclude] attribute?
                    if (fullName == AuditIncludeAttribute)
                    {
                        // Return the class.
                        return classDeclarationSyntax;
                    }
                }
            }

            // We didn't find the attribute we were looking for.
            return null;
        }

        private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
        {
            if (classes.IsDefaultOrEmpty)
            {
                // Nothing to do yet.
                return;
            }

            // I'm not sure if this is actually necessary, but [LoggerMessage] does it, so seems like a good idea!
            IEnumerable<ClassDeclarationSyntax> distinctClasses = classes.Distinct();

            // Generate the audit classes.
            foreach (var classDeclaration in distinctClasses)
            {
                var allClassSymbols = new List<ISymbol>();
                var model = compilation.GetSemanticModel(classDeclaration.SyntaxTree, true);
                var type = model.GetDeclaredSymbol(classDeclaration) as ITypeSymbol;

                if (type is null)
                {
                    continue;
                }

                var symbols = model.LookupSymbols(0, type.ContainingNamespace, type.Name);
                if (symbols != null && symbols.Any())
                {
                    foreach (var symbol in symbols)
                    {
                        var members = type.GetMembers().Where(m => m.Kind == SymbolKind.Property && m.DeclaredAccessibility == Accessibility.Public);
                        allClassSymbols.AddRange(members);
                    }
                }

                // Warn if we're about to create a class with no auditable properties.
                if (!allClassSymbols.Any())
                {
                    context.ReportDiagnostic(DiagnosticHelper.CreateEmptyAuditEntityDiagnostic(classDeclaration));
                }

                // Generate the source code and add it to the output.
                var code = SourceGenerationHelper.GenerateAuditClass(type, allClassSymbols);
                context.AddSource($"{type.Name}Audit.g.cs", SourceText.From(code, Encoding.UTF8));
            }
        }
    }
}
