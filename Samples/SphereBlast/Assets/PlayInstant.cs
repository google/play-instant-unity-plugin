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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GooglePlayInstant;

public class PlayInstant : MonoBehaviour
{

    /// <summary>
    /// Provides methods that an instant app can use to display a Play Store install dialog. This dialog will allow
    /// the user to install the current instant app as a full app.
    ///
    /// Provides methods that an instant app can use for howing the install prompt, and set and get strings via the
    /// cookie API, and that installed apps can also use to get values from via the cookie. Both instant and
    /// installed apps can call the isInstantApp method to check if this current app is an instant app.
    /// 
    /// 
    /// Example code for the instant app's install button click handler:
    /// <code>
    /// pi = new PlayInstant();
    /// install();
    /// </code>
    /// 
    /// Or, you can call install with a referrer id
    /// 
    /// <code>
    /// pi = new PlayInstant();
    /// pi.install ("YOUR_CAMPAING_ID");
    /// </code>
    ///
    /// Example code for setting a cookie
    /// <code>
    /// pi = new PlayInstant();
    /// string gameStateCSV = level + "," + score;
    /// pi.SetCookie(gameStateCSV);
    /// </code>
    /// 
    /// Example code for getting a cookie
    /// <code>
    /// pi = new PlayInstant();
    /// string results = pi.GetCookie();
    /// </code>
    /// 
    /// Example code for checking if currently running game is an instant app
    /// <code>
    /// if (pi.IsInstantApp())
    /// {
    ///     Debug.Log("This app is an instant app");
    /// }
    /// </code>
    /// </summary>

	public void Install()
	{
		// The requestCode referenced in the onActivityResult() callback to the
		// activity. Extend UnityPlayerActivity and check for the specified
		// requestCode integer to know whether the dialog was cancelled. Use
		// UnityPlayer.UnitySendMessage() to relay the response from Java back to
		// Unity scripts
		const int requestCode = 123;
		// The activity that should launch the store's install dialog.
		using (var currentActivity = InstallLauncher.GetCurrentActivity())
		{
			// The intent to launch after the instant app has been installed.
			// Must resolve to an activity in the installed app package, or it will
			// not be used
			using (var postInstallIntent
				= InstallLauncher.CreatePostInstallIntent(currentActivity))
			{
				InstallLauncher.ShowInstallPrompt(currentActivity, requestCode,
					postInstallIntent, null);
			}
		}
	}

	public void Install(string referrerId)
	{
		// The requestCode referenced in the onActivityResult() callback to the
		// activity. Extend UnityPlayerActivity and check for the specified
		// requestCode integer to know whether the dialog was cancelled. Use
		// UnityPlayer.UnitySendMessage() to relay the response from Java back to
		// Unity scripts
		const int requestCode = 123;
		// The activity that should launch the store's install dialog.
		try
		{
			using (var currentActivity = InstallLauncher.GetCurrentActivity())
			{
				// The intent to launch after the instant app has been installed.
				// Must resolve to an activity in the installed app package, or it will
				// not be used
				using (var postInstallIntent
					= InstallLauncher.CreatePostInstallIntent(currentActivity))
				{	
					// Optional install referrer string can be used as a way to pass
					// information to the installed app about the source of the install
					InstallLauncher.ShowInstallPrompt(currentActivity, requestCode,
						postInstallIntent, referrerId);
				}
			}
		}
		catch (System.Exception e)
		{
			Debug.Log("Exception in setCookie:\n" + e.Message + "\n" + e.StackTrace);
			return;
		}
	}

	public void SetCookie(string content)
	{
		// Uses the PackageManagerCompat class to set a cookie. Relies on Play Services version
        // of the InstantApps module. Takes a string and converts it to byte array befor calling
        // setInstantAppCookie
        try
		{
			using (var currentActivity = InstallLauncher.GetCurrentActivity())
			{
				using (var instantAppsClazz =
					       new AndroidJavaClass("com.google.android.gms.instantapps.InstantApps"))
				{
					var pm = instantAppsClazz.CallStatic<AndroidJavaObject>
						("getPackageManagerCompat",
						 currentActivity);
					var cookieResult = pm.Call<bool>
						("setInstantAppCookie", 
						 System.Text.Encoding.ASCII.GetBytes(content));
					Debug.Log("setCookie result " + cookieResult);
					Debug.Log("Check if cookie is set " + GetCookie());
				}
			}
		}
		catch (System.Exception e)
		{
			Debug.Log("Exception in setCookie:\n" + e.Message + "\n" + e.StackTrace);
			return;
		}
	}

	public string GetCookie()
	{
        // Uses the PackageManagerCompat class to get a cookie. Relies on Play Services version
        // of the InstantApps module. Returns a string from the byte array returned by
        // call to getInstantAppCookie
		try
		{
			using (var currentActivity = InstallLauncher.GetCurrentActivity())
			{
				using (var instantAppsClazz =
					        new AndroidJavaClass("com.google.android.gms.instantapps.InstantApps"))
				{
					var pm = instantAppsClazz.CallStatic<AndroidJavaObject>
						("getPackageManagerCompat",
						          currentActivity);
					var cookieContent = pm.Call<byte[]>
						("getInstantAppCookie");
					Debug.Log("Retrieved cookie content "
						+ System.Text.Encoding.ASCII.GetString(cookieContent));
					return System.Text.Encoding.ASCII.GetString(cookieContent);
				}
			}
		}
		catch (System.Exception e)
		{
			Debug.Log("Exception in getCookie:\n" + e.Message + "\n" + e.StackTrace);
			return null;
		}
	}

	public bool IsInstantApp()
	{
        // Uses the PackageManagerCompat class to get a cookie. Relies on Play Services version
        // of the InstantApps module. Returns the boolean returned by isInstantApp
		try
		{
			using (var currentActivity = InstallLauncher.GetCurrentActivity())
			{
				var pm = currentActivity.Call<AndroidJavaObject>
					("getPackageManager");
				bool isIA = pm.Call<bool>
					("isInstantApp");
				return isIA;
			}
		}
		catch (System.Exception e)
		{
			Debug.Log("Exception in isInstantApp:\n"
				+ e.Message + "\n" + e.StackTrace);
			return false;
		}
	}

}
