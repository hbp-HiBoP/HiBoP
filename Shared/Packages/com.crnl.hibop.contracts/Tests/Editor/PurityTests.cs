using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace CRNL.HiBoP.Contracts.Tests
{
    public class PurityTests
    {
        [Test]
        public void RuntimeAssemblyHasNoUnityDesktopIoUiNativeOrSerializerDependency()
        {
            Assembly assembly = typeof(ContractId).Assembly;
            string[] forbiddenPrefixes =
            {
                "Unity",
                "HBP.",
                "CRNL.HiBoP.RenderModel",
                "CRNL.HiBoP.Protocol",
                "Newtonsoft",
                "System.IO",
            };

            string[] forbidden = assembly.GetReferencedAssemblies().Select(reference => reference.Name).Where(name => forbiddenPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal))).ToArray();

            Assert.That(forbidden, Is.Empty);
        }

        [Test]
        public void PublicContractClassesAreSealedAndPropertiesHaveNoPublicSetter()
        {
            Type[] publicTypes = typeof(ContractId).Assembly.GetExportedTypes();

            Type[] extensibleClasses = publicTypes.Where(type => type.IsClass && !type.IsAbstract && !type.IsSealed).ToArray();
            PropertyInfo[] mutableProperties = publicTypes.SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public)).Where(property => property.SetMethod != null && property.SetMethod.IsPublic).ToArray();

            Assert.That(extensibleClasses, Is.Empty);
            Assert.That(mutableProperties, Is.Empty);
        }

        [Test]
        public void ToStringDoesNotExposeLogicalPayloadValues()
        {
            ContractValue scalar = ContractValue.FromNumber(123456.789);
            StateProperty property = new(V1PropertyKeys.TimelineLogicalTime, scalar);

            Assert.That(scalar.ToString(), Does.Not.Contain("123456"));
            Assert.That(property.ToString(), Does.Not.Contain("123456"));
        }
    }
}
