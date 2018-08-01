# Google Play Instant Plugin for Unity Beta

## Overview

The Google Play Instant Plugin for Unity Beta simplifies conversion of a Unity-based Android app into an instant app that can be deployed through [Google Play Instant](https://developer.android.com/topic/google-play-instant/).

The plugin’s Unity Editor (IDE) features include:
 * The option to switch between Installed and Instant build modes.
 * A centralized view of Android Player Settings that should be changed to support Google Play Instant.
 * An action to build the instant app for publishing on Google Play Console.
 * An action to build and run the instant app on an adb connected Android device.

The plugin’s Unity Engine (runtime) features include:
 * A method for displaying a Play Store dialog to install the full version of the instant app.

## Installing the Plugin

### Prerequisites
 * Unity 5.6, 2017.4, or 2018.2.
   * Note: other versions may work, but are not tested regularly.
 * A device running Android 5.0 (Lollipop) or newer.

### Download and Install
 * Obtain the latest .unitypackage from the releases page.
 * Import the .unitypackage by clicking the Unity IDE menu option _Assets > Import package > Custom Package_ and importing all items.

## Unity Editor Features
After import there will be a “PlayInstant” menu in Unity providing several options described below.

### Build Settings...
Opens a window that enables switching between "Installed" and "Instant" development modes. Switching to "Instant" performs the following changes:
 * Creates a Scripting Define Symbol called PLAY_INSTANT that can be used for scripting with #if PLAY_INSTANT / #endif.
 * Provides a text box for optionally entering an "Instant Apps URL" that is used to launch the instant app.
   * If a URL is provided, you will need verify ownership of the domain by [Configuring Digital Asset Links](https://developer.android.com/training/app-links/verify-site-associations#web-assoc).
   * If a URL is not entered, a URL will be provided for you at https://instant.apps/your.package.name.
 * Manages updates to the AndroidManifest.xml for certain required changes such as [android:targetSandboxVersion](https://developer.android.com/guide/topics/manifest/manifest-element#targetSandboxVersion).

#### Scenes in Build
The Play Instant Build Settings window also provides control over the scenes included in the build:
 * By default the scenes included in the build are the enabled scenes from Unity's "Build Settings" window.
 * The scenes included in the build can be customized via a comma separated list of scene names.
 * Scenes that are not included in the build, but that are loaded via Asset Bundles, may have required components removed by engine stripping. Specify the path to an Asset Bundle Manifest file to retain these required components.

### Player Settings...
Opens a window that lists Android Player Settings that should be changed to make the app Google Play Instant compatible. These are divided into Required and Recommended settings. Click on an “Update” button to change a setting.

### Set up Instant Apps Development SDK...
Installs or updates the “Instant Apps Development SDK” using [sdkmanager](https://developer.android.com/studio/command-line/sdkmanager). The plugin requires SDK version 1.2 or higher. If there is a license that needs to be accepted, the plugin will prompt for acceptance.

### Build for Play Console...
[Google Play Console](https://play.google.com/apps/publish/) requires that the APKs for an instant app are published together in a ZIP file. Although most Unity instant apps will consist of a single APK, the requirement holds. This menu option performs a build and stores the resulting APK in a ZIP file suitable for publishing.

### Build and Run
This option runs the instant app on an adb connected device by performing the following steps:
 * Verifies that required Unity Build Settings and Android Player Settings are configured correctly.
 * Invokes Unity's BuildPlayer method to create an APK containing all scenes that are currently enabled in “Build Settings”.
 * If the connected device is running an Android version before 8.0 (Oreo), provisions the device for Instant App development by installing "Google Play Services for Instant Apps" and "Instant Apps Development Manager" (only if not done already).
 * Runs the APK as an instant app on the adb connected device.

## Unity Engine Features

### Show Install Prompt
The goal of many instant apps is to give users a chance to experience the app before installing the full version. An instant app with an "Install" button can call the `InstallLauncher.ShowInstallPrompt()` method to display a Play Store install dialog. For example, the following code can be called from an install button click handler:

```cs
const int requestCode = 123;
using (var activity = InstallLauncher.GetCurrentActivity())
using (var postInstallIntent = InstallLauncher.CreatePostInstallIntent(activity))
{
    InstallLauncher.PutPostInstallIntentStringExtra(postInstallIntent, "payload", "test");
    InstallLauncher.ShowInstallPrompt(activity, requestCode, postInstallIntent, "test-referrer");
}
```

To determine if the user cancels out of the installation process, override `onActivityResult()` in the instant app's main activity and check for `RESULT_CANCELED`.

If the user completes app installation, the Play Store will re-launch the app using the provided `postInstallIntent`. This intent can include context about the user's state in the instant app, e.g. as with the key "payload" and value "test" in the example above. The installed app can retrieve this value using the following code:

```cs
string payload = InstallLauncher.GetPostInstallIntentStringExtra("payload");
```

**Note:** anyone can construct an intent with extra fields to launch the app, so if the payload grants something of value, design the payload so that it can only be used once, cryptographically sign it, and verify the signature on a server.

## Known Issues
 * If the Unity project has an existing AndroidManifest.xml with multiple VIEW Intents on the main Activity and if an Instant Apps URL is provided, the plugin doesn't know which Intent to modify. This can be worked around by not providing an Instant Apps URL or by manually updating the manifest file.
