using System.Text;

namespace osuBancho.Helpers
{
    static class Utils
    {
        public static float CalcAccuracy(uint countMiss, uint count50, uint count100, uint count300)
        {
            return (float)((double)((count50 * 50)+(count100 * 100)+(count300 * 300)) / (double)((count50 + count100 + count300 + countMiss) * 300));
        }

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
