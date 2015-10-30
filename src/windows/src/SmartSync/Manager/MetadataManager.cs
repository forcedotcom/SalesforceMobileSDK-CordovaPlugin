using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Salesforce.SDK.Auth;
using Salesforce.SDK.Rest;
using Salesforce.SDK.SmartSync.Model;
using Salesforce.SDK.SmartSync.Util;

namespace Salesforce.SDK.SmartSync.Manager
{
    public class MetadataManager
    {
        private readonly string _apiVersion;
        private readonly string _communityId;
        private readonly CacheManager _cacheManager;
        private static volatile Dictionary<string, MetadataManager> _instances;
        private static readonly object Synclock = new Object();
       
        private MetadataManager(Account account, string communityId)
        {
            _apiVersion = ApiVersionStrings.VersionNumber;
            _communityId = communityId;
            _cacheManager = CacheManager.GetInstance(account, communityId);
        }

        /// <summary>
        ///     Returns the instance of this class associated with this user and community.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="communityId"></param>
        /// <returns></returns>
        public static MetadataManager GetInstance(Account account, string communityId = null)
        {
            if (account == null)
            {
                account = AccountManager.GetAccount();
            }
            if (account == null)
            {
                return null;
            }
            string uniqueId = Constants.GenerateAccountCommunityId(account, communityId);
            lock (Synclock)
            {
                MetadataManager instance = null;
                if (_instances != null)
                {
                    if (_instances.TryGetValue(uniqueId, out instance))
                    {
                        return instance;
                    }
                    instance = new MetadataManager(account, communityId);
                    _instances.Add(uniqueId, instance);
                }
                else
                {
                    _instances = new Dictionary<string, MetadataManager>();
                    instance = new MetadataManager(account, communityId);
                    _instances.Add(uniqueId, instance);
                }
                return instance;
            }
        }

        /// <summary>
        ///     Resets the Sync manager associated with this user and community.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="communityId"></param>
        public static void Reset(Account account, string communityId = null)
        {
            if (account == null)
            {
                account = AccountManager.GetAccount();
            }
            if (account != null)
            {
                lock (Synclock)
                {
                    MetadataManager instance = GetInstance(account, communityId);
                    if (instance == null) return;
                    _instances.Remove(Constants.GenerateAccountCommunityId(account, communityId));
                }
            }
        }
    }
}
