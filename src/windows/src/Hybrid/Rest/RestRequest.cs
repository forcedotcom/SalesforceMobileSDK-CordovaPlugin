using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace Salesforce.SDK.Hybrid.Rest
{
    public enum ContentTypeValues
    {
        FormUrlEncoded,
        Json,
        Xml,
        None
    }

    public sealed class RestRequest
    {
        internal SDK.Rest.RestRequest Request { set; get; }

        internal RestRequest(SDK.Rest.RestRequest request)
        {
            Request = request;
        }

        public RestRequest()
        {
            
        }

        /// <summary>
        ///     Generic constructors for arbitrary requests.
        /// </summary>
        /// <param name="method">The HTTP method for the request (GET/POST/DELETE etc)</param>
        /// <param name="path">The URI path, this will automatically be resolved against the users current instance host.</param>
        public RestRequest(HttpMethod method, string path)
            : this(method, path, null, ContentTypeValues.None, new Dictionary<string, string>())
        {
        }

        public RestRequest(HttpMethod method, string path, string requestBody)
            : this(method, path, requestBody, ContentTypeValues.FormUrlEncoded, new Dictionary<string, string>())
        {
        }

        public RestRequest(HttpMethod method, string path, string requestBody, ContentTypeValues contentType)
            : this(method, path, requestBody, contentType, new Dictionary<string, string>())
        {
        }

        public RestRequest(HttpMethod method, string path, string requestBody, ContentTypeValues contentType,
            IDictionary<string, string> additionalHeaders)
        {
            SDK.Net.ContentTypeValues value = (SDK.Net.ContentTypeValues) Enum.Parse(typeof(SDK.Net.ContentTypeValues), contentType.ToString());
            Request = new SDK.Rest.RestRequest(new System.Net.Http.HttpMethod(method.Method), path, requestBody, value, new Dictionary<string, string>(additionalHeaders));
        }

        /// <summary>
        ///     Request to get summary information about each Salesforce.com version currently available.
        ///     See http://www.salesforce.com/us/developer/docs/api_rest/index_Left.htm#StartTopic=Content/resources_versions.htm
        /// </summary>
        /// <returns>A RestRequest</returns>
        public static RestRequest GetRequestForVersions()
        {
            return new RestRequest(SDK.Rest.RestRequest.GetRequestForVersions());
        }

        /// <summary>
        ///     Request to list available resources for the specified API version, including resource name and URI.
        ///     See
        ///     http://www.salesforce.com/us/developer/docs/api_rest/index_Left.htm#StartTopic=Content/resources_discoveryresource.htm
        /// </summary>
        /// <param name="apiVersion">API version e.g. v26.0</param>
        /// <returns>A RestRequest</returns>
        public static RestRequest GetRequestForResources(string apiVersion)
        {
            return new RestRequest(SDK.Rest.RestRequest.GetRequestForResources(apiVersion));
        }

        /// <summary>
        ///     Request to list the available objects and their metadata for your organization's data.
        ///     See
        ///     http://www.salesforce.com/us/developer/docs/api_rest/index_Left.htm#StartTopic=Content/resources_describeGlobal.htm
        /// </summary>
        /// <param name="apiVersion">API version e.g. v26.0</param>
        /// <returns>A RestRequest</returns>
        public static RestRequest GetRequestForDescribeGlobal(string apiVersion)
        {
            return new RestRequest(SDK.Rest.RestRequest.GetRequestForDescribeGlobal(apiVersion));
        }

        /// <summary>
        ///     Request to describe the individual metadata for the specified object.
        ///     See
        ///     http://www.salesforce.com/us/developer/docs/api_rest/index_Left.htm#StartTopic=Content/resources_sobject_basic_info.htm
        /// </summary>
        /// <param name="apiVersion">API version e.g. v26.0</param>
        /// <param name="objectType">Ojbect type</param>
        /// <returns>A RestRequest</returns>
        public static RestRequest GetRequestForMetadata(string apiVersion, string objectType)
        {
            return new RestRequest(SDK.Rest.RestRequest.GetRequestForMetadata(apiVersion, objectType));
        }

        /// <summary>
        ///     Request to completely describe the individual metadata at all levels for the specified object.
        ///     See
        ///     http://www.salesforce.com/us/developer/docs/api_rest/index_Left.htm#StartTopic=Content/resources_sobject_describe.htm
        /// </summary>
        /// <param name="apiVersion">API version e.g. v26.0</param>
        /// <param name="objectType">Ojbect type</param>
        /// <returns>A RestRequest</returns>
        public static RestRequest GetRequestForDescribe(string apiVersion, string objectType)
        {
            return new RestRequest(SDK.Rest.RestRequest.GetRequestForDescribe(apiVersion, objectType));
        }

        /// <summary>
        ///     Request to create a record.
        ///     See
        ///     http://www.salesforce.com/us/developer/docs/api_rest/index_Left.htm#StartTopic=Content/resources_sobject_retrieve.htm
        /// </summary>
        /// <param name="apiVersion">API version e.g. v26.0</param>
        /// <param name="objectType">Ojbect type</param>
        /// <param name="fields">Fields</param>
        /// <returns>A RestRequest</returns>
        public static RestRequest GetRequestForCreate(string apiVersion, string objectType,
            IDictionary<string, object> fields)
        {
            return new RestRequest(SDK.Rest.RestRequest.GetRequestForCreate(apiVersion, objectType, new Dictionary<string, object>(fields)));
        }

        /// <summary>
        ///     Request to retrieve a record by object id.
        ///     See
        ///     http://www.salesforce.com/us/developer/docs/api_rest/index_Left.htm#StartTopic=Content/resources_sobject_retrieve.htm
        /// </summary>
        /// <param name="apiVersion">API version e.g. v26.0</param>
        /// <param name="objectType">Ojbect type</param>
        /// <param name="objectId">object id</param>
        /// <param name="fieldsList">Fields</param>
        /// <returns>A RestRequest</returns>
        public static RestRequest GetRequestForRetrieve(string apiVersion, string objectType, string objectId,
            [ReadOnlyArray()] string[] fieldsList)
        {
            return new RestRequest(SDK.Rest.RestRequest.GetRequestForRetrieve(apiVersion, objectType, objectId, fieldsList.ToArray()));
        }

        /// <summary>
        ///     Request to update a record.
        ///     See
        ///     http://www.salesforce.com/us/developer/docs/api_rest/index_Left.htm#StartTopic=Content/resources_sobject_retrieve.htm
        ///     <param name="apiVersion">API version e.g. v26.0</param>
        ///     <param name="objectType">Ojbect type</param>
        ///     <param name="objectId">object id</param>
        ///     <param name="fields">Fields</param>
        /// </summary>
        ///     <returns>A RestRequest</returns>
        public static RestRequest GetRequestForUpdate(string apiVersion, string objectType, string objectId,
            IDictionary<string, object> fields)
        {
            return new RestRequest(SDK.Rest.RestRequest.GetRequestForUpdate(apiVersion, objectType, objectId, new Dictionary<string, object>(fields)));
        }


        /// <summary>
        ///     Request to upsert (update or insert) a record.
        ///     See
        ///     http://www.salesforce.com/us/developer/docs/api_rest/index_Left.htm#StartTopic=Content/resources_sobject_retrieve.htm
        ///     <param name="apiVersion">API version e.g. v26.0</param>
        ///     <param name="objectType">object type</param>
        ///     <param name="externalIdField">External id field</param>
        ///     <param name="externalId">External id</param>
        ///     <param name="fields">Fields</param>
        /// </summary>
        ///     <returns>A RestRequest</returns>
        public static RestRequest GetRequestForUpsert(string apiVersion, string objectType, string externalIdField,
            string externalId, IDictionary<string, object> fields)
        {
            return new RestRequest(SDK.Rest.RestRequest.GetRequestForUpsert(apiVersion, objectType, externalIdField, externalId, new Dictionary<string, object>(fields)));
        }

        /// <summary>
        ///     Request to delete a record.
        ///     See
        ///     http://www.salesforce.com/us/developer/docs/api_rest/index_Left.htm#StartTopic=Content/resources_sobject_retrieve.htm
        /// </summary>
        /// <param name="apiVersion">API version e.g. v26.0</param>
        /// <param name="objectType">Ojbect type</param>
        /// <param name="objectId">object id</param>
        /// <returns>A RestRequest</returns>
        public static RestRequest GetRequestForDelete(string apiVersion, string objectType, string objectId)
        {
            return new RestRequest(SDK.Rest.RestRequest.GetRequestForDelete(apiVersion, objectType, objectId));
        }

        /// <summary>
        ///     Request to execute the specified SOSL search.
        ///     See http://www.salesforce.com/us/developer/docs/api_rest/index_Left.htm#StartTopic=Content/resources_search.htm
        /// </summary>
        /// <param name="apiVersion">API version e.g. v26.0</param>
        /// <param name="q">Query string</param>
        /// <returns>A RestRequest</returns>
        public static RestRequest GetRequestForSearch(string apiVersion, string q)
        {
            return new RestRequest(SDK.Rest.RestRequest.GetRequestForSearch(apiVersion, q));
        }

        /// <summary>
        ///     Request to execute the specified SOQL search.
        ///     See http://www.salesforce.com/us/developer/docs/api_rest/index_Left.htm#StartTopic=Content/resources_query.htm
        /// </summary>
        /// <param name="apiVersion">API version e.g. v26.0</param>
        /// <param name="q">Query string</param>
        /// <returns>A RestRequest</returns>
        public static RestRequest GetRequestForQuery(string apiVersion, string q)
        {
            return new RestRequest(SDK.Rest.RestRequest.GetRequestForQuery(apiVersion, q));
        }
    }
}
