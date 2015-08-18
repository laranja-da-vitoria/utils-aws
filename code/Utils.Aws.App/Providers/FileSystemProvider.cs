using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Utils.Aws.App.Contracts;
using Utils.Aws.App.Helpers;

namespace Utils.Aws.App.Providers
{
    public class FileSystemProvider<TEnum> : IFileSystemProvider<TEnum>
    {
        public FileSystemProvider(
            string accessKey,
            string secretKey,
            string bucketName,
            string awsDomain)
        {
            this.AccessKey = accessKey;
            this.SecretKey = secretKey;
            this.BucketName = bucketName;
            this.AwsDomain = awsDomain;
        }

        private const int FIVE_MINUTES = 5 * 60 * 1000;

        /// <summary>
        /// Configura a idade máxima do arquivo no navegador, ou seja, o tempo de expiração
        /// -  1 hora : 3600
        /// -  1 dia  : 86400
        /// - 30 dias : 2592000
        /// </summary>
        private const int MAX_AGE = 2592000;

        private Dictionary<string, string> ContentMapping
        {
            get
            {
                return ContentHelper.MimeTypes;
            }
        }

        private string AccessKey { get; set; }

        private string SecretKey { get; set; }

        private string BucketName { get; set; }

        private string AwsDomain { get; set; }

        private AmazonS3Client NewClient()
        {
            var s3Config = new AmazonS3Config()
            {
                ServiceURL = this.AwsDomain
            };

            var client = new AmazonS3Client(
                this.AccessKey,
                this.SecretKey,
                s3Config);

            return client;
        }

        private TransferUtility NewTransferUtility()
        {
            var transferUtility = new TransferUtility(
                this.AccessKey,
                this.SecretKey,
                RegionEndpoint.SAEast1);

            return transferUtility;
        }

        public Uri GetUri(
            string key)
        {
            var location = string.Format(
                   CultureInfo.InvariantCulture,
                   "{0}{1}/{2}",
                   this.AwsDomain.Trim(),
                   this.BucketName.Trim(),
                   key);

            var uri = new Uri(location);

            return uri;
        }

        public string UploadFile(
            TEnum fileType,
            string fileName,
            byte[] buffer,
            bool isPublic = false)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException("fileName");
            }

            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (buffer.Length == 0)
            {
                throw new ArgumentException("buffer");
            }

            var dateTicks = DateTime.Now.Ticks;
            var key = TransferFile(fileType, fileName, buffer, dateTicks, isPublic);

            return key;
        }

        public string CopyFile(
            TEnum destinationFileType,
            string destinationFileName,
            string sourceKey)
        {
            if (string.IsNullOrWhiteSpace(destinationFileName))
            {
                throw new ArgumentNullException("fileName");
            }

            if (string.IsNullOrWhiteSpace(sourceKey))
            {
                throw new ArgumentNullException("fileKey");
            }

            var key = string.Format(
                CultureInfo.InvariantCulture,
                "{0}/{1}/{2}",
                destinationFileType,
                DateTime.Now.Ticks,
                destinationFileName);

            var request = new CopyObjectRequest();

            request.SourceBucket = this.BucketName;
            request.SourceKey = sourceKey;

            request.DestinationBucket = this.BucketName;
            request.DestinationKey = key;

            var client = this.NewClient();

            client.CopyObject(request);

            return key;
        }

        public void DeleteFile(
            string key)
        {
            var request = new DeleteObjectRequest
            {
                BucketName = this.BucketName,
                Key = key
            };

            using (var client = this.NewClient())
            {
                client.DeleteObject(request);
            }
        }

        public IDictionary<string, string> GetFiles(
            string searchKey)
        {
            var request = new ListObjectsRequest
            {
                BucketName = this.BucketName,
                Prefix = searchKey,
                MaxKeys = int.MaxValue
            };

            using (var client = this.NewClient())
            {
                var objects = client.ListObjects(request);

                return objects.S3Objects.ToDictionary(
                    obj => obj.Key,
                    obj => obj.ETag);
            }
        }

        public void DownloadFile(
            string key,
            string path)
        {
            var request = new GetObjectRequest
            {
                BucketName = this.BucketName,
                Key = key
            };

            using (var client = this.NewClient())
            {
                var obj = client.GetObject(request);
                obj.WriteResponseStreamToFile(path);
            }
        }
        public byte[] DownloadFile(
            string key)
        {
            var request = new GetObjectRequest
            {
                BucketName = this.BucketName,
                Key = key
            };

            var buffer = null as byte[];
            using (var client = this.NewClient())
            {
                var obj = client.GetObject(request);
                using (var response = obj.ResponseStream)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        response.CopyTo(memoryStream);
                        buffer = memoryStream.ToArray();
                    }
                }
            }

            return buffer;
        }


        private string TransferFile(
            TEnum fileType,
            string fileName,
            byte[] buffer,
            long dateTicks,
            bool isPublic)
        {
            var key = string.Format(
                CultureInfo.InvariantCulture,
                "{0}/{1}/{2}",
                fileType,
                dateTicks,
                fileName);

            using (var stream = new MemoryStream(buffer))
            {
                var headers = new HeadersCollection();
                headers.ContentType = this.GetContentType(fileName);
                headers.CacheControl = string.Format("max-age={0}, must-revalidate", MAX_AGE);

                var request = new TransferUtilityUploadRequest();
                request.Key = key;
                request.BucketName = this.BucketName;
                request.InputStream = stream;

                if (isPublic)
                {
                    request.CannedACL = S3CannedACL.PublicRead;
                }

                var transferUtility = NewTransferUtility();

                transferUtility.Upload(request);
            }
            return key;
        }

        public string GetContentType(string fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            var extension = Path.GetExtension(fileName);
            if (!extension.StartsWith(".")) { extension = "." + extension; }

            var mime = string.Empty;
            return this.ContentMapping.TryGetValue(extension, out mime) ? mime : "application/octet-stream";
        }
    }
}