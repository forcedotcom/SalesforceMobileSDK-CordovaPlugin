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
using Windows.System.Profile;
using Salesforce.SDK.Auth;

namespace Salesforce.SDK.Security
{
    /// <summary>
    ///     This class is a sample encryption key generator. It is highly recommended that you roll your own and provide it's
    ///     configuration in your Application class
    ///     extending SalesforceApplication or SaleforcePhoneApplication.
    /// </summary>
    public sealed class HmacSHA256KeyGenerator : IKeyGenerator
    {
        private readonly string _password;
        public string Password
        {
            get
            {
                return _password;
            }
        }

        private readonly string _salt;
        public string Salt
        {
            get
            {
                return _salt;
            }
        }

        public HmacSHA256KeyGenerator()
        {
            string password;
            string salt;

            var authStorageHelper = AuthStorageHelper.GetAuthStorageHelper();
            if (!authStorageHelper.TryRetrieveEncryptionSettings(out password, out salt))
            {
                GenerateRandomPasswordAndSalt(out password, out salt);
                authStorageHelper.PersistEncryptionSettings(password, salt);
            }
            _password = password;
            _salt = salt;
        }

        public void GenerateKey(string password, string salt, string nonce, out IBuffer keyMaterial, out IBuffer iv)
        {
            IBuffer saltBuffer = CryptographicBuffer.ConvertStringToBinary(salt, BinaryStringEncoding.Utf8);
            KeyDerivationParameters keyParams = KeyDerivationParameters.BuildForSP800108(saltBuffer, GetNonce(nonce));
            KeyDerivationAlgorithmProvider kdf =
                KeyDerivationAlgorithmProvider.OpenAlgorithm(Encryptor.Settings.KeyDerivationAlgorithm);
            IBuffer passwordBuffer = CryptographicBuffer.ConvertStringToBinary(password, BinaryStringEncoding.Utf8);
            CryptographicKey keyOriginal = kdf.CreateKey(passwordBuffer);

            int keySize = 256;
            int ivSize = 128/8;
            var totalData = (uint) (keySize + ivSize);
            IBuffer keyDerived = CryptographicEngine.DeriveKeyMaterial(keyOriginal, keyParams, totalData);

            byte[] keyMaterialBytes = keyDerived.ToArray();
            keyMaterial = WindowsRuntimeBuffer.Create(keyMaterialBytes, 0, keySize, keySize);
            iv = WindowsRuntimeBuffer.Create(keyMaterialBytes, keySize, ivSize, ivSize);
        }

        /// <summary>
        ///     It is recommended you generate a way that is unique for the app/device. In this example we normalize the hardware
        ///     ID for things that rarely change and add a few strings related to the app.
        ///     See http://code.msdn.microsoft.com/windowsapps/How-to-use-ASHWID-to-3742c83e for examples on ASHWID use.
        /// </summary>
        /// <returns></returns>
        private static string GetDeviceUniqueId()
        {
            HardwareToken id = HardwareIdentification.GetPackageSpecificToken(null);
            string normalized = NormalizeHardwareId(id.Id.ToArray());
            HashAlgorithmProvider alg = HashAlgorithmProvider.OpenAlgorithm("MD5");
            IBuffer buff =
                CryptographicBuffer.ConvertStringToBinary(normalized + typeof (HmacSHA256KeyGenerator).FullName,
                    BinaryStringEncoding.Utf8);
            IBuffer hashed = alg.HashData(buff);
            return CryptographicBuffer.EncodeToHexString(hashed);
        }

        /// <summary>
        ///     Simplified version of going through the hardware string. There are many different hardware items that can be looked
        ///     at, and a good number of them can change when things are plugged in or
        ///     turned on or off.  In this we went for a few items that should stay relatively the same.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private static string NormalizeHardwareId(Byte[] id)
        {
            string hardwareIdString = BitConverter.ToString(id).Replace("-", "");
            var normalized = new StringBuilder();
            for (int i = 0; i < hardwareIdString.Length/8; i++)
            {
                switch (hardwareIdString.Substring(i*8, 4))
                {
                    case "0100": // Processor 
                        normalized.Append(hardwareIdString.Substring(i*8 + 4, 4));
                        break;
                    case "0900": // System BIOS 
                        normalized.Append(hardwareIdString.Substring(i*8 + 4, 4));
                        break;
                }
            }
            return normalized.ToString();
        }

        /// <summary>
        ///     This utility function returns a nonce value for authenticated encryption modes.
        /// </summary>
        /// <returns></returns>
        private static IBuffer GetNonce(string nonce)
        {
            if (String.IsNullOrWhiteSpace(nonce))
            {
                nonce = "";
            }
            return CryptographicBuffer.ConvertStringToBinary(GetDeviceUniqueId() + nonce, BinaryStringEncoding.Utf8);
        }

        private void GenerateRandomPasswordAndSalt(out string password, out string salt)
        {
            password = GenerateRandomEncryptionString();
            salt = GenerateRandomEncryptionString();
        }

        // Ensure a truly random string that is good for use in encrpytion.
        private string GenerateRandomEncryptionString()
        {
            // Define the length, in bytes, of the buffer.
            UInt32 length = 16;

            // Generate random data and copy it to a buffer.
            IBuffer buffer = CryptographicBuffer.GenerateRandom(length);

            // Encode the buffer to a hexadecimal string.
            return CryptographicBuffer.EncodeToHexString(buffer);
        }
    }
}