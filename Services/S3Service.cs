using System;
using System.IO;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.S3.Util;
using Amazon.S3.Model;
using Amazon.Runtime;
using System.Threading.Tasks;
using S3Uploader.Interface;
using Serilog;

namespace S3Uploader.Services
{
    class S3Service : IS3Service
    {
        public static IConfigurationRoot _configuration;

        public S3Service(IConfigurationRoot configuration)
        {
            _configuration = configuration;
        }

        private AmazonS3Client GetS3Client()
        {

            try {
                string endpoint = _configuration.GetConnectionString("Endpoint");
                string key = _configuration.GetConnectionString("Key");
                string secret = _configuration.GetConnectionString("Secret");
                bool useHttp = (_configuration.GetConnectionString("UseHttp").ToLower() == "yes")? true: false;

                var config = new AmazonS3Config {
                    ServiceURL = endpoint,
                    ForcePathStyle = true,
                    UseHttp = useHttp
                };

                return new AmazonS3Client(key, secret, config);
            }
            catch(HttpRequestException ex)
            {
                Log.Information($"Please check the endpoint and credential in the configuration.");
                return null;
            }

        }

        public async Task ListS3Folders()
        {
            using (var client = GetS3Client())
            {
                if (client is null) return;

                ListBucketsResponse response = await client.ListBucketsAsync();

                //View resposne data
                Log.Information($"Buckets owner - {response.Owner.DisplayName}");
                foreach (S3Bucket bucket in response.Buckets)
                {
                    Log.Information($"Bucket {bucket.BucketName}, Created on {bucket.CreationDate}");
                }
            }
        }

        public async Task UploadFileToS3()
        {
            string filePath = _configuration["SourceFilePath"];
            string[] fileNames = _configuration.GetSection("SourceFiles").Get<string[]>();
            string bucket = _configuration["DestinationBucket"];
            Log.Information($"{fileNames.Length} files will be uploaded");

            if (String.IsNullOrEmpty(bucket))
            {
                Log.Information($"Bucket Name is Empty");
                return;
            }


            using (var client = GetS3Client())
            {
                if (client is null) return;

                if (await AmazonS3Util.DoesS3BucketExistV2Async(client, bucket) == false)
                {
                    Log.Information($"The Bucket is not exist. Create Bucket {bucket}");
                    await client.PutBucketAsync(new PutBucketRequest { BucketName = bucket });
                }

                foreach( string fileName in fileNames)
                {
                    string fullPath = Path.Combine(filePath, fileName);
                    Log.Information($"Start Upload from: {fullPath} to bucket {bucket}");

                    if (File.Exists(fullPath))
                    {
                        FileStream fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                        string newFileName = $"{Path.GetFileNameWithoutExtension(fullPath)}_{DateTime.Now.ToString("yyyyMMdd")}{Path.GetExtension(fullPath)}";
                        double fileSize = fs.Length / 1024;

                        using (var newMemoryStream = new MemoryStream())
                        {
                            fs.CopyTo(newMemoryStream);
                            var uploadRequest = new TransferUtilityUploadRequest
                            {
                                InputStream = newMemoryStream,
                                Key = newFileName,
                                BucketName = bucket
                            };

                            try {
                                var fileTransferUtility = new TransferUtility(client);
                                fileTransferUtility.Upload(uploadRequest);
                                Log.Information($"File is uploaded successfully - Size: {fileSize.ToString()}kb");
                            }
                            catch (AmazonS3Exception ex)
                            {
                                Log.Information($"Error when connecting to AWS: {ex.Message}");
                            }
                            catch (AmazonServiceException ex)
                            {
                                Log.Information($"Network Error when connecting to AWS: {ex.Message}");
                            }
                            catch (Exception ex)
                            {
                                Log.Information($"General: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        Log.Information("File is not found");
                    }
                }
            }
        }
    }
}