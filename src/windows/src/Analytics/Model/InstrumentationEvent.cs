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
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace Salesforce.SDK.Analytics.Model
{
    /// <summary>
    /// Represents a typical instrumentation event. Transforms can be used to
    /// convert this event into a specific library's event format.
    /// </summary>
    public class InstrumentationEvent
    {
        public static string EVENT_ID_KEY = "eventId";
        public static string START_TIME_KEY = "startTime";
        public static string END_TIME_KEY = "endTime";
        public static string NAME_KEY = "name";
        public static string ATTRIBUTES_KEY = "attributes";
        public static string SESSION_ID_KEY = "sessionId";
        public static string SEQUENCE_ID_KEY = "sequenceId";
        public static string SENDER_ID_KEY = "senderId";
        public static string SENDER_CONTEXT_KEY = "senderContext";
        public static string SCHEMA_TYPE_KEY = "schemaType";
        public static string EVENT_TYPE_KEY = "eventType";
        public static string ERROR_TYPE_KEY = "errorType";
        public static string CONNECTION_TYPE_KEY = "connectionType";
        public static string DEVICE_APP_ATTRIBUTES_KEY = "deviceAppAttributes";
        public static string SENDER_PARENT_ID_KEY = "senderParentId";
        public static string SESSION_START_TIME_KEY = "sessionStartTime";
        public static string PAGE_KEY = "page";
        public static string PREVIOUS_PAGE_KEY = "previousPage";
        public static string MARKS_KEY = "marks";

        private string _eventId;
        private long _startTime;
        private long _endTime;
        private string _name;
        private JObject _attributes;
        private string _sessionId;
        private int _sequenceId;
        private string _senderId;
        private JObject _senderContext;
        private SchemaType _schemaType;
        private EventType _eventType;
        private ErrorType _errorType;
        private string _connectionType;
        private DeviceAppAttributes _deviceAppAttributes;
        private string _senderParentId;
        private long _sessionStartTime;
        private JObject _page;
        private JObject _previousPage;
        private JObject _marks;

        internal InstrumentationEvent(string eventId, long startTime, long endTime, string name,
                         JObject attributes, string sessionId, int sequenceId,
                         string senderId, JObject senderContext,
                         SchemaType schemaType, EventType eventType, ErrorType errorType,
                         DeviceAppAttributes deviceAppAttributes, string connectionType,
                         string senderParentId, long sessionStartTime, JObject page,
                         JObject previousPage, JObject marks)
        {
            _eventId = eventId;
            _startTime = startTime;
            _endTime = endTime;
            _name = name;
            _attributes = attributes;
            _sessionId = sessionId;
            _sequenceId = sequenceId;
            _senderContext = senderContext;
            _schemaType = schemaType;
            _eventType = eventType;
            _errorType = errorType;
            _deviceAppAttributes = deviceAppAttributes;
            _connectionType = connectionType;
            _senderParentId = senderParentId;
            _sessionStartTime = sessionStartTime;
            _page = page;
            _previousPage = previousPage;
            _marks = marks;

        }
        [JsonProperty]
        public string EventId => _eventId;
        [JsonProperty]
        public long StartTime => _startTime;
        [JsonProperty]
        public long EndTime => _endTime;
        [JsonProperty]
        public string Name => _name;
        [JsonProperty]
        public JObject Attributes => _attributes;
        [JsonProperty]
        public string SessionId => _sessionId;
        [JsonProperty]
        public int SequenceId => _sequenceId;
        [JsonProperty]
        public string SenderId => _senderId;
        [JsonProperty]
        public JObject SenderContext => _senderContext;
        [JsonProperty]
        public SchemaType Schema => _schemaType;
        [JsonProperty]
        public EventType Event => _eventType;
        [JsonProperty]
        public ErrorType Error => _errorType;
        [JsonProperty]
        public string ConnectionType => _connectionType;
        [JsonProperty]
        public DeviceAppAttributes DeviceAppAttributes => _deviceAppAttributes;
        [JsonProperty]
        public string SenderParentId => _senderParentId;
        [JsonProperty]
        public long SessionStartTime => _sessionStartTime;
        [JsonProperty]
        public JObject Page => _page;
        [JsonProperty]
        public JObject PreviousPage => _previousPage;
        [JsonProperty]
        public JObject Marks => _marks;

        public InstrumentationEvent(JObject json)
        {
            if(json != null)
            {
                _eventId = json.GetValue(EVENT_ID_KEY).ToString();
                _startTime = (long) json.GetValue(START_TIME_KEY);
                _endTime = (long) json.GetValue(END_TIME_KEY);
                _name = json.GetValue(NAME_KEY).ToString();
                _attributes = json.GetValue(ATTRIBUTES_KEY).ToObject<JObject>();
                _sessionId = json.GetValue(SESSION_ID_KEY).ToString();
                _sequenceId = (int) json.GetValue(SEQUENCE_ID_KEY);
                _senderId = json.GetValue(SENDER_ID_KEY).ToString();
                _senderContext = json.GetValue(SENDER_CONTEXT_KEY).ToObject<JObject>();
                _schemaType = json.GetValue(SCHEMA_TYPE_KEY).ToObject<SchemaType>();
                _eventType = json.GetValue(EVENT_TYPE_KEY).ToObject<EventType>();
                _errorType = json.GetValue(ERROR_TYPE_KEY).ToObject<ErrorType>();
                _deviceAppAttributes = json.GetValue(DEVICE_APP_ATTRIBUTES_KEY).ToObject<DeviceAppAttributes>();
                _connectionType = json.GetValue(CONNECTION_TYPE_KEY).ToString();
                _senderParentId = json.GetValue(SENDER_PARENT_ID_KEY).ToString();
                _sessionStartTime = (long) json.GetValue(SESSION_START_TIME_KEY);
                _page = json.GetValue(PAGE_KEY).ToObject<JObject>();
                _previousPage = json.GetValue(PREVIOUS_PAGE_KEY).ToObject<JObject>();
                _marks = json.GetValue(MARKS_KEY).ToObject<JObject>();
            }
        }

        public JObject ToJson()
        {
            var json = new JObject
            {
                { EVENT_ID_KEY, _eventId },
                { START_TIME_KEY, _startTime },
                { END_TIME_KEY, _endTime },
                { NAME_KEY, _name },
                { ATTRIBUTES_KEY, _attributes},
                { SESSION_ID_KEY, _sessionId},
                { SEQUENCE_ID_KEY, _sequenceId},
                { SENDER_ID_KEY, _senderId},
                { SENDER_CONTEXT_KEY, _senderContext},
                { SCHEMA_TYPE_KEY, _schemaType.ToString()},
                { EVENT_TYPE_KEY, _eventType.ToString()},
                { ERROR_TYPE_KEY, _errorType.ToString()},
                { CONNECTION_TYPE_KEY, _connectionType},
                { DEVICE_APP_ATTRIBUTES_KEY, _deviceAppAttributes.ToJson()},
                { SENDER_PARENT_ID_KEY, _senderParentId},
                { SESSION_START_TIME_KEY, _sessionStartTime },
                { PAGE_KEY, _page},
                { PREVIOUS_PAGE_KEY, _previousPage},
                { MARKS_KEY, _marks}
            };

            return json;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (!(obj is InstrumentationEvent))
            {
                return false;
            }
            var instrumentationEvent = (InstrumentationEvent) obj;
            if (string.IsNullOrEmpty(EventId))
            {
                return false;
            }
            return EventId.Equals(instrumentationEvent.EventId);
        }

        public override int GetHashCode()
        {
            return EventId.GetHashCode();
        }

        /// <summary>
        /// Represents the type of event being logged.
        /// </summary>
        public enum EventType
        {
            //These are lower case as its required by Ailtn endpoint
            user,
            system,
            error,
            crud

        }

        /// <summary>
        /// Represents the type of schema being logged.
        /// </summary>
        public enum SchemaType
        {
            LightningInteraction,
            LightningPageView,
            LightningPerformance,
            LightningError
        }

        /// <summary>
        /// Represents the type of error being logged.
        /// </summary>
        public enum ErrorType
        {
            //These are lower case as its required by Ailtn endpoint
            info,
            warning,
            error
        }
    }
}
