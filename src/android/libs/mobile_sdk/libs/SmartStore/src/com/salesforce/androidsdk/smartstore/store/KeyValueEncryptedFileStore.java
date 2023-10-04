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

package com.salesforce.androidsdk.smartstore.store;

import android.content.Context;
import android.text.TextUtils;

import com.salesforce.androidsdk.analytics.security.Encryptor;
import com.salesforce.androidsdk.security.SalesforceKeyGenerator;
import com.salesforce.androidsdk.smartstore.util.SmartStoreLogger;
import com.salesforce.androidsdk.util.ManagedFilesHelper;

import java.io.ByteArrayInputStream;
import java.io.DataInputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.FileOutputStream;
import java.io.FilenameFilter;
import java.io.IOException;
import java.io.InputStream;
import java.util.HashSet;
import java.util.Set;

/**
 * Key-value store backed by file system. Currently uses an in-memory solution for encryption and
 * decryption. While this solution is not particularly good from a memory standpoint, we will need
 * to employ this as a workaround for now, until we figure out how we can achieve acceptable
 * performance with a streaming solution, since CipherInputStream is a lot slower with AES-GCM.
 */
public class KeyValueEncryptedFileStore implements KeyValueStore {

    // 1 --> 9.0 (no version files, only values are stored in files named hash(key)
    // 2 --> starting at 9.1 (version file, keys stored in files named <hash(key)>.key and values stored in files named <hash(key)>.value
    public static final int KV_VERSION = 2;

    private static final String TAG = KeyValueEncryptedFileStore.class.getSimpleName();
    public static final int MAX_STORE_NAME_LENGTH = 96;
    public static final String KEY_SUFFIX = ".key";
    public static final String VALUE_SUFFIX = ".value";
    public static final String VERSION_FILE_NAME = "version";
    public static final String KEY_VALUE_STORES = "keyvaluestores";

    private String encryptionKey;
    private int kvVersion;
    private final File storeDir;

    /**
     * Constructor
     *
     * @param storeName name for key value store
     * @param encryptionKey encryption key for key value store
     */
    public KeyValueEncryptedFileStore(Context ctx,  String storeName, String encryptionKey) {
        this(computeParentDir(ctx), storeName, encryptionKey);
    }

    /**
     * Constructor
     *
     * @param parentDir parent directory for key value store
     * @param storeName name for key value store
     * @param encryptionKey encryption key for key value store
     */
    KeyValueEncryptedFileStore(File parentDir, String storeName, String encryptionKey) {
        if (!isValidStoreName(storeName)) {
            throw new IllegalArgumentException("Invalid store name: " + storeName);
        }
        storeDir = new File(parentDir, storeName);
        this.encryptionKey = encryptionKey;

        if (!storeDir.exists()) {
            storeDir.mkdirs();
            writeVersion(KV_VERSION);
            kvVersion = KV_VERSION;
        } else {
            kvVersion = readVersion();
        }

        if (!storeDir.exists() || !storeDir.isDirectory()) {
            throw new IllegalArgumentException("Failed to create directory for: " + storeName);
        }
    }

    /**
     * Return boolean indicating if a key value store with the given (full) name already exists
     * @param ctx
     * @param storeName full store name
     * @return True - if store was found
     */
    public static boolean hasKeyValueStore(Context ctx, String storeName) {
        return new File(computeParentDir(ctx), storeName).exists();
    }

    /**
     * Remove key value store with given (full) name
     * @param ctx
     * @param storeName full store name
     */
    public static void removeKeyValueStore(Context ctx, String storeName) {
        ManagedFilesHelper.deleteFile(new File(computeParentDir(ctx), storeName));
    }

    /**
     * Return parent directory for all key stores
     * @param ctx
     * @return File for parent directory
     */
    public static File computeParentDir(Context ctx) {
        return new File(ctx.getApplicationInfo().dataDir, KEY_VALUE_STORES);
    }

    /**
     * Store name can only contain letters, digits and _ and cannot exceed 96 characters
     * @param storeName
     * @return True if the name provided is valid for a store
     */
    public static boolean isValidStoreName(String storeName) {
        return storeName != null && storeName.length() > 0 && storeName.length() <= MAX_STORE_NAME_LENGTH
            && storeName.matches("^[a-zA-Z0-9_]*$");
    }

    /**
     * Return true if store contains a file for the given key
     * @param key
     * @return
     */
    @Override
    public boolean contains(String key) {
        if (!isKeyValid(key, "contains")) {
            return false;
        }

        return getKeyFile(key).exists() && getValueFile(key).exists();
    }

    /**
     * Save value for the given key.
     *
     * @param key Unique identifier.
     * @param value Value to be persisted.
     * @return True - if successful, False - otherwise.
     */
    @Override
    public boolean saveValue(String key, String value) {
        if (!isKeyValid(key, "saveValue")) {
            return false;
        }
        if (value == null) {
            SmartStoreLogger.w(TAG, "saveValue: Invalid value supplied: null");
            return false;
        }
        try {
            if (kvVersion >= 2) encryptStringToFile(getKeyFile(key), key, encryptionKey);
            encryptStringToFile(getValueFile(key), value, encryptionKey);
            return true;
        } catch (Exception e) {
            SmartStoreLogger.e(TAG, "Exception occurred while saving value to filesystem", e);
            return false;
        }
    }

    /**
     * Save value given as an input stream for the given key.
     * NB: does not close provided input stream
     *
     * @param key Unique identifier.
     * @param stream Stream to be persisted.
     * @return True - if successful, False - otherwise.
     */
    @Override
    public boolean saveStream(String key, InputStream stream) {
        if (!isKeyValid(key, "saveStream")) {
            return false;
        }
        if (stream == null) {
            SmartStoreLogger.w(TAG, "saveStream: Invalid stream supplied: null");
            return false;
        }
        try {
            if (kvVersion >=2) encryptStringToFile(getKeyFile(key), key, encryptionKey);
            encryptStreamToFile(getValueFile(key), stream, encryptionKey);
            return true;
        } catch (Exception e) {
            SmartStoreLogger.e(TAG, "Exception occurred while saving stream to filesystem", e);
            return false;
        }
    }

    /**
     * Returns value stored for given key.
     *
     * @param key Unique identifier.
     * @return value for given key or null if key not found.
     */
    @Override
    public String getValue(String key) {
        if (!isKeyValid(key, "getValue")) {
            return null;
        }
        final File file = getValueFile(key);
        if (!file.exists()) {
            SmartStoreLogger.w(TAG, "getValue: File does not exist for key: " + key);
            return null;
        }
        try {
            return decryptFileAsString(file, encryptionKey);
        } catch (Exception e) {
            SmartStoreLogger.e(TAG, "getValue: Threw exception for key: " + key, e);
            return null;
        }
    }

    /**
     * Returns stream for value of given key.
     *
     * @param key Unique identifier.
     * @return stream to value for given key or null if key not found.
     */
    @Override
    public InputStream getStream(String key) {
        if (!isKeyValid(key, "getStream")) {
            return null;
        }
        final File file = getValueFile(key);
        if (!file.exists()) {
            SmartStoreLogger.w(TAG, "getStream: File does not exist for key: " + key);
            return null;
        }
        try {
            return decryptFileAsSteam(file, encryptionKey);
        } catch (Exception e) {
            SmartStoreLogger.e(TAG, "getStream: Threw exception for key: " + key, e);
            return null;
        }
    }

    /**
     * Deletes stored value for given key.
     *
     * @param key Unique identifier.
     * @return True - if successful, False - otherwise.
     */
    @Override
    public synchronized boolean deleteValue(String key) {
        if (!isKeyValid(key, "deleteValue")) {
            return false;
        }
        if (kvVersion >= 2)  getKeyFile(key).delete();
        return getValueFile(key).delete();
    }

    /** Deletes all stored values. */
    @Override
    public void deleteAll() {
        if (kvVersion == 1) {
            for (File file : safeListFiles(null)) {
                SmartStoreLogger.i(TAG, "deleting file :" + file.getName());
                file.delete();
            }
        } else {
            for (File file : safeListFiles(KEY_SUFFIX)) {
                SmartStoreLogger.i(TAG, "deleting file :" + file.getName());
                file.delete();
            }
            for (File file : safeListFiles(VALUE_SUFFIX)) {
                SmartStoreLogger.i(TAG, "deleting file :" + file.getName());
                file.delete();
            }
        }
    }

    /**
     * Get all keys.
     * NB: will throw UnsupportedOperationException for a v1 store
     */
    @Override
    public Set<String> keySet()  {
        if (kvVersion == 1) {
            throw new UnsupportedOperationException("keySet() not supported on v1 stores");
        }

        HashSet<String> keys = new HashSet<>();
        for (File file: safeListFiles(KEY_SUFFIX)) {
            try {
                String key =  decryptFileAsString(file, encryptionKey);
                keys.add(key);
            } catch (Exception e) {
                SmartStoreLogger.e(TAG, "keySet(): Threw exception for:" + file.getName(), e);
                // skip the bad key but keep going
            }
        }
        return keys;
    }

    /** @return number of entries in the store. */
    @Override
    public int count() {
        return kvVersion == 1 ? safeListFiles(null /* all */).length : keySet().size();
    }

    /** @return True if store is empty. */
    @Override
    public boolean isEmpty() {
        return count() == 0;
    }

    /**
     * @return store directory
     */
    public File getStoreDir() {
        return storeDir;
    }

    /**
     * @return store name
     */
    @Override
    public String getStoreName() {
        return storeDir.getName();
    }

    /**
     * @return store version
     */
    public int getStoreVersion() {
        return kvVersion;
    }

    /**
     * Change encryption key
     * All files are read/decrypted with old key and encrypted/written back with new key
     * @param newEncryptionKey
     * @return true if successful
     */
    public boolean changeEncryptionKey(String newEncryptionKey) {
        File originalStoreDir = storeDir;
        String storeName = getStoreName();
        File tmpDir = new File(storeDir.getParent(), storeName + "-tmp");
        tmpDir.mkdirs();
        if (!tmpDir.isDirectory()) {
            SmartStoreLogger.e(TAG, "changeKey: Failed to create tmp directory: " + tmpDir);
            return false;
        }
        // NB: - not allowed for store name so no chances of hitting colliding with existing store
        File[] originalFiles = originalStoreDir.listFiles();
        for (File originalFile : originalFiles) {
            try {
                encryptStreamToFile(
                    new File(tmpDir, originalFile.getName()), // tmp file
                    decryptFileAsSteam(originalFile, encryptionKey),   // reading original file
                    newEncryptionKey);                        // encrypting with new encryption key
            } catch (Exception e) {
                SmartStoreLogger.e(TAG, "changeKey: Threw exception for file: " + originalFile, e);
                //Failed
                return false;
            }
        }
        // Removing old store dir - renaming tmp dir
        ManagedFilesHelper.deleteFile(originalStoreDir);
        tmpDir.renameTo(originalStoreDir);

        // Updating encryption key
        encryptionKey = newEncryptionKey;

        // Successful
        return true;
    }

    private String encodeKey(String key) {
        return SalesforceKeyGenerator.getSHA256Hash(key);
    }

    private File getKeyFile(String key) {
        return new File(storeDir, encodeKey(key) + KEY_SUFFIX);
    }

    private File getValueFile(String key) {
        String valueFileName = kvVersion == 1 ? encodeKey(key) : encodeKey(key) + VALUE_SUFFIX;
        return new File(storeDir, valueFileName);
    }

    private File getVersionFile() {
        return new File(storeDir, VERSION_FILE_NAME);
    }

    private boolean isKeyValid(String key, String operation) {
        if (TextUtils.isEmpty(key)) {
            SmartStoreLogger.w(TAG, operation + ": Invalid key supplied: " + key);
            return false;
        }
        return true;
    }

    /**
     * Get array of Files in storeDir that ends with suffix
     * Returns all files if suffix is null
     * Returns empty array if storeDir does not exist
     */
    private File[] safeListFiles(final String suffix) {
        FilenameFilter filter = new FilenameFilter() {
            @Override
            public boolean accept(File file, String name) {
                return suffix == null ? true : name.endsWith(suffix);
            }
        };
        File[] files = storeDir == null ? null : storeDir.listFiles(filter);
        return files == null ? new File[0] : files;
    }

    String decryptFileAsString(File file, String encryptionKey) throws IOException {
        return Encryptor.getStringFromStream(decryptFileAsSteam(file, encryptionKey));
    }

    InputStream decryptFileAsSteam(File file, String encryptionKey) throws IOException {
        FileInputStream f = null;
        try {
            f = new FileInputStream(file);
            final DataInputStream data = new DataInputStream(f);
            byte[] bytes = new byte[(int) file.length()];
            data.readFully(bytes);
            final byte[] decryptedBytes = Encryptor
                .decryptWithoutBase64Encoding(bytes, encryptionKey);
            if (decryptedBytes != null) {
                return new ByteArrayInputStream(decryptedBytes);
            }
            return null;
        } finally {
             if (f != null) {
                 f.close();
             }
        }
    }

    void encryptStringToFile(File file, String content, String encryptionKey) throws IOException {
        encryptBytesToFile(file, content.getBytes(), encryptionKey);
    }

    void encryptStreamToFile(File file, InputStream stream, String encryptionKey) throws IOException {
        byte[] content = Encryptor.getByteArrayStreamFromStream(stream).toByteArray();
        encryptBytesToFile(file, content, encryptionKey);
    }

    void encryptBytesToFile(File file, byte[] content, String encryptionKey) throws IOException {
        FileOutputStream f = null;
        try {
            byte[] encryptedContent = Encryptor.encryptWithoutBase64Encoding(content, encryptionKey);
            f = new FileOutputStream(file);
            if (encryptedContent != null) {
                f.write(encryptedContent);
            }
        } finally {
            if (f != null) {
                f.close();
            }
        }
    }

    void writeVersion(int kvVersion) {
        try {
            encryptStringToFile(getVersionFile(), kvVersion + "", encryptionKey);
        } catch (Exception e) {
            SmartStoreLogger.e(TAG, "Failed to store version", e);
            // What now ??
        }
    }

    int readVersion() {
        try {
            return Integer.parseInt(decryptFileAsString(getVersionFile(), encryptionKey));
        } catch (Exception e) {
            if (!e.getClass().equals(FileNotFoundException.class)) {
                // Version 1 did not have a version file - no need to log an error
                SmartStoreLogger.e(TAG, "Failed to retrieve version", e);
            }
            return 1;
        }
    }

}
