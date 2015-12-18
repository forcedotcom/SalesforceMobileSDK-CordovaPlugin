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
using Salesforce.SDK.Core;
using Salesforce.SDK.Logging;
using System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Salesforce.SDK.Auth
{
    public class PincodeManager
    {
        private static readonly DispatcherTimer IdleTimer;

        static PincodeManager()
        {
            IdleTimer = new DispatcherTimer();
            IdleTimer.Tick += IdleTimer_Tick;
        }

        /// <summary>
        ///     This will return true if there is a master pincode set.
        /// </summary>
        /// <returns></returns>
        public static bool IsPincodeSet()
        {

            return AuthStorageHelper.IsPincodeSet();
        }

       

        /// <summary>
        ///     Clear the pincode flag.
        /// </summary>
        internal static void Unlock()
        {
            AuthStorageHelper.Unlock();
        }

        public static void SavePinTimer()
        {
            AuthStorageHelper.SavePinTimer();
        }

        public static void TriggerBackgroundedPinTimer()
        {
            Account account = AccountManager.GetAccount();
            MobilePolicy policy = AuthStorageHelper.GetMobilePolicy();
            bool required = AuthStorageHelper.IsPincodeRequired();
            if (account != null)
            {
                if (required || (policy == null && account.Policy != null))
                {
                    LaunchPincodeScreen();
                }
            }
            else if (!required)
            {
                AuthStorageHelper.ClearPinTimer();
            }
        }

        /// <summary>
        ///     This method will launch the pincode screen if the policy requires it.
        ///     If determined that no pincode screen is required, the flag requiring the pincode will be cleared.
        /// </summary>
        public static async void LaunchPincodeScreen()
        {
            var frame = Window.Current.Content as Frame;
            if (frame != null && typeof (PincodeDialog) != frame.SourcePageType)
            {
                await frame.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    SDKServiceLocator.Get<ILoggingService>().Log(" Launching Pincode Screen", LoggingLevel.Information);
                    Account account = AccountManager.GetAccount();
                    if (account != null)
                    {
                        PincodeOptions options = null;
                        bool required = AuthStorageHelper.IsPincodeRequired();
                        if (account.Policy != null && !IsPincodeSet())
                        {
                            options = new PincodeOptions(PincodeOptions.PincodeScreen.Create, account, "");
                        }
                        else if (required)
                        {
                            MobilePolicy policy = AuthStorageHelper.GetMobilePolicy();
                            if (account.Policy != null)
                            {
                                if (policy.ScreenLockTimeout < account.Policy.ScreenLockTimeout)
                                {
                                    policy.ScreenLockTimeout = account.Policy.ScreenLockTimeout;
                                    AuthStorageHelper.GetAuthStorageHelper().PersistPincode(policy);
                                }
                                if (policy.PinLength < account.Policy.PinLength)
                                {
                                    options = new PincodeOptions(PincodeOptions.PincodeScreen.Create, account, "");
                                }
                                else
                                {
                                    options = new PincodeOptions(PincodeOptions.PincodeScreen.Locked, account, "");
                                }
                            }
                            else
                            {
                                options = new PincodeOptions(PincodeOptions.PincodeScreen.Locked, account, "");
                            }
                        }
                        if (options != null)
                        {
                            // As per MSDN documentation (https://msdn.microsoft.com/en-us/library/windows/apps/hh702394.aspx)
                            // the second param of Frame.Navigate must be a basic type otherwise Suspension manager will crash
                            // when serializing frame's state. So we serialize custom object using Json and pass that as the 
                            // second param to avoid this crash.
                            frame.Navigate(typeof (PincodeDialog), PincodeOptions.ToJson(options));
                        }
                    }
                });
            }
        }

        public static void StartIdleTimer()
        {
            if (IsPincodeSet())
            {
                var policy = AuthStorageHelper.GetMobilePolicy();
                if (policy != null)
                {
                    IdleTimer.Interval = TimeSpan.FromMinutes(policy.ScreenLockTimeout);
                    if (IdleTimer.IsEnabled)
                    {
                        IdleTimer.Stop();
                    }
                    SavePinTimer();
                    IdleTimer.Start();
                }
            }
        }


        private static void IdleTimer_Tick(object sender, object e)
        {
            TriggerBackgroundedPinTimer();
        }
    }
}