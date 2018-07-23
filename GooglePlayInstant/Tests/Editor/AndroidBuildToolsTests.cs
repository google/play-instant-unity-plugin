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
using System.Linq;
using GooglePlayInstant.Editor;
using NUnit.Framework;

namespace GooglePlayInstant.Tests.Editor
{
    [TestFixture]
    public class AndroidBuildToolsTests
    {
        [Test]
        public void TestGetNewestVersion_Null()
        {
            Assert.Throws<NullReferenceException>(() => AndroidBuildTools.GetNewestVersion(null));
        }

        [Test]
        public void TestGetNewestVersion_Empty()
        {
            Assert.IsNull(AndroidBuildTools.GetNewestVersion(Enumerable.Empty<string>()));
        }

        [Test]
        public void TestGetNewestVersion_InvalidDirectoryNames()
        {
            AssertInvalid("");
            AssertInvalid("abc");
            AssertInvalid("a.b.c");
            AssertInvalid("1");
            AssertInvalid("1.2");
            AssertInvalid("1.2-rc1");
            AssertInvalid("1.2.3-rc");
            AssertInvalid("1.2.3.4");
            AssertInvalid("1.2.3.4-rc1");
        }

        [Test]
        public void TestGetNewestVersion_ValidDirectoryNames()
        {
            AssertValid("0.0.0");
            AssertValid("1.2.3");
            AssertValid("1.2.3-rc0");
            AssertValid("1.2.3-rc1");
            AssertValid("9999.9999.9999");
            AssertValid("9999.9999.9999-rc4999");
        }

        [Test]
        public void TestGetNewestVersion_VersionTooBig()
        {
            Assert.Throws<ArgumentException>(() => GetNewestVersion("10000.9999.9999-rc4999"));
            Assert.Throws<ArgumentException>(() => GetNewestVersion("9999.10000.9999-rc4999"));
            Assert.Throws<ArgumentException>(() => GetNewestVersion("9999.9999.10000-rc4999"));
            Assert.Throws<ArgumentException>(() => GetNewestVersion("9999.9999.9999-rc5000"));
        }

        [Test]
        public void TestGetNewestVersion_Multiple()
        {
            Assert.AreEqual("0.0.1", GetNewestVersion("0.0.1", "0.0.0"));
            Assert.AreEqual("0.1.0", GetNewestVersion("0.0.0", "0.0.1", "0.1.0"));
            Assert.AreEqual("1.0.0", GetNewestVersion("1.0.0", "0.0.1"));
            Assert.AreEqual("2.0.1", GetNewestVersion("1.2.3", "2.0.1"));
            Assert.AreEqual("10.0.1", GetNewestVersion("9.99.99", "10.0.1"));
            Assert.AreEqual("2.0.1-rc2", GetNewestVersion("2.0.1-rc2", "2.0.0"));
            Assert.AreEqual("2.0.1-rc2", GetNewestVersion("2.0.1-rc2", "2.0.1-rc1"));
            Assert.AreEqual("2.0.1", GetNewestVersion("2.0.1-rc2", "2.0.1"));
            Assert.AreEqual("22.33.44", GetNewestVersion("22.33.44-rc55", "22.33.44"));
        }

        private static void AssertInvalid(string value)
        {
            Assert.IsNull(GetNewestVersion(value));
        }

        private static void AssertValid(string value)
        {
            Assert.AreEqual(value, GetNewestVersion(value));
        }

        // Helper method that converts a params array to an IEnumerable.
        private static string GetNewestVersion(params string[] values)
        {
            return AndroidBuildTools.GetNewestVersion(values);
        }
    }
}