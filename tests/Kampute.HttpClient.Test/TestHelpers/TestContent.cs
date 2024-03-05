namespace Kampute.HttpClient.Test.TestHelpers
{
    using System.Net.Http;
    using System.Text;

    internal class TestContent : StringContent
    {
        public TestContent(object content)
            : base(content?.ToString() ?? string.Empty, Encoding.UTF8, Constants.TestMediaType)
        {
        }
    }
}
