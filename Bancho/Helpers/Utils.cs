using System.Text;

namespace osuBancho.Helpers
{
    internal static class Utils
    {
        public static float CalcAccuracy(uint countMiss, uint count50, uint count100, uint count300)
        {
            return (float)((double)((count50 * 50)+(count100 * 100)+(count300 * 300)) / (double)((count50 + count100 + count300 + countMiss) * 300));
        }

        /// <summary>
        /// Return a MD5 hash of a string
        /// </summary>
        public static string HashString(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Return a string containing a printable representation of an byte array
        /// </summary>
        public static string ByteArrayRepr(byte[] bytes)
        {
            int pos = 0;
            StringBuilder result = new StringBuilder();
            result.Append("\"");
            while (pos < bytes.Length)
            {
                byte code = bytes[pos++];
                switch (code)
                {
                    case 13:
                        result.Append(@"\r");
                        continue;
                    case 10:
                        result.Append(@"\n");
                        continue;
                    case 9:
                        result.Append(@"\t");
                        continue;
                    default:
                        if (code <= 126 && code >= 32)
                        {
                            result.Append((char)code);
                            continue;
                        }
                        break;
                }
                result.Append($@"\x{code:X2}");
            }
            result.Append("\"");
            return result.ToString();
        }
    }
}
