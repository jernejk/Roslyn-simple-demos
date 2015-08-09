using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;
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

                    members = members.Add(CreateMethodDelegate(method, semanticModel));

                    var exceptionBlockNode = GetThrowNotImplementedBlockSyntax();
                    members = members.Add(
                        SF.MethodDeclaration(method.ReturnType, method.Identifier.Text)
                                            .AddModifiers(method.Modifiers.ToArray())
                                            .AddParameterListParameters(method.ParameterList.Parameters.ToArray())
                                            .AddBodyStatements(exceptionBlockNode));
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
            return SF.ParseStatement("new NotImplementedException();");
        }

        private static MemberDeclarationSyntax CreateMethodDelegate(MethodDeclarationSyntax method, SemanticModel model)
        {
            IMethodSymbol methodSemanticData = model.GetDeclaredSymbol(method) as IMethodSymbol;

            string returnType;
            if (methodSemanticData.ReturnsVoid && !methodSemanticData.IsAsync)
            {
                returnType = "void";
            }
            else if (methodSemanticData.ReturnsVoid)
            {
                returnType = "Task";
            }
            else
            {
                var type = method.ReturnType;
                if (type is PredefinedTypeSyntax)
                {
                    var predefinedType = type as PredefinedTypeSyntax;
                    returnType = "predefinedType.Keyword.Text}>";
                }
                else
                {
                    var identifierType = type as IdentifierNameSyntax;
                    returnType = identifierType.Identifier.Text;
                }
            }

            IdentifierNameSyntax delegateType;
            if (!methodSemanticData.Parameters.Any())
            {
                if (returnType == "void")
                {
                    delegateType = SF.IdentifierName("Action");
                }
                else
                {
                    delegateType = SF.IdentifierName($"Func<{returnType}>");
                }
            }
            else
            {
                string arguments = string.Empty;
                foreach (var parameter in methodSemanticData.Parameters)
                {
                    if (!string.IsNullOrEmpty(arguments))
                    {
                        arguments += ", ";
                    }

                    arguments += parameter.Type.Name;
                }

                if (returnType == "void")
                {
                    delegateType = SF.IdentifierName($"Action<{arguments}>");
                }
                else
                {
                    delegateType = SF.IdentifierName($"Func<{returnType}, {arguments}>");
                }
            }

            var name = SF.Identifier("Mock" + method.Identifier.Text);
            var variable = SF.VariableDeclarator(name);
            var variableDeclaration = SF.VariableDeclaration(delegateType).AddVariables(variable);

            EventFieldDeclarationSyntax eventSyntax = SF.EventFieldDeclaration(variableDeclaration);
            eventSyntax = eventSyntax.AddModifiers(SF.Token(SyntaxKind.PublicKeyword));

            return eventSyntax;
        }
    }
}
