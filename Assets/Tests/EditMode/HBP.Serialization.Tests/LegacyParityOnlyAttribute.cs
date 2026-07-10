using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace HBP.Tests.Serialization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
    internal sealed class LegacyParityOnlyAttribute : NUnitAttribute, IApplyToTest
    {
        public const string EnvironmentVariable = "HBP_RUN_LEGACY_PARITY";
        private const string SkipReason =
            "Legacy hbp_export parity is isolated from the production test run. " +
            "Set HBP_RUN_LEGACY_PARITY=1 and run the NativeParity category explicitly.";

        public void ApplyToTest(Test test)
        {
            if (Environment.GetEnvironmentVariable(EnvironmentVariable) == "1")
            {
                return;
            }

            test.RunState = RunState.Explicit;
            test.Properties.Set(PropertyNames.SkipReason, SkipReason);
        }
    }
}
