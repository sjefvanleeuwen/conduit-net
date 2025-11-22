using System;
using System.Reflection;
using System.Threading.Tasks;
using ConduitNet.Client;
using ConduitNet.Core;
using MessagePack;
using Xunit;

namespace ConduitNet.Tests.Unit
{
    public interface ITestService
    {
        Task<string> GetDataAsync(int id);
        Task DoSomethingAsync();
        Task<ConduitMessage> ReturnMessageAsync(); // Edge case
    }

    public class ConduitProxyTests
    {
        [Fact]
        public async Task Invoke_ShouldSerializeArgumentsAndCallSendFunc()
        {
            // Arrange
            ConduitMessage? capturedMessage = null;
            var expectedResult = "Success";
            
            var proxy = DispatchProxy.Create<ITestService, ConduitProxy<ITestService>>();
            ((ConduitProxy<ITestService>)(object)proxy).Initialize(async msg => 
            {
                capturedMessage = msg;
                var payload = MessagePackSerializer.Serialize(expectedResult);
                return await Task.FromResult(new ConduitMessage { Payload = payload });
            });

            // Act
            var result = await proxy.GetDataAsync(123);

            // Assert
            Assert.Equal(expectedResult, result);
            Assert.NotNull(capturedMessage);
            Assert.Equal(nameof(ITestService), capturedMessage.InterfaceName);
            Assert.Equal(nameof(ITestService.GetDataAsync), capturedMessage.MethodName);
            
            var args = MessagePackSerializer.Deserialize<object[]>(capturedMessage.Payload);
            Assert.Single(args);
            Assert.Equal(123, args[0]);
        }

        [Fact]
        public async Task Invoke_ShouldHandleVoidTask()
        {
            // Arrange
            bool called = false;
            var proxy = DispatchProxy.Create<ITestService, ConduitProxy<ITestService>>();
            ((ConduitProxy<ITestService>)(object)proxy).Initialize(async msg => 
            {
                called = true;
                return await Task.FromResult(new ConduitMessage());
            });

            // Act
            await proxy.DoSomethingAsync();

            // Assert
            Assert.True(called);
        }

        [Fact]
        public async Task Invoke_ShouldThrowException_WhenResponseIsError()
        {
            // Arrange
            var errorMessage = "Remote Error";
            var proxy = DispatchProxy.Create<ITestService, ConduitProxy<ITestService>>();
            ((ConduitProxy<ITestService>)(object)proxy).Initialize(async msg => 
            {
                return await Task.FromResult(new ConduitMessage 
                { 
                    IsError = true, 
                    Payload = MessagePackSerializer.Serialize(errorMessage) 
                });
            });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => proxy.GetDataAsync(1));
            Assert.Equal(errorMessage, ex.Message);
        }
    }
}
