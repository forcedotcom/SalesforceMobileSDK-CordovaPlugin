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
using Windows.UI;
using Salesforce.SDK.Source.Settings;
using System;

namespace $safeprojectname$
{
    /// <summary>
    /// Implement this class to configure the settings for your application.  You can find instructions on how to create a connected app from the included website.
    /// https://help.salesforce.com/apex/HTViewHelpDoc?id=connected_app_create.htm
    /// </summary>
    class Config : SalesforceConfig
    {
        /// <summary>
        /// This should return the client id generated when you create a connected app through Salesforce.
        /// </summary>
        public override string ClientId
        {
            get { return "$ClientID$"; }
        }

        /// <summary>
        /// This should return the callback url generated when you create a connected app through Salesforce.
        /// </summary>
        public override string CallbackUrl
        {
            get { return "$CallbackURL$"; }
        }

        /// <summary>
        /// Return the scopes that you wish to use in your app. Limit to what you actually need, try to refrain from listing all scopes.
        /// </summary>
        public override string[] Scopes
        {
            get { return new string[] { $scopes$ }; }
        }

        public override Windows.UI.Color? LoginBackgroundColor
        {
            get
            {
                string color = "#009adb"; 
                if (color.StartsWith("#"))
                    color = color.Remove(0, 1);
                byte r, g, b;
                if (color.Length == 3)
                {
                    r = Convert.ToByte(color[0] + "" + color[0], 16);
                    g = Convert.ToByte(color[1] + "" + color[1], 16);
                    b = Convert.ToByte(color[2] + "" + color[2], 16);
                }
                else if (color.Length == 6)
                {
                    r = Convert.ToByte(color[0] + "" + color[1], 16);
                    g = Convert.ToByte(color[2] + "" + color[3], 16);
                    b = Convert.ToByte(color[4] + "" + color[5], 16);
                }
                else
                {
                    throw new ArgumentException("Hex color " + color + " is invalid.");
                }
                return Color.FromArgb(255, r, g, b);
            }
        }


        public override string ApplicationTitle
        {
            get { return "$safeprojectname$"; }
        }

        public override Uri LoginBackgroundLogo
        {
            get { return null; }
        }

        public override bool IsApplicationTitleVisible
        {
            get { return true; }
        }
    }
}
