using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ConduitNet.Core;
using ConduitNet.Server;
using ConduitNet.Contracts;
using MessagePack;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ConduitNet.Tests.Unit
{
    public class ConduitMessageProcessorTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Mock<IConduitFilter> _mockFilter;
        private readonly Mock<IUserService> _mockUserService;

        public ConduitMessageProcessorTests()
        {
            var services = new ServiceCollection();
            _mockUserService = new Mock<IUserService>();
            services.AddSingleton<IUserService>(_mockUserService.Object);
            _serviceProvider = services.BuildServiceProvider();
            _mockFilter = new Mock<IConduitFilter>();
        }

        [Fact]
        public async Task ProcessAsync_ShouldInvokeServiceMethod_AndReturnResult()
        {
            // Arrange
            var processor = new ConduitMessageProcessor(_serviceProvider, new List<IConduitFilter>());
            var user = new UserDto { Id = 1, Name = "Test User" };
            _mockUserService.Setup(x => x.GetUserAsync(1)).ReturnsAsync(user);

            var args = new object[] { 1 };
            var payload = MessagePackSerializer.Serialize(args);
            
            var request = new ConduitMessage
            {
                InterfaceName = nameof(IUserService),
                MethodName = nameof(IUserService.GetUserAsync),
                Payload = payload
            };

            // Act
            var response = await processor.ProcessAsync(request);

            // Assert
            Assert.False(response.IsError, response.IsError ? MessagePackSerializer.Deserialize<string>(response.Payload) : "");
            var result = MessagePackSerializer.Deserialize<UserDto>(response.Payload);
            Assert.Equal(user.Id, result.Id);
            Assert.Equal(user.Name, result.Name);
        }

        [Fact]
        public async Task ProcessAsync_ShouldInvokeAsyncMethod_AndReturnResult()
        {
            // Arrange
            var processor = new ConduitMessageProcessor(_serviceProvider, new List<IConduitFilter>());
            var user = new UserDto { Id = 1, Name = "New User" };
            _mockUserService.Setup(x => x.RegisterUserAsync(It.IsAny<UserDto>())).ReturnsAsync(user);

            var args = new object[] { user };
            var payload = MessagePackSerializer.Serialize(args);
            
            var request = new ConduitMessage
            {
                InterfaceName = nameof(IUserService),
                MethodName = nameof(IUserService.RegisterUserAsync),
                Payload = payload
            };

            // Act
            var response = await processor.ProcessAsync(request);

            // Assert
            Assert.False(response.IsError, response.IsError ? MessagePackSerializer.Deserialize<string>(response.Payload) : "");
            var result = MessagePackSerializer.Deserialize<UserDto>(response.Payload);
            Assert.Equal(user.Id, result.Id);
        }

        [Fact]
        public async Task ProcessAsync_ShouldReturnError_WhenMethodNotFound()
        {
            // Arrange
            var processor = new ConduitMessageProcessor(_serviceProvider, new List<IConduitFilter>());
            var request = new ConduitMessage
            {
                InterfaceName = nameof(IUserService),
                MethodName = "UnknownMethod",
                Payload = Array.Empty<byte>()
            };

            // Act
            var response = await processor.ProcessAsync(request);

            // Assert
            Assert.True(response.IsError);
            var error = MessagePackSerializer.Deserialize<string>(response.Payload);
            Assert.Contains("not found", error);
        }

        [Fact]
        public async Task ProcessAsync_ShouldExecuteFilters()
        {
            // Arrange
            _mockFilter.Setup(f => f.InvokeAsync(It.IsAny<ConduitMessage>(), It.IsAny<ConduitDelegate>()))
                .Returns(async (ConduitMessage msg, ConduitDelegate next) => 
                {
                    return await next(msg);
                });

            var processor = new ConduitMessageProcessor(_serviceProvider, new List<IConduitFilter> { _mockFilter.Object });
            var user = new UserDto { Id = 1, Name = "Test" };
            _mockUserService.Setup(x => x.GetUserAsync(1)).ReturnsAsync(user);
            
            var args = new object[] { 1 };
            var request = new ConduitMessage
            {
                InterfaceName = nameof(IUserService),
                MethodName = nameof(IUserService.GetUserAsync),
                Payload = MessagePackSerializer.Serialize(args)
            };

            // Act
            await processor.ProcessAsync(request);

            // Assert
            _mockFilter.Verify(f => f.InvokeAsync(It.IsAny<ConduitMessage>(), It.IsAny<ConduitDelegate>()), Times.Once);
        }
    }
}
