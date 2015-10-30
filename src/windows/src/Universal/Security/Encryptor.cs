/*
 * Copyright (c) 2014, salesforce.com, inc.
 * All rights reserved.
 * Redistribution and use of this software in source and binary forms, with or
 * without modification, are permitted provided that the following conditions
 * are met:
 * - Redistributions of source code must retain the above copyright notice, this
 * list of conditions and the following disclaimer.
 * - Redistributions in binary form must reproduce the above copyright notice,
 * this list of conditions and the following disclaimer in the documentation
 * and/or other materials provided with the distribution.
 * - Neither the name of salesforce.com, inc. nor the names of its contributors
 * may be used to endorse or promote products derived from this software without
 * specific prior written permission of salesforce.com, inc.
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace Salesforce.SDK.Security
{
    public class Encryptor : IEncryptionService
    {
        public static readonly string PreferredSymmetricAlgorithm = SymmetricAlgorithmNames.AesCbcPkcs7;

        public static readonly string PreferredKeyDerivationAlgorithm =
            KeyDerivationAlgorithmNames.Sp800108CtrHmacSha256;

        public static EncryptionSettings Settings { get; private set; }

        public static void init(EncryptionSettings settings)
        {
            Settings = settings;
        }

        public string Encrypt(string text)
        {
            return Encrypt(text, null);
        }

        public string Encrypt(string text, string nonce)
        {
            if (String.IsNullOrWhiteSpace(text))
            {
                return null;
            }
            IBuffer keyMaterial;
            IBuffer iv;
            Settings.GenerateKey(out keyMaterial, out iv, nonce);

            IBuffer clearTextBuffer = CryptographicBuffer.ConvertStringToBinary(text, BinaryStringEncoding.Utf8);

            // Setup an AES key, using AES in CBC mode and applying PKCS#7 padding on the input
            SymmetricKeyAlgorithmProvider provider =
                SymmetricKeyAlgorithmProvider.OpenAlgorithm(Settings.SymmetricAlgorithm);
            CryptographicKey key = provider.CreateSymmetricKey(keyMaterial);

            // Encrypt the data and convert it to a Base64 string
            IBuffer encrypted = CryptographicEngine.Encrypt(key, clearTextBuffer, iv);
            string ciphertextString = CryptographicBuffer.EncodeToBase64String(encrypted);
            return ciphertextString;
        }

        public string Decrypt(string text)
        {
            return Decrypt(text, null);
        }

        public string Decrypt(string text, string nonce)
        {
            if (String.IsNullOrWhiteSpace(text) || Settings == null)
            {
                return null;
            }
            IBuffer keyMaterial;
            IBuffer iv;
            Settings.GenerateKey(out keyMaterial, out iv, nonce);

            SymmetricKeyAlgorithmProvider provider =
                SymmetricKeyAlgorithmProvider.OpenAlgorithm(Settings.SymmetricAlgorithm);
            CryptographicKey key = provider.CreateSymmetricKey(keyMaterial);

            IBuffer ciphertextBuffer = CryptographicBuffer.DecodeFromBase64String(text);
            IBuffer decryptedBuffer = CryptographicEngine.Decrypt(key, ciphertextBuffer, iv);
            byte[] decryptedArray = decryptedBuffer.ToArray();
            return Encoding.UTF8.GetString(decryptedArray, 0, decryptedArray.Length);
        }
    }
}