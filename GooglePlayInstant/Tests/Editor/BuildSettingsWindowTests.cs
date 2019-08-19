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

using GooglePlayInstant.Editor;
using NUnit.Framework;

namespace GooglePlayInstant.Tests.Editor
{
    [TestFixture]
    public class BuildSettingsWindowTests
    {
        [Test]
        public void TestGetInstantUri_NullOrBlank()
        {
            string instantUrlError;

            Assert.IsNull(BuildSettingsWindow.GetInstantUri(null, out instantUrlError));
            Assert.IsNull(instantUrlError);

            Assert.IsNull(BuildSettingsWindow.GetInstantUri("", out instantUrlError));
            Assert.IsNull(instantUrlError);

            Assert.IsNull(BuildSettingsWindow.GetInstantUri(" ", out instantUrlError));
            Assert.IsNull(instantUrlError);
        }

        [Test]
        public void TestGetInstantUri_InvalidUri()
        {
            CheckInvalidUri("://");
            CheckInvalidUri("google.com");
            CheckInvalidUri("https://");
            CheckInvalidUri("https://.");
        }

        [Test]
        public void TestGetInstantUri_HttpsScheme()
        {
            CheckHttpsScheme("ftp://google.com");
            CheckHttpsScheme("http://google.com");
        }

        [Test]
        public void TestGetInstantUri_InstantAppHost()
        {
            CheckInstantAppsHost("https://instant.app");
            CheckInstantAppsHost("https://instant.app/my.package.name");
            CheckInstantAppsHost("https://Instant.App");
        }

        [Test]
        public void TestGetInstantUri_ValidUri()
        {
            CheckValidUri("https://a.com/", "https://a.com");
            CheckValidUri("https://b.com/", "https://b.com/");
            CheckValidUri("https://c.com/", "HTTPS://c.com/");
            CheckValidUri("https://d.com/", "https://d.com/.");
            CheckValidUri("https://e.com/", "   https://e.com/   ");
            CheckValidUri("https://f.com/instant", "https://f.com/instant");
            CheckValidUri("https://g.com/instant/", "  HtTpS://g.com/instant/  ");

            // TODO: Should we allow port numbers or query parameters?
            CheckValidUri("https://h.com:1234/instant", "https://h.com:1234/instant");
            CheckValidUri("https://i.com/instant?test", "https://i.com/instant?test");
        }

        private static void CheckInvalidUri(string instantUrl)
        {
            string instantUrlError;
            Assert.IsNull(BuildSettingsWindow.GetInstantUri(instantUrl, out instantUrlError));
            Assert.IsTrue(instantUrlError.StartsWith("The URL is invalid"));
        }

        private static void CheckHttpsScheme(string instantUrl)
        {
            string instantUrlError;
            Assert.IsNull(BuildSettingsWindow.GetInstantUri(instantUrl, out instantUrlError));
            Assert.IsTrue(instantUrlError.StartsWith("The URL scheme"), instantUrl);
        }

        private static void CheckInstantAppsHost(string instantUrl)
        {
            string instantUrlError;
            Assert.IsNull(BuildSettingsWindow.GetInstantUri(instantUrl, out instantUrlError));
            Assert.IsTrue(instantUrlError.Contains("using the Launch API"));
        }

        private static void CheckValidUri(string expectedUrl, string actualUrl)
        {
            string instantUrlError;
            var actualUri = BuildSettingsWindow.GetInstantUri(actualUrl, out instantUrlError);
            Assert.AreEqual(expectedUrl, actualUri.ToString());
            Assert.IsNull(instantUrlError);
        }
    }
}