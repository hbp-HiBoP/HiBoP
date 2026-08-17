using HBP.Core.Data;
using HBP.Core.Data.Container;
using HBP.Core.Errors;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HBP.Tests.Serialization
{
    public class DataInfoValidatorTests
    {
        [Test]
        public async Task ValidationDoesNotPublishBeforeMatchingGenerationIsApplied()
        {
            DataInfo dataInfo = CreateInvalidDataInfo();

            DataInfoValidationResult result = await new DataInfoValidator().ValidateAsync(new[] { dataInfo }, true, 2, CancellationToken.None, generation: 17);

            Assert.That(dataInfo.Errors, Is.Empty);
            Assert.That(dataInfo.Warnings, Is.Empty);
            Assert.That(result.HasIssues, Is.True);
            Assert.That(result.TryApply(16), Is.False);
            Assert.That(dataInfo.Errors, Is.Empty);

            Assert.That(result.TryApply(17), Is.True);
            Assert.That(dataInfo.Errors.Select(error => error.GetType()), Is.EquivalentTo(new[]
            {
                typeof(LabelEmptyError),
                typeof(RequiredFieldEmptyError)
            }));
        }

        [Test]
        public async Task ExplicitSnapshotDoesNotValidateDataOutsideItsScope()
        {
            DataInfo included = CreateInvalidDataInfo();
            DataInfo excluded = CreateInvalidDataInfo();

            DataInfoValidationResult result = await new DataInfoValidator().ValidateAsync(new[] { included }, true, 1, CancellationToken.None);
            Assert.That(result.TryApply(0), Is.True);

            Assert.That(included.Errors, Is.Not.Empty);
            Assert.That(excluded.Errors, Is.Empty);
        }

        [Test]
        public async Task ResultMatchesThePreviousSynchronousValidation()
        {
            DataInfo expected = CreateInvalidDataInfo();
            expected.CheckErrorsAndWarnings(true);
            DataInfo actual = CreateInvalidDataInfo();

            DataInfoValidationResult result = await new DataInfoValidator().ValidateAsync(new[] { actual }, true, 1, CancellationToken.None);
            result.TryApply(0);

            Assert.That(actual.Errors.Select(error => error.GetType()), Is.EqualTo(expected.Errors.Select(error => error.GetType())));
            Assert.That(actual.Warnings.Select(warning => warning.GetType()), Is.EqualTo(expected.Warnings.Select(warning => warning.GetType())));
        }

        [Test]
        public async Task PreCancelledValidationPublishesNothing()
        {
            DataInfo dataInfo = CreateInvalidDataInfo();
            using CancellationTokenSource cancellation = new();
            cancellation.Cancel();

            Exception exception = await CaptureExceptionAsync(async () => await new DataInfoValidator().ValidateAsync(new[] { dataInfo }, true, 1, cancellation.Token));

            Assert.That(exception, Is.TypeOf<OperationCanceledException>());
            Assert.That(dataInfo.Errors, Is.Empty);
            Assert.That(dataInfo.Warnings, Is.Empty);
        }

        private static DataInfo CreateInvalidDataInfo()
        {
            return new DataInfo
            {
                Name = string.Empty,
                DataContainer = new CSV()
            };
        }

        private static async Task<Exception> CaptureExceptionAsync(Func<Task> action)
        {
            try
            {
                await action();
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }
    }
}
