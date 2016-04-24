using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace launcherBypass.Utils
{
    class Crypto
    {
        /// <summary>
        /// decrypts our data.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        internal static byte[] Decrypt(byte[] buffer, AesManaged _aes)
        {
            using (ICryptoTransform decryptor = _aes.CreateDecryptor(_aes.Key, _aes.IV))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(buffer, 0, buffer.Length);
                    }
                    return Ionic.Zlib.ZlibStream.UncompressBuffer(ms.ToArray());
                }
            }// decryptor
        }

        /// <summary>
        /// encrypts our data. (nees to be tested)
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        internal static byte[] Encrypt(byte[] buffer, AesManaged _aes, out int compressedSize)
        {
            buffer = Ionic.Zlib.ZlibStream.CompressBuffer(buffer);
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
