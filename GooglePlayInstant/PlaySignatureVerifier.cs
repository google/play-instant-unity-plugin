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

using System.Linq;
using UnityEngine;

namespace GooglePlayInstant
{
    /// <summary>
    /// Provides methods that verify whether a package's signature matches the expected value.
    /// </summary>
    public static class PlaySignatureVerifier
    {
        /// <summary>
        /// Returns true if Google Play Services is installed and its signature matches the expected value.
        /// </summary>
        public static bool VerifyGooglePlayServices(AndroidJavaObject packageManager)
        {
            return VerifyGooglePlayPackage(packageManager, Android.GooglePlayServicesPackageName);
        }

        private static bool VerifyGooglePlayPackage(AndroidJavaObject packageManager, string packageName)
        {
            try
            {
                // Java: PackageInfo packageInfo =
                //           packageManager.getPackageInfo(packageName, PackageManager.GET_SIGNATURES)
                using (var packageInfo = packageManager.Call<AndroidJavaObject>(
                    Android.PackageManagerMethodGetPackageInfo, packageName, Android.PackageManagerFieldGetSignatures))
                {
                    // Java: Signature[] signatures = packageInfo.signatures
                    var signatures = packageInfo.Get<AndroidJavaObject[]>(Android.PackageInfoFieldSignatures);
                    if (signatures.Length != 1)
                    {
                        Debug.LogErrorFormat(
                            "Unexpected signature count {0} for package {1}", signatures.Length, packageName);
                        return false;
                    }

                    // Java: byte[] bytes = signatures[0].toByteArray()
                    var signature = signatures[0];
                    var bytes = signature.Call<byte[]>(Android.SignatureMethodToByteArray);
                    return Enumerable.SequenceEqual(bytes, GooglePlayPackageSignature);
                }
            }
            catch (AndroidJavaException ex)
            {
                // Most likely a NameNotFoundException indicating the package is not installed on this device.
                Debug.LogErrorFormat("Exception verifying package {0}: {1}", packageName, ex);
                return false;
            }
        }

        /// <summary>
        /// Bytes representing the signing certificate of certain Google Play apps.
        /// </summary>
        private static readonly byte[] GooglePlayPackageSignature =
        {
            0x30, 0x82, 0x04, 0x43, 0x30, 0x82, 0x03, 0x2B, 0xA0, 0x03, 0x02, 0x01, 0x02, 0x02, 0x09, 0x00, 0xC2, 0xE0,
            0x87, 0x46, 0x64, 0x4A, 0x30, 0x8D, 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01,
            0x04, 0x05, 0x00, 0x30, 0x74, 0x31, 0x0B, 0x30, 0x09, 0x06, 0x03, 0x55, 0x04, 0x06, 0x13, 0x02, 0x55, 0x53,
            0x31, 0x13, 0x30, 0x11, 0x06, 0x03, 0x55, 0x04, 0x08, 0x13, 0x0A, 0x43, 0x61, 0x6C, 0x69, 0x66, 0x6F, 0x72,
            0x6E, 0x69, 0x61, 0x31, 0x16, 0x30, 0x14, 0x06, 0x03, 0x55, 0x04, 0x07, 0x13, 0x0D, 0x4D, 0x6F, 0x75, 0x6E,
            0x74, 0x61, 0x69, 0x6E, 0x20, 0x56, 0x69, 0x65, 0x77, 0x31, 0x14, 0x30, 0x12, 0x06, 0x03, 0x55, 0x04, 0x0A,
            0x13, 0x0B, 0x47, 0x6F, 0x6F, 0x67, 0x6C, 0x65, 0x20, 0x49, 0x6E, 0x63, 0x2E, 0x31, 0x10, 0x30, 0x0E, 0x06,
            0x03, 0x55, 0x04, 0x0B, 0x13, 0x07, 0x41, 0x6E, 0x64, 0x72, 0x6F, 0x69, 0x64, 0x31, 0x10, 0x30, 0x0E, 0x06,
            0x03, 0x55, 0x04, 0x03, 0x13, 0x07, 0x41, 0x6E, 0x64, 0x72, 0x6F, 0x69, 0x64, 0x30, 0x1E, 0x17, 0x0D, 0x30,
            0x38, 0x30, 0x38, 0x32, 0x31, 0x32, 0x33, 0x31, 0x33, 0x33, 0x34, 0x5A, 0x17, 0x0D, 0x33, 0x36, 0x30, 0x31,
            0x30, 0x37, 0x32, 0x33, 0x31, 0x33, 0x33, 0x34, 0x5A, 0x30, 0x74, 0x31, 0x0B, 0x30, 0x09, 0x06, 0x03, 0x55,
            0x04, 0x06, 0x13, 0x02, 0x55, 0x53, 0x31, 0x13, 0x30, 0x11, 0x06, 0x03, 0x55, 0x04, 0x08, 0x13, 0x0A, 0x43,
            0x61, 0x6C, 0x69, 0x66, 0x6F, 0x72, 0x6E, 0x69, 0x61, 0x31, 0x16, 0x30, 0x14, 0x06, 0x03, 0x55, 0x04, 0x07,
            0x13, 0x0D, 0x4D, 0x6F, 0x75, 0x6E, 0x74, 0x61, 0x69, 0x6E, 0x20, 0x56, 0x69, 0x65, 0x77, 0x31, 0x14, 0x30,
            0x12, 0x06, 0x03, 0x55, 0x04, 0x0A, 0x13, 0x0B, 0x47, 0x6F, 0x6F, 0x67, 0x6C, 0x65, 0x20, 0x49, 0x6E, 0x63,
            0x2E, 0x31, 0x10, 0x30, 0x0E, 0x06, 0x03, 0x55, 0x04, 0x0B, 0x13, 0x07, 0x41, 0x6E, 0x64, 0x72, 0x6F, 0x69,
            0x64, 0x31, 0x10, 0x30, 0x0E, 0x06, 0x03, 0x55, 0x04, 0x03, 0x13, 0x07, 0x41, 0x6E, 0x64, 0x72, 0x6F, 0x69,
            0x64, 0x30, 0x82, 0x01, 0x20, 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01,
            0x05, 0x00, 0x03, 0x82, 0x01, 0x0D, 0x00, 0x30, 0x82, 0x01, 0x08, 0x02, 0x82, 0x01, 0x01, 0x00, 0xAB, 0x56,
            0x2E, 0x00, 0xD8, 0x3B, 0xA2, 0x08, 0xAE, 0x0A, 0x96, 0x6F, 0x12, 0x4E, 0x29, 0xDA, 0x11, 0xF2, 0xAB, 0x56,
            0xD0, 0x8F, 0x58, 0xE2, 0xCC, 0xA9, 0x13, 0x03, 0xE9, 0xB7, 0x54, 0xD3, 0x72, 0xF6, 0x40, 0xA7, 0x1B, 0x1D,
            0xCB, 0x13, 0x09, 0x67, 0x62, 0x4E, 0x46, 0x56, 0xA7, 0x77, 0x6A, 0x92, 0x19, 0x3D, 0xB2, 0xE5, 0xBF, 0xB7,
            0x24, 0xA9, 0x1E, 0x77, 0x18, 0x8B, 0x0E, 0x6A, 0x47, 0xA4, 0x3B, 0x33, 0xD9, 0x60, 0x9B, 0x77, 0x18, 0x31,
            0x45, 0xCC, 0xDF, 0x7B, 0x2E, 0x58, 0x66, 0x74, 0xC9, 0xE1, 0x56, 0x5B, 0x1F, 0x4C, 0x6A, 0x59, 0x55, 0xBF,
            0xF2, 0x51, 0xA6, 0x3D, 0xAB, 0xF9, 0xC5, 0x5C, 0x27, 0x22, 0x22, 0x52, 0xE8, 0x75, 0xE4, 0xF8, 0x15, 0x4A,
            0x64, 0x5F, 0x89, 0x71, 0x68, 0xC0, 0xB1, 0xBF, 0xC6, 0x12, 0xEA, 0xBF, 0x78, 0x57, 0x69, 0xBB, 0x34, 0xAA,
            0x79, 0x84, 0xDC, 0x7E, 0x2E, 0xA2, 0x76, 0x4C, 0xAE, 0x83, 0x07, 0xD8, 0xC1, 0x71, 0x54, 0xD7, 0xEE, 0x5F,
            0x64, 0xA5, 0x1A, 0x44, 0xA6, 0x02, 0xC2, 0x49, 0x05, 0x41, 0x57, 0xDC, 0x02, 0xCD, 0x5F, 0x5C, 0x0E, 0x55,
            0xFB, 0xEF, 0x85, 0x19, 0xFB, 0xE3, 0x27, 0xF0, 0xB1, 0x51, 0x16, 0x92, 0xC5, 0xA0, 0x6F, 0x19, 0xD1, 0x83,
            0x85, 0xF5, 0xC4, 0xDB, 0xC2, 0xD6, 0xB9, 0x3F, 0x68, 0xCC, 0x29, 0x79, 0xC7, 0x0E, 0x18, 0xAB, 0x93, 0x86,
            0x6B, 0x3B, 0xD5, 0xDB, 0x89, 0x99, 0x55, 0x2A, 0x0E, 0x3B, 0x4C, 0x99, 0xDF, 0x58, 0xFB, 0x91, 0x8B, 0xED,
            0xC1, 0x82, 0xBA, 0x35, 0xE0, 0x03, 0xC1, 0xB4, 0xB1, 0x0D, 0xD2, 0x44, 0xA8, 0xEE, 0x24, 0xFF, 0xFD, 0x33,
            0x38, 0x72, 0xAB, 0x52, 0x21, 0x98, 0x5E, 0xDA, 0xB0, 0xFC, 0x0D, 0x0B, 0x14, 0x5B, 0x6A, 0xA1, 0x92, 0x85,
            0x8E, 0x79, 0x02, 0x01, 0x03, 0xA3, 0x81, 0xD9, 0x30, 0x81, 0xD6, 0x30, 0x1D, 0x06, 0x03, 0x55, 0x1D, 0x0E,
            0x04, 0x16, 0x04, 0x14, 0xC7, 0x7D, 0x8C, 0xC2, 0x21, 0x17, 0x56, 0x25, 0x9A, 0x7F, 0xD3, 0x82, 0xDF, 0x6B,
            0xE3, 0x98, 0xE4, 0xD7, 0x86, 0xA5, 0x30, 0x81, 0xA6, 0x06, 0x03, 0x55, 0x1D, 0x23, 0x04, 0x81, 0x9E, 0x30,
            0x81, 0x9B, 0x80, 0x14, 0xC7, 0x7D, 0x8C, 0xC2, 0x21, 0x17, 0x56, 0x25, 0x9A, 0x7F, 0xD3, 0x82, 0xDF, 0x6B,
            0xE3, 0x98, 0xE4, 0xD7, 0x86, 0xA5, 0xA1, 0x78, 0xA4, 0x76, 0x30, 0x74, 0x31, 0x0B, 0x30, 0x09, 0x06, 0x03,
            0x55, 0x04, 0x06, 0x13, 0x02, 0x55, 0x53, 0x31, 0x13, 0x30, 0x11, 0x06, 0x03, 0x55, 0x04, 0x08, 0x13, 0x0A,
            0x43, 0x61, 0x6C, 0x69, 0x66, 0x6F, 0x72, 0x6E, 0x69, 0x61, 0x31, 0x16, 0x30, 0x14, 0x06, 0x03, 0x55, 0x04,
            0x07, 0x13, 0x0D, 0x4D, 0x6F, 0x75, 0x6E, 0x74, 0x61, 0x69, 0x6E, 0x20, 0x56, 0x69, 0x65, 0x77, 0x31, 0x14,
            0x30, 0x12, 0x06, 0x03, 0x55, 0x04, 0x0A, 0x13, 0x0B, 0x47, 0x6F, 0x6F, 0x67, 0x6C, 0x65, 0x20, 0x49, 0x6E,
            0x63, 0x2E, 0x31, 0x10, 0x30, 0x0E, 0x06, 0x03, 0x55, 0x04, 0x0B, 0x13, 0x07, 0x41, 0x6E, 0x64, 0x72, 0x6F,
            0x69, 0x64, 0x31, 0x10, 0x30, 0x0E, 0x06, 0x03, 0x55, 0x04, 0x03, 0x13, 0x07, 0x41, 0x6E, 0x64, 0x72, 0x6F,
            0x69, 0x64, 0x82, 0x09, 0x00, 0xC2, 0xE0, 0x87, 0x46, 0x64, 0x4A, 0x30, 0x8D, 0x30, 0x0C, 0x06, 0x03, 0x55,
            0x1D, 0x13, 0x04, 0x05, 0x30, 0x03, 0x01, 0x01, 0xFF, 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7,
            0x0D, 0x01, 0x01, 0x04, 0x05, 0x00, 0x03, 0x82, 0x01, 0x01, 0x00, 0x6D, 0xD2, 0x52, 0xCE, 0xEF, 0x85, 0x30,
            0x2C, 0x36, 0x0A, 0xAA, 0xCE, 0x93, 0x9B, 0xCF, 0xF2, 0xCC, 0xA9, 0x04, 0xBB, 0x5D, 0x7A, 0x16, 0x61, 0xF8,
            0xAE, 0x46, 0xB2, 0x99, 0x42, 0x04, 0xD0, 0xFF, 0x4A, 0x68, 0xC7, 0xED, 0x1A, 0x53, 0x1E, 0xC4, 0x59, 0x5A,
            0x62, 0x3C, 0xE6, 0x07, 0x63, 0xB1, 0x67, 0x29, 0x7A, 0x7A, 0xE3, 0x57, 0x12, 0xC4, 0x07, 0xF2, 0x08, 0xF0,
            0xCB, 0x10, 0x94, 0x29, 0x12, 0x4D, 0x7B, 0x10, 0x62, 0x19, 0xC0, 0x84, 0xCA, 0x3E, 0xB3, 0xF9, 0xAD, 0x5F,
            0xB8, 0x71, 0xEF, 0x92, 0x26, 0x9A, 0x8B, 0xE2, 0x8B, 0xF1, 0x6D, 0x44, 0xC8, 0xD9, 0xA0, 0x8E, 0x6C, 0xB2,
            0xF0, 0x05, 0xBB, 0x3F, 0xE2, 0xCB, 0x96, 0x44, 0x7E, 0x86, 0x8E, 0x73, 0x10, 0x76, 0xAD, 0x45, 0xB3, 0x3F,
            0x60, 0x09, 0xEA, 0x19, 0xC1, 0x61, 0xE6, 0x26, 0x41, 0xAA, 0x99, 0x27, 0x1D, 0xFD, 0x52, 0x28, 0xC5, 0xC5,
            0x87, 0x87, 0x5D, 0xDB, 0x7F, 0x45, 0x27, 0x58, 0xD6, 0x61, 0xF6, 0xCC, 0x0C, 0xCC, 0xB7, 0x35, 0x2E, 0x42,
            0x4C, 0xC4, 0x36, 0x5C, 0x52, 0x35, 0x32, 0xF7, 0x32, 0x51, 0x37, 0x59, 0x3C, 0x4A, 0xE3, 0x41, 0xF4, 0xDB,
            0x41, 0xED, 0xDA, 0x0D, 0x0B, 0x10, 0x71, 0xA7, 0xC4, 0x40, 0xF0, 0xFE, 0x9E, 0xA0, 0x1C, 0xB6, 0x27, 0xCA,
            0x67, 0x43, 0x69, 0xD0, 0x84, 0xBD, 0x2F, 0xD9, 0x11, 0xFF, 0x06, 0xCD, 0xBF, 0x2C, 0xFA, 0x10, 0xDC, 0x0F,
            0x89, 0x3A, 0xE3, 0x57, 0x62, 0x91, 0x90, 0x48, 0xC7, 0xEF, 0xC6, 0x4C, 0x71, 0x44, 0x17, 0x83, 0x42, 0xF7,
            0x05, 0x81, 0xC9, 0xDE, 0x57, 0x3A, 0xF5, 0x5B, 0x39, 0x0D, 0xD7, 0xFD, 0xB9, 0x41, 0x86, 0x31, 0x89, 0x5D,
            0x5F, 0x75, 0x9F, 0x30, 0x11, 0x26, 0x87, 0xFF, 0x62, 0x14, 0x10, 0xC0, 0x69, 0x30, 0x8A
        };
    }
}