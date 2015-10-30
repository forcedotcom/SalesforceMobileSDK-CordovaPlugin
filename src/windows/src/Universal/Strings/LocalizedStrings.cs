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
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Data;

namespace Salesforce.SDK.Strings
{
    public class LocalizedStrings : IValueConverter
    {
        private static string _customResourcesLocation = string.Empty;
        private static bool _useCustomResources = false;

        //Lazy initialize these ResourceLoader objects because they can only be initialized
        //on UI thread. If someone calls any of our static methods these will get initialized
        //if they aren't lazy.
        private static readonly Lazy<ResourceLoader> SdkResourceLoader =
            new Lazy<ResourceLoader>(() => ResourceLoader.GetForCurrentView("Salesforce.SDK.Universal/Resources"));
        private static readonly Lazy<ResourceLoader> CustomResourceLoader =
            new Lazy<ResourceLoader>(() => ResourceLoader.GetForCurrentView(_customResourcesLocation));

        public static string GetString(string resourceName)
        {
            if (_useCustomResources)
            {
                var returnString = CustomResourceLoader.Value.GetString(resourceName);

                // only use custom string if it was defined, otherwise fall through to SDK default english string
                if (!string.IsNullOrWhiteSpace(returnString))
                {
                    return returnString;
                }
            }

            return SdkResourceLoader.Value.GetString(resourceName);
        }

        /// <summary>
        /// Sets the location for resource files. If you are consuming this SDK and you want to localize
        /// strings that are used in this SDK then you can use this method to point to your own RESW files.
        /// You can see the label names by looking at the en-us/resources.resw file.  See the
        /// Salesforce.Salesforce1.Container sample to see how this can be used.
        /// </summary>
        /// <param name="resourceLocation">The path to the resource file, of pattern '[assembly name no extension]/[resource file name]'.</param>
        public static void SetResourceLocation(string resourceLocation)
        {
            _customResourcesLocation = resourceLocation;
            _useCustomResources = true;
        }

        #region IValueConverter Implementation

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return GetString(value as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        #endregion //IValueConverter Implementation
    }
}