using launcherBypass.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace launcherBypass.BnS
{
    class BNSXmlFile : IFile
    {
        private AesManaged _aes;

        private string _fileName;
        private XMLNode _base;

        public BNSXmlFile(AesManaged _aes)
        {
            this._aes = _aes;
        }

        /// <summary>
        /// builds the xml data from the byte array.
        /// </summary>
        /// <param name="array"></param>
        public void FromBytes(byte[] array)
        {
            using (MemoryStream ms = new MemoryStream(array))
            using (BinaryReader br = new BinaryReader(ms))
            {
                if (br.ReadUInt64() != 4777284904613858636) //LMXBOSLB
                    throw new Exception("XML File Header mismatch");
                if(br.ReadUInt32() != 3)
                    throw new Exception("XML File Version mismatch");

                var length = br.ReadUInt32();
                br.BaseStream.Seek(64, SeekOrigin.Current);
                if(br.ReadByte() != 0x01)
                {
                    throw new Exception("XML Format has Changed unable to verify name");
                }
                _fileName = readString(br);

                int mode = 1;
                XMLNode current = null;
                Stack<XMLNode> stack = new Stack<XMLNode>();
                while(ms.Position < ms.Length)
                {
                    if(mode == 1)
                    {
                        var _params = new Dictionary<string, string>();
                        int nodes = br.ReadInt32();
                        for(int i =0; i < nodes; i++)
                        {
                            var name = readString(br);
                            var value = readString(br);
                            _params.Add(name, value);
                        }
                        if(br.ReadByte() != 1)
                        {
                            throw new Exception("failed to parse. Expected 1");
                        }
                        var tag = readString(br);
                        var node = new XMLNode(tag, _params, ref current);
                        if (current == null)
                        {
                            current = node;
                        }
                        else
                        {
                           current.AddChild(node);
                        }

                        node.children = br.ReadInt32();
                        node.mytag = br.ReadInt32();
                        mode = br.ReadInt32();
                        if(node.children > 0)
                        {
                            stack.Push(current);
                            current = node;
                        }

                    }
                    else if (mode ==2)
                    {
                        var node = new TextNode();
                        node.text = readString(br);
                        if (br.ReadByte() != 1)
                        {
                            throw new Exception("failed to parse. Expected 1 in mode 2");
                        }
                        var key = readString(br);
                        if(br.ReadInt32() != 0)
                        {
                            throw new Exception("failed to parse. Expected 0 children");
                        }
                        if(key != "text")
                        {
                            throw new Exception("Expected text tag got: " + key);
                        }
                        node.tag = br.ReadInt32();
                        current.AddChild(node);
                        if(ms.Position < ms.Length)
                            mode = br.ReadInt32();                        
                    }

                    do
                    {
                        if (current.Found == current.children)
                        {
                            var old = stack.Pop();
                            current = old;
                        }
                        else
                        {
                            current.Found++;
                            break;
                        }
                    } while (stack.Count > 0);
                                        
                }
                _base = current;
            }
        }

        /// <summary>
        /// reads an xord string from the binary reader
        /// </summary>
        /// <param name="br"></param>
        /// <returns></returns>
        private string readString(BinaryReader br)
        {
            int len = br.ReadInt32();
            var name = br.ReadBytes(len * 2);
            name = Crypto.performXoring(name);
            return Encoding.Unicode.GetString(name);
        }

        /// <summary>
        /// turns our xml document into the bns byte array
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter br = new BinaryWriter(ms))
                {
                    br.Write((UInt64) 4777284904613858636);//header
                    br.Write((UInt32) 3);//version

                    var data = _base.ToArray();
                    br.Write((int)data.Length + 
                        0x51 + //length of header (up to the fileName length
                        4 + //lenght of fileName size (int)
                        _fileName.Length * 2 //filename Unicode length
                        );

                    br.Seek(0x50, SeekOrigin.Begin);
                    br.Write((byte)0x01);
                    br.Write(_fileName.Length);
                    br.Write(Crypto.performXoring(Encoding.Unicode.GetBytes(_fileName)));
                    br.Write(data);

                }
                return ms.ToArray();
            }
        }

        /// <summary>
        /// update a value in the xml tree
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        internal void updateXML(string key, string value)
        {
            _base.GetChild(key).setValue(value);
        }
    }

    interface IXMLNode
    {
       byte[] ToArray();
    }

    class TextNode : IXMLNode
    {
        internal string text;
        internal int tag;
        public byte[] ToArray()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter br = new BinaryWriter(ms))
                {
                    if (text == null)
                        text = " ";
                    br.Write((int)2); //mode
                    br.Write(Utils.Byte.WriteString(text)); //value
                    br.Write((byte)0x01); //check byte
                    br.Write(Utils.Byte.WriteString("text")); //'tag'
                    br.Write((int)0); //children
                    br.Write(tag); //nextTag
                }

                return ms.ToArray();
            }
        }
    }
    class XMLNode : IXMLNode
    {
        string key;
        /// <summary>
        /// number of children nodes this is expected to have.
        /// </summary>
        internal int children;
        XMLNode _parent;
        
        /// <summary>
        /// number of children nodes that we have found.
        /// </summary>
        internal int Found = 0;
        
        /// <summary>
        /// value between open and close tags.
        /// </summary>
        internal string text;

        /// <summary>
        /// text tag value for writing binary data.
        /// </summary>
        internal int textTag = -1;

        /// <summary>
        /// Node tag id
        /// </summary>
        internal int mytag = -1;
        //internal int Length { get { return _params.Count + _children.Sum((x)=>x.Length); } }

        private Dictionary<string, string> _params = new Dictionary<string, string>();
        private List<IXMLNode> _children = new List<IXMLNode>();

        public XMLNode(string key, Dictionary<string, string> _params, ref XMLNode parent)
        {
            this.key = key;
            this._params = _params;
            _parent = parent;
        }

        public void AddChild(IXMLNode c)
        {
            _children.Add(c);
        }

        public override string ToString()
        {
            return "<" + key + " " + paramString() + " />";
        }

        private string paramString()
        {
            var format =  "{0}='{1}' ";

            StringBuilder itemString = new StringBuilder();
            foreach (var item in _params)
                itemString.AppendFormat(format, item.Key, item.Value);

            return itemString.ToString();
        }
        public XMLNode GetChild(string k)
        {
            return _children.OfType<XMLNode>().Where((n) => { return n._params["name"] == k; }).FirstOrDefault();
        }

        internal void setValue(string value)
        {
            _params["value"] = value;
        }

        /// <summary>
        /// converts this xml node into a byte[] that bns can read
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter br = new BinaryWriter(ms))
                {
                    if(_parent != null)
                        br.Write((int)1);

                    br.Write(_params.Count);
                    foreach (var x in _params)
                    {
                        br.Write(Utils.Byte.WriteString(x.Key));
                        br.Write(Utils.Byte.WriteString(x.Value));
                    }
                    br.Write((byte)0x01); //check byte
                    br.Write(Utils.Byte.WriteString(key)); //tag
                    br.Write((int)children); //children --- not sure how this is built. possibly <node> + <text> ?
                    br.Write(mytag); //tagId
                    //if(children > 0)
                    //{
                        foreach (var z in _children)
                        {
                            br.Write(z.ToArray());
                        }
                    //}
                }
                
                return ms.ToArray();
            }
        }
    }
}
