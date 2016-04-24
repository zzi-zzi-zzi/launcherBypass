using launcherBypass.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace launcherBypass.BnS
{
    class uosedalb
    {
        private byte[] _encryptionKey;

        private FileStream _file;
        private BinaryReader _reader;
        private string _fileName;

        private AesManaged _aes;

        private Dictionary<string, BSFile> _files = new Dictionary<string, BSFile>();

        /// <summary>
        /// read a uosedalb file
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="_encryptionKey"></param>
        public uosedalb(string fileName, byte[] _encryptionKey)
        {
            _fileName = fileName;
            _file = File.Open(fileName, FileMode.Open);
            _reader = new BinaryReader(_file);

            if(_reader.ReadUInt64() != 4777265066209922901)
            {
                throw new Exception("Failed to parse File. Not uosedalb file");
            }
            if(_reader.ReadUInt64() != 2)
            {
                throw new Exception("Failed to parse uosedalb file. version mismatch");
            }
            _aes = new AesManaged();
            _aes.Key = _encryptionKey;
            _aes.Padding = PaddingMode.Zeros;
            _aes.Mode = CipherMode.ECB;
            _aes.IV = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            readHeader();
        }
        
        /// <summary>
        /// Reads the compressed header
        /// </summary>
        private void readHeader()
        {
            _reader.BaseStream.Seek(89, SeekOrigin.Begin);
            int compressedSize = _reader.ReadInt32();
            int ucompressedSize = _reader.ReadInt32();
            int padding = (compressedSize % 16 != 0) ? 16 - (compressedSize % 16) : 0;

            byte[] buffer = _reader.ReadBytes(compressedSize + padding);
            byte[] data = Crypto.Decrypt(buffer, _aes);
            if (data.Length  != ucompressedSize)
            {
                throw new Exception("Decompression failed. Data.length != uncompressedSize");
            }
            int positionCheck = _reader.ReadInt32();
            if(positionCheck != _reader.BaseStream.Position)
            {
                throw new Exception("Decompression failed. positionCheck != _reader.BaseStream.Position");
            }

            using (MemoryStream ms = new MemoryStream(data))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    int size = Marshal.SizeOf(typeof(BSFileHeader)); //80
                    while (ms.Position < ms.Length)
                    {
                        var strSize = br.ReadInt32();
                        var fileName = Encoding.Unicode.GetString(br.ReadBytes(strSize * 2));

                        var _struct = br.ReadBytes(size);
                        GCHandle handle = GCHandle.Alloc(_struct, GCHandleType.Pinned);
                        BSFileHeader header = (BSFileHeader)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(BSFileHeader));
                        handle.Free();

                        BSFile bnsFile = new BSFile(fileName, _reader.ReadBytes(header.compressed), _aes, header);
                        _files.Add(fileName, bnsFile);
                    }
                }
            }
        }

        /// <summary>
        /// Replaces a setting in a config file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="settingName"></param>
        /// <param name="newValue">new value</param>
        internal void ReplaceSetting(string fileName, string settingName, string newValue)
        {
            if (!_files.ContainsKey(fileName))
                throw new FileNotFoundException("We don't have that config file");
            _files[fileName].updateXML(settingName, newValue);
        }

        /// <summary>
        /// Save the UOSEDALB file.
        /// </summary>
        internal void Save()
        {
            int offset = 0;
            int headerUncompressed = 0;
            
            byte[] headerBytes;
            BinaryWriter files = new BinaryWriter(new MemoryStream());
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter header = new BinaryWriter(ms))
                {
                    int fileHeaderSize = Marshal.SizeOf(typeof(BSFileHeader)); //80
                    foreach (var iterator in _files)
                    {
                        header.Write(iterator.Key.Length);
                        header.Write(Utils.Byte.GetWideBytes(iterator.Key));
                        //write the data
                        {
                            byte[] arr = new byte[fileHeaderSize];

                            IntPtr ptr = Marshal.AllocHGlobal(fileHeaderSize);
                            var head = iterator.Value.Header;
                            //update the offset values
                            head.offset = offset;
                            offset += head.compressed;

                            Marshal.StructureToPtr(head, ptr, true);
                            Marshal.Copy(ptr, arr, 0, fileHeaderSize);
                            Marshal.FreeHGlobal(ptr);
                            header.Write(arr);
                        }
                        files.Write(iterator.Value.Data);
                    }
                }
                var org = ms.ToArray();
                headerUncompressed = org.Length;
                int compressedSize;
                headerBytes = Crypto.Encrypt(org, _aes, out compressedSize);
            }

            files.Flush();
            _file.Close();
            using (BinaryWriter of = new BinaryWriter(File.Open(_fileName, FileMode.OpenOrCreate | FileMode.Truncate)))
            {
                of.Write((UInt64)4777265066209922901); //UOSELDALB
                of.Write((UInt64)2);//version
                of.Write((byte)0x00);
                of.Write(offset); //position check + sizeof(headerbytes.length) + sizeof(headerUncompressed) + headerbytes.length
                of.Write(_files.Count);
                of.Write((byte)0x01);
                of.Write((byte)0x01);
                of.Write(new byte[62]);
                of.Write((int)headerBytes.Length);
                of.Write(headerUncompressed);

                of.Write(headerBytes);
                of.Write((int)of.BaseStream.Position + 4); //position check

                var bs = files.BaseStream;//have to reset the stream
                bs.Seek(0, SeekOrigin.Begin);
                bs.CopyTo(of.BaseStream);
            }

            files.Close();
            
        }
    }
}
