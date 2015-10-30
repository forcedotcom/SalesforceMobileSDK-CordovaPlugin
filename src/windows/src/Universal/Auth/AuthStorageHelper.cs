/*
 * Copyright (c) 2013, salesforce.com, inc.
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Security.Credentials;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Settings;
using Salesforce.SDK.Logging;
using Salesforce.SDK.Core;
using Salesforce.SDK.Security;

namespace Salesforce.SDK.Auth
{
    /// <summary>
    ///     Store specific implementation if IAuthStorageHelper
    /// </summary>
    ///
    public sealed class AuthStorageHelper
    {
        private const string PasswordVaultAccounts = "Salesforce Accounts";
        private const string PasswordVaultCurrentAccount = "Salesforce Account";
        private const string PasswordVaultSecuredData = "Salesforce Secure";
        private const string PasswordVaultPincode = "Salesforce Pincode";
        private const string PasswordVaultEncryptionSettings = "Salesforce Encryption Settings";
        private const string InstallationStatusKey = "InstallationStatus";
        private const string PinBackgroundedTimeKey = "pintimeKey";
        private const string PincodeRequired = "pincodeRequired";
        
        private static readonly int AccountCacheTime = 20; // 20 minutes
        private static readonly Lazy<AuthStorageHelper> Auth = new Lazy<AuthStorageHelper>(() => new AuthStorageHelper());
        private static ILoggingService LoggingService => SDKServiceLocator.Get<ILoggingService>();
        private static IEncryptionService EncryptionService => SDKServiceLocator.Get<IEncryptionService>();
        private readonly ApplicationDataContainer _persistedData;
        private readonly PasswordVault _vault;
        private Account _currentAccount;
        private DateTime _lastSetAccount;
        private Account CurrentAccount
        {
            set
            {
                var oldAccount = _currentAccount;
                _lastSetAccount = DateTime.Now;
                _currentAccount = value;

                // Raise the event in AccountManager if this is a different account.
                // This check is necessary as sometimes CurrentAccount.Set is called
                // even if the same account was already set, so use the unique combo of
                // InstanceUrl and UserId to tell if the account has actually changed (login/logout).
                if (oldAccount?.InstanceUrl != value?.InstanceUrl && oldAccount?.UserId != value?.UserId)
                {
                    AccountManager.RaiseAuthenticatedAccountChangedEvent(oldAccount, value);
                }
            }
            get
            {
                if (_lastSetAccount != null)
                {
                    var now = DateTime.Now.AddMinutes(AccountCacheTime);
                    if (_lastSetAccount < now)
                    {
                        return _currentAccount;
                    }
                }
                _currentAccount = null;
                return null;
            }
        }


        private AuthStorageHelper()
        {
            _vault = new PasswordVault();
            _persistedData = ApplicationData.Current.LocalSettings;
            InstallationStatusCheck();
        }

        public static AuthStorageHelper GetAuthStorageHelper()
        {
            return Auth.Value;
        }

        private void InstallationStatusCheck()
        {
            if (!_persistedData.Values.ContainsKey(InstallationStatusKey))
            {
                IReadOnlyList<PasswordCredential> accounts = _vault.RetrieveAll();
                foreach (PasswordCredential next in accounts)
                {
                    _vault.Remove(next);
                }
                _persistedData.Values.Add(InstallationStatusKey, "");
            }
        }

        private IEnumerable<PasswordCredential> SafeRetrieveResource(string resource)
        {
            try
            {
                LoggingService.Log($"Attempting to retrieve resource {resource}", LoggingLevel.Verbose);

                var list = _vault.RetrieveAll();
                return (from item in list where resource.Equals(item.Resource) select item);
            }
            catch (Exception ex)
            {
                LoggingService.Log($"Exception occured when retrieving vault data for resource {resource}", LoggingLevel.Critical);

                LoggingService.Log(ex, LoggingLevel.Critical);

                Debug.WriteLine("Failed to retrieve vault data for resource " + resource);
            }
            return new List<PasswordCredential>();
        }

        private PasswordCredential SafeRetrieveUser(string resource, string userName)
        {
            try
            {
                var list = SafeRetrieveResource(resource);

                LoggingService.Log($"Attempting to retrieve user Resource={resource}  UserName={userName}", LoggingLevel.Verbose);

                var passwordCredentials = list as IList<PasswordCredential> ?? list.ToList();
                if (passwordCredentials.Any())
                {
                    return passwordCredentials.FirstOrDefault(n => userName.Equals(n.UserName));
                }
            }
            catch (Exception ex)
            {
                LoggingService.Log($"Exception occured when retrieving vault data for resource {resource}", LoggingLevel.Critical);

                LoggingService.Log(ex, LoggingLevel.Critical);

                Debug.WriteLine("Failed to retrieve vault data for resource " + resource);
            }
            return null;
        }

        private IEnumerable<PasswordCredential> SafeRetrieveUser(string userName)
        {
            try
            {
                LoggingService.Log($"Attempting to retrieve user {userName}", LoggingLevel.Verbose);

                return _vault.FindAllByUserName(userName);
            }
            catch (Exception ex)
            {
                LoggingService.Log($"Exception occured when retrieving vault data for user {userName}", LoggingLevel.Critical);

                LoggingService.Log(ex, LoggingLevel.Critical);

                Debug.WriteLine("Failed to retrieve vault data for user");
            }
            return new List<PasswordCredential>();
        }

        /// <summary>
        ///     Persist account, and sets account as the current account.
        /// </summary>
        /// <param name="account"></param>
        internal void PersistCredentials(Account account)
        {
            PasswordCredential creds = SafeRetrieveUser(PasswordVaultAccounts, account.UserName);
            if (creds != null)
            {
                LoggingService.Log("removing existing credential", LoggingLevel.Verbose);
                _vault.Remove(creds);
                IReadOnlyList<PasswordCredential> current = null;
                try
                {
                    current = _vault.FindAllByResource(PasswordVaultCurrentAccount);
                    if (current != null)
                    {
                        foreach (PasswordCredential user in current)
                        {
                            _vault.Remove(user);
                        }
                    }
                } catch (Exception)
                {
                    LoggingService.Log("did not find existing logged in user while persisting", LoggingLevel.Verbose);
                }
                
            }
            string serialized = EncryptionService.Encrypt(JsonConvert.SerializeObject(account));
            _vault.Add(new PasswordCredential(PasswordVaultAccounts, account.UserName, serialized));
            _vault.Add(new PasswordCredential(PasswordVaultCurrentAccount, account.UserName, serialized));
            CurrentAccount = account;
            var options = new LoginOptions(account.LoginUrl, account.ClientId, account.CallbackUrl,
                LoginOptions.DefaultDisplayType, account.Scopes);
            SalesforceConfig.LoginOptions = options;
            LoggingService.Log("done adding info to vault", LoggingLevel.Verbose);
        }

        internal Account RetrieveCurrentAccount()
        {
            var check = CurrentAccount;
            if (check != null)
            {
                return check;
            }
            PasswordCredential creds = SafeRetrieveResource(PasswordVaultCurrentAccount).FirstOrDefault();
            if (creds != null)
            {
                PasswordCredential account = _vault.Retrieve(creds.Resource, creds.UserName);
                if (String.IsNullOrWhiteSpace(account.Password))
                    _vault.Remove(account);
                else
                {
                    try
                    {
                        LoggingService.Log("getting current account", LoggingLevel.Verbose);
                        var accountStr = EncryptionService.Decrypt(account.Password);
                        CurrentAccount = JsonConvert.DeserializeObject<Account>(accountStr);
                        return CurrentAccount;
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Log("Exception occured when decrypting account, removing account from vault",
                            LoggingLevel.Warning);

                        LoggingService.Log(ex, LoggingLevel.Warning);

                        // if we can't decrypt remove the account
                        _vault.Remove(account);
                    }
                }
            }
            return null;
        }

        /// <summary>
        ///     Retrieve an account based on the id of the user.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal Account RetrievePersistedCredential(String id)
        {
            Dictionary<string, Account> accounts = RetrievePersistedCredentials();
            if (accounts.ContainsKey(id))
                return accounts[id];
            return null;
        }

        /// <summary>
        ///     Retrieve persisted account
        /// </summary>
        /// <returns></returns>
        internal Dictionary<string, Account> RetrievePersistedCredentials()
        {
            List<PasswordCredential> creds = new List<PasswordCredential>();
            var passCreds = SafeRetrieveResource(PasswordVaultAccounts);
            var current = SafeRetrieveResource(PasswordVaultCurrentAccount);
            if (passCreds != null)
            {
                creds.AddRange(passCreds);
            }
            if (current != null)
            {
                creds.AddRange(current);
            }
            var accounts = new Dictionary<string, Account>();
            if (creds != null)
            {
                LoggingService.Log(
                    "AuthStorageHelper.RetrievePersistedCredentials - attempting to get all credentials",
                    LoggingLevel.Verbose);

                foreach (PasswordCredential next in creds)
                {
                    PasswordCredential account = _vault.Retrieve(next.Resource, next.UserName);
                    if (String.IsNullOrWhiteSpace(account.Password))
                        _vault.Remove(next);
                    else
                    {
                        try
                        {
                            accounts[next.UserName] = JsonConvert.DeserializeObject<Account>(EncryptionService.Decrypt(account.Password));
                        }
                        catch (Exception ex)
                        {
                            if (ex is ArgumentException)
                                continue;
                            LoggingService.Log("Exception occured when decrypting account, removing account from vault",
                                LoggingLevel.Warning);

                            LoggingService.Log(ex, LoggingLevel.Warning);

                            // if we can't decrypt remove the account
                           _vault.Remove(next);
                        }
                       
                    }
                }
            }

            LoggingService.Log(
                string.Format("Total number of accounts retrieved = {0}",
                    accounts.Count), LoggingLevel.Verbose);

            return accounts;
        }

        /// <summary>
        ///     Delete a persisted account credential based on the user id.
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="id"></param>
        internal void DeletePersistedCredentials(string userName, string id)
        {
            // if this is the current account then update the property so event will get raised
            if (userName == CurrentAccount?.UserName && id == CurrentAccount?.UserId)
            {
                CurrentAccount = null;
            }

            IEnumerable<PasswordCredential> creds = SafeRetrieveUser(userName);
            if (creds != null)
            {
                foreach (PasswordCredential next in creds)
                {
                    PasswordCredential vaultAccount = _vault.Retrieve(next.Resource, next.UserName);
                    try
                    {
                        var account = JsonConvert.DeserializeObject<Account>(EncryptionService.Decrypt(vaultAccount.Password));
                        if (id.Equals(account.UserId))
                        {
                            LoggingService.Log(
                                string.Format("removing entry from vault for UserName={0}  UserID={1}",
                                    userName, id), LoggingLevel.Verbose);

                            _vault.Remove(next);
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Log("Exception occured when decrypting account, removing account from vault",
                            LoggingLevel.Warning);

                        LoggingService.Log(ex, LoggingLevel.Warning);

                        // if we can't decrypt remove the account
                       _vault.Remove(next);
                    }
                }
            }
        }

        /// <summary>
        ///     Delete all persisted accounts
        /// </summary>
        internal void DeletePersistedCredentials()
        {
            IEnumerable<PasswordCredential> accounts = SafeRetrieveResource(PasswordVaultAccounts);
            IEnumerable<PasswordCredential> current = SafeRetrieveResource(PasswordVaultCurrentAccount);
            if (accounts != null)
            {
                foreach (PasswordCredential next in accounts)
                {
                    _vault.Remove(next);
                }
            }
            if (current != null)
            {
                _currentAccount = null;
                foreach (PasswordCredential next in current)
                {
                    _vault.Remove(next);
                }
            }

            LoggingService.Log("removed all entries from vault", LoggingLevel.Verbose);
        }

        public void PersistPincode(MobilePolicy policy)
        {
            DeletePincode();
            var newPin = new PasswordCredential(PasswordVaultSecuredData, PasswordVaultPincode,
                JsonConvert.SerializeObject(policy));
            _vault.Add(newPin);
            LoggingService.Log("pincode added to vault",
                LoggingLevel.Verbose);
        }

        /// <summary>
        ///     This will return true if there is a master pincode set.
        /// </summary>
        /// <returns></returns>
        public static bool IsPincodeSet()
        {
            bool result = AuthStorageHelper.GetAuthStorageHelper().RetrievePincode() != null;

            LoggingService.Log(string.Format("result = {0}", result), LoggingLevel.Verbose);

            return result;
        }

        /// <summary>
        ///     This will return true if a pincode is required before the app can be accessed.
        /// </summary>
        /// <returns></returns>
        public static bool IsPincodeRequired()
        {
            AuthStorageHelper auth = GetAuthStorageHelper();
            // a flag is set if the timer was exceeded at some point. Automatically return true if the flag is set.
            bool required = auth.RetrieveData(PincodeRequired) != null;
            if (required)
            {
                LoggingService.Log("Pincode is required", LoggingLevel.Verbose);
                return true;
            }
            if (IsPincodeSet())
            {
                MobilePolicy policy = GetMobilePolicy();
                if (policy != null)
                {
                    string time = auth.RetrieveData(PinBackgroundedTimeKey);
                    if (time != null)
                    {
                        DateTime previous = DateTime.Parse(time);
                        DateTime current = DateTime.Now.ToUniversalTime();
                        TimeSpan diff = current.Subtract(previous);
                        if (diff.Minutes >= policy.ScreenLockTimeout)
                        {
                            // flag that requires pincode to be entered in the future. Until the flag is deleted a pincode will be required.
                            auth.PersistData(true, PincodeRequired, time);
                            LoggingService.Log("Pincode is required", LoggingLevel.Verbose);
                            return true;
                        }
                    }
                }
            }
            // We aren't requiring pincode, so remove the flag.
            auth.DeleteData(PincodeRequired);
            LoggingService.Log("Pincode is not required", LoggingLevel.Verbose);
            return false;
        }

        private static string GenerateEncryptedPincode(string pincode)
        {
            HashAlgorithmProvider alg = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha256);
            IBuffer buff = CryptographicBuffer.ConvertStringToBinary(pincode, BinaryStringEncoding.Utf8);
            IBuffer hashed = alg.HashData(buff);
            string res = CryptographicBuffer.EncodeToHexString(hashed);
            LoggingService.Log("Pincode generated, now encrypting and returning it", LoggingLevel.Verbose);
            return EncryptionService.Encrypt(res);
        }

        /// <summary>
        ///     Stores the pincode and associated mobile policy information including pin length and screen lock timeout.
        /// </summary>
        /// <param name="policy"></param>
        /// <param name="pincode"></param>
        public static void StorePincode(MobilePolicy policy, string pincode)
        {
            string hashed = GenerateEncryptedPincode(pincode);
            var mobilePolicy = new MobilePolicy
            {
                ScreenLockTimeout = policy.ScreenLockTimeout,
                PinLength = policy.PinLength,
                PincodeHash = EncryptionService.Encrypt(hashed, pincode)
            };
            AuthStorageHelper.GetAuthStorageHelper().PersistPincode(mobilePolicy);
            LoggingService.Log("Pincode stored", LoggingLevel.Verbose);
        }

        /// <summary>
        ///     Validate the given pincode against the stored pincode.
        /// </summary>
        /// <param name="pincode">Pincode to validate</param>
        /// <returns>True if pincode matches</returns>
        public static bool ValidatePincode(string pincode)
        {
            string compare = GenerateEncryptedPincode(pincode);
            try
            {
                string retrieved = AuthStorageHelper.GetAuthStorageHelper().RetrievePincode();
                var policy = JsonConvert.DeserializeObject<MobilePolicy>(retrieved);
                bool result = compare.Equals(EncryptionService.Decrypt(policy.PincodeHash, pincode));

                LoggingService.Log($"result = {result}", LoggingLevel.Verbose);

                return result;
            }
            catch (Exception ex)
            {
                LoggingService.Log("Exception occurred when validating pincode:", LoggingLevel.Critical);
                LoggingService.Log(ex, LoggingLevel.Critical);
            }

            return false;
        }

        /// <summary>
        ///     Clear the pincode flag.
        /// </summary>
        public static void Unlock()
        {
            GetAuthStorageHelper().DeleteData(PincodeRequired);
            SavePinTimer();
        }

        public static void ClearPinTimer()
        {
            GetAuthStorageHelper().DeleteData(PinBackgroundedTimeKey);
        }

        public static void SavePinTimer()
        {
            MobilePolicy policy = GetMobilePolicy();
            Account account = AccountManager.GetAccount();
            if (account != null && policy != null && policy.ScreenLockTimeout > 0)
            {
                LoggingService.Log("Saving pin timer", LoggingLevel.Verbose);
                AuthStorageHelper.GetAuthStorageHelper()
                    .PersistData(true, PinBackgroundedTimeKey, DateTime.Now.ToUniversalTime().ToString());
            }
        }

        /// <summary>
        ///     Returns the global mobile policy stored.
        /// </summary>
        /// <returns></returns>
        public static MobilePolicy GetMobilePolicy()
        {
            string retrieved = AuthStorageHelper.GetAuthStorageHelper().RetrievePincode();
            if (retrieved != null)
            {
                LoggingService.Log("Returning retrieved mobile policy", LoggingLevel.Verbose);
                return JsonConvert.DeserializeObject<MobilePolicy>(retrieved);
            }
            LoggingService.Log("No mobile policy found", LoggingLevel.Verbose);
            return null;
        }

        /// <summary>
        ///     This will wipe out the pincode and associated data.
        /// </summary>
        public static void WipePincode()
        {
            AuthStorageHelper auth = AuthStorageHelper.GetAuthStorageHelper();
            auth.DeletePincode();
            auth.DeleteData(PinBackgroundedTimeKey);
            auth.DeleteData(PincodeRequired);
            LoggingService.Log("Pincode wiped", LoggingLevel.Verbose);
        }

        internal string RetrievePincode()
        {
            PasswordCredential pin = SafeRetrieveUser(PasswordVaultSecuredData, PasswordVaultPincode);
            if (pin != null)
            {
                LoggingService.Log("retrieved pincode from vault",
                    LoggingLevel.Verbose);
                return pin.Password;
            }
            return null;
        }

        internal void DeletePincode()
        {
            PasswordCredential pin = SafeRetrieveUser(PasswordVaultSecuredData, PasswordVaultPincode);
            if (pin != null)
            {
                LoggingService.Log("removed pincode from vault",
                    LoggingLevel.Verbose);
                _vault.Remove(pin);
            }
        }

        internal void PersistData(bool replace, string key, string data, string nonce = null)
        {
            if (_persistedData.Values.ContainsKey(key))
            {
                if (replace)
                {
                    _persistedData.Values[key] = EncryptionService.Encrypt(data, nonce);
                }
            }
            else
            {
                _persistedData.Values.Add(key, EncryptionService.Encrypt(data));
            }
        }

        internal string RetrieveData(string key, string nonce = null)
        {
            string data = null;
            if (_persistedData.Values.ContainsKey(key))
            {
                data = EncryptionService.Decrypt(_persistedData.Values[key] as string, nonce);
            }
            return data;
        }

        internal void DeleteData(string key)
        {
            if (_persistedData.Values.ContainsKey(key))
            {
                _persistedData.Values.Remove(key);
            }
        }

        internal void PersistEncryptionSettings(string password, string salt)
        {
            DeleteEncryptionSettings();
            var encryptionSettingsObj = new { Password = password, Salt = salt };
            var encrpytionSettings = new PasswordCredential(PasswordVaultSecuredData, PasswordVaultEncryptionSettings,
                JsonConvert.SerializeObject(encryptionSettingsObj));
            _vault.Add(encrpytionSettings);
            LoggingService.Log("encryption settings added to vault",
                LoggingLevel.Verbose);
        }

        internal bool TryRetrieveEncryptionSettings(out string password, out string salt)
        {
            password = null;
            salt = null;
            PasswordCredential creds = SafeRetrieveResource(PasswordVaultSecuredData).FirstOrDefault();
            if (creds != null)
            {
                PasswordCredential encrpytionSettings = _vault.Retrieve(PasswordVaultSecuredData, PasswordVaultEncryptionSettings);
                if (String.IsNullOrWhiteSpace(encrpytionSettings.Password))
                {
                    // Failed to deserialize the data, we should clear it out and start over.
                    LoggingService.Log("Encryption Settings values are corrupt. Assuming bad state and clearing the vault completely",
                        LoggingLevel.Warning);
                    _vault.Remove(encrpytionSettings);
                    DeletePersistedCredentials();
                    DeletePincode();
                }
                else
                {
                    try
                    {
                        var encrpytionSettingsObj = JObject.Parse(encrpytionSettings.Password);
                        password = encrpytionSettingsObj.Value<string>("Password");
                        salt = encrpytionSettingsObj.Value<string>("Salt");
                        LoggingService.Log("Encryption Settings have been retrieved successfully.",
                        LoggingLevel.Verbose);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        // Failed to deserialize the data, we should clear it out and start over.
                        LoggingService.Log("Encryption Settings values can't be deserialized. Assuming bad state and clearing the vault completely",
                            LoggingLevel.Warning);

                        LoggingService.Log(ex, LoggingLevel.Warning);
                        
                        _vault.Remove(encrpytionSettings);
                        DeletePersistedCredentials();
                        DeletePincode();
                    }
                }

            }
            else
            {
                var account = RetrieveCurrentAccount();
                var pincode = RetrievePincode();
                // If either account or pincode are stored, but the Encryption Settings values can't be retrieved, then we should assume we are in a bad state and clear the vault.
                if (account != null || pincode != null)
                {
                    LoggingService.Log("Encryption Settings values can't be retrieved from vault. Assuming bad state and clearing the vault completely",
                        LoggingLevel.Verbose);
                    DeletePersistedCredentials();
                    DeletePincode();
                }
            }
            LoggingService.Log("Encryption Settings have not yet been saved.",
                        LoggingLevel.Verbose);
            return false;
        }

        internal void DeleteEncryptionSettings()
        {
            PasswordCredential encryptionSettings = SafeRetrieveUser(PasswordVaultSecuredData, PasswordVaultEncryptionSettings);
            if (encryptionSettings != null)
            {
                LoggingService.Log("Removed encryption settings from vault",
                    LoggingLevel.Verbose);
                _vault.Remove(encryptionSettings);
            }
        }
    }
}