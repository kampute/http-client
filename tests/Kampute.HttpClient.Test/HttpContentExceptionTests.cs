namespace Kampute.HttpClient.Test
{
    using NUnit.Framework;
    using System;
    using System.Net.Http;
    using System.Text;

    [TestFixture]
    public class HttpContentExceptionTests
    {
        [Test]
        public void ToString_IncludesContentTypeAndExpectedObjectType()
        {
            using var content = new StringContent("A,B", Encoding.UTF8, MediaTypeNames.Text.Csv);
            var exception = new HttpContentException("Unreadable content")
            {
                Content = content,
                ObjectType = typeof(Uri),
            };

            var text = exception.ToString();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(text, Does.Contain(MediaTypeNames.Text.Csv));
                Assert.That(text, Does.Contain(nameof(Uri)));
            }
        }
    }
}
