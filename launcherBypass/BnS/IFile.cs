using System.Security.Cryptography;

namespace launcherBypass.BnS
{
    internal interface IFile
    {
        void FromBytes(byte[] array);
        byte[] ToArray();
    }
}