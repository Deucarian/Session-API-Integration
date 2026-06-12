using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Deucarian.Session.APIBridge.Tests
{
    public sealed class SessionAuthProviderTests
    {
        private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 6, 5, 12, 0, 0, TimeSpan.Zero);

        [Test]
        public void AuthenticatedSessionReturnsAccessToken()
        {
            RunAsync(async () =>
            {
            var store = new InMemorySessionStore(
                new SessionData("current-token", "refresh-token", Now.AddMinutes(5)));
            var service = new SessionService(store, utcNowProvider: () => Now);
            await service.RestoreAsync();
            var provider = new SessionAuthProvider(service);

            string token = await provider.GetAccessTokenAsync(default(CancellationToken));

            Assert.AreEqual("current-token", token);
            });
        }

        [Test]
        public void UnauthenticatedSessionReturnsNull()
        {
            RunAsync(async () =>
            {
            var service = new SessionService(new InMemorySessionStore(), utcNowProvider: () => Now);
            var provider = new SessionAuthProvider(service);

            string token = await provider.GetAccessTokenAsync(default(CancellationToken));

            Assert.IsNull(token);
            });
        }

        [Test]
        public void ExpiringSessionTriggersRefreshWhenEnabled()
        {
            RunAsync(async () =>
            {
            var refreshService = new RecordingRefreshService(
                SessionResult.Success(new SessionData("refreshed-token", "refresh-token", Now.AddMinutes(30))));
            var store = new InMemorySessionStore(
                new SessionData("current-token", "refresh-token", Now.AddSeconds(30)));
            var service = new SessionService(
                store,
                refreshService,
                TimeSpan.FromMinutes(1),
                utcNowProvider: () => Now);
            await service.RestoreAsync();
            var provider = new SessionAuthProvider(service);

            string token = await provider.GetAccessTokenAsync(default(CancellationToken));

            Assert.AreEqual("refreshed-token", token);
            Assert.AreEqual(1, refreshService.CallCount);
            });
        }

        [Test]
        public void RefreshDisabledDoesNotRefresh()
        {
            RunAsync(async () =>
            {
            var refreshService = new RecordingRefreshService(
                SessionResult.Success(new SessionData("refreshed-token", "refresh-token", Now.AddMinutes(30))));
            var store = new InMemorySessionStore(
                new SessionData("current-token", "refresh-token", Now.AddSeconds(30)));
            var service = new SessionService(
                store,
                refreshService,
                TimeSpan.FromMinutes(1),
                utcNowProvider: () => Now);
            await service.RestoreAsync();
            var provider = new SessionAuthProvider(service, refreshIfExpiredOrExpiringSoon: false);

            string token = await provider.GetAccessTokenAsync(default(CancellationToken));

            Assert.AreEqual("current-token", token);
            Assert.AreEqual(0, refreshService.CallCount);
            });
        }

        [Test]
        public void ExpiredRefreshFailureReturnsNull()
        {
            RunAsync(async () =>
            {
            var refreshService = new RecordingRefreshService(
                SessionResult.Failed("refresh_failed", "Refresh failed."));
            var store = new InMemorySessionStore(
                new SessionData("expired-token", "refresh-token", Now.AddSeconds(-1)));
            var service = new SessionService(
                store,
                refreshService,
                TimeSpan.FromMinutes(1),
                utcNowProvider: () => Now);
            await service.RestoreAsync();
            var provider = new SessionAuthProvider(service);

            string token = await provider.GetAccessTokenAsync(default(CancellationToken));

            Assert.IsNull(token);
            Assert.AreEqual(1, refreshService.CallCount);
            });
        }

        private static void RunAsync(Func<Task> asyncTest)
        {
            asyncTest().GetAwaiter().GetResult();
        }

        private sealed class RecordingRefreshService : ISessionRefreshService
        {
            private readonly SessionResult result;

            public RecordingRefreshService(SessionResult result)
            {
                this.result = result;
            }

            public int CallCount { get; private set; }

            public Task<SessionResult> RefreshAsync(
                SessionData currentSession,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                CallCount++;
                return Task.FromResult(result);
            }
        }
    }
}
