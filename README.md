# Google Play Instant Plugin for Unity Beta (Archived)

## Overview

The code for this project has been migrated to the
[Google Play Plugins for Unity](//github.com/google/play-unity-plugins) project,
and this GitHub project is now archived.

Refer to the
[developer documentation](//developer.android.com/topic/google-play-instant/getting-started/game-unity-plugin)
for the latest information about the Google Play Instant Plugin for Unity.

## Migration details

Some of the files in this archived GitHub project have the same Guid as files in
the new GitHub project. This allows projects that are using Play assets, such as
the `LoadingScreen` MonoBehaviour, to still work after migrating.

The new **Google Play Plugins for Unity** project uses the namespace
`Google.Play.Instant` instead of `GooglePlayInstant`, so any `using
GooglePlayInstant;` statements will have to be updated.

The new **Google Play Plugins for Unity** no longer supports the ability to set a custom instant apps URL.

## Migration steps

1.  Familiarize yourself with the
    [download and import](//developer.android.com/topic/google-play-instant/getting-started/game-unity-plugin#import-plugin)
    process for the new plugin, and either download the new `.unitypackage` file
    or set up the **Game Package Registry for Unity**.

1.  Delete the existing `Assets/GooglePlayInstant` directory (if you previously
    imported from a `.unitypackage` file) or the
    `Assets/play-instant-unity-plugin` directory (if you previously imported via
    `git clone`). Note that at this point there may be errors such as "error
    CS0103: The name `InstallLauncher' does not exist in the current context" in
    the project.

1.  Import the `.unitypackage` obtained from the first step or install the
    **Google Play Instant** package in Unity Package Manager.

1.  Change any `using GooglePlayInstant;` statements to `using
    Google.Play.Instant;`.

## Known issues

### Launching an instant app from the Play Store redirects to the browser

This issue occurs when launching an instant app if the following is true:
1. Your currently released instant app is launchable via a custom URL.
1. You have uploaded a version of your instant app that does not specify a custom URL to alpha or internal test.
1. You are trying to launch this non-production version of your instant app.

The latest plugin no longer supports the ability to set a custom instant apps URL. If your app previously included a custom instant apps URL, uploading an app built with the latest plugin could trigger this issue.

There are two workarounds:
1. The issue will not occur for an instant app in production, so release the app to production to eliminate the issue.
1. If you'd prefer to fix the issue in alpha, add the browsable intent filter and default url tags to the UnityPlayerActivity in your app's manifest:
```xml
<intent-filter
    android:autoVerify="true">

    <action
        android:name="android.intent.action.VIEW" />

    <category
        android:name="android.intent.category.BROWSABLE" />

    <category
        android:name="android.intent.category.DEFAULT" />

    <data
        android:scheme="http"
        android:host="<url-host>"
        android:pathPrefix="<url-path-prefix>" />

    <data
        android:scheme="https"
        android:host="<url-host>"
        android:pathPrefix="<url-path-prefix>" />
</intent-filter>

<meta-data
    android:name="default-url"
    android:value="<the-default-url-of-your-released-app>" />

```