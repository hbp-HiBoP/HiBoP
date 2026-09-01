using System.Reflection;
using NUnit.Framework;

namespace CRNL.HiBoP.Contracts.Tests
{
    public class PackageCompilationTests
    {
        [Test]
        public void RuntimeAssemblyIsAvailable()
        {
            Assert.That(Assembly.Load("CRNL.HiBoP.Contracts"), Is.Not.Null);
        }
    }
}
