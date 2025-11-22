using System.Collections.Generic;
using System.Threading.Tasks;
using ConduitNet.Client;
using ConduitNet.Core;
using Moq;
using Xunit;

namespace ConduitNet.Tests.Unit
{
    public class ConduitPipelineExecutorTests
    {
        [Fact]
        public async Task ExecuteAsync_ShouldCallTransport_WhenNoFiltersArePresent()
        {
            // Arrange
            var mockTransport = new Mock<ConduitTransport>();
            var expectedResponse = new ConduitMessage { Payload = new byte[] { 1 } };
            
            mockTransport.Setup(t => t.SendAsync(It.IsAny<ConduitMessage>()))
                .ReturnsAsync(expectedResponse);

            var executor = new ConduitPipelineExecutor(mockTransport.Object, new List<IConduitFilter>());
            var message = new ConduitMessage();

            // Act
            var result = await executor.ExecuteAsync(message);

            // Assert
            Assert.Same(expectedResponse, result);
            mockTransport.Verify(t => t.SendAsync(message), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldExecuteFiltersInOrder()
        {
            // Arrange
            var mockTransport = new Mock<ConduitTransport>();
            mockTransport.Setup(t => t.SendAsync(It.IsAny<ConduitMessage>()))
                .ReturnsAsync(new ConduitMessage());

            var executionOrder = new List<string>();

            var filter1 = new Mock<IConduitFilter>();
            filter1.Setup(f => f.InvokeAsync(It.IsAny<ConduitMessage>(), It.IsAny<ConduitDelegate>()))
                .Callback(() => executionOrder.Add("Filter1-Start"))
                .Returns(async (ConduitMessage msg, ConduitDelegate next) => 
                {
                    var res = await next(msg);
                    executionOrder.Add("Filter1-End");
                    return res;
                });

            var filter2 = new Mock<IConduitFilter>();
            filter2.Setup(f => f.InvokeAsync(It.IsAny<ConduitMessage>(), It.IsAny<ConduitDelegate>()))
                .Callback(() => executionOrder.Add("Filter2-Start"))
                .Returns(async (ConduitMessage msg, ConduitDelegate next) => 
                {
                    var res = await next(msg);
                    executionOrder.Add("Filter2-End");
                    return res;
                });

            var executor = new ConduitPipelineExecutor(mockTransport.Object, new List<IConduitFilter> { filter1.Object, filter2.Object });

            // Act
            await executor.ExecuteAsync(new ConduitMessage());

            // Assert
            Assert.Equal(new List<string> 
            { 
                "Filter1-Start", 
                "Filter2-Start", 
                "Filter2-End", 
                "Filter1-End" 
            }, executionOrder);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldAllowFilterToShortCircuit()
        {
            // Arrange
            var mockTransport = new Mock<ConduitTransport>();
            var shortCircuitResponse = new ConduitMessage { Payload = new byte[] { 99 } };

            var filter1 = new Mock<IConduitFilter>();
            filter1.Setup(f => f.InvokeAsync(It.IsAny<ConduitMessage>(), It.IsAny<ConduitDelegate>()))
                .ReturnsAsync(shortCircuitResponse); // Returns directly, doesn't call next

            var executor = new ConduitPipelineExecutor(mockTransport.Object, new List<IConduitFilter> { filter1.Object });

            // Act
            var result = await executor.ExecuteAsync(new ConduitMessage());

            // Assert
            Assert.Same(shortCircuitResponse, result);
            mockTransport.Verify(t => t.SendAsync(It.IsAny<ConduitMessage>()), Times.Never);
        }
    }
}
