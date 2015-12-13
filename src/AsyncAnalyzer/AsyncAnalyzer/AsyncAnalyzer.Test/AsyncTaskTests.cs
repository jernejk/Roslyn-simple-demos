using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;

namespace AsyncAnalyzer.Test
{
    [TestClass]
    public class AsyncTaskTests : CodeFixVerifier
    {
        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void FindAsyncMethodsOnlyWithReturnTypeTaskTest()
        {
            string test = System.IO.File.ReadAllText(@"TestCodes\AsyncTestCode_1.cs");
            var expected1 = new DiagnosticResult
            {
                Id = DetectAsyncTaskAnalyzer.DiagnosticId,
                Message = string.Format("Async method '{0}' has return type Task.", "Test"),
                Severity = DiagnosticSeverity.Info,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 12, 27),
                    }
            };

            var expected2 = new DiagnosticResult
            {
                Id = DetectAsyncTaskAnalyzer.DiagnosticId,
                Message = string.Format("Async method '{0}' has return type Task.", "Test4Async"),
                Severity = DiagnosticSeverity.Info,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 24, 27),
                    }
            };

            VerifyCSharpDiagnostic(test, expected1, expected2);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new AsyncAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new DetectAsyncTaskAnalyzer();
        }
    }
}
