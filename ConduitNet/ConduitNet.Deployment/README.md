# ConduitNet Deployment Stack

This project provides a unified way to deploy ConduitNet clusters.

## Drivers

### LocalhostDriver
Deploys nodes as local processes on the developer's machine.
- Uses `System.Diagnostics.Process`
- Maps ports automatically
- Pipes logs to the console

## Usage

```csharp
var manifest = new DeploymentManifest();
manifest.Nodes.Add(new ServiceNode { ... });

IDeploymentDriver driver = new LocalhostDriver();
await driver.DeployAsync(manifest);
```

## Future Drivers
- **DockerDriver**: Deploy to local Docker containers.
- **KubernetesDriver**: Deploy to a K8s cluster.
- **AzureDriver**: Deploy to Azure Container Apps or App Service.
