using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Services;

namespace GoogleAppEngine
{
    public class CustomInitializer : CloudAuthenticator
    {
        private readonly BaseClientService.Initializer _initializer;
        private readonly string _projectId;

        public CustomInitializer(string projectId, BaseClientService.Initializer initializer)
        {
            _initializer = initializer;
            _projectId = projectId;
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
