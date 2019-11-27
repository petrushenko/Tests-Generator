using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestsGenerator;

namespace TestsGeneratorTests
{
    [TestClass]
    public class NUnitTestsGeneratorTests
    {
        private readonly string _fileForTest = @"..\..\FilesForTests\MyClassFile.cs";
        private readonly string _folderForTests = @"..\..\TestsFolder\";
        [TestMethod]
        public void FileNameTest()
        {
            ClearFolder(_folderForTests);
            var testsGenerator = new NUnitTestsGenerator();
            var waiter = testsGenerator.GenerateTests(new []{ _fileForTest}, _folderForTests, 8,8,8);
            waiter.Wait();
            var testFiles = Directory.GetFiles(_folderForTests).Select(Path.GetFileName);
            bool contains = false;
            string cs = "BarClassTests.cs";
            var enumerable = testFiles as string[] ?? testFiles.ToArray();
            foreach (var file in enumerable)
            {
                if (Equals(file, cs))
                {
                    contains = true;
                    break;
                }
            }

            Assert.IsTrue(contains);
            Assert.IsTrue(enumerable.Contains("FooClassTests.cs"));
            Assert.AreEqual(2, enumerable.ToArray().Length);
        }

        [TestMethod]
        public void ClassNameTest()
        {
            ClearFolder(_folderForTests);
            var testsGenerator = new NUnitTestsGenerator();
            var waiter = testsGenerator.GenerateTests(new []{ _fileForTest}, _folderForTests, 8,8,8);
            waiter.Wait();
            var testFile = Directory.GetFiles(_folderForTests).First(file => Path.GetFileName(file) == "BarClassTests.cs");
            var text = GetText(testFile);
            var tree = CSharpSyntaxTree.ParseText(text);
            var root = tree.GetRoot();
            var @class = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
            Assert.AreEqual("BarClassTests", @class.Identifier.ValueText);
        }

        [TestMethod]
        public void MethodNameTest()
        {
            ClearFolder(_folderForTests);
            var testsGenerator = new NUnitTestsGenerator();
            var waiter = testsGenerator.GenerateTests(new []{ _fileForTest}, _folderForTests, 8,8,8);
            waiter.Wait();
            var testFile = Directory.GetFiles(_folderForTests).First(file => Path.GetFileName(file) == "BarClassTests.cs");
            var text = GetText(testFile);
            var tree = CSharpSyntaxTree.ParseText(text);
            var root = tree.GetRoot();
            var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            Assert.AreEqual("MyPublicMethodTest", method.Identifier.ValueText);
        }

        [TestMethod]
        public void SyntaxErrorTest()
        {
            ClearFolder(_folderForTests);
            var testsGenerator = new NUnitTestsGenerator();
            var waiter = testsGenerator.GenerateTests(new[] { _fileForTest }, _folderForTests, 8, 8, 8);
            waiter.Wait();
            var testFile = Directory.GetFiles(_folderForTests).First(file => Path.GetFileName(file) == "BarClassTests.cs");
            var text = GetText(testFile);
            var tree = CSharpSyntaxTree.ParseText(text);
            var diagnostics = tree.GetDiagnostics().FirstOrDefault(n => n.Severity == DiagnosticSeverity.Error);
            Assert.IsNull(diagnostics);
        }

        private static string GetText(string file)
        {
            var sb = new StringBuilder();
            using (var sourceStream = new FileStream(file,  
                FileMode.Open, FileAccess.Read, FileShare.Read,  
                bufferSize: 4096, useAsync: true))  
            {
                byte[] buffer = new byte[0x1000];  
                int numRead;  
                while ((numRead = sourceStream.Read(buffer, 0, buffer.Length)) != 0)  
                {  
                    string text = Encoding.UTF8.GetString(buffer, 0, numRead);  
                    sb.Append(text);  
                }  
                return sb.ToString();  
            }
        }

        private void ClearFolder(string folder)
        {
            var di = new DirectoryInfo(folder);

            foreach (var file in di.GetFiles())
            {
                file.Delete(); 
            }
            foreach (var dir in di.GetDirectories())
            {
                dir.Delete(true); 
            }
        }

    }
}
