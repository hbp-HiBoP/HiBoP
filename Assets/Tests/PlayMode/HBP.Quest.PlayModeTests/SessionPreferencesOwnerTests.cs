using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataManager = HBP.Core.Data.DataManager;
using Site = HBP.Core.Data.Site;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Core.Preferences;
using HBP.Transfer.Transport;
using HBP.UI.Quest;
using NUnit.Framework;

namespace HBP.Tests.Quest
{
    public sealed class SessionPreferencesOwnerTests
    {
        [Test]
        public async Task SavesDuringOpeningAreCapturedOrderedAndAbandonedOnDispose()
        {
            string folder = Path.Combine(Path.GetTempPath(), "hibop-owner-test-" + Guid.NewGuid().ToString("N"));
            string originalPath = UserPreferences.PATH;
            var normalization = DataManager.DefaultNormalization;
            var averaging = DataManager.DefaultAveraging;
            var position = DataManager.DefaultPositionAveraging;
            bool correction = Site.SiteNameCorrection;
            DesktopSessionPreferences owner = null;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var opening = new TaskCompletionSource<SessionControlResponse>();
            using var cancel = timeout.Token.Register(() => opening.TrySetCanceled());
            var sent = new List<SessionControlRequest>();
            try
            {
                Directory.CreateDirectory(folder);
                UserPreferences.PATH = Path.Combine(folder, "Preferences.txt");
                var general = new GeneralPreferences(new ProjectPreferences("Before", folder, folder), new ThemePreferences(), new LocalizationPreferences(), new SystemPreferences(), new MiscPreferences());
                var preferences = new UserPreferences(general, new DataPreferences(), new VisualizationPreferences());
                owner = new DesktopSessionPreferences(Guid.NewGuid(), 1, preferences, (request, token) =>
                {
                    sent.Add(request);
                    return request.Kind == SessionControlKind.Open ? opening.Task : Task.FromResult(new SessionControlResponse(request.OperationId, SessionControlStatus.Applied));
                });
                Task start = owner.StartAsync();
                preferences.General.Project.DefaultName = "First save";
                preferences.Save();
                preferences.General.Project.DefaultName = "Second save";
                preferences.Save();
                Assert.That(sent.Count, Is.EqualTo(1));
                opening.SetResult(new SessionControlResponse(sent[0].OperationId, SessionControlStatus.Applied, body: Encoding.UTF8.GetBytes("{}")));
                await start;
                await owner.FlushPreferencesAsync();
                Assert.That(sent.Count, Is.EqualTo(3));
                Assert.That(sent[1].Revision, Is.EqualTo(1));
                Assert.That(sent[2].Revision, Is.EqualTo(2));
                Assert.That(Encoding.UTF8.GetString(sent[1].GetBody()), Does.Contain("First save"));
                Assert.That(Encoding.UTF8.GetString(sent[2].GetBody()), Does.Contain("Second save"));
                owner.Dispose();
                preferences.General.Project.DefaultName = "Offline save";
                preferences.Save();
                Assert.That(sent.Count, Is.EqualTo(3));
                using var next = new DesktopSessionPreferences(Guid.NewGuid(), 2, preferences, (request, token) =>
                {
                    sent.Add(request);
                    return Task.FromResult(new SessionControlResponse(request.OperationId, SessionControlStatus.Applied, body: Encoding.UTF8.GetBytes("{}")));
                });
                await next.StartAsync();
                await next.FlushPreferencesAsync();
                Assert.That(sent.Count, Is.EqualTo(4), "Opening another owner must not replay offline saves.");
                Assert.That(sent[3].Kind, Is.EqualTo(SessionControlKind.Open));
            }
            finally
            {
                owner?.Dispose();
                DataManager.DefaultNormalization = normalization;
                DataManager.DefaultAveraging = averaging;
                DataManager.DefaultPositionAveraging = position;
                Site.SiteNameCorrection = correction;
                UserPreferences.PATH = originalPath;
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
        }
    }
}
