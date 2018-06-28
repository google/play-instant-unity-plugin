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
    /// Provides methods that an instant app can use to display a Play Store install dialog. This dialog will allow
    /// the user to install the current instant app as a full app.
    ///
    /// Example code for the instant app's install button click handler:
    /// <code>
    /// const int requestCode = 123;
    /// using (var activity = InstallLauncher.GetCurrentActivity())
    /// using (var postInstallIntent = InstallLauncher.CreatePostInstallIntent(activity))
    /// {
    ///     InstallLauncher.PutPostInstallIntentStringExtra(postInstallIntent, "payload", "test");
    ///     InstallLauncher.ShowInstallPrompt(activity, requestCode, postInstallIntent, "referrer");
    /// }
    /// </code>
    ///
    /// Example code for the installed app to retrieve the result:
    /// <code>
    /// string payload = InstallLauncher.GetPostInstallIntentStringExtra("payload");
    /// </code>
    /// </summary>
    public static class InstallLauncher
    {
        private const string IntentActionInstantAppInstall = "com.google.android.finsky.action.IA_INSTALL";
        private const int IgnoredRequestCode = 1001;

        /// <summary>
        /// Shows a dialog that allows the user to install the current instant app.
        /// </summary>
        public static void ShowInstallPrompt()
        {
            using (var activity = GetCurrentActivity())
            using (var postInstallIntent = CreatePostInstallIntent(activity))
            {
                ShowInstallPrompt(activity, IgnoredRequestCode, postInstallIntent, null);
            }
        }

        /// <summary>
        /// Shows a dialog that allows the user to install the current instant app. This method is a no-op
        /// if the current running process is an installed app. A post-install intent must be provided,
        /// which will be used to start the application after install is complete.
        /// </summary>
        /// <param name="activity">The activity that should launch the store's install dialog.</param>
        /// <param name="requestCode">The requestCode referenced in the onActivityResult() callback to the activity.
        ///     <a href="https://docs.unity3d.com/Manual/AndroidUnityPlayerActivity.html">Extend UnityPlayerActivity</a>
        ///     and check for the specified requestCode integer to know whether the dialog was cancelled. Use
        ///     UnityPlayer.UnitySendMessage() to relay the response from Java back to Unity scripts.</param>
        /// <param name="postInstallIntent">The intent to launch after the instant app has been installed.
        ///     Must resolve to an activity in the installed app package, or it will not be used.</param>
        /// <param name="referrer">Optional install referrer string.</param>
        /// <exception cref="ArgumentNullException">If either activity or postInstallIntent are null.</exception>
        public static void ShowInstallPrompt(
            AndroidJavaObject activity, int requestCode, AndroidJavaObject postInstallIntent, string referrer)
        {
#if !PLAY_INSTANT
            return;
#endif

            if (activity == null)
            {
                throw new ArgumentNullException("activity");
            }

            if (postInstallIntent == null)
            {
                throw new ArgumentNullException("postInstallIntent");
            }

            using (var uri = CreateMarketDetailsUri(referrer))
            using (var installIntent = new AndroidJavaObject(Android.IntentClass, IntentActionInstantAppInstall))
            using (installIntent.Call<AndroidJavaObject>(Android.IntentMethodSetData, uri))
            using (installIntent.Call<AndroidJavaObject>(
                Android.IntentMethodSetPackage, Android.PlayStorePackageName))
            using (installIntent.Call<AndroidJavaObject>(
                Android.IntentMethodPutExtra, "postInstallIntent", postInstallIntent))
            {
                if (IsLegacyPlayStore(activity, installIntent))
                {
                    ShowLegacyInstallPrompt(activity, requestCode, uri);
                }
                else
                {
                    activity.Call(Android.ActivityMethodStartActivityForResult, installIntent, requestCode);
                }
            }
        }

        /// <summary>
        /// Gets the current activity running in Unity.
        /// This object should be disposed after use.
        /// </summary>
        /// <returns>A wrapped activity object. The AndroidJavaObject should be disposed.</returns>
        public static AndroidJavaObject GetCurrentActivity()
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                return unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            }
        }

        /// <summary>
        /// Creates an Intent for the Play Store to launch after installing the app. It will launch the specified
        /// activity. This Intent can be extended to contain additional extras for passing to the installed app.
        /// This object should be disposed after use.
        /// </summary>
        /// <returns>A wrapped Intent object. The AndroidJavaObject should be disposed.</returns>
        public static AndroidJavaObject CreatePostInstallIntent(AndroidJavaObject activity)
        {
            using (var activityClass = activity.Call<AndroidJavaObject>(Android.ObjectMethodGetClass))
            {
                return new AndroidJavaObject(Android.IntentClass, activity, activityClass);
            }
        }

        /// <summary>
        /// This method can be called with the Intent obtained from <see cref="CreatePostInstallIntent"/>
        /// to add a string extra that will be passed through to the installed app.
        /// </summary>
        /// <param name="postInstallIntent">An Intent obtained from <see cref="CreatePostInstallIntent"/>.</param>
        /// <param name="extraKey">Key for a string extra to add to the post install intent.</param>
        /// <param name="extraValue">Value for a string extra to add to the post install intent.</param>
        public static void PutPostInstallIntentStringExtra(
            AndroidJavaObject postInstallIntent, string extraKey, string extraValue)
        {
            using (postInstallIntent.Call<AndroidJavaObject>(Android.IntentMethodPutExtra, extraKey, extraValue))
            {
            }
        }

        /// <summary>
        /// This method can be called from an installed app to obtain a string that was included in
        /// the postInstallIntent provided by an instant app via <see cref="showInstallPrompt"/>.
        /// It assumes that the current activity was the one that was launched by Play Store.
        /// </summary>
        /// <param name="extraKey">Key for obtaining a string extra from current activity's intent.</param>
        /// <returns>The string extra value.</returns>
        public static string GetPostInstallIntentStringExtra(string extraKey)
        {
            using (var currentActivity = GetCurrentActivity())
            using (var intent = currentActivity.Call<AndroidJavaObject>(Android.ActivityMethodGetIntent))
            {
                return intent.Call<string>(Android.IntentMethodGetStringExtra, extraKey);
            }
        }

        private static AndroidJavaObject CreateMarketDetailsUri(string referrer)
        {
            using (var uriBuilder = new AndroidJavaObject(Android.UriBuilderClass))
            using (uriBuilder.Call<AndroidJavaObject>(Android.UriBuilderMethodScheme, "market"))
            using (uriBuilder.Call<AndroidJavaObject>(Android.UriBuilderMethodAuthority, "details"))
            using (uriBuilder.Call<AndroidJavaObject>(
                Android.UriBuilderMethodAppendQueryParameter, "id", Application.identifier))
            {
                if (!string.IsNullOrEmpty(referrer))
                {
                    using (uriBuilder.Call<AndroidJavaObject>(
                        Android.UriBuilderMethodAppendQueryParameter, "referrer", referrer))
                    {
                    }
                }

                return uriBuilder.Call<AndroidJavaObject>(Android.UriBuilderMethodBuild);
            }
        }

        private static bool IsLegacyPlayStore(AndroidJavaObject context, AndroidJavaObject installIntent)
        {
            using (var packageManager = context.Call<AndroidJavaObject>(Android.ContextMethodGetPackageManager))
            using (var resolveInfo =
                packageManager.Call<AndroidJavaObject>(Android.PackageManagerMethodResolveActivity, installIntent, 0))
            {
                return resolveInfo == null;
            }
        }

        private static void ShowLegacyInstallPrompt(AndroidJavaObject activity, int requestCode, AndroidJavaObject uri)
        {
            using (var intent = new AndroidJavaObject(Android.IntentClass, Android.IntentActionView))
            using (intent.Call<AndroidJavaObject>(Android.IntentMethodAddCategory, Android.IntentCategoryDefault))
            using (intent.Call<AndroidJavaObject>(Android.IntentMethodSetPackage, Android.PlayStorePackageName))
            using (intent.Call<AndroidJavaObject>(Android.IntentMethodSetData, uri))
            using (intent.Call<AndroidJavaObject>(Android.IntentMethodPutExtra, "callerId", Application.identifier))
            using (intent.Call<AndroidJavaObject>(Android.IntentMethodPutExtra, "overlay", true))
            {
                activity.Call(Android.ActivityMethodStartActivityForResult, intent, requestCode);
            }
        }
    }
}