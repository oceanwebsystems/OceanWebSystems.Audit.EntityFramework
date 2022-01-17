using System.Collections.Immutable;
using System.Runtime;
using Audit.EntityFramework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using VerifyXunit;

namespace OceanWebSystems.Audit.EntityFramework.Tests
{
    public static class TestHelper
    {
        public static Task Verify(string source)
        {
            // Parse the provided string into a C# syntax tree.
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

            // Create references for assemblies we require
            // We could add multiple references if required
            var references = GetMetadataReferences();

            // Create a Roslyn compilation for the syntax tree.
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName: "Tests",
                syntaxTrees: new[] { syntaxTree },
                references: references);

            // Create an instance of our EnumGenerator incremental source generator.
            var generator = new AuditEntityGenerator();

            // The GeneratorDriver is used to run our generator against a compilation.
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Run the source generator!
            driver = driver.RunGenerators(compilation);

            // Use verify to snapshot test the source generator output!
            return Verifier
                .Verify(driver)
                .UseDirectory("Snapshots");
        }

        private static IEnumerable<MetadataReference> GetMetadataReferences()
        {
            var dotNetAssemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
            if (!string.IsNullOrEmpty(dotNetAssemblyPath))
            {
                ImmutableArray<MetadataReference> references = ImmutableArray.Create<MetadataReference>(
                    // .NET assemblies are finicky and need to be loaded in a special way.
                    MetadataReference.CreateFromFile(Path.Combine(dotNetAssemblyPath, "mscorlib.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(dotNetAssemblyPath, "System.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(dotNetAssemblyPath, "System.Core.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(dotNetAssemblyPath, "System.Private.CoreLib.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(dotNetAssemblyPath, "System.Runtime.dll")),
                    MetadataReference.CreateFromFile(typeof(AuditIncludeAttribute).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location)
                );

                return references;
            }

            return Enumerable.Empty<MetadataReference>();
        }
    }
}
