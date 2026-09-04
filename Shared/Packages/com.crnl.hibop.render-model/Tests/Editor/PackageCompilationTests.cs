using System.Reflection;
using NUnit.Framework;

namespace CRNL.HiBoP.RenderModel.Tests
{
    public class PackageCompilationTests
    {
        [Test]
        public void RuntimeAssemblyIsAvailable()
        {
            Assert.That(Assembly.Load("CRNL.HiBoP.RenderModel"), Is.Not.Null);
        }
    }
}
