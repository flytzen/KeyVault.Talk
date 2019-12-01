using System.IO;

namespace KeyDemo 
{
    public class EncryptedData
    {
        public byte[] InitialisationVector { get; set; }

        public byte[] WrappedKey { get; set; }

        public string WrappingKeyIdentifier { get; set; }

        // In the real world, this is probably a terrible way to do this: 
        public Stream EncryptedDataStream { get; set; }

    }
}