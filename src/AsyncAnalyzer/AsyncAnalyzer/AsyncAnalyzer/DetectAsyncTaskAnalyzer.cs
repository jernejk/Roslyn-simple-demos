using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace AsyncAnalyzer
{
    /// <summary>
    /// NOTE: This analyzer is for a friend who needs to find all non generic async Task methods.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DetectAsyncTaskAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "JK_ASYNC_T001";
        
        private static readonly LocalizableString Title = "Non-generic async Task method";
        private static readonly LocalizableString MessageFormat = "Async method '{0}' has return type Task.";
        private static readonly LocalizableString Description = "Method format is async Task ...(...).";
        private const string Category = "Naming";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var methodSymbol = (IMethodSymbol)context.Symbol;

            // Find just those methods which are async, don't return void and don't end with Async.
            if (methodSymbol.IsAsync && !methodSymbol.ReturnsVoid && methodSymbol.ReturnType.MetadataName == "Task")
            {
                // For all such symbols, produce a diagnostic.
                var diagnostic = Diagnostic.Create(Rule, methodSymbol.Locations[0], methodSymbol.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
