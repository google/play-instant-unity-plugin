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

using GooglePlayInstant.Editor.QuickDeploy;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace GooglePlayInstant.Tests.Editor.QuickDeploy
{
    /// <summary>
    /// Contains tests for each of AssetBundleVerifier error states.
    /// </summary>
    [TestFixture]
    public class AssetBundleVerifierTest
    {
        [Test]
        public void TestDestinationError()
        {
            var window = AssetBundleVerifierWindow.ShowWindow();

            window.HandleAssetBundleVerifyState(AssetBundleVerifierWindow.AssetBundleVerifyState.DestinationError,
                new UnityWebRequest());

            Assert.IsFalse(window.AssetBundleDownloadIsSuccessful,
                "AssetBundle download should not be marked successful during destination error state.");

            window.Close();
        }

        [Test]
        public void TestWebRequestError()
        {
            var window = AssetBundleVerifierWindow.ShowWindow();

            window.HandleAssetBundleVerifyState(AssetBundleVerifierWindow.AssetBundleVerifyState.WebRequestError,
                new UnityWebRequest());

            Assert.IsFalse(window.AssetBundleDownloadIsSuccessful,
                "AssetBundle download should not be marked successful during destination error state.");

            LogAssert.Expect(LogType.Error,
                string.Format(AssetBundleVerifierWindow.WebRequestErrorFormatMessage, window.AssetBundleUrl,
                    window.ErrorDescription));

            window.Close();
        }

        [Test]
        public void TestBundleError()
        {
            var window = AssetBundleVerifierWindow.ShowWindow();

            window.HandleAssetBundleVerifyState(AssetBundleVerifierWindow.AssetBundleVerifyState.BundleError,
                new UnityWebRequest());

            Assert.IsFalse(window.AssetBundleDownloadIsSuccessful,
                "AssetBundle download should not be marked successful during bundle error state.");

            window.Close();
        }
    }
}