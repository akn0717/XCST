﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Xcst.Compiler.Tests.Language {

   static class LanguageTestsHelper {

      static readonly XcstCompilerFactory CompilerFactory = new XcstCompilerFactory {
         EnableExtensions = true,
         PackageTypeResolver = (typeName) => Assembly.GetExecutingAssembly()
            .GetType(typeName)
      };

      public static Tuple<Type, CompileResult> CompileFromFile(string fileName, bool correct) {

         using (var fileStream = File.OpenRead(fileName)) {

            XcstCompiler compiler = CompilerFactory.CreateCompiler();
            compiler.TargetNamespace = typeof(LanguageTestsHelper).Namespace + ".Runtime";
            compiler.TargetClass = "TestModule";
            compiler.UseLineDirective = true;
            compiler.UsePackageBase = new StackFrame(1, true).GetMethod().DeclaringType.Namespace;

            CompileResult xcstResult;

            try {
               xcstResult = compiler.Compile(fileStream, baseUri: new Uri(fileName, UriKind.Absolute));

            } catch (CompileException ex) {

               if (!correct) {
                  Console.WriteLine(ex.Message);
                  Console.WriteLine($"Module URI: {ex.ModuleUri}");
                  Console.WriteLine($"Line number: {ex.LineNumber}");
               }

               throw;
            }

            foreach (string unit in xcstResult.CompilationUnits) {
               Console.WriteLine(unit);
            }

            if (!correct) {
               return null;
            }

            SyntaxTree[] syntaxTrees = xcstResult.CompilationUnits
               .Select(c => CSharpSyntaxTree.ParseText(c))
               .ToArray();

            // TODO: Should compiler give list of assembly references?

            MetadataReference[] references = {
               MetadataReference.CreateFromFile(typeof(System.Object).Assembly.Location),
               MetadataReference.CreateFromFile(typeof(System.Uri).Assembly.Location),
               MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
               MetadataReference.CreateFromFile(typeof(System.Xml.XmlWriter).Assembly.Location),
               MetadataReference.CreateFromFile(typeof(Xcst.IXcstPackage).Assembly.Location),
               MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location)
            };

            CSharpCompilation compilation = CSharpCompilation.Create(
               Path.GetRandomFileName(),
               syntaxTrees: syntaxTrees,
               references: references,
               options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var assemblyStream = new MemoryStream()) {

               EmitResult csharpResult = compilation.Emit(assemblyStream);

               if (!csharpResult.Success) {

                  Diagnostic error = csharpResult.Diagnostics
                     .Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)
                     .FirstOrDefault();

                  throw new ArgumentException($"{error?.Id}: {error?.GetMessage() ?? "C# compilation failed."}", nameof(fileName));

               } else {

                  assemblyStream.Position = 0;

                  Assembly assembly = Assembly.Load(assemblyStream.ToArray());
                  Type type = assembly.GetType(compiler.TargetNamespace + "." + compiler.TargetClass);

                  return Tuple.Create(type, xcstResult);
               }
            }
         }
      }

      public static bool OutputEqualsToDoc(Type module, string fileName) {

         var expectedDoc = XDocument.Load(fileName, LoadOptions.PreserveWhitespace);
         var actualDoc = new XDocument();

         using (XmlWriter outputWriter = actualDoc.CreateWriter()) {

            XcstEvaluator.Using(module)
               .CallInitialTemplate()
               .OutputTo(outputWriter)
               .Run();
         }

         return XDocumentNormalizer.DeepEqualsWithNormalization(expectedDoc, actualDoc);
      }

      public static bool OutputEqualsToExpected(Type module) {

         var expectedDoc = new XDocument();
         var actualDoc = new XDocument();

         XcstEvaluator evaluator = XcstEvaluator.Using(module);

         using (XmlWriter actualWriter = actualDoc.CreateWriter()) {

            evaluator.CallInitialTemplate()
               .OutputTo(actualWriter)
               .Run();
         }

         using (XmlWriter expectedWriter = expectedDoc.CreateWriter()) {

            evaluator.CallTemplate("expected")
               .OutputTo(expectedWriter)
               .Run();
         }

         return XDocumentNormalizer.DeepEqualsWithNormalization(expectedDoc, actualDoc);
      }
   }
}
