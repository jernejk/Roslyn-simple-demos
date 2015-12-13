using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;

namespace AsyncAnalyzer.Test
{
    [TestClass]
    public class JkAsyncTests : CodeFixVerifier
    {
        //No diagnostics expected to show up
        [TestMethod]
        public void EmptyFileTest()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void MethodNamesTest()
        {
            string test = System.IO.File.ReadAllText(@"TestCodes\AsyncTestCode_1.cs");
            var expected1 = new DiagnosticResult
            {
                Id = AsyncAnalyzerAnalyzer.DiagnosticId,
                Message = String.Format("Async method name '{0}' does not end with Async", "Test"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 12, 27),
                    }
            };

            var expected2 = new DiagnosticResult
            {
                Id = AsyncAnalyzerAnalyzer.DiagnosticId,
                Message = String.Format("Async method name '{0}' does not end with Async", "Something"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 28, 33),
                    }
            };

            VerifyCSharpDiagnostic(test, expected1, expected2);

            string fixtest = System.IO.File.ReadAllText(@"TestCodes\AsyncTestCode_1_fix.cs");
            VerifyCSharpFix(test, fixtest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new AsyncAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new AsyncAnalyzerAnalyzer();
        }
    }
}