using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GeniusX.AXACS.WindowsConsole.Facade
{
    internal static class Decryptor
    {
        internal const string KEY = "FTE553rW4tgw4GF1";
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(typeof(Decryptor));
        public static string DecryptPassword(string encryptedPassword)
        {
            if (logger.IsDebugEnabled)
            {
                logger.Debug(string.Format("DecryptPassword({0})", encryptedPassword));
            }

            DecryptTransformer transformer;
            byte[] initVec;

            if ((encryptedPassword == null) || (encryptedPassword.Length == 0))
            {
                return String.Empty;
            }

            //// Set up the Decryptor object
            transformer = new DecryptTransformer(EncryptionAlgorithm.TripleDes);

            //// decode the iv + password
            byte[] decodedPassword = Convert.FromBase64String(encryptedPassword);
            byte[] lengthArray = new byte[2];

            Array.Copy(decodedPassword, lengthArray, lengthArray.Length);

            int length = int.Parse(Encoding.ASCII.GetString(lengthArray));
            byte[] iv = new byte[length];
            Array.Copy(decodedPassword, 2, iv, 0, iv.Length);
            byte[] password = new byte[decodedPassword.Length - length - 2];
            Array.Copy(decodedPassword, length + 2, password, 0, password.Length);

            // Set the Initialization Vector
            initVec = iv;

            byte[] key = Encoding.ASCII.GetBytes(KEY);
            //// Perform the decryption
            byte[] plainText = Decrypt(password, key, transformer, initVec);

            if (logger.IsDebugEnabled)
            {
                logger.Debug(string.Format("DecryptPassword({0})=> returns password", encryptedPassword));
            }

            //// Display the decrypted string.
            return Encoding.ASCII.GetString(plainText);
        }

        private static byte[] Decrypt(byte[] bytesData, byte[] bytesKey, DecryptTransformer transformer, byte[] initVec)
        {
            if (logger.IsDebugEnabled)
            {
                logger.Debug("DecryptPassword");
            }

            //// Set up the memory stream for the decrypted data.
            MemoryStream memStreamDecryptedData = new MemoryStream();

            //// Pass in the initialization vector.
            transformer.IV = initVec;
            ICryptoTransform transform =
                transformer.GetCryptoServiceProvider(bytesKey);
            CryptoStream decStream = new CryptoStream(memStreamDecryptedData,
                transform,
                CryptoStreamMode.Write);
            try
            {
                decStream.Write(bytesData, 0, bytesData.Length);
            }
            catch (System.Exception ex)
            {
                ////if (logger.IsErrorEnabled)
                ////    logger.Error("Exception", ex);
            }

            decStream.FlushFinalBlock();
            decStream.Close();
            return memStreamDecryptedData.ToArray();
        } ////end Decrypt
    }
}
