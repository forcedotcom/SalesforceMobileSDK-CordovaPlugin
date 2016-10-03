using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
using Windows.UI.Xaml.Navigation;
using Salesforce.SDK.App;
using Salesforce.SDK.Auth;
using Salesforce.SDK.Core;
using __NativeTemplateAppName__.Logging;
using Salesforce.SDK.Logging;
using Salesforce.SDK.Security;
using __NativeTemplateAppName__;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Salesforce.SDK.Settings;

namespace __NativeTemplateAppName__
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>

    sealed partial class App : SalesforceApplication
    {
        /// <summary>
        ///     Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        ///     InitializeConfig should implement the commented out code. You should come up with your own, unique password and
        ///     salt and for added security
        ///     you should implement your own key generator using the IKeyGenerator interface.
        /// </summary>
        /// <returns></returns>
        protected override Task InitializeConfig()
        {
            SDKServiceLocator.RegisterService<IEncryptionService, Encryptor>();
            Encryptor.init(new EncryptionSettings(new HmacSHA256KeyGenerator(HashAlgorithmNames.Sha256)));
            var config = SDKManager.InitializeConfigAsync<Config>().Result;
            return config.SaveConfigAsync();
        }

        protected override Task UpgradeConfigAsync()
        {
            if (!ApplicationData.Current.Version.Equals(0)) return Task.CompletedTask;
            var config = SalesforceConfig.RetrieveConfig<Config>().Result;
            if (config == null) return Task.CompletedTask;
            Encryptor.init(new EncryptionSettings(new HmacSHA256KeyGenerator(HashAlgorithmNames.Md5)));
            config = SDKManager.InitializeConfigAsync<Config>().Result;
            Encryptor.ChangeSettings(
                new EncryptionSettings(new HmacSHA256KeyGenerator(HashAlgorithmNames.Sha256)));
            return config.SaveConfigAsync();
        }

        /// <summary>
        ///     This returns the root application of your application. Please adjust to match your actual root page if you use
        ///     something different.
        /// </summary>
        /// <returns></returns>
        protected override Type SetRootApplicationPage()
        {
            return typeof(MainPage);
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            base.OnActivated(args);
        }
    }
}
