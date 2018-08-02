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
using UnityEngine;

namespace GooglePlayInstant
{
    /// <summary>
    /// Provides methods that an instant app can use to store a persistent cookie or that an installed app
    /// can use to retrieve the cookie that it persisted as an instant app.
    ///
    /// This is a C# implementation of some of the Java methods available in
    /// <a href="https://developers.google.com/android/reference/com/google/android/gms/instantapps/PackageManagerCompat.html">
    /// Google Play Services' PackageManagerCompat class</a>.
    /// </summary>
    public static class CookieApi
    {
        /// <summary>
        /// An exception thrown by the methods of <see cref="CookieApi"/> if there is a failure while
        /// making a call to Google Play Services.
        /// </summary>
        public class InstantAppCookieException : Exception
        {
            public InstantAppCookieException(string message, Exception innerException) : base(message, innerException)
            {
            }

            public InstantAppCookieException(string message) : base(message)
            {
            }
        }

        // Constants specific to PackageManagerCompat.
        private const string Authority = "com.google.android.gms.instantapps.provider.api";
        private const string ContentAuthority = "content://" + Authority + "/";
        private const string KeyCookie = "cookie";
        private const string KeyResult = "result";
        private const string KeyUid = "uid";
        private const string MethodGetInstantAppCookie = "getInstantAppCookie";
        private const string MethodGetInstantAppCookieMaxSize = "getInstantAppCookieMaxSize";
        private const string MethodSetInstantAppCookie = "setInstantAppCookie";

        private static bool _verifiedContentProvider;

        /// <summary>
        /// Gets the maximum size in bytes of the cookie data an instant app can store on the device.
        /// </summary>
        /// <exception cref="InstantAppCookieException">Thrown if there is a failure to obtain the size.</exception>
        public static int GetInstantAppCookieMaxSize()
        {
            using (var extrasBundle = new AndroidJavaObject(Android.BundleClass))
            using (var resultBundle = CallMethod(MethodGetInstantAppCookieMaxSize, extrasBundle))
            {
                return resultBundle.Call<int>(Android.BundleMethodGetInt, KeyResult);
            }
        }

        /// <summary>
        /// Gets the instant application cookie for this app. Non instant apps and apps that were instant but
        /// were upgraded to normal apps can still access this API. For instant apps this cookie is cached for
        /// some time after uninstall while for normal apps the cookie is deleted after the app is uninstalled.
        /// The cookie is always present while the app is installed.
        /// </summary>
        /// <exception cref="InstantAppCookieException">Thrown if there is a failure to obtain the cookie.</exception>
        public static byte[] GetInstantAppCookie()
        {
            using (var extrasBundle = new AndroidJavaObject(Android.BundleClass))
            {
                extrasBundle.Call(Android.BundleMethodPutInt, KeyUid, ProcessGetMyUid());
                using (var resultBundle = CallMethod(MethodGetInstantAppCookie, extrasBundle))
                {
                    return resultBundle.Call<byte[]>(Android.BundleMethodGetByteArray, KeyResult);
                }
            }
        }

        /// <summary>
        /// Sets the instant application cookie for the calling app. Non instant apps and apps that were instant but
        /// were upgraded to normal apps can still access this API. For instant apps this cookie is cached for
        /// some time after uninstall while for normal apps the cookie is deleted after the app is uninstalled.
        /// The cookie is always present while the app is installed. The cookie size is limited by
        /// <see cref="GetInstantAppCookieMaxSize"/>. If the provided cookie size is over the limit this method
        /// returns false. Passing null or an empty array clears the cookie.
        /// </summary>
        /// <param name="cookie">The cookie data.</param>
        /// <returns>True if the cookie was set. False if cookie is too large or I/O fails.</returns>
        /// <exception cref="InstantAppCookieException">Thrown if there is a failure to set the cookie.</exception>
        public static bool SetInstantAppCookie(byte[] cookie)
        {
            using (var extrasBundle = new AndroidJavaObject(Android.BundleClass))
            {
                extrasBundle.Call(Android.BundleMethodPutInt, KeyUid, ProcessGetMyUid());
                extrasBundle.Call(Android.BundleMethodPutByteArray, KeyCookie, cookie);
                using (var resultBundle = CallMethod(MethodSetInstantAppCookie, extrasBundle))
                {
                    return resultBundle.Call<bool>(Android.BundleMethodGetBoolean, KeyResult);
                }
            }
        }

        private static void VerifyContentProvider()
        {
            if (_verifiedContentProvider)
            {
                return;
            }

            using (var context = UnityPlayerHelper.GetCurrentActivity())
            using (var packageManager = context.Call<AndroidJavaObject>(Android.ContextMethodGetPackageManager))
            using (var providerInfo = packageManager.Call<AndroidJavaObject>(
                Android.PackageManagerMethodResolveContentProvider, Authority, 0))
            {
                if (!PlaySignatureVerifier.VerifyGooglePlayServices(packageManager))
                {
                    throw new InstantAppCookieException("Failed to verify the signature of Google Play Services.");
                }

                if (providerInfo == null)
                {
                    throw new InstantAppCookieException("Failed to resolve the instant apps content provider.");
                }

                var packageName = providerInfo.Get<string>(Android.ProviderInfoFieldPackageName);
                if (!string.Equals(packageName, Android.GooglePlayServicesPackageName))
                {
                    throw new InstantAppCookieException(
                        string.Format("Package \"{0}\" is an invalid instant apps content provider.", packageName));
                }
            }

            Debug.Log("Verified instant apps content provider.");
            _verifiedContentProvider = true;
        }

        private static AndroidJavaObject CallMethod(string methodName, AndroidJavaObject extrasBundle)
        {
            VerifyContentProvider();
            AndroidJavaObject resultBundle;
            try
            {
                using (var context = UnityPlayerHelper.GetCurrentActivity())
                using (var contentResolver = context.Call<AndroidJavaObject>(Android.ContextMethodGetContentResolver))
                using (var uriClass = new AndroidJavaClass(Android.UriClass))
                using (var uri = uriClass.CallStatic<AndroidJavaObject>(Android.UriMethodParse, ContentAuthority))
                {
                    resultBundle = contentResolver.Call<AndroidJavaObject>(
                        Android.ContentResolverMethodCall, uri, methodName, null, extrasBundle);
                }
            }
            catch (AndroidJavaException ex)
            {
                throw new InstantAppCookieException(
                    string.Format("Failed to call {0} on the instant apps content provider.", methodName), ex);
            }

            if (resultBundle == null)
            {
                // This should only happen if the content provider is unavailable.
                throw new InstantAppCookieException(
                    string.Format("Null result calling {0} on the instant apps content provider.", methodName));
            }

            return resultBundle;
        }

        private static int ProcessGetMyUid()
        {
            using (var processClass = new AndroidJavaClass(Android.ProcessClass))
            {
                return processClass.CallStatic<int>(Android.ProcessMethodMyUid);
            }
        }
    }
}