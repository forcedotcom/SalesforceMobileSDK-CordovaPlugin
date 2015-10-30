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
using System.Collections.Generic;
using System.Text;
using Salesforce.SDK.Auth;

namespace Salesforce.SDK.SmartStore.Store
{
    public class DBOpenHelper
    {
        public static readonly int DBVersion = 1;
        public static readonly string DBName = "smartstore{0}.db";
        public static readonly string DBNameSuffix = ".db";

        private static Dictionary<string, DBOpenHelper> _openHelpers;
        private static DBOpenHelper _defaultHelper;
        private static readonly object Dbopenlock = new Object();

        private DBOpenHelper(string dbName)
        {
            DatabaseFile = dbName;
        }

        public string DatabaseFile { private set; get; }

        public static DBOpenHelper GetOpenHelper(Account account)
        {
            lock (Dbopenlock)
            {
                return GetOpenHelper(account, null);
            }
        }

        public static DBOpenHelper GetOpenHelper(Account account, string communityId)
        {
            string dbName = String.Format(DBName, "");

            if (account == null) return _defaultHelper ?? (_defaultHelper = new DBOpenHelper(dbName));
            var uniqueId = account.UserId;
            DBOpenHelper helper = null;
            if (_openHelpers == null)
            {
                _openHelpers = new Dictionary<string, DBOpenHelper>();
                helper = new DBOpenHelper(String.Format(DBName, uniqueId));
                _openHelpers.Add(uniqueId, helper);
            }
            else
            {
                if (!_openHelpers.TryGetValue(uniqueId, out helper))
                {
                    helper = new DBOpenHelper(dbName);
                }
            }
            return helper;
        }

        public static DBOpenHelper GetOpenHelper(string dbNamePrefix, Account account, string communityId)
        {
            var dbName = new StringBuilder(dbNamePrefix);

		    // If we have account information, we will use it to create a database suffix for the user.
		    if (account != null)
            {

			    // Default user path for a user is 'internal', if community ID is null.
		        if (String.IsNullOrWhiteSpace(communityId))
		        {
                    dbName.Append(communityId);
		        }
		    }
		    dbName.Append(DBNameSuffix);
		    DBOpenHelper helper = null;
            if (_openHelpers == null)
            {
                _openHelpers = new Dictionary<string, DBOpenHelper>();
                helper = new DBOpenHelper(dbName.ToString());
                _openHelpers.Add(dbName.ToString(), helper);
            }
            else
            {
                if (!_openHelpers.TryGetValue(dbName.ToString(), out helper))
                {
                    helper = new DBOpenHelper(dbName.ToString());
                }
            }
            return helper;
        }

    }
}