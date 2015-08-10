using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System.Threading.Tasks;
using System.Collections.Generic;

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

                string newSource = root.NormalizeWhitespace().SyntaxTree.GetText().ToString();
                Console.WriteLine(newSource);

                File.WriteAllText("source.cs", newSource.ToString());
            }
        }

        public static SyntaxNode MockClass(SyntaxTree tree, SemanticModel semanticModel, ClassDeclarationSyntax classNode)
        {
            SyntaxNode root = tree.GetRoot();
            Console.WriteLine("Class name: " + classNode.Identifier.Text);

            SyntaxList<MemberDeclarationSyntax> members = new SyntaxList<MemberDeclarationSyntax>();

            List<string> usedNames = new List<string>();

            foreach (var member in classNode.Members)
            {
                if (member is MethodDeclarationSyntax)
                {
                    var method = (MethodDeclarationSyntax)member;
                    Console.WriteLine("\tMethod name: " + method.Identifier.Text);

                    IMethodSymbol methodSemanticData = semanticModel.GetDeclaredSymbol(method) as IMethodSymbol;
                    members = members.Add(CreateMethodDelegate(method, methodSemanticData, usedNames));
                    members = members.Add(CreateMethodBody(method, methodSemanticData, usedNames));
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

        private static MethodDeclarationSyntax CreateMethodBody(MethodDeclarationSyntax method, IMethodSymbol methodSemanticData, List<string> usedNames)
        {
            StatementSyntax statement;

            string arguments = string.Empty;
            foreach (var parameter in methodSemanticData.Parameters)
            {
                if (!string.IsNullOrEmpty(arguments))
                {
                    arguments += ", ";
                }

                arguments += parameter.Name;
            }

            var delegateName = GetName(method.Identifier.Text + "Mock", usedNames);
            usedNames.Add(delegateName);

            if (methodSemanticData.ReturnsVoid && !methodSemanticData.IsAsync)
            {
                statement = SF.ParseStatement($"{delegateName}?.Invoke({arguments});");
            }
            else if (methodSemanticData.ReturnsVoid && methodSemanticData.IsAsync)
            {
                statement = SF.ParseStatement($"await {delegateName}?.Invoke({arguments});");
            }
            else if (!methodSemanticData.ReturnsVoid && !methodSemanticData.IsAsync)
            {
                statement = SF.ParseStatement($"return {delegateName}?.Invoke({arguments});");
            }
            else if (!methodSemanticData.ReturnsVoid && methodSemanticData.IsAsync)
            {
                statement = SF.ParseStatement($"return await {delegateName}?.Invoke({arguments});");
            }
            else
            {
                statement = GetThrowNotImplementedBlockSyntax();
            }

            return SF.MethodDeclaration(method.ReturnType, method.Identifier.Text)
                                    .AddModifiers(method.Modifiers.ToArray())
                                    .AddParameterListParameters(method.ParameterList.Parameters.ToArray())
                                    .AddBodyStatements(statement);
        }

        private static MemberDeclarationSyntax CreateMethodDelegate(MethodDeclarationSyntax method, IMethodSymbol methodSemanticData, List<string> usedNames)
        {
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
                returnType = method.ReturnType.ToString();
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
                    delegateType = SF.IdentifierName($"Func<{arguments}, {returnType}>");
                }
            }

            string methodName = GetName(method.Identifier.Text + "Mock", usedNames);
            var name = SF.Identifier(methodName);
            var variable = SF.VariableDeclarator(name);
            var variableDeclaration = SF.VariableDeclaration(delegateType).AddVariables(variable);

            FieldDeclarationSyntax eventSyntax = SF.FieldDeclaration(variableDeclaration);
            eventSyntax = eventSyntax.AddModifiers(SF.Token(SyntaxKind.PublicKeyword));

            return eventSyntax;
        }

        private static string GetName(string memberName, List<string> usedNames)
        {
            if (!usedNames.Contains(memberName))
            {
                return memberName;
            }

            int i = 2;
            while (true)
            {
                string newName = memberName + "_" + i;
                if (!usedNames.Contains(newName))
                {
                    return newName;
                }

                ++i;
            }
        }
    }
}
