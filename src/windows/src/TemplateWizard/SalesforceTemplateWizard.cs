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
using System.Collections.Generic;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;

namespace TemplateWizard
{
    internal class SalesforceTemplateWizard : IWizard
    {
        public void BeforeOpeningFile(ProjectItem projectItem)
        {
        }

        public void ProjectFinishedGenerating(Project project)
        {
        }

        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
        }

        public void RunFinished()
        {
        }

        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary,
            WizardRunKind runKind, object[] customParams)
        {
            var window = new TemplateForm();
            window.ShowDialog();
            if (window.DialogResult.Value)
            {
                PopulateReplacementDictionary(window);
            }
            else
            {
                throw new WizardCancelledException();
            }
        }

        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }

        private void PopulateReplacementDictionary(TemplateForm window)
        {
            var replacementsDictionary = new Dictionary<string, string>();

            if (!String.IsNullOrEmpty(window.ClientID.Text))
            {
                replacementsDictionary.Add("$ClientID$", window.ClientID.Text);
            }

            if (!String.IsNullOrEmpty(window.CallbackURL.Text))
            {
                replacementsDictionary.Add("$CallbackURL$", window.CallbackURL.Text);
            }

            replacementsDictionary.Add("$EncryptionPassword$", Guid.NewGuid().ToString());
            replacementsDictionary.Add("$EncryptionSalt$", Guid.NewGuid().ToString());

            var sb = new StringBuilder();
            bool first = true;
            foreach (var next in window.Scopes)
            {
                if (next.IsChecked)
                {
                    if (!first)
                    {
                        sb.Append(", ");
                    }
                    else
                    {
                        first = false;
                    }
                    sb.Append("\"").Append(next.Item).Append("\"");
                }
            }
            replacementsDictionary.Add("$scopes$", sb.ToString());
            ChildWizard.InheritedParams = replacementsDictionary;
        }
    }
}