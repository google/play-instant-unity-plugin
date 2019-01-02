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

using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Rendering;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Enumerates Unity setting policies that affect whether an instant app can successfully be built. Some settings
    /// are required and must be configured in a certain way, where others are simply recommended. Each policy can
    /// report whether it is configured correctly and if not, provides a delegate to change to the preferred state.
    /// </summary>
    public class PlayInstantSettingPolicy
    {
        private const string MultithreadedRenderingDescription =
            "Pre-Oreo devices do not support instant apps using EGL shared contexts.";

        private const string ApkSizeReductionDescription = "This setting reduces APK size.";

        public delegate bool IsCorrectStateDelegate();

        public delegate bool ChangeStateDelegate();

        private PlayInstantSettingPolicy(
            string name,
            string description,
            IsCorrectStateDelegate isCorrectState,
            ChangeStateDelegate changeState)
        {
            Name = name;
            Description = description;
            IsCorrectState = isCorrectState;
            ChangeState = changeState;
        }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public IsCorrectStateDelegate IsCorrectState { get; private set; }

        public ChangeStateDelegate ChangeState { get; private set; }


        public static IEnumerable<PlayInstantSettingPolicy> GetRequiredPolicies()
        {
            return new List<PlayInstantSettingPolicy>
            {
                new PlayInstantSettingPolicy(
                    "Build Target should be Android",
                    null,
                    () => EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android,
                    () => EditorUserBuildSettings.SwitchActiveBuildTarget(
                        BuildTargetGroup.Android, BuildTarget.Android)),

                // Note: pre-2018 versions of Unity do not have an enum value for AndroidApiLevel26.
                new PlayInstantSettingPolicy(
                    "Android targetSdkVersion should be 26 or higher",
#if UNITY_2017_1_OR_NEWER
                    null,
#else
                    "Set \"Automatic\" and ensure that API 26 or higher is installed in SDK Manager.",
#endif
                    () => (int) PlayerSettings.Android.targetSdkVersion >= 26 ||
                          PlayerSettings.Android.targetSdkVersion == AndroidSdkVersions.AndroidApiLevelAuto,
                    () =>
                    {
                        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
                        return true;
                    }),

#if UNITY_2018_1_OR_NEWER
                new PlayInstantSettingPolicy(
                    "Android build system should be Gradle",
                    "Required for IPostGenerateGradleAndroidProject and APK Signature Scheme v2.",
                    () => EditorUserBuildSettings.androidBuildSystem == AndroidBuildSystem.Gradle,
                    () =>
                    {
                        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
                        return true;
                    }),
#endif

                new PlayInstantSettingPolicy(
                    "Graphics API should be OpenGLES2 only",
                    "Pre-Oreo devices only support instant apps using GLES2. " +
                    "Removing unused Graphics APIs reduces APK size.",
                    () =>
                    {
                        var graphicsDeviceTypes = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
                        return !PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android) &&
                               graphicsDeviceTypes.Length == 1 &&
                               graphicsDeviceTypes[0] == GraphicsDeviceType.OpenGLES2;
                    },
                    () =>
                    {
                        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
                        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] {GraphicsDeviceType.OpenGLES2});
                        return true;
                    }),

#if UNITY_2017_2_OR_NEWER
                new PlayInstantSettingPolicy(
                    "Android Multithreaded Rendering should be disabled",
                    MultithreadedRenderingDescription,
                    () => !PlayerSettings.GetMobileMTRendering(BuildTargetGroup.Android),
                    () =>
                    {
                        PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, false);
                        return true;
                    })
#else
                new PlayInstantSettingPolicy(
                    "Mobile Multithreaded Rendering should be disabled",
                    MultithreadedRenderingDescription,
                    () => !PlayerSettings.mobileMTRendering,
                    () =>
                    {
                        PlayerSettings.mobileMTRendering = false;
                        return true;
                    })
#endif
            };
        }

        public static IEnumerable<PlayInstantSettingPolicy> GetRecommendedPolicies()
        {
            var policies = new List<PlayInstantSettingPolicy>
            {
                new PlayInstantSettingPolicy(
                    "Android minSdkVersion should be 21",
                    "Lower than 21 is fine, though 21 is the minimum supported by Google Play Instant.",
                    // TODO: consider prompting if strictly greater than 21 to say that 21 enables wider reach
                    () => (int) PlayerSettings.Android.minSdkVersion >= 21,
                    () =>
                    {
                        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel21;
                        return true;
                    })
            };

            // For the purpose of switching to .NET 2.0 Subset, it's sufficient to check #if NET_2_0. However, it would
            // be confusing if the option disappeared after clicking the Update button, so we also check NET_2_0_SUBSET.
            // For background on size reduction benefits see https://docs.unity3d.com/Manual/dotnetProfileSupport.html
#if NET_2_0 || NET_2_0_SUBSET
            policies.Add(new PlayInstantSettingPolicy(
                "API Compatibility Level should be \".NET 2.0 Subset\"",
                ApkSizeReductionDescription,
                () => PlayerSettings.GetApiCompatibilityLevel(BuildTargetGroup.Android) ==
                      ApiCompatibilityLevel.NET_2_0_Subset,
                () =>
                {
                    PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Android,
                        ApiCompatibilityLevel.NET_2_0_Subset);
                    return true;
                }));
#endif

            // Unity 2017.1 added experimental support for .NET 4.6, however .NET Standard 2.0 is unavailable.
            // Therefore, we only provide this policy option starting with Unity 2018.1.
            // Our #if check includes NET_STANDARD_2_0 for the reason we check NET_2_0_SUBSET above.
            // https://blogs.unity3d.com/2018/03/28/updated-scripting-runtime-in-unity-2018-1-what-does-the-future-hold/
#if UNITY_2018_1_OR_NEWER && (NET_4_6 || NET_STANDARD_2_0)
            policies.Add(new PlayInstantSettingPolicy(
                "API Compatibility Level should be \".NET Standard 2.0\"",
                ApkSizeReductionDescription,
                () => PlayerSettings.GetApiCompatibilityLevel(BuildTargetGroup.Android) ==
                      ApiCompatibilityLevel.NET_Standard_2_0,
                () =>
                {
                    PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Android,
                        ApiCompatibilityLevel.NET_Standard_2_0);
                    return true;
                }));
#endif

            switch (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android))
            {
                // This will reduce APK size, but may cause runtime issues if needed components are removed.
                // See https://docs.unity3d.com/Manual/IL2CPP-BytecodeStripping.html
                case ScriptingImplementation.IL2CPP:
                    policies.Add(new PlayInstantSettingPolicy(
                        "IL2CPP builds should strip engine code",
                        "This setting reduces APK size, but may cause runtime issues.",
                        () => PlayerSettings.stripEngineCode,
                        () =>
                        {
                            PlayerSettings.stripEngineCode = true;
                            return true;
                        }));

                    break;
#if !UNITY_2018_3_OR_NEWER
                case ScriptingImplementation.Mono2x:
                    policies.Add(new PlayInstantSettingPolicy(
                        "Mono builds should use code stripping",
                        ApkSizeReductionDescription,
                        () => PlayerSettings.strippingLevel == StrippingLevel.UseMicroMSCorlib,
                        () =>
                        {
                            PlayerSettings.strippingLevel = StrippingLevel.UseMicroMSCorlib;
                            return true;
                        }));
                    break;
#endif
            }

#if UNITY_2018_3_OR_NEWER
            policies.Add(new PlayInstantSettingPolicy(
                "Release builds should use managed code stripping",
                ApkSizeReductionDescription,
                // Note: in some alpha/beta builds of 2018.3 ManagedStrippingLevel.High isn't defined (use "Normal").
                () => PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.Android) == ManagedStrippingLevel.High,
                () =>
                {
                    PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.High);
                    return true;
                }));
#endif

            return policies;
        }
    }
}