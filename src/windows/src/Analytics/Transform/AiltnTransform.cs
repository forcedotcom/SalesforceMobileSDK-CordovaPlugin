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
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Analytics.Model;
using Salesforce.SDK.Core;
using Salesforce.SDK.Logging;

namespace Salesforce.SDK.Analytics.Transform
{
    public class AiltnTransform : ITransform
    {
        private static string CONNECTION_TYPE_KEY = "connectionType";
        private static string VERSION_KEY = "version";
        private static string VERSION_VALUE = "0.2";
        private static string SCHEMA_TYPE_KEY = "schemaType";
        private static string ID_KEY = "id";
        private static string EVENT_SOURCE_KEY = "eventSource";
        private static string TS_KEY = "ts";
        private static string PAGE_START_TIME_KEY = "pageStartTime";
        private static string DURATION_KEY = "duration";
        private static string EPT_KEY = "ept";
        private static string CLIENT_SESSION_ID_KEY = "clientSessionId";
        private static string SEQUENCE_KEY = "sequence";
        private static string ATTRIBUTES_KEY = "attributes";
        private static string LOCATOR_KEY = "locator";
        private static string PAGE_KEY = "page";
        private static string PREVIOUS_PAGE_KEY = "previousPage";
        private static string MARKS_KEY = "marks";
        private static string EVENT_TYPE_KEY = "eventType";
        private static string ERROR_TYPE_KEY = "errorType";
        private static string TARGET_KEY = "target";
        private static string SCOPE_KEY = "scope";
        private static string CONTEXT_KEY = "context";
        private static string DEVICE_ATTRIBUTES_KEY = "deviceAttributes";
        private static string PERF_EVENT_TYPE = "defs";


        public JObject Transform(InstrumentationEvent instrumentationEvent)
        {
            if (instrumentationEvent == null)
            {
                return null;
            }
            JObject logLine = BuildPayload(instrumentationEvent);
            try
            {
                logLine?.Add(DEVICE_ATTRIBUTES_KEY, BuildDeviceAttributes(instrumentationEvent));
            }
            catch (Exception ex)
            {
                throw new Exception("Exception thrown while transforming JSON", ex);
            }

            return logLine;
        }

        private JObject BuildDeviceAttributes(InstrumentationEvent instrumentationEvent)
        {
            var deviceAttributes = new JObject();
            try
            {
                var deviceAppAttributes = instrumentationEvent.DeviceAppAttributes;
                if (deviceAppAttributes != null)
                {
                    deviceAttributes = deviceAppAttributes.ToJson();
                }
                deviceAttributes.Add(CONNECTION_TYPE_KEY, instrumentationEvent.ConnectionType);
            }
            catch (Exception ex)
            {
                throw new Exception("Exception thrown while transforming JSON", ex);
            }
            return deviceAttributes;
        }

        private JObject BuildPayload(InstrumentationEvent instrumentationEvent)
        {
            var payload = new JObject();
            try
            {
                payload.Add(VERSION_KEY, VERSION_VALUE);
                var schemaType = instrumentationEvent.Schema;
                payload.Add(SCHEMA_TYPE_KEY, schemaType.ToString());
                payload.Add(ID_KEY, instrumentationEvent.EventId);
                payload.Add(EVENT_SOURCE_KEY, instrumentationEvent.Name);
                payload.Add(TS_KEY, instrumentationEvent.StartTime);
                payload.Add(PAGE_START_TIME_KEY, instrumentationEvent.SessionStartTime);
                var duration = instrumentationEvent.EndTime - instrumentationEvent.StartTime;
                if (duration > 0)
                {
                    if (schemaType == InstrumentationEvent.SchemaType.LightningInteraction ||
                        schemaType == InstrumentationEvent.SchemaType.LightningPerformance)
                    {
                        payload.Add(DURATION_KEY, duration);
                    }
                    else if (schemaType == InstrumentationEvent.SchemaType.LightningPageView)
                    {
                        payload.Add(EPT_KEY, duration);
                    }
                }
                if (!string.IsNullOrWhiteSpace(instrumentationEvent.SessionId))
                {
                    payload.Add(CLIENT_SESSION_ID_KEY, instrumentationEvent.SessionId);
                }

                if (schemaType != InstrumentationEvent.SchemaType.LightningPerformance)
                {
                    payload.Add(SEQUENCE_KEY, instrumentationEvent.SequenceId);
                }
                if (instrumentationEvent.Attributes != null)
                {
                    payload.Add(ATTRIBUTES_KEY, instrumentationEvent.Attributes);
                }
                if (schemaType != InstrumentationEvent.SchemaType.LightningPerformance &&
                    instrumentationEvent.Page != null)
                {
                    payload.Add(PAGE_KEY, instrumentationEvent.Page);
                }
                if (instrumentationEvent.PreviousPage != null &&
                    schemaType == InstrumentationEvent.SchemaType.LightningPageView)
                {
                    payload.Add(PREVIOUS_PAGE_KEY, instrumentationEvent.PreviousPage);
                }
                if (instrumentationEvent.Marks != null &&
                    schemaType == InstrumentationEvent.SchemaType.LightningPageView)
                {
                    payload.Add(MARKS_KEY, instrumentationEvent.Marks);
                }
                if (schemaType == InstrumentationEvent.SchemaType.LightningInteraction
                    || schemaType == InstrumentationEvent.SchemaType.LightningPageView)
                {
                    var locator = BuildLocator(instrumentationEvent);
                    if (locator != null)
                    {
                        payload.Add(LOCATOR_KEY, locator);
                    }
                }
                var eventType = instrumentationEvent.Event;
                string eventTypeString = null;
                if (schemaType == InstrumentationEvent.SchemaType.LightningPerformance)
                {
                    eventTypeString = PERF_EVENT_TYPE;
                }
                else if (schemaType == InstrumentationEvent.SchemaType.LightningInteraction)
                {
                    eventTypeString = eventType.ToString();
                }
                if (!string.IsNullOrWhiteSpace(eventTypeString))
                {
                    payload.Add(EVENT_TYPE_KEY, eventTypeString);
                }
                var errorType = instrumentationEvent.Error;
                if (schemaType == InstrumentationEvent.SchemaType.LightningError)
                {
                    payload.Add(ERROR_TYPE_KEY, errorType.ToString());
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Exception thrown while transforming JSON", ex);
            }

            return payload;
        }

        private JObject BuildLocator(InstrumentationEvent instrumentationEvent)
        {
            var locator = new JObject();
            try
            {
                var senderId = instrumentationEvent.SenderId;
                var senderParentId = instrumentationEvent.SenderParentId;
                if (string.IsNullOrWhiteSpace(senderId) || string.IsNullOrWhiteSpace(senderParentId))
                {
                    return null;
                }
                locator.Add(TARGET_KEY, senderId);
                locator.Add(SCOPE_KEY, senderParentId);

                var senderContext = instrumentationEvent.SenderContext;
                if (senderContext != null)
                {
                    locator.Add(CONTEXT_KEY, senderContext);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Exception thrown while transforming JSON", ex);
            }

            return locator;
        }
    }
}
