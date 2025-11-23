# Distributed Query Architecture: Exposing IQueryable over WebSockets

## 1. The Challenge
Exposing `IQueryable<T>` over a network boundary is complex because:
1.  **Serialization**: .NET Expression Trees are not serializable by default.
2.  **Security**: Accepting arbitrary expression trees allows remote code execution (RCE) if not carefully sanitized.
3.  **Interoperability**: TypeScript/JavaScript clients do not have a native concept of LINQ Expression Trees.
4.  **Performance**: GraphQL or OData parsers add significant CPU overhead.

## 2. The Solution: Conduit Query Protocol (CQP)
Instead of sending raw Expression Trees or using a heavy text-based protocol like GraphQL, ConduitNet will use a **Structured Query Object (SQO)** pattern. This is a lightweight, binary-serializable intermediate representation of a query.

### 2.1. The Wire Format (MessagePack)
The query is serialized into a compact binary structure.

```csharp
[MessagePackObject]
public class ConduitQuery
{
    [Key(0)]
    public List<QueryFilter> Filters { get; set; } = new();

    [Key(1)]
    public List<QuerySort> Sorts { get; set; } = new();

    [Key(2)]
    public int? Skip { get; set; }

    [Key(3)]
    public int? Take { get; set; }

    [Key(4)]
    public List<string> SelectFields { get; set; } // Projection

    [Key(5)]
    public List<QueryInclude> Includes { get; set; } // Relationships

    [Key(6)]
    public List<string> GroupBy { get; set; } // Grouping fields

    [Key(7)]
    public List<QueryAggregate> Aggregates { get; set; } // Sum, Count, Min, Max

    [Key(8)]
    public Dictionary<string, string> CustomProperties { get; set; } // Extensibility bag
}

[MessagePackObject]
public class QueryAggregate
{
    [Key(0)]
    public AggregateType Type { get; set; } // Count, Sum, Min, Max, Avg
    
    [Key(1)]
    public string FieldName { get; set; } // The field to aggregate
    
    [Key(2)]
    public string Alias { get; set; } // The name in the result
}

[MessagePackObject]
public class QueryInclude
{
    [Key(0)]
    public string Path { get; set; } // e.g. "Posts" or "Posts.Comments"

    [Key(1)]
    public ConduitQuery Filter { get; set; } // Nested query for the related collection
}

[MessagePackObject]
public class QueryFilter
{
    [Key(0)]
    public string FieldName { get; set; }
    
    [Key(1)]
    public FilterOperator Operator { get; set; } // Eq, Gt, Lt, Contains, Any, All
    
    [Key(2)]
    public object Value { get; set; } // Can be primitive, or List<QueryFilter> for Any/All
    
    [Key(3)]
    public LogicOperator Logic { get; set; } // And, Or

    [Key(4)]
    public List<QueryFilter> Group { get; set; } // For nested groups (e.g. (A OR B) AND C)
}

[MessagePackObject]
public class QuerySort
{
    [Key(0)]
    public string FieldName { get; set; }

    [Key(1)]
    public bool IsDescending { get; set; }
}
```

---

## 3. C# Client Implementation (The "Magic")
We implement a custom `IQueryProvider`. This allows C# clients to write standard LINQ, which ConduitNet translates into `ConduitQuery` objects at runtime.

### Usage
```csharp
// Client code looks like standard LINQ
var users = await userService.Users
    .Where(u => u.Age > 18 && u.Role == "Admin")
    .OrderByDescending(u => u.LastLogin)
    .Skip(10)
    .Take(20)
    .ToListAsync();
```

### How it works
1.  **Interception**: The `DispatchProxy` intercepts the property access to `Users`.
2.  **Expression Visiting**: A custom `ExpressionVisitor` walks the LINQ expression tree.
    *   `u.Age > 18` becomes `Filter { Field="Age", Op=Gt, Value=18 }`.
3.  **Serialization**: The resulting `ConduitQuery` object is serialized via MessagePack.
4.  **Transport**: Sent over WebSocket to the server.

---

## 4. TypeScript Client Implementation
TypeScript does not have LINQ. While libraries like `linq.ts` exist, they operate on in-memory arrays. We need a **Fluent Query Builder** that constructs our `ConduitQuery` object.

### Why not GraphQL?
*   **Overhead**: GraphQL requires string parsing and schema validation on every request.
*   **Size**: GraphQL queries are verbose strings. CQP is compact binary.
*   **Coupling**: GraphQL requires a separate schema definition. CQP is derived directly from your C# DTOs.

### Proposed TS Syntax
We can generate a type-safe builder based on the DTO interfaces.

```typescript
// Generated or Generic Builder
const query = db.users
    .where(u => u.age.gt(18))
    .where(u => u.role.eq('Admin'))
    .orderByDescending(u => u.lastLogin)
    .skip(10)
    .take(20);

// Executes: sends MessagePack binary of the query structure
const users = await query.toListAsync(); 
```

### Advanced: Type-Safe Builder (Mapped Types)
We can use TypeScript's advanced type system to ensure field names exist.

```typescript
// Type-safe where clause
.where(u => u.age.gt(18)) // Compiler checks 'age' exists on User
```

---

## 5. Server-Side Execution
The server receives the `ConduitQuery` and applies it to the actual data source (Entity Framework, Memory, etc.).

```csharp
public async Task<List<UserDto>> GetUsersAsync(ConduitQuery query)
{
    IQueryable<User> dbQuery = _dbContext.Users;

    // 1. Apply Includes (Eager Loading)
    foreach(var include in query.Includes)
    {
        // If the include has a filter, we need to use .Select() projection or specialized EF Core features
        // For simple includes:
        dbQuery = dbQuery.Include(include.Path);
    }

    // 2. Apply Filters
    foreach(var filter in query.Filters)
    {
        dbQuery = dbQuery.ApplyFilter(filter); // Dynamic Expression Tree construction
    }

    // 3. Apply Sorts
    foreach(var sort in query.Sorts)
    {
        dbQuery = dbQuery.ApplySort(sort);
    }

    // 4. Paging
    if (query.Skip.HasValue) dbQuery = dbQuery.Skip(query.Skip.Value);
    if (query.Take.HasValue) dbQuery = dbQuery.Take(query.Take.Value);

    return await dbQuery.ToListAsync();
}
```

## 6. Handling Relationships (The "Graph" in GraphQL)
To match GraphQL's ability to query related data (e.g., `users { posts { title } }`), CQP uses the `Includes` property.

### Recursive Querying
Each `QueryInclude` contains a full `ConduitQuery` object. This allows you to apply filters, sorts, and paging to the *related* collection.

```typescript
// TypeScript Example: Get Users and their recent active Posts
const query = db.users
    .where(u => u.age.gt(18))
    .include(u => u.posts, q => q
        .where(p => p.active.isTrue())
        .orderByDescending(p => p.createdDate)
        .take(5)
    );
```

This recursive structure allows CQP to handle complex object graphs while maintaining a flat, binary-friendly wire format.

## 7. Distributed Federation (Microservices)
In a microservices architecture, `Users` and `Posts` might live in different services (and different databases). CQP handles this via **Query Stitching**.

### The Problem
A client asks for `Users.Include(u => u.Posts)`.
*   `UserService` has the `User` data.
*   `PostService` has the `Post` data.
*   `UserService` cannot simply join the tables in SQL.

### The Solution: Aggregation at the Edge
The service handling the root request acts as an orchestrator.

#### Example Flow: `Users.Take(5).Include(u => u.Posts)`

1.  **Root Execution**: `UserService` executes the query for `Users` against its local database with `Take(5)`.
2.  **ID Extraction**: It collects the IDs of the returned users (e.g., `[101, 102, 103, 104, 105]`).
3.  **Remote Query Generation**: It constructs a new `ConduitQuery` for the `PostService`.
    *   **The Batching Magic**: It injects a filter: `UserId IN [101, 102, 103, 104, 105]`.
    *   It copies any nested filters/sorts from the original `Include` (e.g. if the client asked for `Posts.Where(p => p.Active)`).
4.  **Parallel Fetch**: It calls `PostService.GetPosts(remoteQuery)` over the high-speed Conduit transport.
    *   This is a **single network call** for all 5 users, avoiding the N+1 problem.
5.  **Stitching**: It maps the returned posts back to their respective users in memory before returning the final result to the client.

This allows the client to treat the entire distributed system as a single cohesive graph, similar to **Apollo Federation** in GraphQL, but without the need for a centralized gateway.

## 8. Advanced Capabilities

### 8.1 Reverse Navigation (Any/All)
To support queries like "Users who have active posts" (`u.Posts.Any(p => p.Active)`), CQP supports collection operators.

```csharp
var filter = new QueryFilter 
{
    FieldName = "Posts",
    Operator = FilterOperator.Any,
    Value = new List<QueryFilter> 
    {
        new QueryFilter { FieldName = "Active", Operator = FilterOperator.Eq, Value = true }
    }
};
```

### 8.2 Partial Results (Resilience)
In a distributed system, one downstream service might fail (e.g., `PostService` is down). Instead of failing the entire request, CQP returns a **Partial Result**.

```csharp
public class ConduitResult<T>
{
    public T Data { get; set; }
    public List<ConduitError> Errors { get; set; }
}
```

If `PostService` fails, the client receives the `User` object, but `Posts` will be null, and the `Errors` collection will contain details about the `PostService` failure.

### 8.3 Sorting Capabilities
CQP supports robust sorting options, including multi-level sorts and sorting on nested collections.

#### Multiple Sorts (ThenBy)
The `Sorts` list implies priority. The first item is the primary sort, the second is the secondary sort, and so on.

```csharp
// Users.OrderBy(u => u.LastName).ThenByDescending(u => u.FirstName)
var query = new ConduitQuery
{
    Sorts = new List<QuerySort>
    {
        new QuerySort { FieldName = "LastName", IsDescending = false },
        new QuerySort { FieldName = "FirstName", IsDescending = true }
    }
};
```

#### Nested Collection Sorting
Because `QueryInclude` contains a full `ConduitQuery`, you can sort related collections independently.

```typescript
// Get Users, and for each user, get their Posts sorted by Date
db.users
    .include(u => u.posts, q => q.orderByDescending(p => p.createdDate));
```

## 9. Aggregations & Grouping
Mature applications need to summarize data, not just list it. CQP supports SQL-like grouping and aggregation.

```typescript
// TypeScript: Count users by Role
db.users
    .groupBy(u => u.role)
    .select(g => ({
        role: g.key,
        count: g.count()
    }))
    .toListAsync();
```

## 10. Real-time Subscriptions (Live Queries)
Since ConduitNet runs over WebSockets, we can offer a feature that REST and gRPC struggle with: **Live Queries**.

By setting a flag, the client tells the server to keep the query active. The server monitors the data source and pushes **Deltas** when data matching the query changes.

```typescript
// TypeScript: Watch for new high-priority tickets
const subscription = db.tickets
    .where(t => t.priority.eq('High'))
    .watch((delta) => {
        if (delta.type === 'Added') console.log('New Ticket:', delta.item);
        if (delta.type === 'Modified') console.log('Ticket Updated:', delta.item);
    });
```

This eliminates the need for manual polling or setting up separate SignalR hubs for every feature.

## 11. Extensibility (Future-Proofing)
To ensure the protocol remains stable even as specific analytics needs arise, `ConduitQuery` includes a `CustomProperties` dictionary.

This allows you to pass domain-specific parameters without altering the core protocol.

```typescript
// Example: Time-Series Bucketing (Custom Extension)
db.sensorReadings
    .where(s => s.deviceId.eq(123))
    .withOption('BucketSize', '5m') // Custom property
    .withOption('FillGaps', 'Zero')
    .toListAsync();
```

The server-side `IQueryProvider` can look for these keys and apply specialized logic (e.g., calling a specific TimeScaleDB function) without breaking the standard query contract.

## 12. Summary
Exposing `IQueryable` is powerful but dangerous.
1.  **Field Whitelisting**: Only allow filtering on properties exposed in the DTO.
2.  **Complexity Limits**: Limit the number of filters/sorts to prevent DoS.
3.  **No Arbitrary Code**: Since we serialize a *structure* (CQP) and not code, we avoid RCE vulnerabilities associated with serializing raw Expression Trees.

## 13. Protocol Comparison
| Feature | GraphQL | Remote LINQ (Raw) | Conduit Query Protocol (CQP) |
| :--- | :--- | :--- | :--- |
| **Wire Format** | Text (JSON) | Binary/JSON | **Binary (MessagePack)** |
| **Performance** | Medium (Parsing) | High | **Very High (Zero-Copy)** |
| **Security** | High (Schema) | Low (RCE Risk) | **High (Structured)** |
| **Client Feel** | Query Language | Native C# | **Native C# / Fluent TS** |

This architecture provides the "best of both worlds": the developer experience of LINQ/OData with the performance of gRPC/MessagePack.
