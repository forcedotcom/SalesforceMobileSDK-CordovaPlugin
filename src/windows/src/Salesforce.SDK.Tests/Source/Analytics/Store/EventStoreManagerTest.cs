/*
 * Copyright (c) 2016-present, salesforce.com, inc.
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Core;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Analytics.Manager;
using Salesforce.SDK.Analytics.Model;
using Salesforce.SDK.Analytics.Store;
using Salesforce.SDK.App;
using Salesforce.SDK.Core;
using Salesforce.SDK.Logging;
using Salesforce.SDK.Security;

namespace Tests.Source.Analytics.Store
{
    [TestClass]
    public class EventStoreManagerTest
    {
        private static DeviceAppAttributes TEST_DEVICE_APP_ATTRIBUTES = new DeviceAppAttributes("TEST_APP_VERSION",
            "TEST_APP_NAME", "TEST_OS_VERSION", "TEST_OS_NAME", "TEST_NATIVE_APP_TYPE",
            "TEST_MOBILE_SDK_VERSION", "TEST_DEVICE_MODEL", "TEST_DEVICE_ID", "TEST_CLIENT_ID");
        private const string TEST_EVENT_NAME = "TEST_EVENT_NAME_%s";
        private const string TEST_SENDER_ID = "TEST_SENDER_ID";
        private const string TEST_SESSION_ID = "TEST_SESSION_ID";
        private const string TEST_FILENAME_SUFFIX = "_test_filename_suffix";
        private static string uniqueId = Guid.NewGuid().ToString();
        private static string TEST_ENCRYPTION_KEY = Guid.NewGuid().ToString();

        IAnalyticsManager AnalyticsManager = new AnalyticsManager(uniqueId, TEST_DEVICE_APP_ATTRIBUTES, TEST_ENCRYPTION_KEY);
        private IEventStoreManager EventStoreManager = new EventStoreManager(TEST_FILENAME_SUFFIX, TEST_ENCRYPTION_KEY);

        [ClassInitialize]
        public static void SetupClass(TestContext context)
        {
            SFApplicationHelper.RegisterServices();
            SDKServiceLocator.RegisterService<ILoggingService, Salesforce.SDK.Hybrid.Logging.Logger>();
        }


        [TestInitialize]
        public void Setup()
        {
            var settings = new EncryptionSettings(new HmacSHA256KeyGenerator(HashAlgorithmNames.Sha256));
            Encryptor.init(settings);

        }

        [TestMethod]
        public async Task TestStoreOneEvent()
        {
            await EventStoreManager.DeleteAllEventsAsync().ConfigureAwait(false);
            InstrumentationEvent instrumentationEvent = createTestEvent();
            Assert.IsNotNull(instrumentationEvent, "Generated event stored should not be null");
            await EventStoreManager.StoreEventAsync(instrumentationEvent).ConfigureAwait(false);
            var events = await EventStoreManager.FetchAllEventsAsync();
            Assert.IsNotNull(events, "List of events stored should not be null");
            Assert.AreEqual(1, events.Count, "Number of events stored should be 1");
            Assert.IsTrue(instrumentationEvent.Equals(events.ElementAt(0)), "Stored event should be the same as generated event");
        }

        [TestMethod]
        public async Task TestSotreMultipleEvents()
        {
            await EventStoreManager.DeleteAllEventsAsync().ConfigureAwait(false);
            InstrumentationEvent instrumentationEvent1 = createTestEvent();
            Assert.IsNotNull(instrumentationEvent1, "Generated event stored should not be null");
            InstrumentationEvent instrumentationEvent2 = createTestEvent();
            Assert.IsNotNull(instrumentationEvent2, "Generated event stored should not be null");
            var generatedEvents = new List<InstrumentationEvent>();
            generatedEvents.Add(instrumentationEvent1);
            generatedEvents.Add(instrumentationEvent2);
            await EventStoreManager.StoreEventsAsync(generatedEvents);
            var storedEvents = await EventStoreManager.FetchAllEventsAsync();
            Assert.IsNotNull(storedEvents, "List of events stored should not be null");
            Assert.AreEqual(2, storedEvents.Count, "Number of events stored should be 2");
            Assert.IsTrue(storedEvents.Contains(instrumentationEvent1), "Stored event should be the same as generated event");
            Assert.IsTrue(storedEvents.Contains(instrumentationEvent2), "Stored event should be the same as generated event");
        }

        [TestMethod]
        public async Task TestFetchOneEvent()
        {
            await EventStoreManager.DeleteAllEventsAsync().ConfigureAwait(false);
            InstrumentationEvent instrumentationEvent = createTestEvent();
            Assert.IsNotNull(instrumentationEvent, "Generated event stored should not be null");
            await EventStoreManager.StoreEventAsync(instrumentationEvent).ConfigureAwait(false);
            var eventId = instrumentationEvent.EventId;
            var fetchedEvent = await EventStoreManager.FetchEventAsync(eventId);
            Assert.IsNotNull(fetchedEvent, "Event stored should not be null");
            Assert.IsTrue(instrumentationEvent.Equals(fetchedEvent), "Stored event should be the same as generated event");
        }

        [TestMethod]
        public async Task TestFetchAllEvents()
        {
            await EventStoreManager.DeleteAllEventsAsync().ConfigureAwait(false);
            var event1 = createTestEvent();
            Assert.IsNotNull(event1, "Generated event stored should not be null");
            await EventStoreManager.StoreEventAsync(event1);
            var event2 = createTestEvent();
            Assert.IsNotNull(event2, "Generated event stored should not be null");
            await EventStoreManager.StoreEventAsync(event2);
            var events = await EventStoreManager.FetchAllEventsAsync();
            Assert.IsNotNull(events, "List of events stored should not be null");
            Assert.AreEqual(2, events.Count, "Number of events stored should be 2");
            Assert.IsTrue(events.Contains(event1), "Stored event should be the same as generated event");
            Assert.IsTrue(events.Contains(event2), "Stored event should be the same as generated event");
        }

        [TestMethod]
        public async Task TestDeleteOneEvent()
        {
            await EventStoreManager.DeleteAllEventsAsync().ConfigureAwait(false);
            var instrumentationEvent = createTestEvent();
            Assert.IsNotNull(instrumentationEvent, "Generated event stored should not be null");
            var eventId = instrumentationEvent.EventId;
            await EventStoreManager.StoreEventAsync(instrumentationEvent);
            var eventsBeforeDelete = await EventStoreManager.FetchAllEventsAsync();
            Assert.IsNotNull(eventsBeforeDelete, "List of events stored should not be null");
            Assert.AreEqual(1, eventsBeforeDelete.Count, "Number of events stored should be 1");
            Assert.IsTrue(instrumentationEvent.Equals(eventsBeforeDelete.ElementAt(0)), "Stored event should be the same as generated event");
            await EventStoreManager.DeleteEventAsync(eventId);
            var eventsAfterDelete = await EventStoreManager.FetchAllEventsAsync();
            Assert.IsNotNull(eventsAfterDelete, "List of events stored should not be null");
            Assert.AreEqual(0, eventsAfterDelete.Count, "Number of events stored should be 0");
        }

        [TestMethod]
        public async Task TestDeleteMultipleEvents()
        {
            await EventStoreManager.DeleteAllEventsAsync().ConfigureAwait(false);
            var event1 = createTestEvent();
            Assert.IsNotNull(event1, "Generated event stored should not be null");
            var eventId1 = event1.EventId;
            var event2 = createTestEvent();
            Assert.IsNotNull(event2, "Generated event stored should not be null");
            var eventId2 = event2.EventId;
            var generatedEvents = new List<InstrumentationEvent>();
            generatedEvents.Add(event1);
            generatedEvents.Add(event2);
            await EventStoreManager.StoreEventsAsync(generatedEvents);
            var eventsBeforeDelete = await EventStoreManager.FetchAllEventsAsync();
            Assert.IsNotNull(eventsBeforeDelete, "List of events stored should not be null");
            Assert.AreEqual(2, eventsBeforeDelete.Count, "Number of events stored should be 2");
            Assert.IsTrue(eventsBeforeDelete.Contains(event1), "Stored event should be the same as generated event");
            Assert.IsTrue(eventsBeforeDelete.Contains(event2), "Stored event should be the same as generated event");
            var eventIds = new List<String>();
            eventIds.Add(eventId1);
            eventIds.Add(eventId2);
            await EventStoreManager.DeleteEventsAsync(eventIds);
            var eventsAfterDelete = await EventStoreManager.FetchAllEventsAsync();
            Assert.IsNotNull(eventsAfterDelete, "List of events stored should not be null");
            Assert.AreEqual(0, eventsAfterDelete.Count, "Number of events stored should be 0");
        }

        [TestMethod]
        public async Task TestDeleteAllEvents()
        {
            await EventStoreManager.DeleteAllEventsAsync().ConfigureAwait(false);
            var  event1 = createTestEvent();
            Assert.IsNotNull(event1, "Generated event stored should not be null");
            var event2 = createTestEvent();
            Assert.IsNotNull(event2, "Generated event stored should not be null");
            var generatedEvents = new List<InstrumentationEvent>();
            generatedEvents.Add(event1);
            generatedEvents.Add(event2);
            await EventStoreManager.StoreEventsAsync(generatedEvents);
            var  eventsBeforeDelete = await EventStoreManager.FetchAllEventsAsync();
            Assert.IsNotNull(eventsBeforeDelete, "List of events stored should not be null");
            Assert.AreEqual(2, eventsBeforeDelete.Count, "Number of events stored should be 2");
            Assert.IsTrue(eventsBeforeDelete.Contains(event1), "Stored event should be the same as generated event");
            Assert.IsTrue(eventsBeforeDelete.Contains(event2), "Stored event should be the same as generated event");
            await EventStoreManager.DeleteAllEventsAsync();
            var eventsAfterDelete = await EventStoreManager.FetchAllEventsAsync();
            Assert.IsNotNull(eventsAfterDelete, "List of events stored should not be null");
            Assert.AreEqual(0, eventsAfterDelete.Count, "Number of events stored should be 0");
        }

        [TestMethod]
        public async Task TestDisableLogging()
        {
            await EventStoreManager.DeleteAllEventsAsync().ConfigureAwait(false);
            var instrumentationEvent = createTestEvent();
            Assert.IsNotNull(instrumentationEvent, "Generated event stored should not be null");
            EventStoreManager.DisableEnableLogging(false);
            await EventStoreManager.StoreEventAsync(instrumentationEvent);
            var events = await EventStoreManager.FetchAllEventsAsync();
            Assert.IsNotNull(events, "List of events stored should not be null");
            Assert.AreEqual(0, events.Count, "Number of events stored should be 0");
        }

        [TestMethod]
        public async Task TestEnableLogging()
        {
            await EventStoreManager.DeleteAllEventsAsync().ConfigureAwait(false);
            var instrumentationEvent = createTestEvent();
            Assert.IsNotNull(instrumentationEvent, "Generated event stored should not be null");
            EventStoreManager.DisableEnableLogging(false);
            await EventStoreManager.StoreEventAsync(instrumentationEvent);
            var events = await EventStoreManager.FetchAllEventsAsync();
            Assert.IsNotNull(events, "List of events stored should not be null");
            Assert.AreEqual(0, events.Count, "Number of events stored should be 0");
            EventStoreManager.DisableEnableLogging(true);
            await EventStoreManager.StoreEventAsync(instrumentationEvent);
            events = await EventStoreManager.FetchAllEventsAsync();
            Assert.IsNotNull(events, "List of events stored should not be null");
            Assert.AreEqual(1, events.Count, "Number of events stored should be 1");
            Assert.IsTrue(instrumentationEvent.Equals(events.ElementAt(0)), "Stored event should be the same as generated event");
        }

        [TestMethod]
        public async Task TestEventLimitExceeded()
        {
            await EventStoreManager.DeleteAllEventsAsync().ConfigureAwait(false);
            var instrumentationEvent = createTestEvent();
            Assert.IsNotNull(instrumentationEvent, "Generated event stored should not be null");
            EventStoreManager.SetMaxEvents(0);
            await EventStoreManager.StoreEventAsync(instrumentationEvent);
            var events = await EventStoreManager.FetchAllEventsAsync();
            Assert.IsNotNull(events, "List of events stored should not be null");
            Assert.AreEqual(0, events.Count, "Number of events stored should be 0");
        }

        [TestMethod]
        public async Task TestEventLimitNotExceeded()
        {
            await EventStoreManager.DeleteAllEventsAsync().ConfigureAwait(false);
            var instrumentationEvent = createTestEvent();
            Assert.IsNotNull(instrumentationEvent, "Generated event stored should not be null");
            EventStoreManager.SetMaxEvents(0);
            await EventStoreManager.StoreEventAsync(instrumentationEvent);
            var events = await EventStoreManager.FetchAllEventsAsync();
            Assert.IsNotNull(events, "List of events stored should not be null");
            Assert.AreEqual(0, events.Count, "Number of events stored should be 0");
            EventStoreManager.SetMaxEvents(1);
            await EventStoreManager.StoreEventAsync(instrumentationEvent);
            events = await EventStoreManager.FetchAllEventsAsync();
            Assert.IsNotNull(events, "List of events stored should not be null");
            Assert.AreEqual(1, events.Count, "Number of events stored should be 1");
        }

        [TestMethod]
        private InstrumentationEvent createTestEvent()
        {
            var eventBuilder = new InstrumentationEventBuilder(AnalyticsManager);
            var curTime = System.DateTime.Now.Ticks;
            var eventName = String.Format(TEST_EVENT_NAME, curTime);
            eventBuilder.StartTime = curTime;
            eventBuilder.Name = eventName;
            eventBuilder.SessionId = TEST_SESSION_ID;
            eventBuilder.SenderId = TEST_SENDER_ID;
            eventBuilder.Page = new JObject();
            eventBuilder.EventType = InstrumentationEvent.EventType.system;
            eventBuilder.ErrorType = InstrumentationEvent.ErrorType.warning;
            eventBuilder.SchemaType = InstrumentationEvent.SchemaType.LightningError;
            return eventBuilder.BuildEvent();
        }
    }
}
