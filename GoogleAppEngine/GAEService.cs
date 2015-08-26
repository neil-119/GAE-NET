using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleAppEngine
{
    public class GAEService
    {
        protected readonly CloudAuthenticator Authenticator;

        protected GAEService(CloudAuthenticator authenticator)
        {
            Authenticator = authenticator;
        }

        public CloudAuthenticator GetAuthenticator()
        {
            return Authenticator;
        }
    }
}
