using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace osuBancho.Helpers
{
    class AES
    {
        public static string DecryptStringFromBytes_Aes(string text, string key, ref string iv)
        {
            byte[] _newData;
            int Int = 0;
            byte[] bytes = Encoding.UTF8.GetBytes(key);
            using (RijndaelManaged rm = new RijndaelManaged())
            {
                rm.Key = bytes;
                rm.BlockSize = 256;
                rm.Mode = CipherMode.CBC;
                MemoryStream memory = new MemoryStream(Convert.FromBase64String(text));
                using (CryptoStream cryptoStream = new CryptoStream(memory, rm.CreateDecryptor(bytes, Encoding.UTF8.GetBytes(iv)), CryptoStreamMode.Read))
                {
                    _newData = new byte[cryptoStream.Length];
                    Int = cryptoStream.Read(_newData, 0, _newData.Length);
                }
                return Encoding.UTF8.GetString(_newData, 0, Int);
            }
        }
    }
}
