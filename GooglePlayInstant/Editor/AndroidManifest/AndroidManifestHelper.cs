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
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace GooglePlayInstant.Editor.AndroidManifest
{
    /// <summary>
    /// A helper class for updating the AndroidManifest to target installed vs instant apps.
    /// </summary>
    public static class AndroidManifestHelper
    {
        private const string Action = "action";
        private const string ActionMain = "android.intent.action.MAIN";
        private const string ActionView = "android.intent.action.VIEW";
        private const string Activity = "activity";
        private const string Application = "application";
        private const string Category = "category";
        private const string CategoryBrowsable = "android.intent.category.BROWSABLE";
        private const string CategoryDefault = "android.intent.category.DEFAULT";
        private const string CategoryLauncher = "android.intent.category.LAUNCHER";
        private const string Data = "data";
        private const string DefaultUrl = "default-url";
        private const string IntentFilter = "intent-filter";
        private const string Manifest = "manifest";
        private const string MetaData = "meta-data";
        private const string AndroidNamespaceAlias = "android";
        private const string AndroidNamespaceUrl = "http://schemas.android.com/apk/res/android";
        private static readonly XName AndroidXmlns = XNamespace.Xmlns + AndroidNamespaceAlias;
        private static readonly XName AndroidAutoVerifyXName = XName.Get("autoVerify", AndroidNamespaceUrl);
        private static readonly XName AndroidHostXName = XName.Get("host", AndroidNamespaceUrl);
        private static readonly XName AndroidNameXName = XName.Get("name", AndroidNamespaceUrl);
        private static readonly XName AndroidPathXName = XName.Get("path", AndroidNamespaceUrl);
        private static readonly XName AndroidSchemeXName = XName.Get("scheme", AndroidNamespaceUrl);
        private static readonly XName AndroidValueXName = XName.Get("value", AndroidNamespaceUrl);

        private static readonly XName AndroidTargetSandboxVersionXName =
            XName.Get("targetSandboxVersion", AndroidNamespaceUrl);

        // These precondition check strings are visibile for testing.
        internal const string PreconditionOneManifestElement = "expect 1 manifest element";
        internal const string PreconditionMissingXmlnsAndroid = "missing manifest attribute xmlns:android";
        internal const string PreconditionInvalidXmlnsAndroid = "invalid value for xmlns:android";
        internal const string PreconditionOneApplicationElement = "expect 1 application element";
        internal const string PreconditionOneMainActivity = "expect 1 activity with action MAIN and category LAUNCHER";

        /// <summary>
        /// Creates a new XDocument representing a basic Unity AndroidManifest XML file.
        /// </summary>
        public static XDocument CreateManifestXDocument()
        {
            return new XDocument(new XElement(
                Manifest,
                new XAttribute(AndroidXmlns, XNamespace.Get(AndroidNamespaceUrl)),
                new XElement(Application,
                    new XElement(Activity,
                        new XAttribute(AndroidNameXName, "com.unity3d.player.UnityPlayerActivity"),
                        new XElement(IntentFilter,
                            new XElement(Action, new XAttribute(AndroidNameXName, ActionMain)),
                            new XElement(Category, new XAttribute(AndroidNameXName, CategoryLauncher)))))));
        }

        /// <summary>
        /// Converts the specified XDocument representing an AndroidManifest to support an installed app build.
        /// </summary>
        public static void ConvertManifestToInstalled(XDocument doc)
        {
            foreach (var manifestElement in doc.Elements(Manifest))
            {
                manifestElement.Attributes(AndroidTargetSandboxVersionXName).Remove();
                foreach (var applicationElement in manifestElement.Elements(Application))
                {
                    foreach (var mainActivity in GetMainActivities(applicationElement))
                    {
                        // TODO: also remove view intent filters?
                        GetDefaultUrlMetaDataElements(mainActivity).Remove();
                    }
                }
            }
        }

        /// <summary>
        /// Converts the specified XDocument representing an AndroidManifest to support an instant app build.
        /// </summary>
        /// <param name="doc">An XDocument representing an AndroidManifest.</param>
        /// <param name="uri">The Default URL to use, or null for a URL-less instant app.</param>
        /// <returns>An error message if there was a problem updating the manifest, or null if successful.</returns>
        public static string ConvertManifestToInstant(XDocument doc, Uri uri)
        {
            var manifestElement = GetExactlyOne(doc.Elements(Manifest));
            if (manifestElement == null)
            {
                return PreconditionOneManifestElement;
            }

            var androidAttribute = manifestElement.Attribute(AndroidXmlns);
            if (androidAttribute == null)
            {
                return PreconditionMissingXmlnsAndroid;
            }

            if (androidAttribute.Value != AndroidNamespaceUrl)
            {
                return PreconditionInvalidXmlnsAndroid;
            }

            // TSV2 is required for instant apps starting with Android Oreo.
            manifestElement.SetAttributeValue(AndroidTargetSandboxVersionXName, "2");

            return uri == null ? null : AddDefaultUrl(manifestElement, uri);
        }

        private static string AddDefaultUrl(XElement manifestElement, Uri uri)
        {
            var applicationElement = GetExactlyOne(manifestElement.Elements(Application));
            if (applicationElement == null)
            {
                return PreconditionOneApplicationElement;
            }

            var mainActivity = GetExactlyOne(GetMainActivities(applicationElement));
            if (mainActivity == null)
            {
                return PreconditionOneMainActivity;
            }

            var updateViewIntentFilterResult = UpdateViewIntentFilter(mainActivity, uri);
            if (updateViewIntentFilterResult != null)
            {
                return updateViewIntentFilterResult;
            }

            return UpdateDefaultUrlElement(mainActivity, uri);
        }

        private static string UpdateViewIntentFilter(XElement mainActivity, Uri uri)
        {
            // Find the Activity's Intent Filter with action type VIEW to update, or create a new Intent Filter.
            var actionViewIntentFilters = GetActionViewIntentFilters(mainActivity);
            XElement viewIntentFilter;
            switch (actionViewIntentFilters.Count())
            {
                case 0:
                    viewIntentFilter = new XElement(IntentFilter);
                    mainActivity.Add(viewIntentFilter);
                    break;
                case 1:
                    viewIntentFilter = actionViewIntentFilters.First();
                    // TODO: preserve existing elements and just update
                    viewIntentFilter.RemoveAll();
                    break;
                default:
                    // TODO: add support for activities with multiple VIEW intent filters
                    return "more than one VIEW intent-filter";
            }

            // See https://developer.android.com/topic/google-play-instant/getting-started/game-instant-app#app-links
            // and https://developer.android.com/training/app-links/verify-site-associations for info on "autoVerify".
            viewIntentFilter.SetAttributeValue(AndroidAutoVerifyXName, "true");
            viewIntentFilter.Add(CreateElementWithAttribute(Action, AndroidNameXName, ActionView));
            viewIntentFilter.Add(
                CreateElementWithAttribute(Category, AndroidNameXName, CategoryBrowsable));
            viewIntentFilter.Add(CreateElementWithAttribute(Category, AndroidNameXName, CategoryDefault));
            viewIntentFilter.Add(CreateElementWithAttribute(Data, AndroidSchemeXName, "http"));
            viewIntentFilter.Add(CreateElementWithAttribute(Data, AndroidSchemeXName, "https"));
            viewIntentFilter.Add(CreateElementWithAttribute(Data, AndroidHostXName, uri.Host));
            var path = uri.AbsolutePath;
            if (!string.IsNullOrEmpty(path) && path != "/")
            {
                viewIntentFilter.Add(CreateElementWithAttribute(Data, AndroidPathXName, path));
            }

            return null;
        }

        private static string UpdateDefaultUrlElement(XElement mainActivity, Uri uri)
        {
            // Find the Activity's existing meta-data element for default-url to update, or create a new one.
            var metaDataElements = GetDefaultUrlMetaDataElements(mainActivity);
            XElement defaultUrlMetaData;
            switch (metaDataElements.Count())
            {
                case 0:
                    defaultUrlMetaData = new XElement(MetaData);
                    mainActivity.Add(defaultUrlMetaData);
                    break;
                case 1:
                    defaultUrlMetaData = metaDataElements.First();
                    defaultUrlMetaData.RemoveAttributes();
                    break;
                default:
                    return "more than one meta-data element for default-url";
            }

            defaultUrlMetaData.SetAttributeValue(AndroidNameXName, DefaultUrl);
            defaultUrlMetaData.SetAttributeValue(AndroidValueXName, uri);

            return null;
        }

        private static IEnumerable<XElement> GetMainActivities(XContainer applicationElement)
        {
            // Find all activities with an <intent-filter> that contains
            //  <action android:name="android.intent.action.MAIN" />
            //  <category android:name="android.intent.category.LAUNCHER" />
            return
                from activityElement in applicationElement.Elements(Activity)
                where
                    (from intentFilter in activityElement.Elements(IntentFilter)
                        where
                            intentFilter.Elements(Action)
                                .Any(e => (string) e.Attribute(AndroidNameXName) == ActionMain) &&
                            intentFilter.Elements(Category)
                                .Any(e => (string) e.Attribute(AndroidNameXName) == CategoryLauncher)
                        select intentFilter)
                    .Any()
                select activityElement;
        }

        private static IEnumerable<XElement> GetActionViewIntentFilters(XContainer mainActivity)
        {
            // Find all intent filters that contain <action android:name="android.intent.action.VIEW" />
            return from intentFilter in mainActivity.Elements(IntentFilter)
                where intentFilter.Elements(Action).Any(e => (string) e.Attribute(AndroidNameXName) == ActionView)
                select intentFilter;
        }

        private static IEnumerable<XElement> GetDefaultUrlMetaDataElements(XContainer mainActivity)
        {
            // Find all elements of the form <meta-data android:name="default-url" />
            return from metaData in mainActivity.Elements(MetaData)
                where (string) metaData.Attribute(AndroidNameXName) == DefaultUrl
                select metaData;
        }

        private static XElement CreateElementWithAttribute(string elementName, XName attributeName,
            string attributeValue)
        {
            var element = new XElement(elementName);
            element.SetAttributeValue(attributeName, attributeValue);
            return element;
        }

        private static XElement GetExactlyOne(IEnumerable<XElement> elements)
        {
            // If the IEnumerable has exactly 1 element, return it. If the IEnumerable has 0 or 2+ elements, return
            // null. Cannot use FirstOrDefault() here since that will return the first element if 2+ elements.
            return elements.Count() == 1 ? elements.First() : null;
        }
    }
}