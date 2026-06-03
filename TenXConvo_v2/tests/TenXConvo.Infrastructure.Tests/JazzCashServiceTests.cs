using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TenXConvo.Infrastructure.Services;
using Xunit;
using System.Collections.Generic;

namespace TenXConvo.Infrastructure.Tests
{
    public class JazzCashServiceTests
    {
        [Fact]
        public void ConfigAccessors_ReturnExpectedValues()
        {
            // Arrange
            var configValues = new Dictionary<string, string?>
            {
                { "JazzCash:MerchantId", "test_merchant" },
                { "JazzCash:Password", "test_password" },
                { "JazzCash:HashKey", "test_hashkey" },
                { "JazzCash:Sandbox", "false" },
                { "JazzCash:Currency", "USD" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            var loggerMock = new Mock<ILogger<JazzCashService>>();
            var httpFactoryMock = new Mock<IHttpClientFactory>();
            httpFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

            var service = new JazzCashService(configuration, loggerMock.Object, httpFactoryMock.Object);

            // Act & Assert
            Assert.Equal("test_merchant", service.MerchantId);
            Assert.Equal("test_password", service.Password);
            Assert.Equal("test_hashkey", service.HashKey);
            Assert.False(service.Sandbox);
            Assert.Equal("USD", service.Currency);
        }

        [Fact]
        public void ConfigAccessors_ReturnDefaultValues_WhenConfigIsMissing()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().Build(); // Empty config

            var loggerMock = new Mock<ILogger<JazzCashService>>();
            var httpFactoryMock = new Mock<IHttpClientFactory>();
            httpFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

            var service = new JazzCashService(configuration, loggerMock.Object, httpFactoryMock.Object);

            // Act & Assert
            Assert.Equal("", service.MerchantId);
            Assert.Equal("", service.Password);
            Assert.Equal("", service.HashKey);
            Assert.True(service.Sandbox); // Default is true
            Assert.Equal("PKR", service.Currency); // Default is PKR
        }

        [Fact]
        public void ConfigAccessors_ReturnSandboxTrue_WhenSandboxConfigIsInvalid()
        {
            // Arrange
            var configValues = new Dictionary<string, string?>
            {
                { "JazzCash:Sandbox", "invalid_boolean_value" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            var loggerMock = new Mock<ILogger<JazzCashService>>();
            var httpFactoryMock = new Mock<IHttpClientFactory>();
            httpFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

            var service = new JazzCashService(configuration, loggerMock.Object, httpFactoryMock.Object);

            // Act & Assert
            Assert.True(service.Sandbox); // Default is true when TryParse fails
        }
    }
}
