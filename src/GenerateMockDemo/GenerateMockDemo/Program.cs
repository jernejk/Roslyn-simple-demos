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
                Console.WriteLine(root.NormalizeWhitespace().SyntaxTree.GetText());
            }
        }

        public static SyntaxNode MockClass(SyntaxTree tree, SemanticModel semanticModel, ClassDeclarationSyntax classNode)
        {
            SyntaxNode root = tree.GetRoot();
            Console.WriteLine("Class name: " + classNode.Identifier.Text);

            SyntaxList<MemberDeclarationSyntax> members = new SyntaxList<MemberDeclarationSyntax>();

            foreach (var member in classNode.Members)
            {
                if (member is MethodDeclarationSyntax)
                {
                    var method = (MethodDeclarationSyntax)member;
                    Console.WriteLine("\tMethod name: " + method.Identifier.Text);

                    var exceptionBlockNode = GetThrowNotImplementedBlockSyntax();
                    members = members.Add(SF.MethodDeclaration(method.ReturnType, method.Identifier.Text).AddBodyStatements(exceptionBlockNode));
                }
                else if (member is PropertyDeclarationSyntax)
                {
                    var property = (PropertyDeclarationSyntax)member;
                    Console.WriteLine("\tProperty name: " + property.Identifier.Text);

                    var propertyRoot = SF.PropertyDeclaration(property.Type, property.Identifier.Text);
                    members = members.Add(member);
                }
                else
                {
                    members = members.Add(member);
                }
            }
            
            SyntaxToken classIdentifier = SF.IdentifierName(classNode.Identifier.Text + "Mock").GetFirstToken();
            var classDeclaration = SF.ClassDeclaration(classNode.AttributeLists, classNode.Modifiers, classIdentifier, classNode.TypeParameterList, classNode.BaseList, classNode.ConstraintClauses, members);

            return SF.NamespaceDeclaration(SF.IdentifierName("TestNamespace")).AddUsings(SF.UsingDirective(SF.IdentifierName("System"))).AddMembers(classDeclaration);
        }

        private static StatementSyntax GetThrowNotImplementedBlockSyntax()
        {
            // Block -> { token -> ThrowStatement -> { token -> ObjectCreationExpression -> NewKeyword, IdentifierName -> { IdentifierToken, ArgumentList -> { ( token, ) token } }, ; token, } token, } token
            //var notImplemented = SF.ObjectCreationExpression(SF.IdentifierName("NotImplementedException"));
            //var throwStatement = SF.ThrowStatement(notImplemented);

            return SF.ParseStatement("new NotImplementedException();");
        }
    }
}
