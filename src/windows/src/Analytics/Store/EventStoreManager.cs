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
using System.Windows;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Analytics.Model;
using Salesforce.SDK.Core;
using Salesforce.SDK.Logging;
using PCLStorage;
using Salesforce.SDK.Security;

namespace Salesforce.SDK.Analytics.Store
{
    public class EventStoreManager : IEventStoreManager
    {
        private readonly IFolder _rootDir;
        private bool _isLoggingEnabled = true;
        private int _maxEvents = 1000;
        private static ILoggingService LoggingService => SDKServiceLocator.Get<ILoggingService>();
        private static IEncryptionService EncryptionService => SDKServiceLocator.Get<IEncryptionService>();
        public string FilenameSuffix { get; set; }
        public string EncryptionKey { get; set; }

        public EventStoreManager(string fileNameSuffix, string encryptionKey)

        {
            FilenameSuffix = fileNameSuffix;
            EncryptionKey = encryptionKey;
            _rootDir = FileSystem.Current.LocalStorage;
        }

        public async Task StoreEventAsync(InstrumentationEvent instrumentationEvent)
        {
            if (string.IsNullOrEmpty(instrumentationEvent?.ToJson().ToString()))
            {
                LoggingService.Log("Invalid Event", LoggingLevel.Error);
                return;
            }
            if (await ShouldStoreEventAsync().ConfigureAwait(false))
            {
                var fileName = instrumentationEvent.EventId + FilenameSuffix;
                //Open file
                IFile file = await _rootDir.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
                //Wrtie to file after encrypting contents
                await file.WriteAllTextAsync(Encrypt(instrumentationEvent.ToJson().ToString(), EncryptionKey)).ConfigureAwait(false);
            }
        }

        public async Task StoreEventsAsync(List<InstrumentationEvent> instrumentationEvents)
        {
            if (instrumentationEvents == null)
            {
                throw new ArgumentNullException(nameof(instrumentationEvents), "InstrumentationEvents list is null");
            }
            if (instrumentationEvents.Count == 0)
            {
                LoggingService.Log("No events to store", LoggingLevel.Information);
                return;
            }
            if (!await ShouldStoreEventAsync().ConfigureAwait(false))
            {
                return;
            }
            await Task.WhenAll(instrumentationEvents.Select(StoreEventAsync));
        }

        public async Task<InstrumentationEvent> FetchEventAsync(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                throw new ArgumentNullException(nameof(eventId), "Invalid event ID supplied");
            }
            var fileName = eventId + FilenameSuffix;
            var file = await _rootDir.GetFileAsync(fileName).ConfigureAwait(false);
            return await FetchEventAsync(file);
        }

        public async Task<List<InstrumentationEvent>> FetchAllEventsAsync()
        {
            var files = await _rootDir.GetFilesAsync().ConfigureAwait(false);
            var events = new List<InstrumentationEvent>();

            foreach (var file in files)
            {
                if (!(file.Name.Contains(".db") || file.Name.Contains(".xml")))
                {
                    var instrumentationEvent = await FetchEventAsync(file);
                    if (instrumentationEvent != null)
                    {
                        events.Add(instrumentationEvent);
                    }
                }
                
            }

            return events;
        }

        public async Task<bool> DeleteEventAsync(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                throw new ArgumentNullException(nameof(eventId), "Invalid event ID supplied");
            }
            var fileName = eventId + FilenameSuffix;
            var file = await _rootDir.GetFileAsync(fileName).ConfigureAwait(false);
            try
            {
                await file.DeleteAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task DeleteEventsAsync(List<string> eventIds)
        {
            if (eventIds == null)
            {
                throw new ArgumentNullException(nameof(eventIds), "Event Ids list is null");
            }
            if(eventIds.Count == 0)
            {
                LoggingService.Log("No events to delete", LoggingLevel.Information);
                return;
            }
            await Task.WhenAll(eventIds.Select(DeleteEventAsync));
        }

        public async Task DeleteAllEventsAsync()
        {
            var files = await _rootDir.GetFilesAsync().ConfigureAwait(false);
            foreach (var file in files)
            {
                await file.DeleteAsync();
            }
        }

        public void DisableEnableLogging(bool enabled)
        {
            _isLoggingEnabled = enabled;
        }

        public bool IsLoggingEnabled()
        {
            return _isLoggingEnabled;
        }

        public void SetMaxEvents(int maxEvents)
        {
            _maxEvents = maxEvents;
        }

        private async Task<InstrumentationEvent> FetchEventAsync(IFile file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file), "File does not exist");
            }
            var json = await file.ReadAllTextAsync().ConfigureAwait(false);
            //decrypt contents read from file
            var eventString = Decrypt(json, EncryptionKey);
            if (eventString == null)
            {
                throw new ArgumentNullException(nameof(eventString), "Error in decrypting contents of file");
            }

            return new InstrumentationEvent(JObject.Parse(eventString));
        }

        private async Task<bool> ShouldStoreEventAsync()
        {
            var files = await _rootDir.GetFilesAsync().ConfigureAwait(false);
            int filesCount = 0;
            if (files != null)
            {
                filesCount = files.Count;
            }
            return _isLoggingEnabled && (filesCount < _maxEvents);
        }

        private string Encrypt(string data, string key)
        {
            return EncryptionService.Encrypt(data, key);
        }

        private string Decrypt(string data, string key)
        {
            return EncryptionService.Decrypt(data, key);
        }
    }
}
