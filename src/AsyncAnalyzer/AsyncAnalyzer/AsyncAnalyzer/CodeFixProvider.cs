using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace AsyncAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AsyncAnalyzerCodeFixProvider)), Shared]
    public class AsyncAnalyzerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(AsyncAnalyzerAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();

            // NOTE: Needs getInnermostNodeForTie because top most token can be argument or similar.
            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) as MethodDeclarationSyntax;

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create("Add Async", c => AddAsyncToMethodName(context.Document, node, c)),
                diagnostic);
        }

        private async Task<Solution> AddAsyncToMethodName(Document document, MethodDeclarationSyntax node, CancellationToken cancellationToken)
        {
            // Compute new uppercase name.
            var newName = node.Identifier.Text + "Async";

            // Get the symbol representing the type to be renamed.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            
            var list = semanticModel.LookupSymbols(node.Identifier.Span.Start).Where(l => l is IMethodSymbol).ToList();
            var methodSymbol = list.FirstOrDefault(l => l is IMethodSymbol && l.Locations[0].SourceSpan.Start == node.Identifier.SpanStart);

            // Produce a new solution that has all references to that method renamed, including the declaration.
            var originalSolution = document.Project.Solution;
            var optionSet = originalSolution.Workspace.Options;
            var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, methodSymbol, newName, optionSet, cancellationToken).ConfigureAwait(false);

            // Return the new solution with the now-uppercase type name.
            return newSolution;
        }
    }
}