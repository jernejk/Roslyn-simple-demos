using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Diagnostics;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace GenerateMockDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            string code = File.ReadAllText("test.cs");

            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("testAssembly", new[] { tree });
            SemanticModel semanticModel = compilation.GetSemanticModel(tree, false);

            var classKeywords = tree.GetRoot().DescendantTokens().Where(x => x.IsKind(SyntaxKind.ClassKeyword)).ToList();

            foreach (SyntaxToken type in classKeywords)
            {
                SyntaxNode root = MockClass(tree, semanticModel, type.Parent as ClassDeclarationSyntax);

                Console.WriteLine("New source: ");
                Console.WriteLine(root.SyntaxTree.GetText());
            }
        }

        public static SyntaxNode MockClass(SyntaxTree tree, SemanticModel semanticModel, ClassDeclarationSyntax classNode)
        {
            SyntaxNode root = tree.GetRoot();
            Console.WriteLine("Class name: " + classNode.Identifier.Text);

            foreach (var stuff in classNode.Members)
            {
                if (stuff is MethodDeclarationSyntax)
                {
                    Console.WriteLine("\tMethod name: " + ((MethodDeclarationSyntax)stuff).Identifier.Text);

                    var exceptionBlockNode = GetThrowNotImplementedBlockSyntax();
                    root = root.ReplaceNode(((MethodDeclarationSyntax)stuff).Body, exceptionBlockNode);
                }
                else if (stuff is PropertyDeclarationSyntax)
                {
                    Console.WriteLine("\tProperty name: " + ((PropertyDeclarationSyntax)stuff).Identifier.Text);
                }
            }

            return root;
        }

        public static BlockSyntax GetThrowNotImplementedBlockSyntax()
        {
            // Block -> { token -> ThrowStatement -> { token -> ObjectCreationExpression -> NewKeyword, IdentifierName -> { IdentifierToken, ArgumentList -> { ( token, ) token } }, ; token, } token, } token
            //var notImplemented = SF.ObjectCreationExpression(SF.IdentifierName("NotImplementedException"));
            //var throwStatement = SF.ThrowStatement(notImplemented);

            var throwStatement = SF.ParseStatement("new NotImplementedException();");

            return SF.Block(throwStatement);
        }
    }
}
