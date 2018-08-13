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
using GooglePlayInstant.Editor.QuickDeploy;
using NUnit.Framework;

namespace GooglePlayInstant.Tests.Editor.QuickDeploy
{
    /// <summary>
    /// Contains unit tests for OAuth2Server methods.
    /// </summary>
    [TestFixture]
    public class OAuth2ServerTest
    {
        // Testing strategy:
        //     - Test methods that do not require starting the server.
        //     - TODO: Implement E2E tests to ensure that the server handles requests as expected.

        private const string AddressPrefix = "http://localhost:5000/";

        [Test]
        public void TestGetAuthorizationResponse()
        {
            var someString = Path.GetRandomFileName();
            // response is code
            var codeResponse =
                OAuth2Server.GetAuthorizationResponse(new Uri(string.Format("{0}?code={1}", AddressPrefix,
                    someString)));
            Assert.AreEqual(new KeyValuePair<string, string>("code", someString), codeResponse,
                "Expected valid code response");

            // response is error
            var errorResponse =
                OAuth2Server.GetAuthorizationResponse(new Uri(string.Format("{0}?error={1}", AddressPrefix,
                    someString)));
            Assert.AreEqual(new KeyValuePair<string, string>("error", someString), errorResponse,
                "Expected valid error response");

            // response has valid key with escaped value
            Assert.AreEqual(new KeyValuePair<string, string>("code", "a B c"),
                OAuth2Server.GetAuthorizationResponse(new Uri(string.Format("{0}?code=a%20B%20c", AddressPrefix))));

            // response has invalid keys
            var invalidResponse = new Uri(string.Format("{0}?someKey=someValue", AddressPrefix));
            Assert.Throws<ArgumentException>(() => OAuth2Server.GetAuthorizationResponse(invalidResponse));

            // No response. Uri has no params
            Assert.Throws<ArgumentException>(() => OAuth2Server.GetAuthorizationResponse(new Uri(AddressPrefix)));
        }

        [Test]
        public void TestGetQueryString()
        {
            var validUriWithCode = new Uri(string.Format("{0}?code=someValue", AddressPrefix));
            Assert.AreEqual("code=someValue", OAuth2Server.GetQueryString(validUriWithCode));
            Assert.Throws<ArgumentException>(() => OAuth2Server.GetQueryString(new Uri(AddressPrefix)));
        }

        [Test]
        public void TestGetCodeOrErrorResponsePairOnValidInputs()
        {
            Assert.AreEqual(new KeyValuePair<string, string>("code", "codeValue"),
                OAuth2Server.GetCodeOrErrorResponsePair("code=codeValue"));

            Assert.AreEqual(new KeyValuePair<string, string>("error", "errorValue"),
                OAuth2Server.GetCodeOrErrorResponsePair("error=errorValue"));
            Assert.AreEqual(new KeyValuePair<string, string>("error", " "),
                OAuth2Server.GetCodeOrErrorResponsePair("error=%20"));
        }

        [Test]
        public void TestGetCodeOrErrorResponsePairOnInvalidInputs()
        {
            // The authorization response should consist of a single query parameter.
            Assert.Throws<ArgumentException>(
                () => OAuth2Server.GetCodeOrErrorResponsePair("code=codeValue&error=errorValue"));
            Assert.Throws<ArgumentException>(
                () => OAuth2Server.GetCodeOrErrorResponsePair("code=codeValue&otherKey=someValue"));
            Assert.Throws<ArgumentException>(
                () => OAuth2Server.GetCodeOrErrorResponsePair("error=errorValue&otherKey=someValue"));

            // The single query parameter should be an equals-separated key/value pair.
            Assert.Throws<ArgumentException>(() => OAuth2Server.GetCodeOrErrorResponsePair(""));
            Assert.Throws<ArgumentException>(() => OAuth2Server.GetCodeOrErrorResponsePair("&"));
            Assert.Throws<ArgumentException>(() => OAuth2Server.GetCodeOrErrorResponsePair("="));
            Assert.Throws<ArgumentException>(() => OAuth2Server.GetCodeOrErrorResponsePair("code"));
            Assert.Throws<ArgumentException>(() => OAuth2Server.GetCodeOrErrorResponsePair("code=value1=value2"));
            Assert.Throws<ArgumentException>(() => OAuth2Server.GetCodeOrErrorResponsePair("code&codeValue"));

            // Key for the single query parameter should be either "code" or "error"
            Assert.Throws<ArgumentException>(() => OAuth2Server.GetCodeOrErrorResponsePair("someKey=someValue"));
        }
    }
}