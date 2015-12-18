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
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Salesforce.SDK.Auth;
using Salesforce.SDK.Exceptions;
using Salesforce.SDK.Logging;
using Salesforce.SDK.Core;
using System.Threading.Tasks;

namespace Salesforce.SDK.App
{
    /// <summary>
    ///     Abstract application class used to provide access to functions in the SalesforceSDK.  use this for your main
    ///     App.xaml to allow support for the
    ///     SDK, an entry point for handling oauth and account switching, and providing a client manager that can be used
    ///     across the app in a central location.
    /// </summary>
    public abstract class SalesforceApplication : Application
    {
        private static DispatcherTimer TokenRefresher = new DispatcherTimer();
        private static ISFApplicationHelper AppHelper = SDKServiceLocator.Get<ISFApplicationHelper>();
        private static ILoggingService LoggingService => SDKServiceLocator.Get<ILoggingService>();

        /// <summary>
        /// Refresh interval for token refresh. Value is in minutes.
        /// </summary>
        public const int TokenRefreshInterval = 3;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        protected SalesforceApplication()
        {
            SFApplicationHelper.RegisterServices();
            Suspending += OnSuspending;
            InitializeConfig();
            SDKManager.CreateClientManager(false);
            SDKManager.RootApplicationPage = SetRootApplicationPage();
            TokenRefresher = new DispatcherTimer { Interval = TimeSpan.FromMinutes(TokenRefreshInterval) };
            TokenRefresher.Tick += RefreshToken;
            AppHelper.Initialize();
        }



        /// <summary>
        ///     Use this to initialize your custom SalesforceConfig source, and to set up the Encryptor to use your own app
        ///     specific, unique salt, password, and key generator.
        ///     An example of code that may go into this method would be as follows:
        ///     protected override void InitializeConfig()
        ///     {
        ///     new Config();
        ///     EncryptionSettings settings = new EncryptionSettings(new HmacSHA256KeyGenerator())
        ///     {
        ///     Password = "mypassword",
        ///     Salt = "mysalt"
        ///     };
        ///     Encryptor.init(settings);
        ///     }
        /// </summary>
        protected abstract Task InitializeConfig();

        /// <summary>
        ///     Implement to return the type of the root page to switch to once oauth completes.
        /// </summary>
        /// <returns>Type of the root page</returns>
        protected abstract Type SetRootApplicationPage();

        protected override void OnWindowCreated(WindowCreatedEventArgs args)
        {
            base.OnWindowCreated(args);
            SetupVisibilityHandler();
        }

        private async void SetupVisibilityHandler()
        {
            CoreWindow core = CoreWindow.GetForCurrentThread();
            await core.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                CoreWindow coreWindow = CoreApplication.MainView.CoreWindow;
                coreWindow.VisibilityChanged += CoreVisibilityChanged;
                coreWindow.PointerMoved += coreWindow_PointerMoved;
            });
        }

        private void coreWindow_PointerMoved(CoreWindow sender, PointerEventArgs args)
        {
            PincodeManager.StartIdleTimer();
        }

        private void CoreVisibilityChanged(CoreWindow sender, VisibilityChangedEventArgs args)
        {
            if (args.Visible)
            {
                PincodeManager.TriggerBackgroundedPinTimer();
                TokenRefresher.Start();
            }
            else
            {
                PincodeManager.SavePinTimer();
                TokenRefresher.Stop();
            }
        }

        private async void OnNavigationFailed(object sender, NavigationFailedEventArgs args)
        {
            if (SDKManager.GlobalClientManager != null)
            {
                await SDKManager.GlobalClientManager.Logout();
            }
        }

        private async void RefreshToken(object sender, object e)
        {
            try
            {
                //Assign this to a var as ref requires it.
                var account = AccountManager.GetAccount();
                await OAuth2.RefreshAuthTokenAsync(account);
                SDKServiceLocator.Get<IAuthHelper>().RefreshCookies();
            }
            catch (OAuthException ex)
            {
                LoggingService.Log("Error occured when refreshing token", LoggingLevel.Critical);
                LoggingService.Log(ex, LoggingLevel.Critical);
            }
        }

        protected void OnSuspending(object sender, SuspendingEventArgs e)
        {
            AppHelper.OnSuspending(e);
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            base.OnActivated(args);
            AppHelper.OnActivated(args);
            var rootFrame = Window.Current.Content as Frame;
            if (rootFrame != null)
            {
                rootFrame.NavigationFailed += OnNavigationFailed;
            }
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            base.OnLaunched(e);
            AppHelper.OnLaunched(e);
        }
    }
}
