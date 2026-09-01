using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace CRNL.HiBoP.RenderModel.Tests
{
    public class PurityTests
    {
        [Test]
        public void RuntimeAssemblyReferencesOnlyContractsAndBcl()
        {
            string[] forbiddenPrefixes = { "Unity", "HBP.", "CRNL.HiBoP.Protocol", "Newtonsoft", "System.IO" };
            string[] forbidden = typeof(SurfaceAsset).Assembly.GetReferencedAssemblies().Select(reference => reference.Name).Where(name => forbiddenPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal))).ToArray();

            Assert.That(forbidden, Is.Empty);
            Assert.That(typeof(SurfaceAsset).Assembly.GetReferencedAssemblies().Select(reference => reference.Name), Does.Contain("CRNL.HiBoP.Contracts"));
        }

        [Test]
        public void PublicClassesAreSealedAndExposeNoMutableArraysOrSetters()
        {
            Type[] publicTypes = typeof(SurfaceAsset).Assembly.GetExportedTypes();
            Type[] extensibleClasses = publicTypes.Where(type => type.IsClass && !type.IsAbstract && !type.IsSealed).ToArray();
            PropertyInfo[] setters = publicTypes.SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public)).Where(property => property.SetMethod != null && property.SetMethod.IsPublic).ToArray();
            PropertyInfo[] arrays = publicTypes.SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public)).Where(property => property.PropertyType.IsArray).ToArray();

            Assert.That(extensibleClasses, Is.Empty);
            Assert.That(setters, Is.Empty);
            Assert.That(arrays, Is.Empty);
        }
    }
}
