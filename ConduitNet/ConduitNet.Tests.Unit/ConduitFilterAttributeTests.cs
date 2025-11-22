using ConduitNet.Core;
using Xunit;
using System;
using System.Threading.Tasks;

namespace ConduitNet.Tests.Unit
{
    public class ConduitFilterAttributeTests
    {
        private class ValidFilter : IConduitFilter
        {
            public ValueTask<ConduitMessage> InvokeAsync(ConduitMessage message, ConduitDelegate next)
            {
                return new ValueTask<ConduitMessage>(message);
            }
        }

        private class InvalidFilter { }

        [Fact]
        public void Constructor_WithValidType_ShouldSetFilterType()
        {
            // Arrange
            var type = typeof(ValidFilter);

            // Act
            var attribute = new ConduitFilterAttribute(type);

            // Assert
            Assert.Equal(type, attribute.FilterType);
        }

        [Fact]
        public void Constructor_WithInvalidType_ShouldThrowArgumentException()
        {
            // Arrange
            var type = typeof(InvalidFilter);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new ConduitFilterAttribute(type));
            Assert.Contains("must implement IConduitFilter", ex.Message);
        }
    }
}
