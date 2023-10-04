/*
 * Copyright (c) 2020-present, salesforce.com, inc.
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
package com.salesforce.androidsdk.smartstore.ui;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.ClipData;
import android.content.ClipboardManager;
import android.content.Context;
import android.content.Intent;
import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.AdapterView;
import android.widget.ArrayAdapter;
import android.widget.AutoCompleteTextView;
import android.widget.Button;
import android.widget.EditText;
import android.widget.ListView;
import android.widget.TextView;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import com.salesforce.androidsdk.smartstore.R;
import com.salesforce.androidsdk.smartstore.app.SmartStoreSDKManager;
import com.salesforce.androidsdk.smartstore.store.KeyValueEncryptedFileStore;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collections;
import java.util.List;

public class KeyValueStoreInspectorActivity extends Activity {
    // Keys for extras bundle
    private static final String TAG = "KeyValueStoreInspectorActivity";
    public static final String NO_STORE = "No KeyValueEncryptedFileStore found.";
    public static final String GLOBAL_STORE = " (global store)";
    public static final String ERROR_DIALOG_TITLE = "Error";
    public static final String ERROR_DIALOG_MESSAGE = "Key not found in the current store.";

    // Store
    private KeyValueEncryptedFileStore currentStore;
    private List<String> allStores;

    // View elements
    private AutoCompleteTextView storesDropdown;
    private EditText keyInput;
    private Button getValueButton;
    private ListView resultsListView;
    private ArrayList<KeyValuePair> keyValueList;
    private ArrayAdapter listAdapter;

    /**
     * Create intent to bring up inspector
     * @param parentActivity
     * @return KeyValueStoreInspectorActivity intent
     */
    public static Intent getIntent(Activity parentActivity) {
        return new Intent(parentActivity, KeyValueStoreInspectorActivity.class);
    }

    @Override
    protected void onCreate(@Nullable Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setTheme(com.salesforce.androidsdk.R.style.SalesforceSDK_Inspector);

        setContentView(R.layout.sf__key_value_inspector);
        storesDropdown = findViewById(R.id.sf__inspector_stores_dropdown);
        keyInput = findViewById(R.id.sf__inspector_key_text);
        getValueButton = findViewById(R.id.sf__inspector_get_value_button);
        resultsListView = findViewById(R.id.sf__inspector_key_value_list);
        keyValueList = new ArrayList<KeyValuePair>();
        listAdapter = new ArrayAdapter(this, R.layout.sf__inspector_key_value_results_cell, R.id.sf__inspector_value, keyValueList) {
            @Override
            public View getView(int position, @Nullable View convertView, @NonNull ViewGroup parent) {
                if (convertView == null) {
                    convertView = LayoutInflater.from(this.getContext()).inflate(R.layout.sf__inspector_key_value_results_cell, null);
                }

                KeyValuePair pair = keyValueList.get(position);
                ((TextView) convertView.findViewById(R.id.sf__inspector_value)).setText(pair.getValue());
                ((TextView) convertView.findViewById(R.id.sf__inspector_key)).setText(pair.getKey());
                return convertView;
            }
        };
        resultsListView.setAdapter(listAdapter);
        resultsListView.setOnItemLongClickListener(new AdapterView.OnItemLongClickListener() {
            @Override
            public boolean onItemLongClick(AdapterView<?> parent, View view, int position, long id) {
                String label = "Value for key: " + keyValueList.get(position).key;
                String value = keyValueList.get(position).value;
                ClipboardManager clipboard = (ClipboardManager) getSystemService(Context.CLIPBOARD_SERVICE);
                clipboard.setPrimaryClip(ClipData.newPlainText(label, value));

                Toast.makeText(parent.getContext(), "Value copied to clipboard.", Toast.LENGTH_SHORT).show();
                return true;
            }
        });
        setupStoresDropdown();
        keyInput.requestFocus();
    }

    private void setupStoresDropdown() {
        SmartStoreSDKManager mgr = SmartStoreSDKManager.getInstance();
        allStores = new ArrayList<>();
        for (String storeName : mgr.getKeyValueStoresPrefixList()) allStores.add(storeName);
        for (String storeName : mgr.getGlobalKeyValueStoresPrefixList()) allStores.add(storeName + GLOBAL_STORE);

        if (allStores.isEmpty()) {
            allStores.add(NO_STORE);
            getValueButton.setEnabled(false);
            getValueButton.setAlpha(.5f);
        } else {
            setCurrentStore(allStores.get(0));
        }

        Collections.sort(allStores);
        ArrayAdapter<String> adapter = new ArrayAdapter<>(this, R.layout.sf__inspector_menu_popup_item, allStores);
        storesDropdown.setAdapter(adapter);
        storesDropdown.setText(allStores.get(0), false);
    }

    private void setCurrentStore(String storeName) {
        String name = storeName;
        if (storeName.endsWith(GLOBAL_STORE)) {
            name = storeName.substring(0, storeName.length() - GLOBAL_STORE.length());
            currentStore = SmartStoreSDKManager.getInstance().getGlobalKeyValueStore(name);
        } else {
            currentStore = SmartStoreSDKManager.getInstance().getKeyValueStore(name);
        }
    }

    public void onGetValueClick(View v) {
        String typedKey = keyInput.getText().toString();
        setCurrentStore(storesDropdown.getText().toString());
        keyValueList.clear();

        if (typedKey.length() == 0) {
            return;
        }

        // Return all key/value pairs were key matches typedKey
        // if v2 kv store AND typedKey contains a *
        if (currentStore.getStoreVersion() == 2 && typedKey.contains("*")) {
            String[] allKeys = currentStore.keySet().toArray(new String[0]); 
            Arrays.sort(allKeys);

            for (String key : allKeys) {
                if (matches(typedKey, key)) {
                    keyValueList.add(new KeyValuePair(key, currentStore.getValue(key)));
                }
            }
        }
        // Otherwise simply lookup typeKey
        else {
            String value = currentStore.getValue(typedKey);

            if (value != null) {
                keyValueList.add(new KeyValuePair(typedKey, value));
            }
        }

        // Show alert if nothing matched
        if (keyValueList.size() == 0) {
            new AlertDialog.Builder(this).setTitle(ERROR_DIALOG_TITLE)
                .setMessage(ERROR_DIALOG_MESSAGE).show();
        } else {
            listAdapter.notifyDataSetChanged();
        }
    }

    private boolean matches(String typedKey, String key) {
        if (typedKey.contains("*")) {
            return key.matches(typedKey.replaceAll("\\*", ".*"));
        } else {
            return key.equals(typedKey);
        }
    }

    private class KeyValuePair {
        private String key, value;

        KeyValuePair(String key, String value) {
            this.key = key;
            this.value = value;
        }

        public String getKey() {
            return key;
        }
        public String getValue() {
            return value;
        }
    }
}
