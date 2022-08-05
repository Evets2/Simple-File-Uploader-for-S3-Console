using System;
using System.Threading.Tasks;

namespace S3Uploader.Interface
{
    public interface IS3Service
    {
        public Task ListS3Folders();

        public Task UploadFileToS3();
    }
}