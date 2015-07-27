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

using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace EmtpyStringAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EmtpyStringAnalyzerCodeFixProvider)), Shared]
    public class EmtpyStringAnalyzerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(EmtpyStringAnalyzerAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                // NOTE: Needs getInnermostNodeForTie because top most token can be argument or similar.
                SyntaxNode node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

                // Register a code action that will invoke the fix.
                context.RegisterCodeFix(
                    CodeAction.Create("Make string.Empty", c => ReplaceEmptyStringWithStringEmpty(context.Document, node, c)),
                    diagnostic);
            }
        }

        private async Task<Document> ReplaceEmptyStringWithStringEmpty(Document document, SyntaxNode oldNode, CancellationToken cancellationToken)
        {
            // NOTE: string.Empty
            var stringType = SF.PredefinedType(SF.Token(SyntaxKind.StringKeyword));
            var empty = SF.IdentifierName("Empty");
            var stringEmpty = SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, stringType, SF.Token(SyntaxKind.DotToken), empty);

            var tree = await document.GetSyntaxTreeAsync();
            var node = tree.GetRoot().ReplaceNode(oldNode, stringEmpty);

            return document.WithSyntaxRoot(node);
        }
    }
}