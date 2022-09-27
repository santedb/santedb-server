using Microsoft.IdentityModel.Tokens;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Authentication.OAuth2
{
    internal static class ISymmetricCryptoServiceProviderExtensions
    {
        internal const int IV_SIZE = 16;

        public static string EncryptString(this ISymmetricCryptographicProvider provider, string plainText)
        {
            return Base64UrlEncoder.Encode(Encrypt(provider, Encoding.UTF8.GetBytes(plainText)));
        }

        public static string DecryptString(this ISymmetricCryptographicProvider provider, string cipherText)
        {
            return Encoding.UTF8.GetString(Decrypt(provider, Base64UrlEncoder.DecodeBytes(cipherText)));
        }

        public static byte[] Encrypt(this ISymmetricCryptographicProvider provider, byte[] plainText)
        {
            if (null == provider)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            var key = provider.GetContextKey();
            var iv = new byte[IV_SIZE];
            System.Security.Cryptography.RandomNumberGenerator.Create().GetBytes(iv);

            var ciphertext = provider.Encrypt(plainText, key, iv);

            var result = new byte[IV_SIZE + ciphertext.Length];

            Buffer.BlockCopy(iv, 0, result, 0, IV_SIZE);
            Buffer.BlockCopy(ciphertext, 0, result, IV_SIZE, ciphertext.Length);

            return result;
        }

        public static byte[] Decrypt(this ISymmetricCryptographicProvider provider, byte[] cipherText)
        {
            if (null == provider)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            var key = provider.GetContextKey();
            var iv = new byte[IV_SIZE];
            var encrypteddata = new byte[cipherText.Length - IV_SIZE];
            Buffer.BlockCopy(cipherText, 0, iv, 0, IV_SIZE);
            Buffer.BlockCopy(cipherText, IV_SIZE, encrypteddata, 0, encrypteddata.Length);

            return provider.Decrypt(encrypteddata, key, iv);
        }
    }
}
