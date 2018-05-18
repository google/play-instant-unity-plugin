# Google Play Instant Plugin for Unity Beta

## Overview

The Google Play Instant Plugin for Unity Beta simplifies conversion of a Unity-based Android app into an instant app that can be deployed through Google Play Instant.

The plugin’s features include:
 * The option to switch between Installed and Instant build modes.
 * A centralized view of Unity Build Settings and Android Player Settings that should be changed to support Google Play Instant.
 * An action to build and run the instant app on an adb connected Android device.

## Installing the Plugin

### Prerequisites
 * Unity 5.6 or higher.
 * A device running Android 5.0 (Lollipop) or newer.

### Download and Install
 * Obtain the latest .unitypackage from the releases page.
 * Import the .unitypackage by clicking the Unity IDE menu option _Assets > Import package > Custom Package_ and importing all items.

## Using the Plugin
After import there will be a “PlayInstant” menu in Unity providing several options described below.

### Configure Instant or Installed...
Opens a window that enables switching between "Installed" and "Instant" development modes. Switching to "Instant" performs the following changes:
 * Creates a Scripting Define Symbol called PLAY_INSTANT that can be used for scripting with #if PLAY_INSTANT / #endif.
 * Provides a text box for optionally entering an "Instant Apps URL" that is used to launch the instant app.
   * If a URL is provided, you will need verify ownership of the domain by [Configuring Digital Asset Links](https://developer.android.com/training/app-links/verify-site-associations#web-assoc).
   * If a URL is not entered, a URL will be provided for you at https://instant.apps/your.package.name.
 * Manages updates to the AndroidManifest.xml for certain required changes such as [android:targetSandboxVersion](https://developer.android.com/guide/topics/manifest/manifest-element#targetSandboxVersion).

### Check Player Settings...
Opens a window that indicates Unity Build Settings and Android Player Settings that should be changed to make the app Google Play Instant compatible. These are divided into Required and Recommended settings. Click on an “Update” button to change a setting.

### Set up Play Instant SDK...
Installs or updates the “Instant Apps Development SDK” using [sdkmanager](https://developer.android.com/studio/command-line/sdkmanager). The plugin requires SDK version 1.2 or higher. If there is a license that needs to be accepted, the plugin will prompt for acceptance.

### Build and Run
This option runs the instant app on an adb connected device by performing the following steps:
 * Verifies that required Unity Build Settings and Android Player Settings are configured correctly.
 * Invokes Unity's BuildPlayer method to create an APK containing all scenes that are currently enabled in “Build Settings”.
 * If the connected device is running an Android version before 8.0 (Oreo), provisions the device for Instant App development by installing "Google Play Services for Instant Apps" and "Instant Apps Development Manager" (only if not done already).
 * Runs the APK as an instant app on the adb connected device.

## Known Issues
 * If the Unity project has an existing AndroidManifest.xml with multiple VIEW Intents on the main Activity and if an Instant Apps URL is provided, the plugin doesn't know which Intent to modify. This can be worked around by not providing an Instant Apps URL or by manually updating the manifest file.
