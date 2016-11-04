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
using Salesforce.SDK.App;
using Salesforce.SDK.Core;
using Salesforce.SDK.Logging;
using Salesforce.SDK.Security;

namespace Salesforce.SDK.Analytics.Model
{
    [TestClass]
    public class InstrumentationEventBuilderTest
    {
        
        private const string TEST_EVENT_NAME = "TEST_EVENT_NAME_%s";
        private const string TEST_SENDER_ID = "TEST_SENDER_ID";
        private const string TEST_SESSION_ID = "TEST_SESSION_ID";
        private static string TEST_ENCRYPTION_KEY = Guid.NewGuid().ToString();

        private static DeviceAppAttributes TEST_DEVICE_APP_ATTRIBUTES = new DeviceAppAttributes("TEST_APP_VERSION",
            "TEST_APP_NAME", "TEST_OS_VERSION", "TEST_OS_NAME", "TEST_NATIVE_APP_TYPE",
            "TEST_MOBILE_SDK_VERSION", "TEST_DEVICE_MODEL", "TEST_DEVICE_ID", "TEST_CLIENT_ID");
        private static IEncryptionService EncryptionService => SDKServiceLocator.Get<IEncryptionService>();
        private static ILoggingService LoggingService => SDKServiceLocator.Get<ILoggingService>();
        private static string uniqueId = Guid.NewGuid().ToString();
        IAnalyticsManager AnalyticsManager = new AnalyticsManager(uniqueId, TEST_DEVICE_APP_ATTRIBUTES, TEST_ENCRYPTION_KEY);

        [ClassInitialize]
        public static void SetupClass(TestContext context)
        {
            SFApplicationHelper.RegisterServices();
            SDKServiceLocator.RegisterService<ILoggingService, Hybrid.Logging.Logger>();

        }

        [TestInitialize]
        public void Setup()
        {
            var settings = new EncryptionSettings(new HmacSHA256KeyGenerator(HashAlgorithmNames.Sha256));
            Encryptor.init(settings);
        }

        /// <summary>
        /// Test for missing mandatory field 'schema type'
        /// </summary>
        [TestMethod]
        public void TestMissingSchemaType()
        {
            var eventBuilder = new InstrumentationEventBuilder(AnalyticsManager);
            var curTime = System.DateTime.Now.Ticks;
            var eventName = string.Format(TEST_EVENT_NAME, curTime);
            eventBuilder.StartTime = curTime;
            eventBuilder.Name = eventName;
            eventBuilder.SessionId = TEST_SESSION_ID;
            eventBuilder.SenderId = TEST_SENDER_ID;
            eventBuilder.Page = new JObject();
            eventBuilder.EventType = InstrumentationEvent.EventType.system;
            eventBuilder.ErrorType = InstrumentationEvent.ErrorType.warning;
            eventBuilder.BuildEvent();
            Assert.AreEqual("LightningInteraction", eventBuilder.SchemaType.ToString());
        }

        [TestMethod]
        public void TestMissingEventTypeInInteraction()
        {
            var eventBuilder = new InstrumentationEventBuilder(AnalyticsManager);
            var curTime = System.DateTime.Now.Ticks;
            var eventName = string.Format(TEST_EVENT_NAME, curTime);
            eventBuilder.SchemaType = InstrumentationEvent.SchemaType.LightningInteraction;
            eventBuilder.StartTime = curTime;
            eventBuilder.Name = eventName;
            eventBuilder.SessionId = TEST_SESSION_ID;
            eventBuilder.Page = new JObject();
            eventBuilder.SenderId = TEST_SENDER_ID;
            eventBuilder.BuildEvent();
            Assert.AreEqual("user", eventBuilder.EventType.ToString());
        }

        [TestMethod]
        public void TestMissingEventTypeInError()
        {
            var eventBuilder = new InstrumentationEventBuilder(AnalyticsManager);
            var curTime = System.DateTime.Now.Ticks;
            var eventName = string.Format(TEST_EVENT_NAME, curTime);
            eventBuilder.SchemaType = InstrumentationEvent.SchemaType.LightningError;
            eventBuilder.StartTime = curTime;
            eventBuilder.Name = eventName;
            eventBuilder.SessionId = TEST_SESSION_ID;
            eventBuilder.Page = new JObject();
            eventBuilder.SenderId = TEST_SENDER_ID;
            InstrumentationEvent instrumentationEvent = null;
            try
            {
                instrumentationEvent = eventBuilder.BuildEvent();
            }
            catch (EventBuilderException )
            {
                Assert.Fail("Exception should not have been thrown");
            }
            Assert.IsNotNull(instrumentationEvent, "Event should not be null");
        }

        [TestMethod]
        public void TestMissingPage()
        {
            var eventBuilder = new InstrumentationEventBuilder(AnalyticsManager);
            var curTime = System.DateTime.Now.Ticks;
            var eventName = string.Format(TEST_EVENT_NAME, curTime);
            eventBuilder.Name = eventName;
            eventBuilder.SessionId = TEST_SESSION_ID;
            eventBuilder.SchemaType = InstrumentationEvent.SchemaType.LightningError;
            eventBuilder.StartTime = curTime;
            eventBuilder.SenderId = TEST_SENDER_ID;
            eventBuilder.EventType = InstrumentationEvent.EventType.system;
            eventBuilder.ErrorType = InstrumentationEvent.ErrorType.warning;
            try
            {
                eventBuilder.BuildEvent();
                Assert.Fail("Exception should have been thrown for missing mandatory field 'page'");
            }
            catch (EventBuilderException)
            {
                LoggingService.Log("Exception thrown as expected", LoggingLevel.Information);
            }
        }

        [TestMethod]
        public void TestMissingName()
        {
            var eventBuilder = new InstrumentationEventBuilder(AnalyticsManager);
            var curTime = System.DateTime.Now.Ticks;
            eventBuilder.SchemaType = InstrumentationEvent.SchemaType.LightningError;
            eventBuilder.StartTime = curTime;
            eventBuilder.SessionId = TEST_SESSION_ID;
            eventBuilder.SenderId = TEST_SENDER_ID;
            eventBuilder.EventType = InstrumentationEvent.EventType.system;
            eventBuilder.ErrorType = InstrumentationEvent.ErrorType.warning;
            eventBuilder.Page = new JObject();
            try
            {
                eventBuilder.BuildEvent();
                Assert.Fail("Exception should have been thrown for missing mandatory field 'name'");
            }
            catch (EventBuilderException)
            {
                LoggingService.Log("Exception thrown as expected", LoggingLevel.Information);
            }
        }

        [TestMethod]
        public async Task TestMissingDeviceAppAttributesAsync()
        {
            var analyticsManager = new AnalyticsManager(uniqueId, null, TEST_ENCRYPTION_KEY);
            await analyticsManager.ResetAsync().ConfigureAwait(false);
            var eventBuilder = new InstrumentationEventBuilder(analyticsManager);
            var curTime = System.DateTime.Now.Ticks;
            var eventName = string.Format(TEST_EVENT_NAME, curTime);
            eventBuilder.Name = eventName;
            eventBuilder.SessionId = TEST_SESSION_ID;
            eventBuilder.SchemaType = InstrumentationEvent.SchemaType.LightningError;
            eventBuilder.StartTime = curTime;
            eventBuilder.SenderId = TEST_SENDER_ID;
            eventBuilder.EventType = InstrumentationEvent.EventType.system;
            eventBuilder.ErrorType = InstrumentationEvent.ErrorType.warning;
            eventBuilder.Page = new JObject();
            try
            {
                eventBuilder.BuildEvent();
                Assert.Fail("Exception should have been thrown for missing mandatory field 'device app attributes'");
            }
            catch (EventBuilderException)
            {
                LoggingService.Log("Exception thrown as expected", LoggingLevel.Information);
            }
            finally
            {
                await analyticsManager.ResetAsync();
            }
        }

        [TestMethod]
        public void TestAutoPopulateStartTime()
        {
            var eventBuilder = new InstrumentationEventBuilder(AnalyticsManager);
            var curTime = System.DateTime.Now.Ticks;
            var eventName = string.Format(TEST_EVENT_NAME, curTime);
            eventBuilder.Name = eventName;
            eventBuilder.SchemaType = InstrumentationEvent.SchemaType.LightningError;
            eventBuilder.SessionId = TEST_SESSION_ID;
            eventBuilder.SenderId = TEST_SENDER_ID;
            eventBuilder.EventType = InstrumentationEvent.EventType.system;
            eventBuilder.ErrorType = InstrumentationEvent.ErrorType.warning;
            eventBuilder.Page = new JObject();
            var instrumentationEvent = eventBuilder.BuildEvent();
            long startTime = instrumentationEvent.StartTime;
            Assert.IsTrue(startTime > 0, "Start time should have been auto populated");
        }

        [TestMethod]
        public void TestAutoPopulateEventId()
        {
            var eventBuilder = new InstrumentationEventBuilder(AnalyticsManager);
            var curTime = System.DateTime.Now.Ticks;
            var eventName = string.Format(TEST_EVENT_NAME, curTime);
            eventBuilder.Name = eventName;
            eventBuilder.SchemaType = InstrumentationEvent.SchemaType.LightningError;
            eventBuilder.SessionId = TEST_SESSION_ID;
            eventBuilder.SenderId = TEST_SENDER_ID;
            eventBuilder.EventType = InstrumentationEvent.EventType.system;
            eventBuilder.ErrorType = InstrumentationEvent.ErrorType.warning;
            eventBuilder.Page = new JObject();
            var instrumentationEvent = eventBuilder.BuildEvent();
            var eventId = instrumentationEvent.EventId;
            Assert.IsFalse(string.IsNullOrWhiteSpace(eventId), "Event ID should have been auto populated");
        }

        [TestMethod]
        public void TestAutoPopulateSequenceId()
        {
            var eventBuilder = new InstrumentationEventBuilder(AnalyticsManager);
            var curTime = System.DateTime.Now.Ticks;
            var eventName = string.Format(TEST_EVENT_NAME, curTime);
            eventBuilder.Name = eventName;
            eventBuilder.SchemaType = InstrumentationEvent.SchemaType.LightningError;
            eventBuilder.SessionId = TEST_SESSION_ID;
            eventBuilder.SenderId = TEST_SENDER_ID;
            eventBuilder.EventType = InstrumentationEvent.EventType.system;
            eventBuilder.ErrorType = InstrumentationEvent.ErrorType.warning;
            eventBuilder.Page = new JObject();
            var instrumentationEvent = eventBuilder.BuildEvent();
            int sequenceId = instrumentationEvent.SequenceId;
            Assert.IsTrue(sequenceId > 0, "Sequence ID should have been auto populated");
            var globalSequenceId = AnalyticsManager.GetGlobalSequenceId();
            Assert.AreEqual(0, globalSequenceId - sequenceId, "Global sequence ID should have been updated");
        }
    }
}
