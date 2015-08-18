using System.IO;
using System.Net;
using Amazon.Glacier;
using Amazon.Glacier.Model;
using Utils.Aws.App.Contracts;
using Utils.Aws.App.Helpers;

namespace Utils.Aws.App.Providers
{
    public class FileSystemGlacierProvider : IFileSystemGlacierProvider
    {
        private readonly IAmazonGlacier Service;

        private string VaultName { get; set; }

        public FileSystemGlacierProvider(
            IAmazonGlacier amazonGlacier, 
            string vaultName)
        {
            this.VaultName = vaultName;
            this.Service = amazonGlacier;
        }

        public FileSystemGlacierProvider(
            string accessKey, 
            string secretKey, 
            Amazon.RegionEndpoint endPoint, 
            string vaultName)
        {
            this.VaultName = vaultName;
            this.Service = new AmazonGlacierClient(accessKey, secretKey, endPoint);
        }

        public string UploadFile(
            string description,
            Stream file)
        {
            var treeHashString = HashHelper.SHA256HashString(file);

            var response =
                Service.UploadArchive(new UploadArchiveRequest()
                {
                    VaultName = this.VaultName,
                    ArchiveDescription = description,
                    Body = file,
                    Checksum = treeHashString
                });

            return response.ArchiveId;
        }

        public string RequestDownload(string id)
        {
            var jobRequest = new InitiateJobRequest()
            {
                VaultName = this.VaultName,
                JobParameters = new JobParameters()
                {
                    Type = "archive-retrieval",
                    ArchiveId = id
                }
            };

            var jobResponse = Service.InitiateJob(jobRequest);

            return jobResponse.JobId;
        }

        public StatusCode GetDownloadStatus(string id)
        {
            var job = Service.DescribeJob(
            new DescribeJobRequest()
            {
                VaultName = this.VaultName,
                JobId = id
            }
            );

            return job.StatusCode;
        }

        public byte[] DownloadFile(string jobId)
        {
            var getJobRequest = new GetJobOutputRequest()
            {
                JobId = jobId,
                VaultName = this.VaultName
            };

            var getJobResponse = Service.GetJobOutput(getJobRequest);
            var stream = getJobResponse.Body;

            return GetBytes(stream);
        }

        public HttpStatusCode DeleteFile(string id)
        {
            var response = Service.DeleteArchive(
                new DeleteArchiveRequest()
                {
                    ArchiveId = id,
                    VaultName = this.VaultName
                });

            return response.HttpStatusCode;
        }
        
        #region Auxiliary

        private byte[] GetBytes(Stream stream)
        {
            byte[] buffer = new byte[stream.Length];
            var ms = new MemoryStream();
            int read;

            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                ms.Write(buffer, 0, read);
            }

            return ms.ToArray();
        }

        private MemoryStream GetMemoryStream(Stream stream)
        {
            byte[] buffer = new byte[stream.Length];
            var ms = new MemoryStream();
            int read;

            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                ms.Write(buffer, 0, read);
            }

            return ms;
        }

        #endregion Private Methods
    }
}