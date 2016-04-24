using System.Security.Cryptography;

namespace bnsmultiwindow.BnS
{
    internal interface IFile
    {
        void FromBytes(byte[] array);
        byte[] ToArray();
    }
}