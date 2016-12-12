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
using Salesforce.SDK.Analytics.Model;

namespace Salesforce.SDK.Analytics.Store
{
    /// <summary>
    /// Provides APIs to store events in an encrypted store on the filesystem.
    /// Each event is stored in a separate file on the filesystem.
    /// </summary>
    public interface IEventStoreManager
    {
     /// <summary>
     /// Stores an event to the filesystem. A combination of event's unique ID and
     /// filename suffix is used to generate a unique filename per event.
     /// </summary>
     /// <param name="instrumentationEvent"></param>
     /// <returns></returns>
        Task StoreEventAsync(InstrumentationEvent instrumentationEvent);

        /// <summary>
        /// Stores a list of events to the filesystem
        /// </summary>
        /// <param name="instrumentationEvents"></param>
        /// <returns></returns>
        Task StoreEventsAsync(List<InstrumentationEvent> instrumentationEvents);

        /// <summary>
        /// Returns a specific event stored on the filesystem
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns></returns>
        Task<InstrumentationEvent> FetchEventAsync(string eventId);

        /// <summary>
        /// Returns all the events stored on the filesystem for that unique identifier
        /// </summary>
        /// <returns></returns>
        Task<List<InstrumentationEvent>> FetchAllEventsAsync();

        /// <summary>
        /// Deletes a specific event stored on the filesystem
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns>true if successful, false otherwise</returns>
        Task<bool> DeleteEventAsync(string eventId);

        /// <summary>
        /// Deletes all the events stored on the filesystem for that unique identifier
        /// </summary>
        /// <param name="eventIds"></param>
        /// <returns></returns>
        Task DeleteEventsAsync(List<string> eventIds);

        /// <summary>
        /// Deletes all the events stored on the filesystem
        /// </summary>
        /// <returns></returns>
        Task DeleteAllEventsAsync();

        /// <summary>
        /// Disables or enables logging of events. If logging is disabled, no events
        /// will be stored. However, publishing of events is still possible
        /// </summary>
        /// <param name="enabled"></param>
        void DisableEnableLogging(bool enabled);

        /// <summary>
        /// Returns whether logging is enabled or disabled
        /// </summary>
        /// <returns></returns>
        bool IsLoggingEnabled();

        /// <summary>
        /// Sets the maximum number of events that can be stored
        /// </summary>
        /// <param name="maxEvents"></param>
        void SetMaxEvents(int maxEvents);
    }
}
