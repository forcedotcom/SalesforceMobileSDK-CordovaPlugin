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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Salesforce.SDK.Auth;
using System.Threading.Tasks;
using Salesforce.SDK.Core;
using Salesforce.SDK.Security;
using Salesforce.SDK.Settings;

namespace Salesforce.SDK.App
{
    public sealed class SFApplicationHelper : ISFApplicationHelper
    {
        public static void RegisterServices()
        {
            SDKServiceLocator.RegisterService<ISFApplicationHelper, SFApplicationHelper>();
            SDKServiceLocator.RegisterService<IAuthHelper, AuthHelper>();
            SDKServiceLocator.RegisterService<IEncryptionService, Encryptor>();
            SDKServiceLocator.RegisterService<IApplicationInformationService, ApplicationService>();
        }
        public void Initialize()
        {
           // nothing to do here
        }

        private Frame CreateRootFrame()
        {
            var rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                // TODO: change this value to a cache size that is appropriate for your application
                rootFrame.CacheSize = 1;

                SuspensionManager.RegisterFrame(rootFrame, "AppFrame");

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            return rootFrame;
        }

        private async Task RestoreStatus(ApplicationExecutionState previousExecutionState)
        {
            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (previousExecutionState == ApplicationExecutionState.Terminated)
            {
                // Restore the saved session state only when appropriate
                try
                {
                    await SuspensionManager.RestoreAsync();
                }
                catch (SuspensionManagerException)
                {
                    //Something went wrong restoring state.
                    //Assume there is no state and continue
                }
            }
        }

        /// <summary>
        ///     Invoked when the application is launched normally by the end user.  Other entry points
        ///     will be used when the application is launched to open a specific file, to display
        ///     search results, and so forth.
        /// </summary>
        /// <remarks>This method ensures that Window.Current.Content is set to a valid Frame.</remarks>
        /// <remarks>This method calls Window.Current.Activate.</remarks>
        /// <param name="e">Details about the launch request and process.</param>
        public async void OnLaunched(LaunchActivatedEventArgs e)
        {
            var rootFrame = CreateRootFrame();
            await RestoreStatus(e.PreviousExecutionState);

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (!rootFrame.Navigate(SDKManager.RootApplicationPage, e.Arguments))
                {
                    throw new Exception("Failed to create initial page");
                }
            }

            // Ensure the current window is active
            Window.Current.Activate();
        }

        public async void OnActivated(IActivatedEventArgs args)
        {
            CreateRootFrame();
            await RestoreStatus(args.PreviousExecutionState);

            PincodeManager.TriggerBackgroundedPinTimer();
        }

        public async void OnSuspending(SuspendingEventArgs e)
        {
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();
            PincodeManager.SavePinTimer();
            await SuspensionManager.SaveAsync();
            deferral.Complete();
        }
    }
}