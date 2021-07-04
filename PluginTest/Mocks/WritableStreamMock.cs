namespace PluginTest
{
    using System.IO;

    using PipeCommunication.Interfaces;

    public class WritableStreamMock : IStandardWritablePipe
    {
        private MemoryStream _stream;

        public WritableStreamMock()
        {
            this._stream = new MemoryStream();
        }

        public Stream GetStream()
        {

            return this._stream;
        }

        public void SetPositionBackToZero()
        {
            this._stream.Position = 0;
        }

        public byte[] GetStreamContent()
        {
            this.SetPositionBackToZero();
            return this._stream.ToArray();
        }

        public void Dispose()
        {
            _stream.Close();
            this._stream = null;
        }
    }
}