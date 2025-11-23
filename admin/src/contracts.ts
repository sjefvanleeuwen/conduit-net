export interface UserDto {
    Id: number;
    Name?: string;
    Email?: string;
    Username?: string;
    Roles: string[];
}

export function mapUserDto(data: any): UserDto {
    if (Array.isArray(data)) {
        return {
            Id: data[0],
            Name: data[1],
            Email: data[2],
            Username: data[3],
            Roles: data[4]
        };
    }
    return data;
}

export interface IUserService {
    GetUserAsync(id: number): Promise<UserDto | null>;
    GetAllUsersAsync(): Promise<UserDto[]>;
    RegisterUserAsync(user: UserDto): Promise<UserDto>;
    UpdateUserAsync(user: UserDto): Promise<void>;
    DeleteUserAsync(id: number): Promise<void>;
}

export interface IAclService {
    CreateRoleAsync(roleName: string): Promise<void>;
    GrantPermissionAsync(roleName: string, permission: string): Promise<void>;
    CheckPermissionAsync(userId: number, permission: string): Promise<boolean>;
    GetRolePermissionsAsync(roleName: string): Promise<string[]>;
}

export interface TraceSpanDto {
    TraceId: string;
    SpanId: string;
    ParentSpanId?: string;
    Name: string;
    Kind: string;
    StartTime: Date; 
    Duration: number; // ms
    ServiceName: string;
    Tags: Record<string, string>;
}

export function mapTraceSpanDto(data: any): TraceSpanDto {
    if (Array.isArray(data)) {
        // Handle DateTimeOffset (likely [ticks, offset] or similar, or just a timestamp string if using ISO)
        // MessagePack C# DateTimeOffset default is [ticks, offset]
        // But let's see what we get. For now, assume it might need conversion.
        
        // Handle TimeSpan (ticks) -> ms
        // 1 tick = 100 ns = 0.0001 ms
        const durationTicks = data[6];
        const durationMs = durationTicks / 10000;

        return {
            TraceId: data[0],
            SpanId: data[1],
            ParentSpanId: data[2],
            Name: data[3],
            Kind: data[4],
            StartTime: new Date(data[5]), // This might need adjustment
            Duration: durationMs,
            ServiceName: data[7],
            Tags: data[8]
        };
    }
    return data;
}

export interface ITelemetryCollector {
    IngestBatchAsync(spans: TraceSpanDto[]): Promise<void>;
    GetRecentSpansAsync(): Promise<TraceSpanDto[]>;
    GetTraceAsync(traceId: string): Promise<TraceSpanDto[]>;
}

export interface NodeInfo {
    Id: string;
    Address: string;
    Services: string[];
}

export function mapNodeInfo(data: any): NodeInfo {
    if (Array.isArray(data)) {
        return {
            Id: data[0],
            Address: data[1],
            Services: data[2]
        };
    }
    return data;
}

export interface IConduitDirectory {
    RegisterAsync(node: NodeInfo): Promise<void>;
    DiscoverAsync(serviceName: string): Promise<NodeInfo[]>;
}
