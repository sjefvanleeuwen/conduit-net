using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConduitNet.Contracts;
using ConduitNet.Node;
using ConduitNet.Directory;
using Xunit;

namespace ConduitNet.Tests.Unit
{
    public class ConduitDirectoryServiceTests
    {
        [Fact]
        public async Task RegisterAsync_ShouldAddNodeToRegistry()
        {
            // Arrange
            var service = new ConduitDirectoryService();
            var node = new NodeInfo 
            { 
                Id = "node1", 
                Address = "localhost:8080", 
                Services = new List<string> { "TestService" } 
            };

            // Act
            await service.RegisterAsync(node);
            var nodes = await service.DiscoverAsync("TestService");

            // Assert
            Assert.Single(nodes);
            Assert.Equal("node1", nodes[0].Id);
        }

        [Fact]
        public async Task RegisterAsync_ShouldUpdateExistingNode()
        {
            // Arrange
            var service = new ConduitDirectoryService();
            var node1 = new NodeInfo 
            { 
                Id = "node1", 
                Address = "localhost:8080", 
                Services = new List<string> { "TestService" } 
            };
            
            await service.RegisterAsync(node1);

            var nodeUpdate = new NodeInfo 
            { 
                Id = "node1", 
                Address = "localhost:9090", // Changed address
                Services = new List<string> { "TestService" } 
            };

            // Act
            await service.RegisterAsync(nodeUpdate);
            var nodes = await service.DiscoverAsync("TestService");

            // Assert
            Assert.Single(nodes);
            Assert.Equal("localhost:9090", nodes[0].Address);
        }

        [Fact]
        public async Task DiscoverAsync_ShouldReturnEmptyList_WhenServiceNotFound()
        {
            // Arrange
            var service = new ConduitDirectoryService();

            // Act
            var nodes = await service.DiscoverAsync("UnknownService");

            // Assert
            Assert.Empty(nodes);
        }

        [Fact]
        public async Task RegisterAsync_ShouldHandleMultipleServicesForOneNode()
        {
            // Arrange
            var service = new ConduitDirectoryService();
            var node = new NodeInfo 
            { 
                Id = "node1", 
                Address = "localhost:8080", 
                Services = new List<string> { "ServiceA", "ServiceB" } 
            };

            // Act
            await service.RegisterAsync(node);
            var nodesA = await service.DiscoverAsync("ServiceA");
            var nodesB = await service.DiscoverAsync("ServiceB");

            // Assert
            Assert.Single(nodesA);
            Assert.Single(nodesB);
            Assert.Equal("node1", nodesA[0].Id);
            Assert.Equal("node1", nodesB[0].Id);
        }
    }
}
