using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Google.Apis.Services;
using GoogleAppEngine.Datastore;
using LightMock;
using Xunit;

namespace GoogleAppEngine.Tests
{
    public class CloudAuthenticatorMock : CloudAuthenticator
    {
        private readonly IInvocationContext<CloudAuthenticator> context;

        public CloudAuthenticatorMock(IInvocationContext<CloudAuthenticator> context)
        {
            this.context = context;
        }

        public override BaseClientService.Initializer GetInitializer()
        {
            return context.Invoke(f => f.GetInitializer());
        }

        public override string GetProjectId()
        {
            return context.Invoke(f => f.GetProjectId());
        }
    }

    public class AuthTest
    {
        [Fact]
        public void Cloud_auth_object_returns_project_id()
        {
            var mockContext = new MockContext<CloudAuthenticator>();
            mockContext.Arrange(f => f.GetProjectId()).Returns("ProjectId");

            var cloudAuthMock = new CloudAuthenticatorMock(mockContext);
            var result = cloudAuthMock.GetProjectId();

            mockContext.Assert(f => f.GetProjectId(), Invoked.Once);
            Assert.Equal("ProjectId", result);
        }

        [Fact]
        public void Cloud_auth_object_returns_initializer()
        {
            var initializer = new BaseClientService.Initializer();
            var mockContext = new MockContext<CloudAuthenticator>();
            mockContext.Arrange(f => f.GetInitializer()).Returns(initializer);

            var cloudAuthMock = new CloudAuthenticatorMock(mockContext);
            var result = cloudAuthMock.GetInitializer();

            mockContext.Assert(f => f.GetInitializer(), Invoked.Once);
            Assert.Equal(initializer, result);
        }

        [Fact]
        public void Custom_initializer_returns_project_id_and_initializer()
        {
            var initializer = new BaseClientService.Initializer();
            var customInitializer = new CustomInitializer("ProjectId", initializer);

            Assert.Equal("ProjectId", customInitializer.GetProjectId());
            Assert.Equal(initializer, customInitializer.GetInitializer());
        }

        [Fact]
        public void Service_account_initializer_should_throw_if_invalid_arguments()
        {
            // Throw if null or empty
            Assert.Throws<ArgumentException>(() => new ServiceAccountAuthenticator("", "", "", ""));
            Assert.Throws<ArgumentException>(() => new ServiceAccountAuthenticator(null, null, null, null));

            // Throw if invalid email
            Assert.Throws<InvalidOperationException>(() => new ServiceAccountAuthenticator("ProjectId", "test.com", "./test.p12", "notasecret"));

            // Certificate does not actually exist, so should throw
            Assert.Throws<CryptographicException>(() => new ServiceAccountAuthenticator("ProjectId", "test@test.com", "./test.p12", "notasecret"));
        }
    }
}
