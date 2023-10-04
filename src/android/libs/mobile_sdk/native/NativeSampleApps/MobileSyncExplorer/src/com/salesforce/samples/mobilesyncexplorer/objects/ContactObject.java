/*
 * Copyright (c) 2014-present, salesforce.com, inc.
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
package com.salesforce.samples.mobilesyncexplorer.objects;

import android.text.TextUtils;

import com.salesforce.androidsdk.mobilesync.model.SalesforceObject;
import com.salesforce.androidsdk.mobilesync.target.SyncTarget;
import com.salesforce.androidsdk.mobilesync.util.Constants;

import org.json.JSONObject;

/**
 * A simple representation of a Contact object.
 *
 * @author bhariharan
 */
public class ContactObject extends SalesforceObject {

	public static final String FIRST_NAME = "FirstName";
	public static final String LAST_NAME = "LastName";
	public static final String TITLE = "Title";
	public static final String PHONE = "MobilePhone";
	public static final String EMAIL = "Email";
	public static final String DEPARTMENT = "Department";
	public static final String HOME_PHONE = "HomePhone";

	private final boolean isLocallyCreated;
	private final boolean isLocallyDeleted;
	private final boolean isLocallyModified;

	/**
	 * Parameterized constructor.
	 *
	 * @param data Raw data.
	 */
	public ContactObject(JSONObject data) {
		super(data);
		objectType = Constants.CONTACT;
		objectId = data.optString(Constants.ID);
		name = data.optString(FIRST_NAME) + " " + data.optString(LAST_NAME);
		isLocallyCreated = data.optBoolean(SyncTarget.LOCALLY_CREATED);
		isLocallyDeleted = data.optBoolean(SyncTarget.LOCALLY_DELETED);
		final boolean isLocallyUpdated = data.optBoolean(SyncTarget.LOCALLY_UPDATED);
		isLocallyModified = isLocallyCreated || isLocallyUpdated || isLocallyDeleted;
	}

	/**
	 * Returns first name of the contact.
	 *
	 * @return First name of the contact.
	 */
	public String getFirstName() {
		return sanitizeText(rawData.optString(FIRST_NAME));
	}

	/**
	 * Returns last name of the contact.
	 *
	 * @return Last name of the contact.
	 */
	public String getLastName() {
		return sanitizeText(rawData.optString(LAST_NAME));
	}

	/**
	 * Returns title of the contact.
	 *
	 * @return Title of the contact.
	 */
	public String getTitle() {
		return sanitizeText(rawData.optString(TITLE));
	}

	/**
	 * Returns phone number of the contact.
	 *
	 * @return Phone number of the contact.
	 */
	public String getPhone() {
		return sanitizeText(rawData.optString(PHONE));
	}

	/**
	 * Returns e-mail address of the contact.
	 *
	 * @return E-mail address of the contact.
	 */
	public String getEmail() {
		return sanitizeText(rawData.optString(EMAIL));
	}

	/**
	 * Returns department of the contact.
	 *
	 * @return Department of the contact.
	 */
	public String getDepartment() {
		return sanitizeText(rawData.optString(DEPARTMENT));
	}

	/**
	 * Returns home phone number of the contact.
	 *
	 * @return Home phone number of the contact.
	 */
	public String getHomePhone() {
		return sanitizeText(rawData.optString(HOME_PHONE));
	}

	/**
	 * Returns whether the contact has been locally modified or not.
	 *
	 * @return True - if the contact has been locally modified, False - otherwise.
	 */
	public boolean isLocallyModified() {
		return isLocallyModified;
	}

	/**
	 * Returns whether the contact has been locally deleted or not.
	 *
	 * @return True - if the contact has been locally deleted, False - otherwise.
	 */
	public boolean isLocallyDeleted() {
		return isLocallyDeleted;
	}

	/**
	 * Returns whether the contact has been locally created or not.
	 *
	 * @return True - if the contact has been locally created, False - otherwise.
	 */
	public boolean isLocallyCreated() {
		return isLocallyCreated;
	}


	private String sanitizeText(String text) {
		if (TextUtils.isEmpty(text) || text.equals(Constants.NULL_STRING)) {
			return Constants.EMPTY_STRING;
		}
		return text;
	}

	public String getObjectId() {
		return objectId;
	}

	public String getName() {
		return name;
	}
}
