using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace osuBancho.Core.Helpers
{
    internal class AES
    {
        public static string AESDecrypt(string message, string IVString)
        {
            byte[] Key = ASCIIEncoding.UTF8.GetBytes("osu!-scoreburgr---------20160227");
            byte[] IV = Convert.FromBase64String(IVString);

            string decrypted = null;
            string plaintext = null;

            // Create an RijndaelManaged object 
            // with the specified key and IV. 
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = Key;
                rijAlg.BlockSize = 256;
                rijAlg.Mode = CipherMode.CBC;
                rijAlg.IV = IV;
                rijAlg.Padding = PaddingMode.None;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for decryption. 
                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(message)))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream 
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }

            return plaintext;
        }
        public static string _AESDecrypt(string message, string IVString)
        {
            byte[] bytes = Encoding.UTF8.GetBytes("osu!-scoreburgr---------20160227");
            string str;
            using (RijndaelManaged rijndaelManaged = new RijndaelManaged())
            {
                rijndaelManaged.Key = bytes;
                rijndaelManaged.BlockSize = 256;
                rijndaelManaged.Mode = CipherMode.CBC;
                rijndaelManaged.Padding = PaddingMode.None;
                
                if (IVString != null)
                    rijndaelManaged.IV = Convert.FromBase64String(IVString);
                else
                {
                    rijndaelManaged.GenerateIV();
                    IVString = Convert.ToBase64String(rijndaelManaged.IV);
                }
                try
                {
                    MemoryStream memoryStream = new MemoryStream(Convert.FromBase64String(message));
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, rijndaelManaged.CreateDecryptor(bytes, rijndaelManaged.IV), CryptoStreamMode.Read))
                    {
                        using (StreamReader streamWriter = new StreamReader(cryptoStream))
                        {
                            str = streamWriter.ReadToEnd();
                            streamWriter.Close();
                        }
                        cryptoStream.Close();
                    }
                    memoryStream.Close();
                }
                finally
                {
                    rijndaelManaged.Clear();
                }
            }
            return str;
        }
        public static string AesEncrypt(string toEncode, string key, string iv)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(key);
            string str;
            using (RijndaelManaged rijndaelManaged = new RijndaelManaged())
            {
                rijndaelManaged.Key = bytes;
                rijndaelManaged.BlockSize = 256;
                rijndaelManaged.Mode = CipherMode.CBC;
                if (iv != null)
                    rijndaelManaged.IV = Convert.FromBase64String(iv);
                else
                {
                    rijndaelManaged.GenerateIV();
                    iv = Convert.ToBase64String(rijndaelManaged.IV);
                }
                try
                {
                    MemoryStream memoryStream = new MemoryStream();
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, rijndaelManaged.CreateEncryptor(bytes, rijndaelManaged.IV), CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                        {
                            streamWriter.Write(toEncode);
                            streamWriter.Close();
                        }
                        cryptoStream.Close();
                    }
                    str = Convert.ToBase64String(memoryStream.ToArray());
                    memoryStream.Close();
                }
                finally
                {
                    rijndaelManaged.Clear();
                }
            }
            return str;
        }
    }
}
