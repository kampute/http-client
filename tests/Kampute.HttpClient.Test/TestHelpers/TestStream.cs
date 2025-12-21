namespace Kampute.HttpClient.Test.TestHelpers
{
    using System;
    using System.IO;

    internal class TestStream : MemoryStream
    {
        private readonly bool seekable;

        public TestStream(bool seekable) : base() => this.seekable = seekable;

        public override bool CanSeek => seekable;

        public override long Seek(long offset, SeekOrigin loc) => seekable
            ? base.Seek(offset, loc)
            : throw new NotSupportedException();
    }
}
