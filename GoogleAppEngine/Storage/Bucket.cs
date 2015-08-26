using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Storage.v1.Data;
using GoogleAppEngine.Shared;

namespace GoogleAppEngine.Storage
{
    public class Bucket
    {
        private readonly string _bucketId;
        private readonly CloudAuthenticator _authenticator;
        private Google.Apis.Storage.v1.StorageService _googleStorageService;
        private readonly StorageConfiguration _config;

        public Bucket(string bucketId, CloudAuthenticator authenticator, StorageConfiguration configuration)
        {
            this._bucketId = bucketId;
            this._authenticator = authenticator;
            this._config = configuration;

            _authenticator.GetInitializer().GZipEnabled = _config.EnableGzip;
            _googleStorageService = new Google.Apis.Storage.v1.StorageService(_authenticator.GetInitializer());
        }

        private Google.Apis.Storage.v1.StorageService GetGooogleStorageService()
        {
            return _googleStorageService;
        }

        /// <summary>
        /// Returns a list of filenames in the bucket.
        /// </summary>
        /// <returns>List of filenames</returns>
        public List<string> GetFileNames()
        {
            var storageService = GetGooogleStorageService();
            var list = storageService.Objects.List(_bucketId).Execute();

            return list.Items.Select(x => x.Name).ToList();
        }

        /// <summary>
        /// Downloads a file from the bucket to the specified stream.
        /// </summary>
        /// <param name="fileName">The filename in the bucket</param>
        /// <param name="outputStream">The stream to write to</param>
        public void DownloadStream(string fileName, Stream outputStream)
        {
            var storageService = GetGooogleStorageService();
            storageService.Objects.Get(_bucketId, fileName).Download(outputStream);
        }

        /// <summary>
        /// Downloads a file from the bucket as a byte array.
        /// </summary>
        /// <param name="fileName">The filename in the bucket</param>
        /// <returns>Byte array of file</returns>
        public byte[] DownloadData(string fileName)
        {
            using (var memoryStream = new MemoryStream())
            {
                DownloadStream(fileName, memoryStream);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Downloads a plain-text UTF8 encoded representation of a file in the bucket.
        /// </summary>
        /// <param name="fileName">The filename in the bucket</param>
        /// <returns>Plain-text of file in storage</returns>
        public string DownloadText(string fileName)
        {
            return Encoding.UTF8.GetString(DownloadData(fileName));
        }

        /// <summary>
        /// Downloads a file from the bucket and saves it to the specified local path.
        /// The directory will be created if it does not exist.
        /// E.g. DownloadFile("somefile.txt", "./pathToFile/somefile.txt");
        /// </summary>
        /// <param name="fileNameInBucket">The filename in the bucket</param>
        /// <param name="pathToLocalFileName">Path to save the file at (including the new filename)</param>
        public void DownloadFile(string fileNameInBucket, string pathToLocalFileName)
        {
            if (string.IsNullOrWhiteSpace(pathToLocalFileName))
                throw new ArgumentNullException(nameof(pathToLocalFileName));

            pathToLocalFileName = UriExtensions.GetAbsoluteUri(pathToLocalFileName);
            var directory = Path.GetDirectoryName(pathToLocalFileName);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            using (var fileStream = new FileStream(pathToLocalFileName, FileMode.Create, FileAccess.ReadWrite))
            {
                DownloadStream(fileNameInBucket, fileStream);
            }
        }

        /// <summary>
        /// Downloads the specified file from the bucket to the current directory.
        /// </summary>
        /// <param name="fileNameInBucket">Name of file in the storage</param>
        /// <returns>The saved local file location</returns>
        public string DownloadFile(string fileNameInBucket)
        {
            var pathToFile = Path.Combine(Directory.GetCurrentDirectory(), fileNameInBucket);
            DownloadFile(fileNameInBucket, pathToFile);
            return pathToFile;
        }

        /// <summary>
        /// Uploads a data stream to the bucket
        /// </summary>
        /// <param name="fileName">Filename to save as in bucket</param>
        /// <param name="stream">The stream to read from</param>
        /// <param name="mimeType">The mimetype of the stream</param>
        /// <param name="permissionLevel">The permission level to set</param>
        public Bucket UploadStream(string fileName, Stream stream, string mimeType, Permissions permissionLevel = Permissions.OwnerOnly)
        {
            var storageService = GetGooogleStorageService();
            var googleObject = BuildGoogleObject(fileName, permissionLevel);
            storageService.Objects.Insert(googleObject, _bucketId, stream, mimeType).Upload();
            return this;
        }

        /// <summary>
        /// Uploads data to the bucket
        /// </summary>
        /// <param name="fileName">Filename to save as in bucket</param>
        /// <param name="data">The byte array to read from</param>
        /// <param name="mimeType">The mimetype of the stream</param>
        /// <param name="permissionLevel">The permission level to set</param>
        public Bucket UploadData(string fileName, byte[] data, string mimeType, Permissions permissionLevel = Permissions.OwnerOnly)
        {
            using (var streamOut = new MemoryStream(data))
            {
                return UploadStream(fileName, streamOut, mimeType, permissionLevel);
            }
        }

        /// <summary>
        /// Uploads data to the bucket (the mimetype is inferred).
        /// </summary>
        /// <param name="fileName">Filename to save as in bucket</param>
        /// <param name="data">The byte array to read from</param>
        /// <param name="permissionLevel">The permission level to set</param>
        public Bucket UploadData(string fileName, byte[] data, Permissions permissionLevel = Permissions.OwnerOnly)
        {
            return UploadData(fileName, data, MimeTypeMap.GetMimeType(Path.GetExtension(fileName)), permissionLevel);
        }

        /// <summary>
        /// Uploads plain-text with UTF8 encoding to the bucket (the mimetype is inferred)
        /// </summary>
        /// <param name="fileName">Filename to save as in bucket</param>
        /// <param name="text">The text to write</param>
        /// <param name="permissionLevel">The permission level to set</param>
        public Bucket UploadText(string fileName, string text, Permissions permissionLevel = Permissions.OwnerOnly)
        {
            var data = System.Text.Encoding.UTF8.GetBytes(text);
            return UploadData(fileName, data, permissionLevel);
        }

        /// <summary>
        /// Uploads a file to the bucket. Mimetype is inferred.
        /// </summary>
        /// <param name="fileNameInBucket">The filename to save as in the bucket</param>
        /// <param name="pathToFile">Path to the file (relative or absolute)</param>
        /// <param name="permissionLevel">Permission level to set</param>
        public Bucket UploadFile(string fileNameInBucket, string pathToFile, Permissions permissionLevel = Permissions.OwnerOnly)
        {
            return UploadFile(fileNameInBucket, pathToFile, MimeTypeMap.GetMimeType(Path.GetExtension(fileNameInBucket)), permissionLevel);
        }

        /// <summary>
        /// Uploads a file to the bucket. Filename in the bucket is set to be the
        /// same as the filename being uploaded. Mimetype is inferred.
        /// </summary>
        /// <param name="pathToFile">Path to the file (relative or absolute)</param>
        /// <param name="permissionLevel">Permission level to set</param>
        public Bucket UploadFile(string pathToFile, Permissions permissionLevel = Permissions.OwnerOnly)
        {
            return UploadFile(Path.GetFileName(pathToFile), pathToFile, permissionLevel);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileNameInBucket">The filename to save as in the bucket</param>
        /// <param name="pathToFile">Path to the file (relative or absolute)</param>
        /// <param name="mimeType">The mimetype to set</param>
        /// <param name="permissionLevel">Permission level to set</param>
        /// <returns></returns>
        public Bucket UploadFile(string fileNameInBucket, string pathToFile, string mimeType, Permissions permissionLevel = Permissions.OwnerOnly)
        {
            pathToFile = UriExtensions.GetAbsoluteUri(pathToFile);

            using (var fileStream = File.OpenRead(pathToFile))
            {
                return UploadStream(fileNameInBucket, fileStream, mimeType, permissionLevel);
            }
        }

        private Google.Apis.Storage.v1.Data.Object BuildGoogleObject(string fileName, Permissions permissionLevel)
        {
            var fileobj = new Google.Apis.Storage.v1.Data.Object()
            {
                Name = fileName
            };

            if (permissionLevel == Permissions.PublicallyViewable)
                fileobj.Acl = new List<ObjectAccessControl>
                {
                    new ObjectAccessControl {Role = "READER", Entity = "allUsers"}
                };

            return fileobj;
        }
    }
}
