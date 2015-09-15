using System;
using System.Collections;
using System.Security.Cryptography;

namespace GeniusX.AXACS.WindowsConsole.Facade
{
    internal enum EncryptionAlgorithm 
    {
        Des = 1,
        Rc2,
        Rijndael,
        TripleDes
    }

    internal class DecryptTransformer
    {
        private EncryptionAlgorithm algorithmID;
        private byte[] initVec;   

        internal DecryptTransformer(EncryptionAlgorithm decryptId)
        {
            this.algorithmID = decryptId;
        }

        internal byte[] IV
        {
            set { this.initVec = value; }
        }

        internal ICryptoTransform GetCryptoServiceProvider(byte[] bytesKey)
        {
            //// Pick the provider.
            switch (this.algorithmID)
            {
                case EncryptionAlgorithm.TripleDes:
                    {
                        TripleDES des3 = new TripleDESCryptoServiceProvider();
                        des3.Mode = CipherMode.CBC;
                        return des3.CreateDecryptor(bytesKey, this.initVec);
                    }

                default:
                    {
                        Hashtable ht = new Hashtable();
                        ht.Add("algorithmID", this.algorithmID.ToString());
                        throw new Exception();
                    }
            }
        } ////end GetCryptoServiceProvider
    }
}
