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
using Newtonsoft.Json.Linq;
using Salesforce.SDK.SmartStore.Store;
using Salesforce.SDK.SmartSync.Util;
using SQLitePCL;

namespace Salesforce.SDK.SmartSync.Model
{
    public class SyncState
    {
        public enum MergeModeOptions
        {
            None,
            Overwrite,
            LeaveIfChanged
        }

        public enum SyncStatusTypes
        {
            New,
            Running,
            Done,
            Failed
        }

        public enum SyncTypes
        {
            SyncDown,
            SyncUp
        }

        public long Id { private set; get; }
        public SyncTypes SyncType { private set; get; }
        public SyncTarget Target { private set; get; }
        public SyncOptions Options { private set; get; } // null for sync-down
        public String SoupName { private set; get; }
        public SyncStatusTypes Status { set; get; }
        public int Progress { set; get; }
        public int TotalSize { set; get; }
        public long MaxTimeStamp { set; get; }

        public MergeModeOptions MergeMode
        {
            get
            {
                return (Options != null && Options.MergeMode != MergeModeOptions.None ? Options.MergeMode : MergeModeOptions.None);
            }
        }

        /// <summary>
        ///     Create syncs soup if needed.
        /// </summary>
        /// <param name="store"></param>
        public static void SetupSyncsSoupIfNeeded(SmartStore.Store.SmartStore store)
        {
            if (store.HasSoup(Constants.SyncsSoup))
            {
                return;
            }
            IndexSpec[] indexSpecs = {new IndexSpec(Constants.SyncType, SmartStoreType.SmartString)};
            store.RegisterSoup(Constants.SyncsSoup, indexSpecs);
        }

        /// <summary>
        ///     Create sync state in database for a sync down and return corresponding SyncState
        /// </summary>
        /// <param name="store"></param>
        /// <param name="target"></param>
        /// <param name="options"></param>
        /// <param name="soupName"></param>
        /// <returns></returns>
        public static SyncState CreateSyncDown(SmartStore.Store.SmartStore store, SyncDownTarget target, string soupName, SyncOptions options = null)
        {
            var sync = new JObject
            {
                {Constants.SyncType, SyncTypes.SyncDown.ToString()},
                {Constants.SyncTarget, target.AsJson()},
                {Constants.SyncSoupName, soupName},
                {Constants.SyncStatus, SyncStatusTypes.New.ToString()},
                {Constants.SyncProgress, 0},
                {Constants.SyncTotalSize, -1},
                {Constants.SyncMaxTimeStamp, -1},
            };
            if (options != null)
            {
                sync[Constants.SyncOptions] = options.AsJson();
            }
            JObject upserted = store.Upsert(Constants.SyncsSoup, sync);
            if (upserted != null)
            {
                return FromJson(upserted);
            }
            sync[Constants.SyncStatus] = SyncStatusTypes.Failed.ToString();
            return FromJson(sync);
        }

        /// <summary>
        ///     Create sync state in database for a sync up and return corresponding SyncState
        /// </summary>
        /// <param name="store"></param>
        /// <param name="target"></param>
        /// <param name="options"></param>
        /// <param name="soupName"></param>
        /// <returns></returns>
        public static SyncState CreateSyncUp(SmartStore.Store.SmartStore store, SyncUpTarget target, SyncOptions options, string soupName)
        {
            var sync = new JObject
            {
                {Constants.SyncType, SyncTypes.SyncUp.ToString()},
                {Constants.SyncSoupName, soupName},
                {Constants.SyncOptions, options.AsJson()},
                {Constants.SyncStatus, SyncStatusTypes.New.ToString()},
                {Constants.SyncProgress, 0},
                {Constants.SyncTotalSize, -1},
                {Constants.SyncMaxTimeStamp, -1},
                {Constants.SyncTarget, target.AsJson()}
            };
            JObject upserted = store.Upsert(Constants.SyncsSoup, sync);
            if (upserted != null)
            {
                return FromJson(upserted);
            }
            sync[Constants.SyncStatus] = SyncStatusTypes.Failed.ToString();
            return FromJson(sync);
        }

        /// <summary>
        ///     Build SyncState from json
        /// </summary>
        /// <param name="sync"></param>
        /// <returns></returns>
        public static SyncState FromJson(JObject sync)
        {
            if (sync == null) return null;
            var jsonTarget = sync.ExtractValue<JObject>(Constants.SyncTarget);
            var syncType = (SyncTypes)Enum.Parse(typeof(SyncTypes), sync.ExtractValue<string>(Constants.SyncType));
            var state = new SyncState
            {
                Id = sync.ExtractValue<long>(SmartStore.Store.SmartStore.SoupEntryId),
                Target = (syncType == SyncTypes.SyncDown ? (SyncTarget) SyncDownTarget.FromJson(jsonTarget) : SyncUpTarget.FromJSON(jsonTarget)),
                Options = SyncOptions.FromJson(sync.ExtractValue<JObject>(Constants.SyncOptions)),
                SoupName = sync.ExtractValue<string>(Constants.SyncSoupName),
                Progress = sync.ExtractValue<int>(Constants.SyncProgress),
                TotalSize = sync.ExtractValue<int>(Constants.SyncTotalSize),
                SyncType = syncType,
                Status =
                    (SyncStatusTypes)
                        Enum.Parse(typeof (SyncStatusTypes), sync.ExtractValue<string>(Constants.SyncStatus)),
                MaxTimeStamp = sync.ExtractValue<long>(Constants.SyncMaxTimeStamp)
            };
            return state;
        }

        /// <summary>
        ///     Build SyncState from store sync given by id
        /// </summary>
        /// <param name="store"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static SyncState ById(SmartStore.Store.SmartStore store, long id)
        {
            JArray syncs = store.Retrieve(Constants.SyncsSoup, id);
            if (syncs == null || syncs.Count == 0)
            {
                return null;
            }
            return FromJson(syncs[0] as JObject);
        }

        /// <summary>
        ///     json representation of sync
        /// </summary>
        /// <returns></returns>
        public JObject AsJson()
        {
            var sync = new JObject
            {
                {SmartStore.Store.SmartStore.SoupEntryId, Id},
                {Constants.SyncType, SyncType.ToString()},
                {Constants.SyncSoupName, SoupName},
                {Constants.SyncStatus, Status.ToString()},
                {Constants.SyncProgress, Progress},
                {Constants.SyncTotalSize, TotalSize},
                {Constants.SyncMaxTimeStamp, MaxTimeStamp}
            };
            if (Target != null) sync[Constants.SyncTarget] = Target.AsJson();
            if (Options != null) sync[Constants.SyncOptions] = Options.AsJson();
            return sync;
        }

        /// <summary>
        ///     Save SyncState to db
        /// </summary>
        /// <param name="store"></param>
        public void Save(SmartStore.Store.SmartStore store)
        {
            try
            {
                store.Update(Constants.SyncsSoup, AsJson(), Id, false);
            }
            catch (SQLiteException sqe)
            {
                var se = new SmartStoreException("SqliteError occurred ", sqe);
                throw se;
            }
        }
    }
}