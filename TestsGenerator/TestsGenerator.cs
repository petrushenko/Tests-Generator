using System.Threading.Tasks;

namespace TestsGenerator
{
    public abstract class TestsGenerator
    {
        public abstract Task GenerateTests(string[] classFiles, string folderToSave, int maxFileToRead, int maxThreads, int maxFileToWrite);
    }
}
