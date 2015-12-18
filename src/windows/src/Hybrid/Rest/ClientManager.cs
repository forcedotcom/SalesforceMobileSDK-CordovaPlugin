using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Salesforce.SDK.Hybrid.Rest
{
    public sealed class ClientManager
    {
        private SDK.Rest.ClientManager _manager;

        public ClientManager()
        {
            _manager = new SDK.Rest.ClientManager();
        }

        public IAsyncOperation<bool> Logout()
        {
            Task<bool> response = _manager.Logout();
            return response.AsAsyncOperation<bool>();
        }

        /// <summary>
        ///     Returns a RestClient if user is already authenticated or null if not.
        /// </summary>
        /// <returns></returns>
        public RestClient PeekRestClient()
        {
            var client = _manager.PeekRestClient();
            return new RestClient(client);
        }
    }
}
