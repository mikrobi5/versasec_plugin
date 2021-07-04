namespace PipeCommunication.Extension
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    /// <summary>
    /// keeps extension methods to make life easier
    /// </summary>
    public static class PipeFile
    {
        /// <summary>
        /// Appends all bytes.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="bytes">The bytes.</param>
        public static void AppendAllBytes(string path, byte[] bytes)
        {
            //argument-checking here.
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            using var stream = new FileStream(path, FileMode.Append);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
