namespace Kampute.HttpClient.Test
{
    using Kampute.HttpClient.Utilities;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;

    [TestFixture]
    public class ArgumentValidationTests
    {
        private static readonly KeyValuePair<string, string>[] FormPayload = [new("key", "value")];

        private static IEnumerable<TestCaseData> InvalidCalls()
        {
            yield return Case("SendAsync<T> with null method", c => c.SendAsync<string>(null!, "/resource"));
            yield return Case("SendAsync<T> with null uri", c => c.SendAsync<string>(HttpMethod.Get, null!));
            yield return Case("SendAsync with null method", c => c.SendAsync(null!, "/resource"));
            yield return Case("SendAsync with null uri", c => c.SendAsync(HttpMethod.Get, null!));
            yield return Case("HeadAsync with null uri", c => c.HeadAsync(null!));
            yield return Case("OptionsAsync with null uri", c => c.OptionsAsync(null!));
            yield return Case("GetAsync<T> with null uri", c => c.GetAsync<string>(null!));
            yield return Case("GetAsByteArrayAsync with null uri", c => c.GetAsByteArrayAsync(null!));
            yield return Case("GetAsStringAsync with null uri", c => c.GetAsStringAsync(null!));
            yield return Case("GetAsStreamAsync with null uri", c => c.GetAsStreamAsync(null!));
            yield return Case("GetToStreamAsync with null uri", c => c.GetToStreamAsync(null!, Stream.Null));
            yield return Case("GetToStreamAsync with null stream", c => c.GetToStreamAsync("/resource", null!));
            yield return Case("PostAsync with null uri", c => c.PostAsync(null!, null));
            yield return Case("PutAsync with null uri", c => c.PutAsync(null!, null));
            yield return Case("PatchAsync with null uri", c => c.PatchAsync(null!, null));
            yield return Case("DeleteAsync with null uri", c => c.DeleteAsync(null!));
            yield return Case("DownloadAsync with null method", c => c.DownloadAsync(null!, "/resource", null, _ => Stream.Null));
            yield return Case("DownloadAsync with null stream provider", c => c.DownloadAsync(HttpMethod.Get, "/resource", null, null!));
            yield return Case("SendAsFormAsync with null method", c => c.SendAsFormAsync(null!, "/resource", FormPayload));
            yield return Case("SendAsFormAsync<T> with null uri", c => c.SendAsFormAsync<string>(HttpMethod.Post, null!, FormPayload));
            yield return Case("PerformAsync with null action", c => c.WithScope().PerformAsync(null!));
            yield return Case("PerformAsync<T> with null function", c => c.WithScope().PerformAsync<string>(null!));

            static TestCaseData Case(string name, Func<HttpRestClient, Task> call) => new TestCaseData(call).SetName(name);
        }

        [TestCaseSource(nameof(InvalidCalls))]
        public void InvalidArgument_ThrowsBeforeReturningTask(Func<HttpRestClient, Task> call)
        {
            using var client = new HttpRestClient(new HttpClient(), disposeClient: true);

            Assert.Throws<ArgumentNullException>(() => call(client));
        }

        [Test]
        public void AsyncUpdateThrottle_TryUpdateAsync_WithNullUpdater_ThrowsBeforeReturningTask()
        {
            using var throttle = new AsyncUpdateThrottle<string>(null);

            Assert.Throws<ArgumentNullException>(() => throttle.TryUpdateAsync(null!));
        }
    }
}
