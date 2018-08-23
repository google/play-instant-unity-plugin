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
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    /// <summary>
    /// Provides methods for interacting with Google Cloud Platform (GCP), e.g. to upload an Asset Bundle file to
    /// Google Cloud Storage.
    /// </summary>
    public static class GcpClient
    {
        /// <summary>
        /// Executes all the steps required for deploying a file to GCP according to developer's configuration.
        /// First verifies if configured bucket exists, and creates the bucket if it does not exist. It then uploads
        /// the file to GCP and sets the visibility of the file to public.
        /// </summary>
        public static void DeployConfiguredFile()
        {
            InvokeAccessRestrictedAction(CheckBucketExistence, bucketInfoResponse =>
            {
                if (string.IsNullOrEmpty(bucketInfoResponse.error))
                {
                    // No error indicates that the bucket exists. Confirm the bucket name is correct and upload the file.
                    var response = JsonUtility.FromJson<BucketInfoResponse>(bucketInfoResponse.text);
                    var bucketName = response.name;
                    if (!string.Equals(bucketName, QuickDeployConfig.CloudStorageBucketName))
                    {
                        throw new Exception(string.Format(
                            "Response bucket name \"{0}\" doesn't match expected name \"{1}\"",
                            bucketName, QuickDeployConfig.CloudStorageBucketName));
                    }

                    UploadBundleAndMakePublic();
                    return;
                }

                // Any HTTP Status Code other than 404 is a fatal error.
                if (!bucketInfoResponse.error.StartsWith("404"))
                {
                    throw new Exception(string.Format(
                        "Error verifying bucket existence: {0} \n {1}", bucketInfoResponse.error,
                        bucketInfoResponse.error));
                }

                // HTTP Status Code 404 indicates that the bucket doesn't exist, so create it. 
                InvokeAccessRestrictedAction(CreateBucket, bucketCreationResponse =>
                {
                    var error = bucketCreationResponse.error;
                    if (!string.IsNullOrEmpty(error))
                    {
                        throw new Exception(string.Format("Error creating bucket: {0}\n{1}",
                            error, bucketCreationResponse.text));
                    }

                    Debug.Log("Google Cloud Storage bucket was successfully created.");
                    UploadBundleAndMakePublic();
                });
            });
        }


        /// <summary>
        /// Uploads configured file to GCP and makes the file public.
        /// </summary>
        /// <exception cref="Exception">Exception thrown if there was an error uploading the bundle or setting the
        /// visibility of file to public.</exception>
        private static void UploadBundleAndMakePublic()
        {
            InvokeAccessRestrictedAction(UploadBundleFile, uploadBundleWww =>
            {
                if (!string.IsNullOrEmpty(uploadBundleWww.error))
                {
                    throw new Exception(string.Format("Error uploading bundle: {0}\n{1}", uploadBundleWww.error,
                        uploadBundleWww.text));
                }

                Debug.Log("Uploaded bundle to Google Cloud Platform.");
                var response = JsonUtility.FromJson<FileUploadResponse>(uploadBundleWww.text);
                var bucketName = response.bucket;
                var fileName = response.name;
                if (bucketName != QuickDeployConfig.CloudStorageBucketName)
                {
                    throw new Exception(string.Format(
                        "Response bucket name \"{0}\" doesn't match expected name \"{1}\"",
                        bucketName, QuickDeployConfig.CloudStorageBucketName));
                }

                if (fileName != QuickDeployConfig.CloudStorageObjectName)
                {
                    throw new Exception(string.Format(
                        "Response file name \"{0}\" doesn't match expected name \"{1}\"",
                        fileName, QuickDeployConfig.CloudStorageObjectName));
                }

                QuickDeployConfig.AssetBundleUrl =
                    string.Format("https://storage.googleapis.com/{0}/{1}", bucketName, fileName);

                InvokeAccessRestrictedAction(MakeBundlePublic, makeBundlePublicWww =>
                {
                    var error = makeBundlePublicWww.error;
                    if (!string.IsNullOrEmpty(error))
                    {
                        throw new Exception(string.Format("Error making file public: {0}\n{1}", error,
                            makeBundlePublicWww.text));
                    }

                    Debug.Log("Set visibility of deployed file to public.");
                });
            });
        }

        /// <summary>
        /// Sends an HTTP request to GCP to upload the file according to quick deploy configurations, and
        /// invokes the handler action on the response.
        /// </summary>
        /// <param name="postResponseAction">An action to be invoked on the www instance holding the http request
        /// once the response to the request to upload the bundle is available</param>
        private static void UploadBundleFile(Action<WWW> postResponseAction)
        {
            // See https://cloud.google.com/storage/docs/uploading-objects
            var uploadEndpoint =
                string.Format("https://www.googleapis.com/upload/storage/v1/b/{0}/o?uploadType=media&name={1}",
                    QuickDeployConfig.CloudStorageBucketName, QuickDeployConfig.CloudStorageObjectName);
            var assetBundleFileBytes = File.ReadAllBytes(QuickDeployConfig.AssetBundleFileName);
            var request =
                SendAuthenticatedPostRequest(uploadEndpoint, assetBundleFileBytes, "application/octet-stream");
            WwwRequestInProgress.TrackProgress(request, "Uploading file to Google Cloud Storage", postResponseAction);
        }

        /// <summary>
        /// Sends an HTTP request to GCP to create a bucket with the configured name, and invokes the response
        /// handler on the HTTP response.
        /// </summary>
        /// <param name="onCreateBucketResponseAction">An action to be invoked on the www instance holding the HTTP request
        /// once the response to the request to create bucket is available.</param>
        private static void CreateBucket(Action<WWW> onCreateBucketResponseAction)
        {
            var credentials = OAuth2Credentials.GetCredentials();
            // see https://cloud.google.com/storage/docs/creating-buckets on creating buckets.
            var createBucketEndPoint = string.Format("https://www.googleapis.com/storage/v1/b?project={0}",
                credentials.project_id);
            var createBucketRequest = new CreateBucketRequest
            {
                name = QuickDeployConfig.CloudStorageBucketName
            };

            var jsonBytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(createBucketRequest));
            var createBucketWww = SendAuthenticatedPostRequest(createBucketEndPoint, jsonBytes, "application/json");
            WwwRequestInProgress.TrackProgress(
                createBucketWww,
                string.Format("Creating bucket with name \"{0}\"", createBucketRequest.name),
                onCreateBucketResponseAction);
        }

        /// <summary>
        /// Sends a request to GCP to change visibility of specified file to public, and invokes the result handler
        /// action on the HTTP response.
        /// </summary>
        /// <param name="onMakeBundlePublicResponseAction">An action to be invoked on the WWW instance holding the HTTP
        /// request once the response to the request to make the bundle public is available.</param>
        private static void MakeBundlePublic(Action<WWW> onMakeBundlePublicResponseAction)
        {
            // see https://cloud.google.com/storage/docs/access-control/making-data-public on making data public.
            var makePublicEndpoint = string.Format("https://www.googleapis.com/storage/v1/b/{0}/o/{1}/acl",
                QuickDeployConfig.CloudStorageBucketName, QuickDeployConfig.CloudStorageObjectName);
            var requestJsonContents = JsonUtility.ToJson(new PublicAccessRequest());
            var makeBundlePublicWww = SendAuthenticatedPostRequest(makePublicEndpoint,
                Encoding.UTF8.GetBytes(requestJsonContents), "application/json");
            WwwRequestInProgress.TrackProgress(makeBundlePublicWww, "Making remote file public",
                onMakeBundlePublicResponseAction);
        }

        /// <summary>
        /// Sends an HTTP request to GCP to verify whether or not configured bucket name exist. Invokes action
        /// postResponseAction on the WWW instance holding the request when a response to the request is received.
        /// </summary>
        /// <param name="postResponseAction">An action to be invoked on the WWW instance holding the HTTP request
        /// when the response is available.</param>
        private static void CheckBucketExistence(Action<WWW> postResponseAction)
        {
            // see https://cloud.google.com/storage/docs/getting-bucket-information on getting bucket information.
            var bucketInfoUrl =
                string.Format("https://www.googleapis.com/storage/v1/b/{0}",
                    QuickDeployConfig.CloudStorageBucketName);
            var request =
                HttpRequestHelper.SendHttpGetRequest(bucketInfoUrl, null, GetDictionaryWithAuthorizationHeader());
            WwwRequestInProgress.TrackProgress(request, "Checking whether bucket exists.", postResponseAction);
        }

        /// <summary>
        /// Invokes an initial action that requires access token to send an HTTP requests, and schedules invocation of
        /// the post completion action on the WWW instance holding the request for when the
        /// response to the request is received. Updates access token before making this request if necessary.
        /// </summary>
        /// <param name="initialAction">An action that will need an updated access token.</param>
        /// <param name="postResponseAction">An action to execute when the response to the request has been received.</param>
        private static void InvokeAccessRestrictedAction(
            Action<Action<WWW>> initialAction, Action<WWW> postResponseAction)
        {
            if (!AccessTokenGetter.AccessToken.IsValid())
            {
                AccessTokenGetter.ValidateAccessToken(() => initialAction(postResponseAction));
            }
            else
            {
                initialAction(postResponseAction);
            }
        }

        /// <summary>
        /// Helps send HTTP POST request that includes Authorization header to a GCP endpoint.
        /// </summary>
        /// <param name="endpoint">A GCP endpoint to which the request is going.</param>
        /// <param name="content">Content bytes to be put in the body of the request.</param>
        /// <param name="contentType">Type of content to be used in headers.</param>
        private static WWW SendAuthenticatedPostRequest(string endpoint, byte[] content, string contentType)
        {
            var requestHeaders = GetDictionaryWithAuthorizationHeader(
                new Dictionary<string, string> {{"Content-Type", contentType}});
            return HttpRequestHelper.SendHttpPostRequest(endpoint, content, requestHeaders);
        }

        /// <summary>
        /// Creates and returns new combined dictionary that includes Authorization header to use for HTTP requests.
        /// </summary>
        /// <param name="headers">Other headers to include in the new dictonary.</param>
        private static Dictionary<string, string> GetDictionaryWithAuthorizationHeader(
            Dictionary<string, string> headers = null)
        {
            var dictionaryWithAuthorizationHeader = new Dictionary<string, string>
            {
                {"Authorization", string.Format("Bearer {0}", AccessTokenGetter.AccessToken.Value)}
            };
            return dictionaryWithAuthorizationHeader.Union(headers ?? new Dictionary<string, string>())
                .ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        /// <summary>
        /// A representation of the body of the JSON request sent to GCP to create a bucket.
        /// </summary>
        [Serializable]
        private class CreateBucketRequest
        {
            // Uses unconventional naming for public fields to conform to the format of GCP JSON API requests.
            public string name;
        }

        /// <summary>
        /// A representation of the body of the JSON request sent to GCP to set file visibility to public. 
        /// </summary>
        [Serializable]
        private class PublicAccessRequest
        {
            // Uses unconventional naming for public fields to conform to the format of GCP JSON API requests.
            public string entity = "allUsers";
            public string role = "READER";
        }

        /// <summary>
        /// A representation of a JSON response received once the file has been successfully uploaded to GCP.
        /// </summary>
#pragma warning disable CS0649
        [Serializable]
        private class FileUploadResponse
        {
            // Uses unconventional naming for public fields to conform to the format of GCP JSON API requests.
            public string name;
            public string bucket;
        }

        /// <summary>
        /// A representation fo a JSON received when checking bucket info.
        /// <see cref="https://cloud.google.com/storage/docs/getting-bucket-information"/>
        /// </summary>
        [Serializable]
        private class BucketInfoResponse
        {
            public string name;
        }
    }
}