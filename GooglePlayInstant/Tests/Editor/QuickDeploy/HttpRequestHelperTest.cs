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
using System.Text;
using GooglePlayInstant.Editor.QuickDeploy;
using NUnit.Framework;

namespace GooglePlayInstant.Tests.Editor.QuickDeploy
{
    /// <summary>
    /// Tests for HttpRequestHelper class methods.
    /// </summary>
    [TestFixture]
    public class HttpRequestHelperTest
    {
        [Test]
        public void TestGetWwwForm()
        {
            var formFromNonEmptyDictionary = new Dictionary<string, string> {{"a", "b"}, {"c", "d"}};
            var form = HttpRequestHelper.GetWwwForm(formFromNonEmptyDictionary);
            Assert.AreEqual("a=b&c=d", Encoding.UTF8.GetString(form.data));

            var formFromEmptyDictionary = HttpRequestHelper.GetWwwForm(new Dictionary<string, string>());
            Assert.IsEmpty(Encoding.UTF8.GetString(formFromEmptyDictionary.data));

            var formFromNullDictionary = HttpRequestHelper.GetWwwForm(null);
            Assert.IsEmpty(Encoding.UTF8.GetString(formFromNullDictionary.data));
        }

        [Test]
        public void TestGetEndpointWithGetParams()
        {
            const string addressPrefix = "http://localhost:5000";
            var getParams = new Dictionary<string, string> {{"a", "b"}, {"c", "d"}, {"e", "f"}};
            var endPointWithQuery = HttpRequestHelper.GetEndpointWithGetParams(addressPrefix, getParams);
            Assert.AreEqual("?a=b&c=d&e=f", new Uri(endPointWithQuery).Query);

            var endPointWithEscapedQuery = HttpRequestHelper.GetEndpointWithGetParams(addressPrefix,
                new Dictionary<string, string> {{"a", " "}, {" ", "b"}});
            Assert.AreEqual(string.Format("?a={0}&{0}=b", "%20"), new Uri(endPointWithEscapedQuery).Query);
        }

        [Test]
        public void TestGetCombinedDictionaryNonEmpty()
        {
            // Case 1: Dictionaries have mutually exclusive sets of keys.
            var firstDictionary = new Dictionary<string, string> {{"a", "A"}, {"b", "B"}};
            var secondDictionary = new Dictionary<string, string> {{"c", "C"}, {"d", "D"}};
            var firstAndSecondDictionary = HttpRequestHelper.GetCombinedDictionary(secondDictionary, firstDictionary);
            Assert.AreEqual(new Dictionary<string, string> {{"a", "A"}, {"b", "B"}, {"c", "C"}, {"d", "D"}},
                firstAndSecondDictionary);

            // Case 2: Dictionaries share keys. Keys in the second dictionary override keys in the first.
            var thirdDictionary = new Dictionary<string, string> {{"a", "1"}, {"b", "2"}};
            var fourthDictionary = new Dictionary<string, string> {{"b", "3"}, {"c", "4"}};
            var thirdAndFourthDictionary = HttpRequestHelper.GetCombinedDictionary(thirdDictionary, fourthDictionary);
            Assert.AreEqual(new Dictionary<string, string> {{"a", "1"}, {"b", "3"}, {"c", "4"}},
                thirdAndFourthDictionary);
        }

        [Test]
        public void TestGetCombinedDictionaryNullOrEmpty()
        {
            var emptyDictionary = new Dictionary<string, string>();
            var nonEmptyDictionary = new Dictionary<string, string>{{"a","b"}};
            // Case 1: Null dictionaries
            Assert.AreEqual(nonEmptyDictionary, HttpRequestHelper.GetCombinedDictionary(nonEmptyDictionary, null));
            Assert.AreEqual(nonEmptyDictionary, HttpRequestHelper.GetCombinedDictionary(null, nonEmptyDictionary));
            Assert.AreEqual(emptyDictionary, HttpRequestHelper.GetCombinedDictionary(null, null));
            
            // Case 2 : Empty Dictionaries
            Assert.AreEqual(emptyDictionary, HttpRequestHelper.GetCombinedDictionary(emptyDictionary, emptyDictionary));
            Assert.AreEqual(nonEmptyDictionary, HttpRequestHelper.GetCombinedDictionary(emptyDictionary, nonEmptyDictionary));
            Assert.AreEqual(nonEmptyDictionary, HttpRequestHelper.GetCombinedDictionary(nonEmptyDictionary, emptyDictionary));
        }
    }
}