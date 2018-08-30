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

using UnityEngine;

namespace GooglePlayInstant.Samples.TestApp
{
    /// <summary>
    /// Tests instant app plugin features through button clicks
    /// </summary>
    public class TestApp : MonoBehaviour
    {
        private const string CookiePrefix = "test-cookie";
        private string _storedCookie;

        /// <summary>
        /// Sets the instant app cookie to a unique string
        /// </summary>
        public void ButtonEventWriteCookie()
        {
            //Write a random value so WriteCookie will always change the cookie
            // Note: System.Guid is unavailable with micro mscorlib.
            var guid = Random.Range(int.MinValue, int.MaxValue);
            _storedCookie = string.Format("{0}:{1}", guid, CookiePrefix);
            Debug.LogFormat("Attempting to write cookie: {0}", _storedCookie);
            if (CookieApi.SetInstantAppCookie(_storedCookie))
            {
                Debug.LogFormat("Successfully wrote cookie");
            }
            else
            {
                Debug.LogError("Failed to write cookie");                
            }
        }
        
        /// <summary>
        /// Reads the cookie and verifies if it matches the one we stored
        /// </summary>
        public void ButtonEventReadCookie()
        {
            // TODO: Currently reading the cookie from the instant app. Prefer to read it from installed app.
            var readCookie = CookieApi.GetInstantAppCookie();
            Debug.LogFormat("Successfully read cookie: {0}", readCookie);
            if (string.Equals(readCookie, _storedCookie))
            {
                Debug.Log("Read cookie matches the value we stored");
            }
            else
            {
                Debug.LogError("Read cookie does not match the value we stored");
            }
        }
        
        public void ButtonEventShowInstallPrompt()
        {
            // TODO: test all aspects of this API
            InstallLauncher.ShowInstallPrompt();
        }
    }
}