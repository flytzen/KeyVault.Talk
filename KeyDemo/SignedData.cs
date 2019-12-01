namespace KeyDemo
{
    public class SignedData
    {
        public string SigningKeyIdentifier { get; set; }

        public byte[] Signature { get; set; }

        public string Data { get; set; } 
    }
}