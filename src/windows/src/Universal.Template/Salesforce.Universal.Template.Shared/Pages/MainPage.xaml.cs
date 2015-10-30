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
using Salesforce.SDK.App;
using Salesforce.SDK.Auth;
using Salesforce.SDK.Native;
using Salesforce.SDK.Rest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Exceptions;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace $safeprojectname$.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : NativeMainPage
    {
        private ObservableCollection<Contact> _contacts = new ObservableCollection<Contact>();
        
        public ObservableCollection<Contact> Contacts
        {
            get { return this._contacts; }
        }

        public MainPage() : base()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            var color = new Config().LoginBackgroundColor;
            if (color != null)
            {
                grid.Background = new SolidColorBrush(color.Value);
            }
            Account account = AccountManager.GetAccount();
            if (account != null)
            {
                try
                {
                    account = await OAuth2.RefreshAuthToken(account);
                    _contacts = await SendRequest("SELECT Name FROM Contact");
                    contactList.DataContext = Contacts;
                }
                catch (OAuthException ex)
                {
                    SDKManager.GlobalClientManager.Logout();
                }

            }
            else
            {
                base.OnNavigatedTo(e);
            }
        }
        
        private void SwitchAccount(object sender, RoutedEventArgs e)
        {
            AccountManager.SwitchAccount();
        }

        private async void Logout(object sender, RoutedEventArgs e)
        {

            if (SDKManager.GlobalClientManager != null)
            {
                await SDKManager.GlobalClientManager.Logout();
            }
            AccountManager.SwitchAccount();
        }

        private async Task<ObservableCollection<Contact>> SendRequest(string soql)
        {
            var restRequest = RestRequest.GetRequestForQuery(ApiVersionStrings.VersionNumber, soql);
            var client = SDKManager.GlobalClientManager.GetRestClient() ??
                         new RestClient(AccountManager.GetAccount().InstanceUrl);
            var response = await client.SendAsync(restRequest);
            if (!response.Success)
            {
                return null;
            }
            var records = response.AsJObject.GetValue("records").ToObject<JArray>();
            foreach (var item in records)
            {
                _contacts.Add(new Contact { Name = item.Value<string>("Name") });
            }

            return _contacts;
        }
    }

    public class Contact
    {
        public string Name { get; set; }
    }
}
