using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Newtonsoft.Json;

namespace Salesforce.SDK.Hybrid.Auth
{
    public sealed class OAuth2
    {
        public static IAsyncOperation<Account> RefreshAuthToken(Account account)
        {
            return Task.Run(async () =>
            {
                var response =
                    await SDK.Auth.OAuth2.RefreshAuthTokenAsync(account.ConvertToSDKAccount());
                return Account.FromJson(SDK.Auth.Account.ToJson(response));
            }).AsAsyncOperation<Account>();
        }

        public static IAsyncOperation<bool> RevokeAuthToken(LoginOptions loginOptions, string refreshToken)
        {
            return Task.Run(async () => await SDK.Auth.OAuth2.RevokeAuthTokenAsync(loginOptions.ConvertToSDKLoginOptions(), refreshToken)).AsAsyncOperation<bool>();
        }

        public static string ComputeAuthorizationUrl(LoginOptions options)
        {
            return SDK.Auth.OAuth2.ComputeAuthorizationUrl(options.ConvertToSDKLoginOptions());
        }

        public static string ComputeFrontDoorUrl(string instanceUrl, string accessToken, string url)
        {
            return SDK.Auth.OAuth2.ComputeFrontDoorUrl(instanceUrl, accessToken, url);
        }

        public static string ComputeFrontDoorUrl(string instanceUrl, string displayType, string accessToken, string url)
        {
            return SDK.Auth.OAuth2.ComputeFrontDoorUrl(instanceUrl, displayType, accessToken, url);
        }

        public static AuthResponse ParseFragment(string fragmentstring)
        {
            var response = JsonConvert.SerializeObject(SDK.Auth.OAuth2.ParseFragment(fragmentstring));
            return JsonConvert.DeserializeObject<AuthResponse>(response);
        }
    }
}
