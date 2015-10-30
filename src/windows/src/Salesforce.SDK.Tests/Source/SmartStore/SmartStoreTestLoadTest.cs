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
using System.Diagnostics;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.App;
using Salesforce.SDK.Core;
using Salesforce.SDK.Logging;

namespace Salesforce.SDK.SmartStore.Store
{
    [TestClass]
    public class SmartStoreTestLoadTest
    {
        private static readonly int MAX_NUMBER_ENTRIES = 2048;
        private static readonly int NUMBER_FIELDS_PER_ENTRY = 128;
        private static readonly int NUMBER_ENTRIES_PER_BATCH = 64;
        private static readonly int QUERY_PAGE_SIZE = 64;
        private static readonly string TEST_SOUP = "test_soup";
        private static SmartStore Store;

        [ClassInitialize]
        public static void TestSetup(TestContext context)
        {
            SFApplicationHelper.RegisterServices();
            SDKServiceLocator.RegisterService<ILoggingService, Hybrid.Logging.Logger>();
            Store = SmartStore.GetGlobalSmartStore();
            Store.ResetDatabase();
            Store.RegisterSoup(TEST_SOUP, new[] {new IndexSpec("key", SmartStoreType.SmartString)});
        }

        [TestMethod]
        public void TestUpsertManyEntries()
        {
            Debug.WriteLine("SmartStoreLoadTest", "In testUpsertManyEntries");
            for (int k = 1; k < MAX_NUMBER_ENTRIES; k *= 2)
            {
                UpsertManyEntriesWithManyFields(k);
            }
            JArray result =
                Store.Query(
                    QuerySpec.BuildSmartQuerySpec(
                        "select {test_soup:key} from {" + TEST_SOUP + "} WHERE {test_soup:key} = 'k_100'", 1), 0);
            JArray compare = JArray.Parse("[[\"k_100\"]]");
            Assert.AreEqual(compare.ToString(), result.ToString());
        }

        [TestMethod]
        public void TestAddAndRetrieveManyEntries()
        {
            Debug.WriteLine("SmartStoreLoadTest: In testAddAndRetrieveManyEntries");
            long start = SmartStore.CurrentTimeMillis;
            long end;
            var soupEntryIds = new List<long>();
            var times = new List<long>();
            JObject firstEntry = null;
            JObject lastEntry = null;
            JObject upsertedEntry = null;
            for (int i = 0; i < MAX_NUMBER_ENTRIES; i++)
            {
                String paddedIndex = i.ToString("D4");
                var entry = new JObject();
                entry.Add("Name", "Todd Stellanova" + paddedIndex);
                entry.Add("Id", "003" + paddedIndex);
                var attributes = new JObject();
                attributes.Add("type", "Contact");
                attributes.Add("url", "/foo/Contact" + paddedIndex);
                entry.Add("attributes", attributes);

                start = SmartStore.CurrentTimeMillis;
                upsertedEntry = Store.Upsert(TEST_SOUP, entry);
                if (firstEntry == null)
                {
                    firstEntry = upsertedEntry;
                }
                var soupEntryId = upsertedEntry.GetValue(SmartStore.SoupEntryId).Value<long>();
                soupEntryIds.Add(soupEntryId);
                end = SmartStore.CurrentTimeMillis;
                times.Add(end - start);
            }
            lastEntry = upsertedEntry;
            // Compute average time taken
            long avg = 0;
            for (int i = 0; i < times.Count; i++)
            {
                avg += times[i];
            }
            avg /= times.Count;

            // Log avg time taken
            Debug.WriteLine("SmartStoreLoadTest",
                "upserting " + MAX_NUMBER_ENTRIES + " entries avg time taken: " + avg + " ms");

            // Retrieve
            start = SmartStore.CurrentTimeMillis;
            JArray retrieved = Store.Retrieve(TEST_SOUP, soupEntryIds.ToArray());
            end = SmartStore.CurrentTimeMillis;

            Assert.AreEqual(retrieved.Count, MAX_NUMBER_ENTRIES);
            Assert.AreEqual(firstEntry.ToString(), retrieved[0].ToString());
            Assert.AreEqual(lastEntry.ToString(), retrieved[MAX_NUMBER_ENTRIES - 1].ToString());
            // Log retrieve time taken
            Debug.WriteLine("SmartStoreLoadTest",
                "retrieve " + MAX_NUMBER_ENTRIES + " entries time taken: " + (end - start) + " ms");
        }

        private static void UpsertManyEntriesWithManyFields(int batch)
        {
            int startKey = batch*NUMBER_ENTRIES_PER_BATCH;
            int endKey = (batch + 1)*NUMBER_ENTRIES_PER_BATCH;

            var times = new List<long>();
            Store.Database.BeginTransaction();
            for (int i = startKey; i < endKey; i++)
            {
                var entry = new JObject();
                entry.Add("key", "k_" + i);
                entry.Add("value", "x");
                for (int j = 0; j < NUMBER_FIELDS_PER_ENTRY; j++)
                {
                    entry.Add("v" + j, "value_" + j);
                }
                long start = SmartStore.CurrentTimeMillis;
                Store.Upsert(TEST_SOUP, entry);
                long end = SmartStore.CurrentTimeMillis;
                times.Add(end - start);
            }

            Store.Database.CommitTransaction();
            long avg = 0;
            for (int i = 0; i < times.Count; i++)
            {
                avg += times[i];
            }

            avg /= times.Count;
            Debug.WriteLine("SmartStoreLoadTest: upserting " + NUMBER_ENTRIES_PER_BATCH + " entries avg time taken: " +
                            avg + " ms");
        }

        private static void QueryEntries(int batch)
        {
            QuerySpec qs = QuerySpec.BuildAllQuerySpec(TEST_SOUP, "key", QuerySpec.SqlOrder.ASC, QUERY_PAGE_SIZE);
            long start = SmartStore.CurrentTimeMillis;
            Store.Query(qs, 0);
            long end = SmartStore.CurrentTimeMillis;

            // Log query time
            Debug.WriteLine("SmartStoreLoadTest",
                "querying out of soup with " + (batch + 1)*NUMBER_ENTRIES_PER_BATCH + " entries time taken: " +
                (end - start) + " ms");
        }
    }
}