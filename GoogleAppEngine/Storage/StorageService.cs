using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleAppEngine.Storage
{
    public class StorageService : GAEService
    {
        private StorageConfiguration _config;

        public StorageService(CloudAuthenticator authenticator) : base(authenticator)
        {
            if (authenticator == null)
                throw new ArgumentNullException(nameof(authenticator));

            _config = new StorageConfiguration();
        }

        public StorageService(CloudAuthenticator authenticator, StorageConfiguration config) : this(authenticator)
        {
            _config = config;
        }

        public Bucket Bucket(string bucketId)
        {
            return new Bucket(bucketId, GetAuthenticator(), _config);
        }
    }
}
