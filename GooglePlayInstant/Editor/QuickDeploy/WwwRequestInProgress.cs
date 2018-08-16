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
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

[assembly: InternalsVisibleTo("GooglePlayInstant.Tests.Editor.QuickDeploy")]

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Provides functionality for tracking HTTP requests represented by corresponding WWW instances, as well as
    /// executing scheduled actions on the responses when the requests are complete.
    /// </summary>
    public static class WwwRequestInProgress
    {
        // Only one request can be performed at a time
        private static OngoingRequest _ongoingRequest;


        /// <summary>
        /// Monitor an HTTP request being made. Assumes one request will be made at a time, therefore will throw
        /// an exception if a previous request is still being monitored.
        /// </summary>
        /// <param name="www">A www instance holding a request that was made</param>
        /// <param name="progressBarTitleText">A descriptive text to display as the title of the progress bar.</param>
        /// <param name="onResponseAvailableAction">An action to be invoked on www once the response to the request
        /// is available.</param>
        public static void TrackProgress(WWW www, string progressBarTitleText, Action<WWW> onResponseAvailableAction)
        {
            if (_ongoingRequest != null)
            {
                throw new Exception("Cannot start another request while the previous one is not complete.");
            }

            _ongoingRequest = new OngoingRequest(www, progressBarTitleText, onResponseAvailableAction);
        }

        /// <summary>
        /// Verifies the state of the currently monitored ongoing request. Displays progress bar for the request if
        /// it is still going on. If the response is available, the scheduled post-completion action will be invoked on
        /// the www instance of the ongoing request, the request will stop being monitored and resources used by this
        /// request will be disposed.
        /// </summary>
        public static void Update()
        {
            if (_ongoingRequest == null)
            {
                return;
            }

            if (_ongoingRequest.RequestWww.isDone)
            {
                EditorUtility.ClearProgressBar();
                var onGoingRequest = _ongoingRequest;
                // Set the static field _requestInProgress to null before invoking the  post-completion action
                // because the action could be executing an action that will read or overwrite the static field.
                _ongoingRequest = null;
                onGoingRequest.OnResponseAvailableAction(onGoingRequest.RequestWww);
                onGoingRequest.Dispose();
            }
            else
            {
                if (EditorUtility.DisplayCancelableProgressBar(_ongoingRequest.ProgressBarTitleText,
                    string.Format("Progress: {0}%", Math.Floor(_ongoingRequest.RequestWww.uploadProgress * 100)),
                    _ongoingRequest.RequestWww.uploadProgress))
                {
                    EditorUtility.ClearProgressBar();
                    _ongoingRequest.Dispose();
                    _ongoingRequest = null;
                }
            }
        }

        /// <summary>
        /// Encapsulates a WWW instance used to send an HTTP request, the title to display in the progress bar and
        /// an action to invoke on the WWW instance when the request is complete.
        /// </summary>
        private class OngoingRequest : IDisposable
        {
            public WWW RequestWww { get; private set; }
            public string ProgressBarTitleText { get; private set; }
            public Action<WWW> OnResponseAvailableAction { get; private set; }

            /// <summary>
            /// Create an instance of a disposable OnGoingRequest.
            /// </summary>
            /// <param name="requestWww">A www instance holding a request that was made</param>
            /// <param name="progressBarTitleText">A descriptive text to display as the title of the progress bar.</param>
            /// <param name="onResponseAvailableAction">An action to be invoked on the WWW instance holding the request
            /// when the response is available.</param>
            public OngoingRequest(WWW requestWww, string progressBarTitleText, Action<WWW> onResponseAvailableAction)
            {
                RequestWww = requestWww;
                ProgressBarTitleText = progressBarTitleText;
                OnResponseAvailableAction = onResponseAvailableAction;
            }

            /// <summary>
            /// Disposes of any existing resources used by the OnGoingRequest instance.
            /// </summary>
            public void Dispose()
            {
                RequestWww.Dispose();
            }
        }
    }
}