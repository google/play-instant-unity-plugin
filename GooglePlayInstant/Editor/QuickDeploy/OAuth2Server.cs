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
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

[assembly: InternalsVisibleTo("GooglePlayInstant.Tests.Editor.QuickDeploy")]

namespace GooglePlayInstant.Editor.QuickDeploy
{
    /// <summary>
    /// A class representing a server that provides an endpoint to use for getting authorization code from Google's
    /// OAuth2 API's authorization page. On that page, the user grants the application to access their data on Google
    /// Cloud platform. The server attempt to open and start listening at a fixed port. The server announces
    /// its chosen endpoint with the CallbackEndpoint property. This server will run until it receives the first
    /// request, which it will process to retrieve the authorization response and handle the by invoking on the response
    /// the handler passed to the server during instatiation. The server will then stop listening for further requests.
    ///
    /// <see cref="https://developers.google.com/identity/protocols/OAuth2#installed"/> for an overview of OAuth2
    /// protocol for installed applications that interact with Google APIs.
    /// </summary>
    public class OAuth2Server
    {
        private const string CloseTabText = "You may close this tab.";

        // Arbitrarily chosen port to listen for the authorization callback.
        private const int ServerPort = 2806;

        private readonly HttpListener _httpListener;
        private readonly string _callbackEndpoint;
        private readonly Action<KeyValuePair<string, string>> _onResponseAction;

        /// <summary>
        /// An instance of a server that will run locally to retrieve authorization code. The server will stop running
        /// once the first response gets received.
        /// </summary>
        /// <param name="onResponseAction">An action to be invoked on the key-value pair representing the first
        /// response that will be caught by the server. Note that the invocation of this action will not be done
        /// on Unity's main thread, therefore the action should only be performing operations that can be run off the
        /// main thread. 
        /// </param>
        public OAuth2Server(Action<KeyValuePair<string, string>> onResponseAction)
        {
            _onResponseAction = onResponseAction;
            _callbackEndpoint = string.Format("http://localhost:{0}/{1}/", ServerPort, Path.GetRandomFileName());
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(_callbackEndpoint);
        }

        /// <summary>
        /// The callback endpoint on which the server is listening.
        /// </summary>
        public string CallbackEndpoint
        {
            get { return _callbackEndpoint; }
        }

        /// <summary>
        /// Allow this instance to start listening for incoming requests containing authorization code or error data
        /// from OAuth2. Server will stop after processing the first request.
        /// </summary>
        public void Start()
        {
            _httpListener.Start();
            // Server will only respond to the first request, therefore one new thread should handle it just fine.
            new Thread(() => { ProcessContext(_httpListener.GetContext()); }).Start();
        }

        /// <summary>
        /// Processes the object as an HttpListenerContext instance and retrieves authorization response. Invokes the
        /// response handler action on the response, responds to request with a string asking the user to close tab,
        /// and stops the server from listening for future incoming requests.
        /// </summary>
        private void ProcessContext(HttpListenerContext context)
        {
            var authorizationResponse = GetAuthorizationResponse(context.Request.Url);
            _onResponseAction(authorizationResponse);
            context.Response.KeepAlive = false;
            var responsebBytes = Encoding.UTF8.GetBytes(CloseTabText);
            var outputStream = context.Response.OutputStream;
            outputStream.Write(responsebBytes, 0, responsebBytes.Length);
            outputStream.Flush();
            outputStream.Close();
            context.Response.Close();
            Stop();
        }

        /// <summary>
        /// Returns a key value pair corresponding to the authorization response sent from OAuth2 authorization page.
        /// Logs error message and throws ArgumentException if the uri query contains invalid params.
        /// </summary>
        /// <see cref="GetCodeOrErrorResponsePair"/>On criteria for valid query params.
        /// <param name="uri">The uri of the incoming request</param>
        /// <exception cref="ArgumentException">Exception thrown if the uri contains invalid params.</exception>
        internal static KeyValuePair<string, string> GetAuthorizationResponse(Uri uri)
        {
            var query = GetQueryString(uri);
            return GetCodeOrErrorResponsePair(query);
        }

        /// <summary>
        /// Returns a query string from the given uri.
        /// </summary>
        /// <exception cref="ArgumentException">Exception thrown when the uri query is null or empty or if it does not
        /// start with '?'.</exception>
        internal static string GetQueryString(Uri uri)
        {
            var query = uri.Query;
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentException("URI is missing query parameters");
            }

            if (query[0] != '?')
            {
                throw new ArgumentException("Expect first character of URI query is \"?\"");
            }

            return query.Substring(1);
        }

        /// <summary>
        /// Returns a key/value pair corresponding to the code or error response present in the query, and throws
        /// an ArgumentException when the query is not valid.
        /// For a query to be valid:
        ///     1. The query representing authorization response should consist of a single query parameter
        ///     2. The single query parameter should be an equals-separated key/value pair
        ///     3. The key for the single query parameter should be either "code" or "error"
        /// </summary>
        internal static KeyValuePair<string, string> GetCodeOrErrorResponsePair(string query)
        {
            // The authorization response should consist of a single query parameter.
            var paramPairs = query.Split('&');
            if (paramPairs.Length != 1)
            {
                throw new ArgumentException(string.Format("Unexpected number of URI parameters {0}",
                    paramPairs.Length));
            }

            // The single query parameter should be an equals-separated key/value pair.
            var keyAndValue = paramPairs[0].Split('=');
            if (keyAndValue.Length != 2)
            {
                throw new ArgumentException(string.Format("URI parameter pair has {0} components", keyAndValue.Length));
            }

            // The key for the single query parameter should either be "code" or "error".
            var key = keyAndValue[0];
            var value = Uri.UnescapeDataString(keyAndValue[1]);
            if (key != "code" && key != "error")
            {
                throw new ArgumentException(string.Format("Unexpected URI parameter key \"{0}\"", key));
            }

            return new KeyValuePair<string, string>(key, value);
        }

        /// <summary>
        /// Stops the server from listening from incoming requests.
        /// </summary>
        private void Stop()
        {
            _httpListener.Stop();
        }
    }
}