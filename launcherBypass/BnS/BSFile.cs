using launcherBypass.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace launcherBypass.BnS
{
    class BSFile
    {
        private string fileName;
        private AesManaged _aes;
        private byte[] _data;
        private bool _decrypted = false;
        private BSFileHeader _header;

        private IFile _realfile;

        /// <summary>
        /// always get the most up to date header.
        /// </summary>
        internal BSFileHeader Header
        {
            get
            {
                if (_decrypted)
                    Encrypt();
                return _header;
            }
        }

        /// <summary>
        /// always get the most up to date data.
        /// </summary>
        internal byte[] Data
        {
            get
            {
                if (_decrypted)
                    Encrypt();
                return _data;
            }
        }

        public BSFile(string fileName, byte[] data, AesManaged aes, BSFileHeader header)
        {
            this.fileName = fileName;
            this._data = data;
            this._aes = aes;
            this._header = header;
        }

        /// <summary>
        /// modifies the data
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void updateXML(string key, string value)
        {
            if (!_decrypted)
                Decrypt();
            if (_realfile.GetType() == typeof(BNSXmlFile))
                ((BNSXmlFile)_realfile).updateXML(key, value);
            else
                throw new Exception("file is not an XML file");
        }

        private void Decrypt()
        {
            _decrypted = true;
            _data = Crypto.Decrypt(_data, _aes);

#if DEBUG
            try
            {
                using (FileStream fs = File.Open("test.dat", FileMode.OpenOrCreate))
                using (BinaryWriter bin = new BinaryWriter(fs))
                    bin.Write(_data);
            }
            catch { }
#endif
            //not sure but i think this number is the file type...
            //might have to just rely on file extensions
            if (_header.somenum == 2)
                {
                    _realfile = new BNSXmlFile(_aes);
                    _realfile.FromBytes(_data);
                }
        }

        /// <summary>
        /// Updates the data array and the header
        /// </summary>
        private void Encrypt()
        {
            _decrypted = false;
            _data = _realfile.ToArray();
#if DEBUG
            using (FileStream fs = File.Open("test.new.dat", FileMode.OpenOrCreate))
            using (BinaryWriter bin = new BinaryWriter(fs))
                bin.Write(_data);
#endif
            _header.uncompressed = _data.Length;
            int compressedSize;
            _data = Crypto.Encrypt(_data, _aes, out compressedSize);
            _header.compressedNoPadding = compressedSize;
            _header.compressed = _data.Length;

        }
    }
}
