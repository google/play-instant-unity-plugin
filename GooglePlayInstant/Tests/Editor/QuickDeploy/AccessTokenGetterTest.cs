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

namespace GooglePlayInstant.Tests.Editor.QuickDeploy
{
    /// <summary>
    /// Contains tests for the AccessTokenGetter class.
    /// </summary>
    [TestFixture]
    public class AccessTokenGetterTest
    {
        private static int HourInSeconds = 3600;

        [Test]
        public void TestStartsWithInvalidAccesstoken()
        {
            Assert.IsFalse(AccessTokenGetter.AccessToken.IsValid());
        }

        [Test]
        public void TestAccessTokenIsValid()
        {
            Assert.IsTrue(new AccessTokenGetter.GcpAccessToken("Access", "Refresh", HourInSeconds).IsValid());
            Assert.IsFalse(new AccessTokenGetter.GcpAccessToken("Access", "Refresh", -1).IsValid());
        }

        [Test]
        public void TestHasCorrectAccessAndRefreshTokenValues()
        {
            // Ensure that new created access token has a correct value.
            const string firstAccessToken = "firstAccessToken";
            const string firstRefreshToken = "firstRefreshToken";
            var firstGcpcessToken =
                new AccessTokenGetter.GcpAccessToken(firstAccessToken, firstRefreshToken, HourInSeconds);
            Assert.AreEqual(firstAccessToken, firstGcpcessToken.Value);
            // Ensure that refresh token has been shared by the entire class.
            Assert.AreEqual(firstRefreshToken, AccessTokenGetter.GcpAccessToken.RefreshToken);

            // Create new token instances with null and empty refresh tokens, which should not replace the first refresh token.
            const string secondAccessToken = "secondAccessToken";
            var secondGcpAccessToken = new AccessTokenGetter.GcpAccessToken(secondAccessToken, null, HourInSeconds);
            var thirdGcpAccessToken = new AccessTokenGetter.GcpAccessToken(secondAccessToken, "", HourInSeconds);
            // Ensure that created token has correct value, and that the refresh token is not different from the first valid one.
            Assert.AreEqual(secondAccessToken, secondGcpAccessToken.Value);
            Assert.AreEqual(secondAccessToken, thirdGcpAccessToken.Value);
            Assert.AreEqual(firstRefreshToken, AccessTokenGetter.GcpAccessToken.RefreshToken);


            // Ensure that the refresh token is updated when a new valid one is available.
            const string thirdAccessToken = "thirdAccessToken";
            const string thirdRefreshToken = "thirdRefreshToken";
            // Pass new refresh token
            var fourthGcpAccessToken =
                new AccessTokenGetter.GcpAccessToken(thirdAccessToken, thirdRefreshToken, HourInSeconds);
            Assert.AreEqual(thirdAccessToken, fourthGcpAccessToken.Value);
            // Ensure that the shared refresh token has been updated this time.
            Assert.AreEqual(thirdRefreshToken, AccessTokenGetter.GcpAccessToken.RefreshToken);
        }
    }
}