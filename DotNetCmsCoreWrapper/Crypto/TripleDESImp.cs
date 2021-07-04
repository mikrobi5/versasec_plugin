using System.Security.Cryptography;
using System.Text;

namespace VSec.DotNet.CmsCore.Wrapper.Crypto
{
    /// <summary>
    /// 
    /// </summary>
    public class TripleDESImp
    {
        /// <summary>
        /// The encoder
        /// </summary>
        public static readonly Encoding Encoder = Encoding.UTF8;

        /// <summary>
        /// Triples the DES encrypt.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="plainText">The plain text.</param>
        /// <returns></returns>
        public static byte[] TripleDesEncrypt(string key, byte[] plainText)
        {
            var des = CreateDes(key);
            var ct = des.CreateEncryptor();
            //var input = Encoding.UTF8.GetBytes(plainText);
            var output = ct.TransformFinalBlock(plainText, 0, plainText.Length);
            //return Encoding.Default.GetString(output);
            return output;
        }

        /// <summary>
        /// Triples the DES decrypt.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="cypherText">The cypher text.</param>
        /// <returns></returns>
        public static byte[] TripleDesDecrypt(string key, byte[] cypherText)
        {
            var des = CreateDes(key);
            var ct = des.CreateDecryptor();
            //var input = Convert.FromBase64String(cypherText);
            var output = ct.TransformFinalBlock(cypherText, 0, 8);
            return output;
        }

        /// <summary>
        /// Creates the DES.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public static TripleDES CreateDes(string key)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            TripleDES des = new TripleDESCryptoServiceProvider();
            var desKey = md5.ComputeHash(Encoding.UTF8.GetBytes(key));
            des.Key = desKey;
            des.IV = new byte[des.BlockSize / 8];
            des.Padding = PaddingMode.PKCS7;
            des.Mode = CipherMode.ECB;
            return des;
        }
    }
}
