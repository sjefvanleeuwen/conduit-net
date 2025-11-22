using ConduitNet.Core;
using Xunit;
using System;

namespace ConduitNet.Tests.Unit
{
    public class ConduitMessageTests
    {
        [Fact]
        public void Constructor_ShouldInitializeDefaults()
        {
            // Arrange & Act
            var message = new ConduitMessage();

            // Assert
            Assert.False(string.IsNullOrEmpty(message.Id));
            Assert.True(Guid.TryParse(message.Id, out _));
            Assert.NotNull(message.Headers);
            Assert.Empty(message.Headers);
            Assert.NotNull(message.Payload);
            Assert.Empty(message.Payload);
            Assert.Equal(string.Empty, message.MethodName);
            Assert.Equal(string.Empty, message.InterfaceName);
            Assert.False(message.IsError);
        }

        [Fact]
        public void Properties_ShouldBeSettable()
        {
            // Arrange
            var message = new ConduitMessage();
            var id = Guid.NewGuid().ToString();
            var payload = new byte[] { 1, 2, 3 };

            // Act
            message.Id = id;
            message.MethodName = "Test";
            message.Payload = payload;
            message.IsError = true;
            message.InterfaceName = "ITest";
            message.Headers.Add("key", "value");

            // Assert
            Assert.Equal(id, message.Id);
            Assert.Equal("Test", message.MethodName);
            Assert.Equal(payload, message.Payload);
            Assert.True(message.IsError);
            Assert.Equal("ITest", message.InterfaceName);
            Assert.Single(message.Headers);
            Assert.Equal("value", message.Headers["key"]);
        }
    }
}
