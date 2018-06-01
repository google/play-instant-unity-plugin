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

namespace GooglePlayInstant
{
    /// <summary>
    /// Android API constants. Also includes some Java class, method, and field names for use by
    /// <see href="https://docs.unity3d.com/ScriptReference/AndroidJavaObject.html">AndroidJavaObject</see>.
    /// </summary>
    public static class Android
    {
        public const string PlayStorePackageName = "com.android.vending";

        public const string ActivityMethodGetIntent = "getIntent";
        public const string ActivityMethodStartActivityForResult = "startActivityForResult";

        public const string ContextMethodGetPackageManager = "getPackageManager";

        public const string IntentActionMain = "android.intent.action.MAIN";
        public const string IntentActionView = "android.intent.action.VIEW";
        public const string IntentCategoryBrowsable = "android.intent.category.BROWSABLE";
        public const string IntentCategoryDefault = "android.intent.category.DEFAULT";
        public const string IntentCategoryLauncher = "android.intent.category.LAUNCHER";
        public const string IntentClass = "android.content.Intent";
        public const string IntentMethodAddCategory = "addCategory";
        public const string IntentMethodGetStringExtra = "getStringExtra";
        public const string IntentMethodPutExtra = "putExtra";
        public const string IntentMethodSetData = "setData";
        public const string IntentMethodSetPackage = "setPackage";

        public const string ObjectMethodGetClass = "getClass";

        public const string PackageManagerMethodResolveActivity = "resolveActivity";

        public const string UriBuilderClass = "android.net.Uri.Builder";
        public const string UriBuilderMethodAppendQueryParameter = "appendQueryParameter";
        public const string UriBuilderMethodAuthority = "authority";
        public const string UriBuilderMethodBuild = "build";
        public const string UriBuilderMethodScheme = "scheme";
    }
}