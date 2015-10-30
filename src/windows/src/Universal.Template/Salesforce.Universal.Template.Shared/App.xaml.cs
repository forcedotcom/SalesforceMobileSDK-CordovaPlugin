using Salesforce.SDK.App;
using Salesforce.SDK.Source.Settings;
using $safeprojectname$.Pages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Salesforce.SDK.Source.Security;
using Salesforce.SDK.Auth;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=234227

namespace $safeprojectname$
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : SalesforceApplication
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
            : base()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// If you wish to roll your own EncryptionSettings, this would be the place to initialize it. 
        /// </summary>
        /// <returns></returns>
        protected override void InitializeConfig()
        {
            var config = SDKManager.InitializeConfig<Config>(new EncryptionSettings(new HmacSHA256KeyGenerator()));
            config.SaveConfig();
        }

        /// <summary>
        /// This returns the root application of your application. Please adjust to match your actual root page if you use something different.
        /// </summary>
        /// <returns></returns>
        protected override Type SetRootApplicationPage()
        {
            return typeof(MainPage);
        }
    }
}