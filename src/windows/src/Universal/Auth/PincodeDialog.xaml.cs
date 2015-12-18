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
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Salesforce.SDK.Strings;
using Salesforce.SDK.Logging;
using Salesforce.SDK.Core;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Salesforce.SDK.Auth
{
    public sealed partial class PincodeDialog : Page
    {
        /// <summary>
        ///     Defines number of retries of locked screen. We log user out when RetryCounter hits 1.
        /// </summary>
        private const int MaximumRetries = 10;

        private static int RetryCounter = MaximumRetries;
        private const int MinimumWidthForBarMode = 500;
        private const int BarModeHeight = 400;
        private PincodeOptions Options;
        private static ILoggingService LoggingService => SDKServiceLocator.Get<ILoggingService>();

        public PincodeDialog()
        {
            InitializeComponent();
            Passcode.PlaceholderText = LocalizedStrings.GetString("passcode");
            ErrorFlyout.Closed += ErrorFlyout_Closed;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // start with a default PincodeOptions object
            Options = new PincodeOptions(PincodeOptions.PincodeScreen.Create, AccountManager.GetAccount(), "");
            if (e.Parameter != null)
            {
                try
                {
                    Options = PincodeOptions.FromJson((string)e.Parameter);
                }
                catch
                {
                    // it is not a valid PincodeOptions object so ignore and keep using the default one
                }
            }

            if (Options != null)
            {
                switch (Options.Screen)
                {
                    case PincodeOptions.PincodeScreen.Locked:
                        SetupLocked();
                        break;
                    case PincodeOptions.PincodeScreen.Confirm:
                        SetupConfirm();
                        break;
                    default:
                        SetupCreate();
                        break;
                }
            }
            else
            {
                SetupCreate();
            }
              
            Passcode.Password = "";
            Rect bounds = Window.Current.Bounds;
            // This screen will adjust size for "narrow" views; if the view is sufficiently narrow it will fill the screen and move text to the top, so it can be seen with virtual keyboard.
            if (bounds.Width < MinimumWidthForBarMode)
            {
                PincodeContainer.Height = bounds.Height;
                PincodeWindow.VerticalAlignment = VerticalAlignment.Top;
            }
            else
            {
                // Act as a "bar" for screens that have enough horizontal resolution.
                PincodeWindow.VerticalAlignment = VerticalAlignment.Center;
                PincodeContainer.Height = BarModeHeight;
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (e != null
                && Options != null
                && PincodeOptions.PincodeScreen.Locked.Equals(Options.Screen)
                && AuthStorageHelper.IsPincodeRequired())
            {
                e.Cancel = true;
            }
            base.OnNavigatingFrom(e);
        }

        /// <summary>
        ///     Configures the dialog for creating a pincode.
        /// </summary>
        private void SetupCreate()
        {
            LoggingService.Log("Configuring the dialog for creating a pincode", LoggingLevel.Information);
            Title.Text = LocalizedStrings.GetString("passcode_create_title");
            Description.Text = LocalizedStrings.GetString("passcode_create_security");
            ContentFooter.Visibility = Visibility.Visible;
            ContentFooter.Text = String.Format(LocalizedStrings.GetString("passcode_length"),
                Options.User.Policy.PinLength);
            Passcode.KeyDown += CreateClicked;
        }

        /// <summary>
        ///     Configures the dialog to act as a lockscreen.
        /// </summary>
        private void SetupLocked()
        {
            LoggingService.Log("Configuring the dialog to act as a lockscreen", LoggingLevel.Information);
            Title.Text = LocalizedStrings.GetString("passcode_enter_code_title");
            ContentFooter.Text = LocalizedStrings.GetString("passcode_confirm");
            Description.Text = "";
            // code needed to "underline" the text
            var description = new Run();
            var underline = new Underline();
            description.Text = LocalizedStrings.GetString("passcode_forgot_passcode");
            underline.Inlines.Add(description);
            Description.Inlines.Add(underline);
            Passcode.KeyDown += LockedClick;
            Description.Tapped += Description_Tapped;
            RetryCounter = MaximumRetries;
        }

        /// <summary>
        ///     Configures the dialog to act as a confirmation for the entered pincode.
        /// </summary>
        private void SetupConfirm()
        {
            LoggingService.Log("Configuring the dialog to act as a confirmation for the entered pincode", LoggingLevel.Information);
            Title.Text = LocalizedStrings.GetString("passcode_reenter");
            Description.Text = LocalizedStrings.GetString("passcode_confirm");
            ContentFooter.Visibility = Visibility.Collapsed;
            Passcode.KeyDown += ConfirmClicked;
        }

        private void CreateClicked(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != VirtualKey.Accept && e.Key != VirtualKey.Enter)
                return;
            e.Handled = true;
            if (Passcode.Password.Length >= Options.User.Policy.PinLength)
            {
                LoggingService.Log("Going to confirmation page", LoggingLevel.Verbose);
                var options = new PincodeOptions(PincodeOptions.PincodeScreen.Confirm, Options.User, Passcode.Password);
                // As per MSDN documentation (https://msdn.microsoft.com/en-us/library/windows/apps/hh702394.aspx)
                // the second param of Frame.Navigate must be a basic type otherwise Suspension manager will crash
                // when serializing frame's state. So we serialize custom object using Json and pass that as the 
                // second param to avoid this crash.
                Frame.Navigate(typeof (PincodeDialog), PincodeOptions.ToJson(options));
            }
            else
            {
                DisplayErrorFlyout(String.Format(LocalizedStrings.GetString("passcode_length"),
                    Options.User.Policy.PinLength));
            }
        }

        private void ConfirmClicked(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != VirtualKey.Accept && e.Key != VirtualKey.Enter)
                return;
            e.Handled = true;
            if (Passcode.Password.Equals(Options.Passcode))
            {
                LoggingService.Log($"Pincode matched, going to {SDKManager.RootApplicationPage}", LoggingLevel.Verbose);
                AuthStorageHelper.StorePincode(Options.Policy, Options.Passcode);
                PincodeManager.Unlock();
                Frame.Navigate(SDKManager.RootApplicationPage);
            }
            else
            {
                DisplayErrorFlyout(LocalizedStrings.GetString("passcode_must_match"));
            }
        }

        private async void LockedClick(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != VirtualKey.Accept && e.Key != VirtualKey.Enter)
                return;
            e.Handled = true;
            Account account = AccountManager.GetAccount();
            if (account == null)
            {
                await SDKServiceLocator.Get<IAuthHelper>().StartLoginFlowAsync();
            }
            else if (AuthStorageHelper.ValidatePincode(Passcode.Password))
            {
                PincodeManager.Unlock();
                if (Frame.CanGoBack)
                {
                    Frame.GoBack();
                }
                else
                {
                    Frame.Navigate(SDKManager.RootApplicationPage);
                }
            }
            else
            {
                if (RetryCounter <= 1)
                {
                    await SDKManager.GlobalClientManager.Logout();
                }
                RetryCounter--;
                ContentFooter.Text = String.Format(LocalizedStrings.GetString("passcode_incorrect"), RetryCounter);
                ContentFooter.Visibility = Visibility.Visible;
            }
        }


        private void Description_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ForgotFlyout.ShowAt(Passcode);
        }

        private void DisplayErrorFlyout(string text)
        {
            ErrorContent.Text = text;
            ErrorFlyout.ShowAt(Passcode);
        }

        private void ErrorFlyoutClicked(object sender, RoutedEventArgs e)
        {
            ErrorFlyout.Hide();
        }

        private void LogoutClicked(object sender, RoutedEventArgs e)
        {
            ForgotFlyout.Hide();
            if (LogoutConfirmButton.Equals(sender))
            {
                AccountManager.WipeAccounts();
            }
        }

        private void ErrorFlyout_Closed(object sender, object e)
        {
            if (PincodeOptions.PincodeScreen.Confirm == Options.Screen)
            {
                var options = new PincodeOptions(PincodeOptions.PincodeScreen.Create, Options.User, "");
                // As per MSDN documentation (https://msdn.microsoft.com/en-us/library/windows/apps/hh702394.aspx)
                // the second param of Frame.Navigate must be a basic type otherwise Suspension manager will crash
                // when serializing frame's state. So we serialize custom object using Json and pass that as the 
                // second param to avoid this crash.
                Frame.Navigate(typeof (PincodeDialog), PincodeOptions.ToJson(options));
            }
        }
    }
}