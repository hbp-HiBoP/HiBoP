using System.Reflection;
using NUnit.Framework;

namespace CRNL.HiBoP.Protocol.Tests
{
    public class PackageCompilationTests
    {
        [Test]
        public void RuntimeAssemblyIsAvailable()
        {
            Assert.That(Assembly.Load("CRNL.HiBoP.Protocol"), Is.Not.Null);
        }
    }
}
