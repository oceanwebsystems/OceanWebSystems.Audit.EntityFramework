using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OceanWebSystems.Audit.EntityFramework
{
    internal static class DiagnosticHelper
    {
        internal static Diagnostic CreateEmptyAuditEntityDiagnostic(ClassDeclarationSyntax syntax)
        {
            var className = syntax.Identifier.ToString();

            var descriptor = new DiagnosticDescriptor(
                id: "AU0001",
                title: "Empty audit entity",
                messageFormat: "The generated audit class for the entity '{0}' contains no auditable properties.",
                category: "Audit.EntityFramework",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            return Diagnostic.Create(descriptor, syntax.GetLocation(), className);
        }
    }
}
