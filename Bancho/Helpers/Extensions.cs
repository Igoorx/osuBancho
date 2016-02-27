using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using osuBancho.Core;

namespace osuBancho.Helpers
{
    static class Extensions
    {
        public static string InsertHrefInUrls(this String input)
        {
            MatchCollection matches = Regex.Matches(input, @"(http|https)\:\/\/[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}(\/\S*)?");
            foreach (Match match in matches)
            {
                string value = match.Value;
                input = input.Replace(value, string.Format("<a href=\"{0}\">{1}</a>", value, value));
            }
            return input;
        }

        private static string Join(this IEnumerable<string> source)
        {
            var builder = new StringBuilder();

            foreach (var value in source)
            {
                builder.Append(value);
            }

            return builder.ToString();
        }

        public static void Write(this Stream stream, byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
        }

        public static byte[] Read(this Stream stream, int count)
        {
            byte[] result = new byte[count];
            stream.Read(result, 0, count);
            return result;
        }

        public static bool IsInEnd(this Stream stream)
        {
            return stream.Position == stream.Length;
        }

        public static byte[] ReadToEnd(this Stream stream)
        {
            byte[] bffer = new byte[1024];
            int totalCount = 0;

            while (true)
            {
                int currentCount = stream.Read(bffer, totalCount, bffer.Length - totalCount);
                if (currentCount == 0)
                    break;

                totalCount += currentCount;
                if (totalCount == bffer.Length)
                    Array.Resize(ref bffer, bffer.Length * 2);
            }

            Array.Resize(ref bffer, totalCount); //TODO: is this right? O_o
            return bffer;
        }

        /// <summary>
        /// Copies the contents between two <see cref="Stream"/> instances in an async fashion.
        /// </summary>
        /// <param name="source">The source stream to copy from.</param>
        /// <param name="destination">The destination stream to copy to.</param>
        /// <param name="onComplete">Delegate that should be invoked when the operation has completed. Will pass the source, destination and exception (if one was thrown) to the function. Can pass in <see langword="null" />.</param>
        public static void CopyTo(this Stream source, Stream destination, Action<Stream, Stream, Exception> onComplete)
        {
            var buffer =
                new byte[4096];

            Action<Exception> done = e =>
            {
                if (onComplete != null)
                {
                    onComplete.Invoke(source, destination, e);
                }
            };

            AsyncCallback rc = null;

            rc = readResult =>
            {
                try
                {
                    var read =
                        source.EndRead(readResult);

                    if (read <= 0)
                    {
                        done.Invoke(null);
                        return;
                    }

                    destination.BeginWrite(buffer, 0, read, writeResult =>
                    {
                        try
                        {
                            destination.EndWrite(writeResult);
                            source.BeginRead(buffer, 0, buffer.Length, rc, null);
                        }
                        catch (Exception ex)
                        {
                            done.Invoke(ex);
                        }

                    }, null);
                }
                catch (Exception ex)
                {
                    done.Invoke(ex);
                }
            };

            source.BeginRead(buffer, 0, buffer.Length, rc, null);
        }


        public static ushort ReadUInt16(this Stream stream)
        {
            return (ushort)(stream.ReadByte() | stream.ReadByte() << 8);
        }

        public static uint ReadUInt32(this Stream stream)
        {
            return (uint)(stream.ReadByte() | stream.ReadByte() << 8 | stream.ReadByte() << 16 | stream.ReadByte() << 24);
        }

        public static void WriteLoginResult(this Stream stream, LoginResult result)
        {
            SerializationWriter obw = new SerializationWriter(stream);
            obw.Write((short)5);
            obw.Write((byte)0);
            obw.Write(4);
            obw.Write(-(int)result);
        }
    }
}
