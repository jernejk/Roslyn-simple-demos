using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using EmtpyStringAnalyzer;

namespace EmtpyStringAnalyzer.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {

        //No diagnostics expected to show up
        [TestMethod]
        public void TestMethod1()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void TestMethod2()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public readonly string ReadOnlyEmptyString = """";
            public const string ConstEmptyString = """";
            
            public string Test(string text = """")
            {
                string test = """";
                
                Console.WriteLine("""");

                switch("""")
                {
                    case """":
                        break;
                }
                
                return """";
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = EmtpyStringAnalyzerAnalyzer.DiagnosticId,
                Message = "This is not correct empty string!",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 13, 58)
                        }
            };

            var expected1 = new DiagnosticResult
            {
                Id = EmtpyStringAnalyzerAnalyzer.DiagnosticId,
                Message = "This is not correct empty string!",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 18, 31)
                        }
            };

            var expected2 = new DiagnosticResult
            {
                Id = EmtpyStringAnalyzerAnalyzer.DiagnosticId,
                Message = "This is not correct empty string!",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 20, 35)
                        }
            };

            var expected3 = new DiagnosticResult
            {
                Id = EmtpyStringAnalyzerAnalyzer.DiagnosticId,
                Message = "This is not correct empty string!",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 22, 24)
                        }
            };

            var expected4 = new DiagnosticResult
            {
                Id = EmtpyStringAnalyzerAnalyzer.DiagnosticId,
                Message = "This is not correct empty string!",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 28, 24)
                        }
            };

            VerifyCSharpDiagnostic(test, expected, expected1, expected2, expected3, expected4);

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public readonly string ReadOnlyEmptyString = string.Empty;
            public const string ConstEmptyString = """";
            
            public string Test(string text = """")
            {
                string test = string.Empty;
                
                Console.WriteLine(string.Empty);

                switch(string.Empty)
                {
                    case """":
                        break;
                }
                
                return string.Empty;
            }
        }
    }";
            VerifyCSharpFix(test, fixtest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new EmtpyStringAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new EmtpyStringAnalyzerAnalyzer();
        }
    }
}