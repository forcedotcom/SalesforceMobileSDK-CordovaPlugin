﻿/*
 * Copyright (c) 2016, salesforce.com, inc.
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

using Salesforce.SDK.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Salesforce.SDK.Hybrid.Logging
{
    sealed internal class Logger : ILoggingService
    {
        public void Log(Exception exception, LoggingLevel loggingLevel, string logMessage = "", [CallerMemberName] string memberName = "", [CallerFilePath] string classFilePath = "", [CallerLineNumber]int line = 0)
        {
            if (exception != null)
            {
                var message = exception.Message;
                if (!string.IsNullOrEmpty(logMessage))
                {
                    message = $"LogMessage: {logMessage}{Environment.NewLine}Exception Message: {message}";
                }
                Log(message, loggingLevel, memberName, classFilePath, line);
            }
        }

        public void Log(string message, LoggingLevel loggingLevel, [CallerMemberName] string memberName = "", [CallerFilePath] string classFilePath = "", [CallerLineNumber]int line = 0)
        {
            Debug.WriteLine($"{loggingLevel}:{Path.GetFileName(classFilePath)}:{memberName}:line {line} - {message}");
        }
    }
}
