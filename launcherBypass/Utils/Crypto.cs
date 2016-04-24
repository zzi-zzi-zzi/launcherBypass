using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace launcherBypass.Utils
{
    class Crypto
    {

        /// <summary>
        /// Creates a valid zlib stream that the game will take
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static byte[] ZlibCompress(byte[] data)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                // zlib header
                outStream.WriteByte(0x58);
                outStream.WriteByte(0x85);

                // zlib body
                using (var compressor = new DeflateStream(outStream, CompressionMode.Compress, true))
                    compressor.Write(data, 0, data.Length);

                // zlib checksum - a naive implementation of adler-32 checksum
                const uint A32Mod = 65521;
                uint s1 = 1, s2 = 0;
                foreach (byte b in data)
                {
                    s1 = (s1 + b) % A32Mod;
                    s2 = (s2 + s1) % A32Mod;
                }

                int adler32 = unchecked((int)((s2 << 16) + s1));
                outStream.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(adler32)), 0, sizeof(uint));

                // zlib compatible compressed query
                var bytes = outStream.ToArray();
                outStream.Close();

                return bytes;
            }
        }

        /// <summary>
        /// decrypts our data.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        internal static byte[] Decrypt(byte[] buffer, AesManaged _aes)
        {
            using (ICryptoTransform decryptor = _aes.CreateDecryptor(_aes.Key, _aes.IV))
            {
                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(buffer, 0, buffer.Length);
                    }
                    data = ms.ToArray();
                }
                using (MemoryStream ms = new MemoryStream(data))
                {
                    MemoryStream results = new MemoryStream();
                    ms.Seek(2, SeekOrigin.Begin);
                    using (DeflateStream z = new DeflateStream(ms, CompressionMode.Decompress))
                    {
                        z.CopyTo(results);
                    }
                    return results.ToArray();
                }
            }
        }

        /// <summary>
        /// encrypts our data. (nees to be tested)
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        internal static byte[] Encrypt(byte[] buffer, AesManaged _aes, out int compressedSize)
        {
            buffer = ZlibCompress(buffer);
            compressedSize =(int)buffer.Length; //because we want zlib not deflate
            using (ICryptoTransform encryptor = _aes.CreateEncryptor())
            {
                using (MemoryStream oms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(oms, encryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(buffer, 0, buffer.Length);
                        cs.FlushFinalBlock();
                    }
                    return oms.ToArray();
                }
            }// encryptor
        }

        /// <summary>
        /// Xors a byte array
        /// </summary>
        /// <param name="data"></param>
        /// <param name="size"></param>
        internal static byte[] performXoring(byte[] data)
        {
            byte[] xorArray = { 0xA4, 0x9F, 0xD8, 0xB3, 0xF6, 0x8E, 0x39, 0xC2, 0x2D, 0xE0, 0x61, 0x75, 0x5C, 0x4B, 0x1A, 0x07 };
            int length = data.Length;
            int lengthLeft = length;
            while (lengthLeft > 0)
            {
                for (int i = 0; i < lengthLeft && i < 16; i++)
                {
                    data[length - lengthLeft + i] =(byte) ( data[length - lengthLeft + i] ^ xorArray[i]);
                }
                lengthLeft = lengthLeft - 16;
            }

            return data;
        }
    }
}
