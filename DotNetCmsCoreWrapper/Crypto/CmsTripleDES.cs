using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace VSec.DotNet.CmsCore.Wrapper.Crypto
{
    /// <summary>
    /// 
    /// </summary>
    public class CmsTripleDES
    {
        // define the triple des provider
        private TripleDESCryptoServiceProvider _tripleDesCryptoProvider = new TripleDESCryptoServiceProvider();

        // define the string handler
        private UTF8Encoding _utf8Encoding = new UTF8Encoding();

        // define the local property arrays
        private byte[] _keyBytes;
        private byte[] _ivBytes = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        /// <summary>
        /// Initializes a new instance of the <see cref="CmsTripleDES"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="iv">The iv.</param>
        public CmsTripleDES(byte[] key, byte[] iv = null)
        {
            _keyBytes = key;
            if (iv != null)
            {
                _ivBytes = iv;
            }
            DoSettings();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CmsTripleDES"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="iv">The iv.</param>
        public CmsTripleDES(string key, byte[] iv = null)
        {
            _keyBytes = StrToByteArray(key);
            if (iv != null)
            {
                _ivBytes = iv;
            }
            DoSettings();
        }

        /// <summary>
        /// Strings to byte array.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns></returns>
        public byte[] StrToByteArray(string str)
        {
            Dictionary<string, byte> hexindex = new Dictionary<string, byte>();
            for (int i = 0; i <= 255; i++)
                hexindex.Add(i.ToString("X2"), (byte)i);

            List<byte> hexres = new List<byte>();
            for (int i = 0; i < str.Length; i += 2)
                hexres.Add(hexindex[str.Substring(i, 2)]);

            return hexres.ToArray();
        }

        /// <summary>
        /// Encrypts the specified input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public byte[] Encrypt(byte[] input)
        {
            return Transform(input, _tripleDesCryptoProvider.CreateEncryptor(_keyBytes, _ivBytes));
        }

        /// <summary>
        /// Decrypts the specified input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public byte[] Decrypt(byte[] input)
        {
            return Transform(input, _tripleDesCryptoProvider.CreateDecryptor(_keyBytes, _ivBytes));
        }

        /// <summary>
        /// Encrypts the specified text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        public string Encrypt(string text)
        {
            byte[] input = _utf8Encoding.GetBytes(text);
            byte[] output = Transform(input, _tripleDesCryptoProvider.CreateEncryptor(_keyBytes, _ivBytes));
            return Convert.ToBase64String(output);
        }

        /// <summary>
        /// Decrypts the specified text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        public string Decrypt(string text)
        {
            byte[] input = Convert.FromBase64String(text);
            byte[] output = Transform(input, _tripleDesCryptoProvider.CreateDecryptor(_keyBytes, _ivBytes));
            return _utf8Encoding.GetString(output);
        }

        private void DoSettings()
        {
            _tripleDesCryptoProvider.BlockSize = 64;
            _tripleDesCryptoProvider.Mode = CipherMode.CBC;
            _tripleDesCryptoProvider.Padding = PaddingMode.None;
            _tripleDesCryptoProvider.KeySize = 192;
        }

        private byte[] Transform(byte[] input, ICryptoTransform CryptoTransform)
        {
            using (MemoryStream memStream = new MemoryStream())
            using (CryptoStream cryptStream = new CryptoStream(memStream, CryptoTransform, CryptoStreamMode.Write))
            {
                // transform the bytes as requested
                cryptStream.Write(input, 0, input.Length);
                cryptStream.FlushFinalBlock();
                // Read the memory stream and
                // convert it back into byte array
                memStream.Position = 0;
                byte[] result = memStream.ToArray();
                // close and release the streams
                memStream.Close();
                cryptStream.Close();
                // hand back the encrypted buffer
                return result;
            }
        }
    }
}
