using System.Collections;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace OceanWebSystems.Audit.EntityFramework
{
    [Generator]
    public class AuditEntityGenerator : IIncrementalGenerator
    {
        private const string AuditDbContextName = "Audit.EntityFramework.AuditDbContext";
        private const string AuditIncludeAttributeName = "Audit.EntityFramework.AuditIncludeAttribute";
        private const string AuditConfigurationAttributeName = "OceanWebSystems.Audit.EntityFramework.AuditConfigurationAttribute";

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
                "IAuditRecord.g.cs",
                SourceText.From(SourceGenerationHelper.Interface, Encoding.UTF8)));

            // Add the base class to the compilation.
            context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
                "AuditRecordBase.g.cs",
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

                    // Is the attribute the [AuditInclude] or [AuditConfiguration] attribute?
                    if (fullName == AuditIncludeAttributeName ||
                        fullName == AuditConfigurationAttributeName)
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

            string? tableNamePrefix = string.Empty;
            string? tableNameSuffix = string.Empty;

            ClassDeclarationSyntax? auditDbContextClass = null;
            List<ClassDeclarationSyntax> auditEntityClasses = new();

            // Separate the class types (AuditDbContext and entities).
            foreach (var classDeclaration in distinctClasses)
            {
                var model = compilation.GetSemanticModel(classDeclaration.SyntaxTree, true);
                var type = model.GetDeclaredSymbol(classDeclaration) as ITypeSymbol;

                if (type is null)
                {
                    continue;
                }

                var isAudit = InheritsFrom(type, AuditDbContextName);
                if (isAudit)
                {                    
                    auditDbContextClass = classDeclaration;
                }
                else
                {
                    auditEntityClasses.Add(classDeclaration);
                }
            }

            if (auditDbContextClass != null)
            {
                var model = compilation.GetSemanticModel(auditDbContextClass.SyntaxTree, true);

                // Grab the attribute parameters.
                INamedTypeSymbol? classSymbol = model.GetDeclaredSymbol(auditDbContextClass);
                if (classSymbol != null)
                {
                    foreach (AttributeData attributeData in classSymbol.GetAttributes())
                    {
                        var constructorArgs = attributeData.ConstructorArguments;
                        if (constructorArgs.Any())
                        {
                            // Make sure we don't have any errors.
                            foreach (TypedConstant arg in constructorArgs)
                            {
                                if (arg.Kind == TypedConstantKind.Error)
                                {
                                    // Have an error, so don't try and do any generation.
                                    return;
                                }
                            }

                            if (constructorArgs.Count() == 4)
                            {
                                if (!constructorArgs[0].IsNull)
                                {
                                    tableNamePrefix = constructorArgs[0].Value?.ToString();
                                }

                                if (!constructorArgs[1].IsNull)
                                {
                                    tableNameSuffix = constructorArgs[1].Value?.ToString();
                                }
                            }
                        }
                    }
                }
            }

            var dbContextProperties = new Hashtable();

            // Generate the audit classes.
            if (auditEntityClasses.Any())
            {
                dbContextProperties = GenerateAuditEntityClasses(compilation, auditEntityClasses, context, tableNamePrefix, tableNameSuffix);
            }

            // Generate the partial DbContext class.
            if (auditDbContextClass != null && dbContextProperties.Count > 0)
            {
                GenerateAuditDbContextClass(compilation, auditDbContextClass, context, dbContextProperties);
            }
        }

        private static void GenerateAuditDbContextClass(
            Compilation compilation,
            ClassDeclarationSyntax auditDbContextClass,
            SourceProductionContext context,
            Hashtable properties)
        {
            var classModel = compilation.GetSemanticModel(auditDbContextClass.SyntaxTree, true);
            var classType = classModel.GetDeclaredSymbol(auditDbContextClass) as ITypeSymbol;
            if (classType != null)
            {
                var code = SourceGenerationHelper.GenerateDbContextClass(classType, properties);
                context.AddSource($"{classType.Name}.g.cs", SourceText.From(code, Encoding.UTF8));
            }
        }

        private static Hashtable GenerateAuditEntityClasses(
            Compilation compilation,
            List<ClassDeclarationSyntax> auditEntityClasses,
            SourceProductionContext context,
            string tableNamePrefix,
            string tableNameSuffix)
        {
            var dbContextProperties = new Hashtable();

            foreach (var classDeclaration in auditEntityClasses)
            {
                var classProperties = new List<ISymbol>();
                var classModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree, true);
                var classType = classModel.GetDeclaredSymbol(classDeclaration) as ITypeSymbol;

                if (classType is null)
                {
                    continue;
                }

                // Get the properties of the class.
                var symbols = classModel.LookupSymbols(0, classType.ContainingNamespace, classType.Name);
                if (symbols != null && symbols.Any())
                {
                    foreach (var symbol in symbols)
                    {
                        var members = classType.GetMembers().Where(m => m.Kind == SymbolKind.Property && m.DeclaredAccessibility == Accessibility.Public);
                        classProperties.AddRange(members);
                    }
                }

                // Warn if we're about to create a class with no auditable properties.
                if (!classProperties.Any())
                {
                    context.ReportDiagnostic(DiagnosticHelper.CreateEmptyAuditEntityDiagnostic(classDeclaration));
                }

                dbContextProperties.Add(
                    GenerateAuditEntityClass(classType, classProperties, context, tableNamePrefix, tableNameSuffix),
                    classType.ContainingNamespace.ToString());
            }

            return dbContextProperties;
        }

        private static string GenerateAuditEntityClass(
            ITypeSymbol classType,
            List<ISymbol> classProperties,
            SourceProductionContext context,
            string tableNamePrefix,
            string tableNameSuffix)
        {            
            var code = SourceGenerationHelper.GenerateAuditClass(classType, classProperties, tableNamePrefix, tableNameSuffix);
            var auditEntityName = $"{tableNamePrefix}{classType.Name}{tableNameSuffix}";
            context.AddSource($"{auditEntityName}.g.cs", SourceText.From(code, Encoding.UTF8));
            return $"public virtual DbSet<{auditEntityName}> {auditEntityName}s {{ get; set; }}";
        }

        private static bool InheritsFrom(ITypeSymbol symbol, string expectedParentTypeName)
        {
            while (true)
            {
                if (symbol.ToString().Equals(expectedParentTypeName))
                {
                    return true;
                }

                if (symbol.BaseType != null)
                {
                    symbol = symbol.BaseType;
                    continue;
                }
                break;
            }

            return false;
        }
    }
}
