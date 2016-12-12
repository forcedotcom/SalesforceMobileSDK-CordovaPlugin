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
using Salesforce.SDK.Analytics.Manager;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Core;
using Salesforce.SDK.Settings;

namespace Salesforce.SDK.Analytics.Model
{
    public class InstrumentationEventBuilder
    {
        //private Context context;
        private long _startTime;
        private long _endTime;
        private string _name;
        private JObject _attributes;
        private string _sessionId;
        private string _senderId;
        private JObject _senderContext;
        private InstrumentationEvent.SchemaType _schemaType;
        private InstrumentationEvent.EventType _eventType;
        private InstrumentationEvent.ErrorType _errorType;
        private string _senderParentId;
        private long _sessionStartTime;
        private JObject _page;
        private JObject _previousPage;
        private JObject _marks;

        private static IAnalyticsManager AnalyticsManager;

        private static IApplicationInformationService ApplicationService =
            SDKServiceLocator.Get<IApplicationInformationService>();

        public InstrumentationEventBuilder(IAnalyticsManager analyticsManager)
        {
            AnalyticsManager = analyticsManager;
        }

        public long StartTime
        {
            get { return _startTime; }
            set { _startTime = value; }
        }

        public long EndTime
        {
            get { return _endTime; }
            set { _endTime = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public JObject Attributes
        {
            get { return _attributes; }
            set { _attributes = value; }
        }

        public string SessionId
        {
            get { return _sessionId; }
            set { _sessionId = value; }
        }

        public string SenderId
        {
            get { return _senderId; }
            set { _senderId = value; }
        }

        public JObject SenderContext
        {
            get { return _senderContext; }
            set { _senderContext = value; }
        }

        public InstrumentationEvent.SchemaType SchemaType
        {
            get { return _schemaType; }
            set { _schemaType = value; }
        }

        public InstrumentationEvent.EventType EventType
        {
            get { return _eventType; }
            set { _eventType = value; }
        }

        public InstrumentationEvent.ErrorType ErrorType
        {
            get { return _errorType; }
            set { _errorType = value; }
        }

        public string SenderParentId
        {
            get { return _senderParentId; }
            set { _senderParentId = value; }
        }

        public long SessionStartTime
        {
            get { return _sessionStartTime; }
            set { _sessionStartTime = value; }
        }

        public JObject Page
        {
            get { return _page; }
            set { _page = value; }
        }

        public JObject PreviousPage
        {
            get { return _previousPage; }
            set { _previousPage = value; }
        }

        public JObject Marks
        {
            get { return _marks; }
            set { _marks = value; }
        }

        public InstrumentationEvent BuildEvent()
        {
            string eventId = Guid.NewGuid().ToString();
            if (string.IsNullOrEmpty(Name))
            {
                throw new EventBuilderException("Mandatory field 'name' not set!");
            }
            var deviceAppAttributes = AnalyticsManager.GetDeviceAppAttributes();
            if (deviceAppAttributes == null)
            {
                throw new EventBuilderException("Mandatory field 'device app attributes' not set!");
            }
            if (SchemaType != InstrumentationEvent.SchemaType.LightningPerformance && Page == null)
            {
                throw new EventBuilderException("Mandatory field 'page' not set!");
            }

            var sequenceId = AnalyticsManager.GetGlobalSequenceId() + 1;
            AnalyticsManager.SetGlobalSequenceId(sequenceId);

            // Defaults to current time if not explicitly set.
            long curTime = DateTime.Now.Ticks;
            StartTime = StartTime == 0 ? curTime : StartTime;
            SessionStartTime = SessionStartTime == 0 ? curTime : SessionStartTime;
            return new InstrumentationEvent(eventId, StartTime, EndTime, Name, Attributes, SessionId,
                    sequenceId, SenderId, SenderContext, SchemaType, EventType, ErrorType,
                    deviceAppAttributes, GetConnectionType(), SenderParentId, SessionStartTime, Page,
                    PreviousPage, Marks);
        }

        public string GetConnectionType()
        {
            return ApplicationService.GetConnectionType();
        }

    }
}
