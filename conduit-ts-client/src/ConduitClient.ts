import { encode, decode } from '@msgpack/msgpack';

// The Envelope Structure
export interface ConduitMessage {
    Id: string;
    MethodName: string;
    Payload: Uint8Array; // Serialized arguments or result
    IsError: boolean;
    Headers: Record<string, string>;
    InterfaceName: string;
}

export class ConduitClient {
    private ws: WebSocket | null = null;
    private pendingCalls = new Map<string, { resolve: (value: any) => void, reject: (reason: any) => void }>();
    private isConnected = false;

    constructor(private url: string) {}

    public async connect(): Promise<void> {
        return new Promise((resolve, reject) => {
            this.ws = new WebSocket(this.url);
            this.ws.binaryType = 'arraybuffer';

            this.ws.onopen = () => {
                this.isConnected = true;
                resolve();
            };

            this.ws.onerror = (err) => {
                reject(err);
            };

            this.ws.onclose = () => {
                this.isConnected = false;
            };

            this.ws.onmessage = async (event) => {
                this.handleMessage(event.data as ArrayBuffer);
            };
        });
    }

    private handleMessage(data: ArrayBuffer) {
        // The server sends [Length (4 bytes)][MessagePack Payload]
        
        const buffer = new Uint8Array(data);
        
        // We expect at least 4 bytes for length
        if (buffer.length < 4) return;

        // Read Length (Little Endian)
        const view = new DataView(buffer.buffer, buffer.byteOffset, buffer.byteLength);
        const length = view.getInt32(0, true);

        // Validate length
        if (buffer.length < 4 + length) {
            console.error("Received incomplete message");
            return;
        }

        // Extract Payload
        const payload = buffer.subarray(4, 4 + length);
        
        try {
            const message = decode(payload) as ConduitMessage;
            
            if (this.pendingCalls.has(message.Id)) {
                const { resolve, reject } = this.pendingCalls.get(message.Id)!;
                this.pendingCalls.delete(message.Id);

                if (message.IsError) {
                    // If payload is error, it might be a string or serialized exception
                    const errorData = decode(message.Payload);
                    reject(errorData);
                } else {
                    // Deserialize the result
                    const result = message.Payload.length > 0 ? decode(message.Payload) : null;
                    resolve(result);
                }
            }
        } catch (err) {
            console.error("Failed to decode message", err);
        }
    }

    public async invoke<T>(interfaceName: string, methodName: string, args: any[]): Promise<T> {
        if (!this.isConnected || !this.ws) {
            throw new Error("Not connected");
        }

        const id = crypto.randomUUID();
        
        // 1. Serialize Arguments to MessagePack
        const argsPayload = encode(args);
        
        // 2. Create Envelope
        const message: ConduitMessage = {
            Id: id,
            MethodName: methodName,
            InterfaceName: interfaceName,
            Payload: argsPayload,
            IsError: false,
            Headers: {}
        };
        
        // 3. Serialize Envelope
        const messageBytes = encode(message);
        
        // 4. Prepend Length Prefix (4 bytes, Little Endian)
        const length = messageBytes.length;
        const packet = new Uint8Array(4 + length);
        const view = new DataView(packet.buffer);
        view.setInt32(0, length, true); // Little Endian
        packet.set(messageBytes, 4);
        
        // 5. Send
        this.ws.send(packet);
        
        // 6. Wait for response
        return new Promise<T>((resolve, reject) => {
            this.pendingCalls.set(id, { resolve, reject });
            
            // Timeout after 30s
            setTimeout(() => {
                if (this.pendingCalls.has(id)) {
                    this.pendingCalls.delete(id);
                    reject(new Error("Timeout"));
                }
            }, 30000);
        });
    }

    /**
     * Creates a transparent proxy for a remote interface.
     * This allows calling methods directly on a TypeScript object, similar to .NET's DispatchProxy.
     * 
     * @param interfaceName The name of the interface on the server (e.g., "IUserService")
     * @returns A proxy object implementing T
     */
    public createProxy<T extends object>(interfaceName: string): T {
        return new Proxy({} as T, {
            get: (target, prop) => {
                // Intercept method access
                if (typeof prop === 'string' && prop !== 'then') {
                    // Return a function that invokes the RPC method
                    return (...args: any[]) => {
                        return this.invoke(interfaceName, prop, args);
                    };
                }
                return Reflect.get(target, prop);
            }
        });
    }
}
