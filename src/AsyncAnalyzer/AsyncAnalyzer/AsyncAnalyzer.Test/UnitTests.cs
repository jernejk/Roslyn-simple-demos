using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using AsyncAnalyzer;

namespace AsyncAnalyzer.Test
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
            public async Task Test()
            {
            }

            public async void Test2()
            {
            }

            public void Test3()
            {
            }

            public async Task Test4Async()
            {
            }

            public async Task<bool> Something()
            {
                return false;
            }
        }
    }";
            var expected1 = new DiagnosticResult
            {
                Id = AsyncAnalyzerAnalyzer.DiagnosticId,
                Message = String.Format("Async method name '{0}' does not end with Async", "Test"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] 
                    {
                        new DiagnosticResultLocation("Test0.cs", 13, 31),
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
                        new DiagnosticResultLocation("Test0.cs", 29, 37),
                    }
            };

            VerifyCSharpDiagnostic(test, expected1, expected2);

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
            public async Task TestAsync()
            {
            }

            public async void Test2()
            {
            }

            public void Test3()
            {
            }

            public async Task Test4Async()
            {
            }

            public async Task<bool> SomethingAsync()
            {
                return false;
            }
        }
    }";
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