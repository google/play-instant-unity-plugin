# Google Play Instant Plugin for Unity Beta

## Overview

This GitHub project is now archived, and the code has been migrated to the
[Play Unity Plugins](//github.com/google/play-unity-plugins) project.

Refer to the
[developer documentation](//developer.android.com/topic/google-play-instant/getting-started/game-unity-plugin)
for the latest information about this plugin.

## Migration details

Some of the files in this archived GitHub project have the same Guids as files
in the new GitHub project. This allows projects that are using Play assets, such
as the `LoadingScreen` MonoBehaviour, to still work after migrating.

The new **Play Unity Plugins** project uses the namespace `Google.Play.Instant`
instead of `GooglePlayInstant`, so any `using GooglePlayInstant;` statements
will have to be changed.

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

1.  Import the `.unitypackage` or install the **Google Play Instant** plugin
    obtained in the first step.

1.  Change any `using GooglePlayInstant;` statements to `using
    Google.Play.Instant;`.
