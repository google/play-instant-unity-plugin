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
using System.IO;
using System.Text;
using System.Xml.Linq;
using GooglePlayInstant.Editor.AndroidManifest;
using NUnit.Framework;

namespace GooglePlayInstant.Tests.Editor.AndroidManifest
{
    [TestFixture]
    public class AndroidManifestHelperTests
    {
        // The following constants are duplicated between AndroidManifestHelper and here since they are part of the
        // Android manifest schema. Changing one of these strings in AndroidManifestHelper should break a test.
        private const string Action = "action";
        private const string Activity = "activity";
        private const string Application = "application";
        private const string Category = "category";
        private const string Data = "data";
        private const string DefaultUrl = "default-url";
        private const string IntentFilter = "intent-filter";
        private const string Manifest = "manifest";
        private const string MetaData = "meta-data";
        private const string PlayInstantUnityPluginVersion = "play-instant-unity-plugin.version";
        private const string ValueTrue = "true";
        private const string AndroidNamespaceAlias = "android";
        private const string AndroidNamespaceUrl = "http://schemas.android.com/apk/res/android";
        private const string DistributionNamespaceAlias = "dist";
        private const string DistributionNamespaceUrl = "http://schemas.android.com/apk/distribution";

        private static readonly XName AndroidXmlns = XNamespace.Xmlns + AndroidNamespaceAlias;
        private static readonly XName AndroidAutoVerifyXName = XName.Get("autoVerify", AndroidNamespaceUrl);
        private static readonly XName AndroidHostXName = XName.Get("host", AndroidNamespaceUrl);
        private static readonly XName AndroidNameXName = XName.Get("name", AndroidNamespaceUrl);
        private static readonly XName AndroidPathXName = XName.Get("path", AndroidNamespaceUrl);
        private static readonly XName AndroidSchemeXName = XName.Get("scheme", AndroidNamespaceUrl);
        private static readonly XName AndroidValueXName = XName.Get("value", AndroidNamespaceUrl);
        private static readonly XName DistributionXmlns = XNamespace.Xmlns + DistributionNamespaceAlias;
        private static readonly XName DistributionModuleXName = XName.Get("module", DistributionNamespaceUrl);
        private static readonly XName DistributionInstantXName = XName.Get("instant", DistributionNamespaceUrl);

        private static readonly XAttribute AndroidNamespaceAttribute =
            new XAttribute(AndroidXmlns, XNamespace.Get(AndroidNamespaceUrl));

        private static readonly XAttribute DistributionNamespaceAttribute =
            new XAttribute(DistributionXmlns, XNamespace.Get(DistributionNamespaceUrl));

        private const string MainActivityName = "MainActivity";
        private const string TestUrl = "https://example.com";
        private static readonly Uri TestUri = new Uri(TestUrl);

        private static readonly XAttribute TargetSandboxVersion2Attribute =
            new XAttribute(XName.Get("targetSandboxVersion", AndroidNamespaceUrl), "2");

        private static readonly XElement PluginVersion =
            new XElement(MetaData,
                new XAttribute(AndroidNameXName, PlayInstantUnityPluginVersion),
                new XAttribute(AndroidValueXName, GooglePlayInstantUtils.PluginVersion));

        private static readonly XElement MainLauncherIntentFilter =
            new XElement(
                IntentFilter,
                new XElement(Action, new XAttribute(AndroidNameXName, Android.IntentActionMain)),
                new XElement(Category, new XAttribute(AndroidNameXName, Android.IntentCategoryLauncher)));

        private static readonly XElement MainActivityNoUrls =
            new XElement(Activity, new XAttribute(AndroidNameXName, MainActivityName), MainLauncherIntentFilter);

        private static readonly XElement OtherBasicActivity =
            new XElement(Activity, new XAttribute(AndroidNameXName, "OtherBasicActivity"));

        private static readonly XElement OtherActivityWithViewIntent =
            new XElement(Activity,
                new XAttribute(AndroidNameXName, "OtherActivityWithViewIntent"),
                CreateViewIntentFilter("other.com", "path"));

        private static readonly XDocument InstalledManifestWithoutUrl =
            new XDocument(
                new XElement(Manifest,
                    AndroidNamespaceAttribute,
                    new XElement(Application, OtherBasicActivity, MainActivityNoUrls, OtherActivityWithViewIntent)));

        private static readonly XDocument InstantManifestWithoutUrl =
            new XDocument(
                new XElement(Manifest,
                    AndroidNamespaceAttribute,
                    DistributionNamespaceAttribute,
                    TargetSandboxVersion2Attribute,
                    new XElement(Application,
                        OtherBasicActivity, MainActivityNoUrls, OtherActivityWithViewIntent, PluginVersion),
                    CreateDistributionModuleInstant(ValueTrue)));

        private static readonly XDocument InstalledManifestWithUrl =
            new XDocument(
                new XElement(Manifest,
                    AndroidNamespaceAttribute,
                    new XElement(Application,
                        OtherBasicActivity,
                        new XElement(Activity,
                            new XAttribute(AndroidNameXName, MainActivityName),
                            MainLauncherIntentFilter,
                            CreateViewIntentFilter("example.com", null)),
                        OtherActivityWithViewIntent)));

        private static readonly XDocument InstantManifestWithUrl =
            new XDocument(
                new XElement(Manifest,
                    AndroidNamespaceAttribute,
                    DistributionNamespaceAttribute,
                    TargetSandboxVersion2Attribute,
                    new XElement(Application,
                        OtherBasicActivity,
                        new XElement(Activity,
                            new XAttribute(AndroidNameXName, MainActivityName),
                            MainLauncherIntentFilter,
                            CreateViewIntentFilter("example.com", null),
                            CreateDefaultUrl("https://example.com/")),
                        OtherActivityWithViewIntent,
                        PluginVersion),
                    CreateDistributionModuleInstant(ValueTrue)));

        [Test]
        public void TestCreateManifestXDocument()
        {
            // Note: can switch to "utf-8" by creating a custom StringWriter using Encoding.UTF8
            const string expected = @"<?xml version=""1.0"" encoding=""utf-16""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android"">
  <application>
    <activity android:name=""com.unity3d.player.UnityPlayerActivity"">
      <intent-filter>
        <action android:name=""android.intent.action.MAIN"" />
        <category android:name=""android.intent.category.LAUNCHER"" />
      </intent-filter>
    </activity>
  </application>
</manifest>";

            var doc = AndroidManifestHelper.CreateManifestXDocument();

            // Need to use Save() to obtain the "<?xml>" declaration since XDocument.ToString() omits this.
            var stringBuilder = new StringBuilder();
            using (var writer = new StringWriter(stringBuilder))
            {
                doc.Save(writer);
            }

            Assert.AreEqual(expected, stringBuilder.ToString());
        }

        [Test]
        public void TestHasCurrentPluginVersion_NoManifestElement()
        {
            string errorMessage;
            var doc = new XDocument();
            Assert.IsFalse(AndroidManifestHelper.HasCurrentPluginVersion(doc, out errorMessage));
            Assert.AreEqual(AndroidManifestHelper.PreconditionOneManifestElement, errorMessage);
        }

        [Test]
        public void TestTestHasCurrentPluginVersion_NoApplicationElement()
        {
            string errorMessage;
            var doc = new XDocument(new XElement(Manifest));
            Assert.IsFalse(AndroidManifestHelper.HasCurrentPluginVersion(doc, out errorMessage));
            Assert.AreEqual(AndroidManifestHelper.PreconditionOneApplicationElement, errorMessage);
        }

        [Test]
        public void TestHasCurrentPluginVersion_NoPluginVersion()
        {
            string errorMessage;
            var doc = new XDocument(new XElement(Manifest, AndroidNamespaceAttribute, new XElement(Application)));
            Assert.IsFalse(AndroidManifestHelper.HasCurrentPluginVersion(doc, out errorMessage));
            Assert.IsNull(errorMessage);
        }

        [Test]
        public void TestHasCurrentPluginVersion_IncorrectPluginVersion()
        {
            string errorMessage;
            var doc = new XDocument(new XElement(Manifest, AndroidNamespaceAttribute,
                new XElement(Application,
                    new XElement(MetaData,
                        new XAttribute(AndroidNameXName, PlayInstantUnityPluginVersion),
                        new XAttribute(AndroidValueXName, "Incorrect Version")))));
            Assert.IsFalse(AndroidManifestHelper.HasCurrentPluginVersion(doc, out errorMessage));
            Assert.IsNull(errorMessage);
        }

        [Test]
        public void TestTestHasCurrentPluginVersion_TwoPluginVersions()
        {
            string errorMessage;
            var doc = new XDocument(new XElement(Manifest, AndroidNamespaceAttribute,
                new XElement(Application, PluginVersion, PluginVersion)));
            Assert.IsFalse(AndroidManifestHelper.HasCurrentPluginVersion(doc, out errorMessage));
            Assert.AreEqual(AndroidManifestHelper.PreconditionOnePluginVersion, errorMessage);
        }

        [Test]
        public void TestTestHasCurrentPluginVersion_CorrectPluginVersion()
        {
            string errorMessage;
            var doc = new XDocument(new XElement(Manifest, AndroidNamespaceAttribute,
                new XElement(Application, PluginVersion)));
            Assert.IsTrue(AndroidManifestHelper.HasCurrentPluginVersion(doc, out errorMessage));
            Assert.IsNull(errorMessage);
        }

        [Test]
        public void TestConvertManifestToInstant_WithoutUrl()
        {
            var doc = new XDocument(InstalledManifestWithoutUrl);
            var result = AndroidManifestHelper.ConvertManifestToInstant(doc, null);
            Assert.IsNull(result);
            AssertEquals(InstantManifestWithoutUrl, doc);
        }

        [Test]
        public void TestConvertManifestToInstalled_WithoutUrl()
        {
            var doc = new XDocument(InstantManifestWithoutUrl);
            AndroidManifestHelper.ConvertManifestToInstalled(doc);
            AssertEquals(InstalledManifestWithoutUrl, doc);
        }

        [Test]
        public void TestConvertManifestToInstant_WithUrl()
        {
            var doc = new XDocument(InstalledManifestWithUrl);
            var result = AndroidManifestHelper.ConvertManifestToInstant(doc, TestUri);
            Assert.IsNull(result);
            AssertEquals(InstantManifestWithUrl, doc);
        }

        [Test]
        public void TestConvertManifestToInstalled_WithUrl()
        {
            var doc = new XDocument(InstantManifestWithUrl);
            AndroidManifestHelper.ConvertManifestToInstalled(doc);
            AssertEquals(InstalledManifestWithUrl, doc);
        }

        [Test]
        public void TestConvertManifestToInstant_NoManifestElement()
        {
            var doc = new XDocument();
            var result = AndroidManifestHelper.ConvertManifestToInstant(doc, TestUri);
            Assert.AreEqual(AndroidManifestHelper.PreconditionOneManifestElement, result);
        }

        [Test]
        public void TestConvertManifestToInstant_NoAndroidNamespace()
        {
            var doc = new XDocument(new XElement(Manifest));
            var result = AndroidManifestHelper.ConvertManifestToInstant(doc, TestUri);
            Assert.AreEqual(AndroidManifestHelper.PreconditionMissingXmlnsAndroid, result);
        }

        [Test]
        public void TestConvertManifestToInstant_InvalidAndroidNamespace()
        {
            var doc = new XDocument(new XElement(Manifest,
                new XAttribute(AndroidXmlns, XNamespace.Get("http://wrong.schema.com")),
                DistributionNamespaceAttribute));
            var result = AndroidManifestHelper.ConvertManifestToInstant(doc, TestUri);
            Assert.AreEqual(AndroidManifestHelper.PreconditionInvalidXmlnsAndroid, result);
        }

        [Test]
        public void TestConvertManifestToInstant_InvalidDistributionNamespace()
        {
            var doc = new XDocument(new XElement(Manifest,
                AndroidNamespaceAttribute,
                new XAttribute(DistributionXmlns, XNamespace.Get("http://wrong.schema.com"))));
            var result = AndroidManifestHelper.ConvertManifestToInstant(doc, TestUri);
            Assert.AreEqual(AndroidManifestHelper.PreconditionInvalidXmlnsDistribution, result);
        }

        [Test]
        public void TestConvertManifestToInstant_NoApplicationElement()
        {
            var doc = new XDocument(new XElement(Manifest, AndroidNamespaceAttribute));
            var result = AndroidManifestHelper.ConvertManifestToInstant(doc, TestUri);
            Assert.AreEqual(AndroidManifestHelper.PreconditionOneApplicationElement, result);
        }

        [Test]
        public void TestConvertManifestToInstant_TwoPluginVersions()
        {
            var doc = new XDocument(new XElement(Manifest, AndroidNamespaceAttribute,
                new XElement(Application, PluginVersion, PluginVersion)));
            var result = AndroidManifestHelper.ConvertManifestToInstant(doc, TestUri);
            Assert.AreEqual(AndroidManifestHelper.PreconditionOnePluginVersion, result);
        }

        [Test]
        public void TestConvertManifestToInstant_NoActivityElement()
        {
            var doc = new XDocument(new XElement(Manifest, AndroidNamespaceAttribute, new XElement(Application)));
            var result = AndroidManifestHelper.ConvertManifestToInstant(doc, TestUri);
            Assert.AreEqual(AndroidManifestHelper.PreconditionOneMainActivity, result);
        }

        [Test]
        public void TestConvertManifestToInstant_ActivityMissingMainLauncher()
        {
            var doc = new XDocument(new XElement(Manifest, AndroidNamespaceAttribute,
                new XElement(Application,
                    new XElement(Activity))));
            var result = AndroidManifestHelper.ConvertManifestToInstant(doc, TestUri);
            Assert.AreEqual(AndroidManifestHelper.PreconditionOneMainActivity, result);
        }

        [Test]
        public void TestConvertManifestToInstant_TwoActivitiesWithMainLauncher()
        {
            var doc = new XDocument(new XElement(Manifest, AndroidNamespaceAttribute,
                new XElement(Application,
                    new XElement(Activity, new XAttribute(AndroidNameXName, "Activity1"), MainLauncherIntentFilter),
                    new XElement(Activity, new XAttribute(AndroidNameXName, "Activity2"), MainLauncherIntentFilter))));
            var result = AndroidManifestHelper.ConvertManifestToInstant(doc, TestUri);
            Assert.AreEqual(AndroidManifestHelper.PreconditionOneMainActivity, result);
        }

        [Test]
        public void TestConvertManifestToInstant_TwoViewIntentFilters()
        {
            var doc = new XDocument(new XElement(Manifest, AndroidNamespaceAttribute,
                new XElement(Application,
                    new XElement(Activity,
                        new XAttribute(AndroidNameXName, MainActivityName),
                        MainLauncherIntentFilter,
                        CreateViewIntentFilter("example.com", null),
                        CreateViewIntentFilter("example2.com", null)))));
            var result = AndroidManifestHelper.ConvertManifestToInstant(doc, TestUri);
            Assert.AreEqual(AndroidManifestHelper.PreconditionOneViewIntentFilter, result);
        }

        [Test]
        public void TestConvertManifestToInstant_TwoDefaultUrls()
        {
            var doc = new XDocument(new XElement(Manifest, AndroidNamespaceAttribute,
                new XElement(Application,
                    new XElement(Activity,
                        new XAttribute(AndroidNameXName, MainActivityName),
                        MainLauncherIntentFilter,
                        CreateViewIntentFilter("example.com", null),
                        CreateDefaultUrl("https://example.com/"),
                        CreateDefaultUrl("https://example2.com/")))));
            var result = AndroidManifestHelper.ConvertManifestToInstant(doc, TestUri);
            Assert.AreEqual(AndroidManifestHelper.PreconditionOneMetaDataDefaultUrl, result);
        }

        [Test]
        public void TestConvertManifestToInstant_TwoInstantModules()
        {
            var doc = new XDocument(new XElement(Manifest, AndroidNamespaceAttribute, DistributionNamespaceAttribute,
                new XElement(Application,
                    new XElement(Activity,
                        new XAttribute(AndroidNameXName, MainActivityName),
                        MainLauncherIntentFilter)),
                CreateDistributionModuleInstant(ValueTrue),
                CreateDistributionModuleInstant("false")));
            var result = AndroidManifestHelper.ConvertManifestToInstant(doc, TestUri);
            Assert.AreEqual(AndroidManifestHelper.PreconditionOneModuleInstant, result);
        }

        private static void AssertEquals(XNode expected, XNode actual)
        {
            // Since Assert.AreEqual(new XDocument(), new XDocument()) fails, check DeepEquals and compare strings.
            if (XNode.DeepEquals(expected, actual))
            {
                return;
            }

            Assert.AreEqual(expected.ToString(), actual.ToString());
        }

        private static XElement CreateViewIntentFilter(string host, string path)
        {
            return new XElement(
                IntentFilter,
                new XAttribute(AndroidAutoVerifyXName, ValueTrue),
                new XElement(Action, new XAttribute(AndroidNameXName, Android.IntentActionView)),
                new XElement(Category, new XAttribute(AndroidNameXName, Android.IntentCategoryBrowsable)),
                new XElement(Category, new XAttribute(AndroidNameXName, Android.IntentCategoryDefault)),
                new XElement(Data, new XAttribute(AndroidSchemeXName, "http")),
                new XElement(Data, new XAttribute(AndroidSchemeXName, "https")),
                new XElement(Data, new XAttribute(AndroidHostXName, host)),
                path == null ? null : new XElement(Data, new XAttribute(AndroidPathXName, path)));
        }

        private static XElement CreateDefaultUrl(string defaultUrl)
        {
            return new XElement(MetaData,
                new XAttribute(AndroidNameXName, DefaultUrl),
                new XAttribute(AndroidValueXName, defaultUrl));
        }

        private static XElement CreateDistributionModuleInstant(string attributeValue)
        {
            return new XElement(DistributionModuleXName, new XAttribute(DistributionInstantXName, attributeValue));
        }
    }
}