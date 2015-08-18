using System.IO;
using System.Net;
using Amazon.Glacier;

namespace Utils.Aws.App.Contracts
{
    public interface IFileSystemGlacierProvider
    {
        string UploadFile(string description, Stream file);

        string RequestDownload(string id);

        StatusCode GetDownloadStatus(string id);

        byte[] DownloadFile(string jobId);

        HttpStatusCode DeleteFile(string id);
    }
}
