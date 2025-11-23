import { ConduitClient } from 'conduit-ts-client';
import { IConduitDirectory, IUserService, IAclService, ITelemetryCollector, NodeInfo, mapNodeInfo } from '../contracts';

class ConduitServiceManager {
    private directoryClient: ConduitClient | null = null;
    private directoryProxy: IConduitDirectory | null = null;
    
    private serviceClients = new Map<string, ConduitClient>();
    private serviceProxies = new Map<string, any>();

    private readonly directoryUrl = 'ws://localhost:5000/conduit';

    async getDirectory(): Promise<IConduitDirectory> {
        if (!this.directoryProxy) {
            this.directoryClient = new ConduitClient(this.directoryUrl);
            await this.directoryClient.connect();
            this.directoryProxy = this.directoryClient.createProxy<IConduitDirectory>('IConduitDirectory');
        }
        return this.directoryProxy!;
    }

    async discover(interfaceName: string): Promise<NodeInfo[]> {
        const directory = await this.getDirectory();
        const rawNodes = await directory.DiscoverAsync(interfaceName) as any[];
        console.log(`Discovered ${interfaceName}:`, rawNodes);
        return rawNodes.map(mapNodeInfo);
    }

    async getUserService(): Promise<IUserService> {
        return this.getService<IUserService>('IUserService');
    }

    async getAclService(): Promise<IAclService> {
        return this.getService<IAclService>('IAclService');
    }

    async getTelemetryService(): Promise<ITelemetryCollector> {
        return this.getService<ITelemetryCollector>('ITelemetryCollector');
    }

    private async getService<T extends object>(interfaceName: string): Promise<T> {
        if (this.serviceProxies.has(interfaceName)) {
            return this.serviceProxies.get(interfaceName);
        }

        const nodes = await this.discover(interfaceName);
        
        if (nodes.length === 0) {
            throw new Error(`No service found for ${interfaceName}`);
        }

        // Pick the first one (simple load balancing could be added here)
        const node = nodes[0];
        const wsUrl = this.convertToWebSocketUrl(node.Address);
        
        console.log(`Connecting to ${interfaceName} at ${wsUrl} (NodeId: ${node.Id})`);

        let client = this.serviceClients.get(wsUrl);
        if (!client) {
            client = new ConduitClient(wsUrl);
            await client.connect();
            this.serviceClients.set(wsUrl, client);
        }

        const proxy = client.createProxy<T>(interfaceName);
        this.serviceProxies.set(interfaceName, proxy);
        return proxy;
    }

    private convertToWebSocketUrl(httpUrl: string): string {
        // Handle multiple urls (e.g. "http://localhost:5000;https://localhost:5001")
        const url = httpUrl.split(';')[0];
        let wsUrl = url.replace(/^http:/, 'ws:').replace(/^https:/, 'wss:');
        
        // Ensure it ends with /conduit
        if (!wsUrl.endsWith('/conduit')) {
            if (wsUrl.endsWith('/')) {
                wsUrl += 'conduit';
            } else {
                wsUrl += '/conduit';
            }
        }
        return wsUrl;
    }
}

export const ConduitService = new ConduitServiceManager();
