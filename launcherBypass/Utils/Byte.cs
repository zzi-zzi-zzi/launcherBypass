using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace launcherBypass.Utils
{
    class Byte
    {
        internal static byte[] GetBytes(string str)
        {
            //byte[] bytes = new byte[str.Length * sizeof(char)];
            //System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return Encoding.ASCII.GetBytes(str);
        }

        internal static byte[] GetWideBytes(string str)
        {
            return Encoding.Unicode.GetBytes(str);
        }

        /// <summary>
        /// encode an xord string to byte array
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        internal static byte[] WriteString(string s)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter br = new BinaryWriter(ms))
                {
                    br.Write((int)s.Length);
                    br.Write(Crypto.performXoring(Encoding.Unicode.GetBytes(s)));
                }
                return ms.ToArray();
            }
        }
    }
}
