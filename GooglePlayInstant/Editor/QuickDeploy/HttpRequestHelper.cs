// Copyright 2018 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("GooglePlayInstant.Tests.Editor.QuickDeploy")]

namespace GooglePlayInstant.Editor.QuickDeploy
{
    /// <summary>
    /// A class with utility methods for sending GET and POST requests. Uses WWW and WWWForm instances to send HTTP
    /// requests for backward compatibility with older versions of Unity.
    /// </summary>
    public static class HttpRequestHelper
    {
        /// <summary>
        /// Sends a general POST request to the provided endpoint, along with the data provided in the byte-array and
        /// header parameters.
        /// </summary>
        /// <param name="endpoint">Endpoint to which the data should be sent.</param>
        /// <param name="postData">An array of bytes representing the data that will be passed in the POST request
        /// body.</param>
        /// <param name="postHeaders">A collection of key-value pairs to be added to the request headers.</param>
        /// <returns>A reference to the WWW instance representing the request in progress.</returns>
        public static WWW SendHttpPostRequest(string endpoint, byte[] postData,
            Dictionary<string, string> postHeaders)
        {
            var form = new WWWForm();
            var newHeaders = GetCombinedDictionary(form.headers, postHeaders);
            return new WWW(endpoint, postData, newHeaders);
        }

        /// <summary>
        /// Send a general POST request to the provided endpoint, along with the data provided in the form and headers.
        /// parameters.
        /// </summary>
        /// <param name="endpoint">Endpoint to which the data should be sent.</param>
        /// <param name="postParams">A set of key-value pairs to be added to the request body.</param>
        /// <param name="postHeaders"> A set of key-value pairs to be added to the request headers.</param>
        /// <returns>A reference to the WWW instance representing the request.</returns>
        public static WWW SendHttpPostRequest(string endpoint, Dictionary<string, string> postParams,
            Dictionary<string, string> postHeaders)
        {
            var form = GetWwwForm(postParams);
            return SendHttpPostRequest(endpoint, postParams != null ? form.data : null,
                GetCombinedDictionary(form.headers, postHeaders));
        }


        /// <summary>
        /// Sends a general GET request to the specified endpoint along with specified parameters and headers.
        /// </summary>
        /// <param name="endpoint">The endpoint where the GET request should be sent. Must have no query params.</param>
        /// <param name="getParams">A collection of key-value pairs to be attached to the endpoint as GET
        /// parameters.</param>
        /// <param name="getHeaders">A collection of key-value pairs to be added to the request headers.</param>
        /// <returns>A reference to the WWW instance representing the request.</returns>
        public static WWW SendHttpGetRequest(string endpoint, Dictionary<string, string> getParams,
            Dictionary<string, string> getHeaders)
        {
            return new WWW(GetEndpointWithGetParams(endpoint, getParams), null,
                GetCombinedDictionary(new WWWForm().headers, getHeaders));
        }

        /// <summary>
        /// Returns a WWWform containing the body params to use for a POST request.
        /// </summary>
        internal static WWWForm GetWwwForm(Dictionary<string, string> postParams)
        {
            var form = new WWWForm();
            if (postParams != null)
            {
                foreach (var pair in postParams)
                {
                    form.AddField(pair.Key, pair.Value);
                }
            }

            return form;
        }

        /// <summary>
        /// Adds GET params to endpoint and returns result.
        /// </summary>
        /// <remarks>Assumes endpoint does not have any queries attached to it.</remarks>
        internal static string GetEndpointWithGetParams(string endpoint, Dictionary<string, string> getParams)
        {
            var uriBuilder = new UriBuilder(endpoint);
            if (getParams != null)
            {
                uriBuilder.Query = string.Join("&",
                    getParams.Select(kvp => string.Format("{0}={1}", Uri.EscapeDataString(kvp.Key), Uri.EscapeDataString(kvp.Value)))
                        .ToArray());
            }

            return uriBuilder.ToString();
        }

        /// <summary>
        /// Combines two dictionaries into a single dictionary. Values in the second argument override values of the
        /// first argument for every key that is present in both dictionaries.
        /// </summary>
        internal static Dictionary<string, string> GetCombinedDictionary(Dictionary<string, string> firstDictionary,
            Dictionary<string, string> secondDictionary)
        {
            var combinedDictionary = new Dictionary<string, string>();
            if (firstDictionary != null)
            {
                foreach (var pair in firstDictionary)
                {
                    combinedDictionary[pair.Key] = pair.Value;
                }
            }

            if (secondDictionary != null)
            {
                foreach (var pair in secondDictionary)
                {
                    combinedDictionary[pair.Key] = pair.Value;
                }
            }

            return combinedDictionary;
        }
    }
}