﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading.Tasks.Dataflow;

namespace TestsGenerator
{
    public sealed class NUnitTestsGenerator : TestsGenerator
    {
        public override Task GenerateTests(string[] classFiles, string pathToSave, int maxFileToRead, int maxThreads, int maxFileToWrite)
        {
            var maxFilesToLoadTasks = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxFileToRead
            };

            var maxTasksExecutedTasks = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxThreads
            };

            var maxFilesToWriteTasks = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxFileToWrite
            };

            var getText = new TransformBlock<string, string>(async file =>
            {
                Console.WriteLine("Start reading {0} PID:{1}", file, Thread.CurrentThread.ManagedThreadId);
                var sb = new StringBuilder();
                using (var sourceStream = new FileStream(file,  
                    FileMode.Open, FileAccess.Read, FileShare.Read,  
                    bufferSize: 4096, useAsync: true))  
                {
                    byte[] buffer = new byte[0x1000];  
                    int numRead;  
                    while ((numRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) != 0)  
                    {  
                        string text = Encoding.UTF8.GetString(buffer, 0, numRead);  
                        sb.Append(text);  
                    }  
  
                    Console.WriteLine("Stop reading {0} PID:{1}", file, Thread.CurrentThread.ManagedThreadId);
                    return sb.ToString();  
                }
            }, maxFilesToLoadTasks);

            var getTests = new TransformBlock<string, string[]>(text =>
            {
                Console.WriteLine("Start generate tests for classes PID:{0}", Thread.CurrentThread.ManagedThreadId);
                var classes = GetClassesFromText(text);
                var result = new BlockingCollection<string>();
                
                Parallel.ForEach(classes, @class =>
                {
                    var classTest = GenerateClassTest(@class);
                    result.Add(classTest);
                });
                Console.WriteLine("Stop generate tests for classes PID:{0}", Thread.CurrentThread.ManagedThreadId);
                return result.ToArray();
            }, maxTasksExecutedTasks);

            var saveTests = new ActionBlock<string[]>(action: async tests =>
            {
                Console.WriteLine("Start Saving PID:{0}", Thread.CurrentThread.ManagedThreadId);
                //var i = 1;
                //var sync = new object();
                //Parallel.ForEach(tests, async test =>
                //{
                //    var originalName = GetTestFilename(test);
                //    var filename = Path.Combine(pathToSave, originalName+ ".cs");
                //    FileStream file;
                //    lock (sync)
                //    {
                //        while (File.Exists(filename))
                //        {

                //            var newName = originalName + (i++).ToString();
                //            filename = Path.Combine(pathToSave, newName + ".cs");
                //        }
                //        file = File.Create(filename);
                //    }

                //    using (var outputFile = new StreamWriter(file))
                //    {
                //        await outputFile.WriteAsync(test);
                //    }
                //    file.Close();
                //});
                var i = 1;
                foreach (var test in tests)
                {
                    var originalName = GetTestFilename(test);
                    var filename = Path.Combine(pathToSave, originalName + ".cs");
                    while (File.Exists(filename))
                    {

                        var newName = originalName + i++.ToString();
                        filename = Path.Combine(pathToSave, newName + ".cs");
                    }

                    using (var outputFile = new StreamWriter(filename))
                    {
                        await outputFile.WriteAsync(test);
                    } 
                }
                Console.WriteLine("Stop Saving PID:{0}", Thread.CurrentThread.ManagedThreadId);
            }, maxFilesToWriteTasks);

            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            getText.LinkTo(getTests, linkOptions);
            getTests.LinkTo(saveTests, linkOptions);
            foreach (var file in classFiles)
            {
                getText.Post(file);
            }
            getText.Complete();
            return getText.Completion;
        }

        private string GetTestFilename(string test)
        {
            var tree = CSharpSyntaxTree.ParseText(test);
            var root = tree.GetRoot();
            var @class = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
            return @class.Identifier.ValueText;
        }

        private ClassDeclarationSyntax[] GetClassesFromText(string fileContent)
        {
            var tree = CSharpSyntaxTree.ParseText(fileContent);
            var root = tree.GetRoot();

            return root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(@class => @class.Modifiers.Any(SyntaxKind.PublicKeyword)).ToArray();
        }

        private string GenerateClassTest(ClassDeclarationSyntax @class)
        {
            string sourceNamespace = null;
            if (@class.TryGetParentSyntax<NamespaceDeclarationSyntax>(out var namespaceDeclarationSyntax))
            {
                sourceNamespace = namespaceDeclarationSyntax.Name.ToString();
            }

            var methodBody = SyntaxFactory.ParseStatement("Assert.Fail(\"autogenerated\");");
            var methods = @class.DescendantNodes().OfType<MethodDeclarationSyntax>();
            var members = new List<MemberDeclarationSyntax>();
            foreach (var method in methods)
            {
                if (!method.Modifiers.Any(SyntaxKind.PublicKeyword)) continue;
                var methodName = method.Identifier.Text;

                var methodDeclaration = SyntaxFactory
                    .MethodDeclaration(SyntaxFactory.ParseTypeName("void"), methodName + "Test")
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithBody(SyntaxFactory.Block(methodBody))
                    .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Test"))
                    )));
                members.Add(methodDeclaration);
            }
            var className = @class.Identifier.Text;
            var classDeclaration = SyntaxFactory.ClassDeclaration(className+"Tests")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddMembers(members.ToArray())
                .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("TestFixture"))
                    )));

            var @namespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(sourceNamespace ?? "My" + ".Tests")).AddMembers(classDeclaration);
            var resultNode = SyntaxFactory.CompilationUnit().AddUsings(
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Collections.Generic")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Linq")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Text")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("NUnit.Framework"))
                );
            if (sourceNamespace != null)
            {
                resultNode =
                    resultNode.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(sourceNamespace)));
            }

            var code = resultNode.AddMembers(@namespace);

            return code.NormalizeWhitespace().ToFullString();
        }
    }
}