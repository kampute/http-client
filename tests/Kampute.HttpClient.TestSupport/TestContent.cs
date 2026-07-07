namespace Kampute.HttpClient.TestSupport
{
    using System.Net.Http;
    using System.Text;

    public class TestContent : StringContent
    {
        public TestContent(object content)
            : base(content?.ToString() ?? string.Empty, Encoding.UTF8, Constants.TestMediaType)
        {
        }
    }
}
