using System;
using System.Security.Cryptography.X509Certificates;
using Google.Apis.Datastore.v1beta2;
using Google.Apis.Services;
using Google.Apis.Storage.v1;
using GoogleAppEngine.Shared;

namespace GoogleAppEngine
{
    public class ServiceAccountAuthenticator : CloudAuthenticator
    {
        private readonly BaseClientService.Initializer _initializer;
        private readonly string _projectId;

        public ServiceAccountAuthenticator(string projectId, string serviceAccountEmail, string certificatePath, string secretKey)
        {
            if (string.IsNullOrWhiteSpace(serviceAccountEmail))
                throw new ArgumentException(nameof(serviceAccountEmail));

            if (string.IsNullOrWhiteSpace(certificatePath))
                throw new ArgumentException(nameof(certificatePath));

            if (string.IsNullOrWhiteSpace(secretKey))
                throw new ArgumentException(nameof(secretKey));

            if (string.IsNullOrWhiteSpace(projectId))
                throw new ArgumentException(nameof(projectId));

            // Service account email must be an email address. A lot of people use the client Id instead of email by accident,
            // so this simple check should save them some time.
            if (!serviceAccountEmail.Contains("@"))
                throw new InvalidOperationException("The `serviceAccountEmail` parameter must be an email address. (Did you use a client Id by accident?)");

            _projectId = projectId;

            var fullpath = UriExtensions.GetAbsoluteUri(certificatePath);
            var certificate = new X509Certificate2(fullpath, secretKey, X509KeyStorageFlags.Exportable);

            var credential = new Google.Apis.Auth.OAuth2.ServiceAccountCredential(
               new Google.Apis.Auth.OAuth2.ServiceAccountCredential.Initializer(serviceAccountEmail)
               {
                   Scopes = new[]
                   {
                           DatastoreService.Scope.Datastore,
                           DatastoreService.Scope.UserinfoEmail,
                           StorageService.Scope.DevstorageReadWrite
                   }
               }.FromCertificate(certificate));

            _initializer = new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = projectId
            };
        }

        public override BaseClientService.Initializer GetInitializer()
        {
            return _initializer;
        }

        public override string GetProjectId()
        {
            return _projectId;
        }
    }
}
