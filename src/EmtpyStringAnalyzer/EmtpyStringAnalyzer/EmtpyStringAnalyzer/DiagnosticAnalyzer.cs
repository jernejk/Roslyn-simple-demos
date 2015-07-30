using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EmtpyStringAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EmtpyStringAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "EmtpyStringAnalyzer";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxTreeAction(AnalyzeTree);
        }

        private static void AnalyzeTree(SyntaxTreeAnalysisContext context)
        {
            var tree = context.Tree;
            var emptyStrings = tree.GetRoot().DescendantTokens()
                .Where(x => x.RawKind == (int)SyntaxKind.StringLiteralToken
                       && string.IsNullOrEmpty(x.ValueText)).ToList();

            foreach (var s in emptyStrings)
            {
                // Skip if it is inside method parameter definition or as case switch or a attribute argument.
                if (s.Parent.Parent.Parent.IsKind(SyntaxKind.Parameter) ||
                    s.Parent.Parent.IsKind(SyntaxKind.CaseSwitchLabel) ||
                    s.Parent.Parent.IsKind(SyntaxKind.AttributeArgument))
                {
                    continue;
                }

                FieldDeclarationSyntax fieldSyntax = s.Parent.Parent.Parent.Parent.Parent as FieldDeclarationSyntax;
                if (fieldSyntax != null && fieldSyntax.DescendantTokens().Any(x => x.IsKind(SyntaxKind.ConstKeyword)))
                {
                    continue;
                }

                var line = s.SyntaxTree.GetLineSpan(s.FullSpan);
                var diagnostic = Diagnostic.Create(Rule, s.GetLocation());

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
