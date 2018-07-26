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
    public class PlayInstantSettingPolicy
    {
        private const string ArmOnlyDescription =
            "Pre-Oreo x86 devices are generally unsupported for instant apps. Removing x86 reduces APK size.";

        private const string MultithreadedRenderingDescription =
            "Pre-Oreo devices do not support instant apps using EGL shared contexts.";

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
                    }),
#else
                new PlayInstantSettingPolicy(
                    "Mobile Multithreaded Rendering should be disabled",
                    MultithreadedRenderingDescription,
                    () => !PlayerSettings.mobileMTRendering,
                    () =>
                    {
                        PlayerSettings.mobileMTRendering = false;
                        return true;
                    }),
#endif

#if UNITY_2018_1_OR_NEWER
                new PlayInstantSettingPolicy(
                    "Android Target Architecture should be ARMv7",
                    ArmOnlyDescription,
                    () => PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARMv7,
                    () =>
                    {
                        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
                        return true;
                    }),
#else
                new PlayInstantSettingPolicy(
                    "Android Device Filter should be ARMv7 only",
                    ArmOnlyDescription,
                    () => PlayerSettings.Android.targetDevice == AndroidTargetDevice.ARMv7,
                    () =>
                    {
                        PlayerSettings.Android.targetDevice = AndroidTargetDevice.ARMv7;
                        return true;
                    }),
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
                    }),

                new PlayInstantSettingPolicy(
                    ".NET API Compatibility Level should be \".NET 2.0 Subset\"",
                    "This setting reduces APK size.",
                    () => PlayerSettings.GetApiCompatibilityLevel(BuildTargetGroup.Android) ==
                          ApiCompatibilityLevel.NET_2_0_Subset,
                    () =>
                    {
                        PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Android,
                            ApiCompatibilityLevel.NET_2_0_Subset);
                        return true;
                    })
            };

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

                case ScriptingImplementation.Mono2x:
                    policies.Add(new PlayInstantSettingPolicy(
                        "Mono builds should use the highest code stripping level (micro mscorlib)",
                        "This setting reduces APK size.",
                        () => PlayerSettings.strippingLevel == StrippingLevel.UseMicroMSCorlib,
                        () =>
                        {
                            PlayerSettings.strippingLevel = StrippingLevel.UseMicroMSCorlib;
                            return true;
                        }));
                    break;
            }

            return policies;
        }
    }
}