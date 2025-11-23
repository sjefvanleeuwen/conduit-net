export class DocArchitecture extends HTMLElement {
    connectedCallback() {
        this.innerHTML = `
            <div class="doc-jumbotron" style="background-image: url('/images/background7.png');">
                <h2>System Architecture</h2>
                <p>Deep dive into the distributed consensus, leader election, and message transport layer.</p>
            </div>
            <div class="doc-content">

                <!-- 1. The Challenge -->
                <h3>1. The Challenge</h3>
                <p>Exposing <code>IQueryable&lt;T&gt;</code> over a network boundary is complex because:</p>
                <ol>
                    <li><strong>Serialization</strong>: .NET Expression Trees are not serializable by default.</li>
                    <li><strong>Security</strong>: Accepting arbitrary expression trees allows remote code execution (RCE) if not carefully sanitized.</li>
                    <li><strong>Interoperability</strong>: TypeScript/JavaScript clients do not have a native concept of LINQ Expression Trees.</li>
                    <li><strong>Performance</strong>: GraphQL or OData parsers add significant CPU overhead.</li>
                </ol>

                <hr />

                <!-- 2. The Solution -->
                <h3>2. The Solution: Conduit Query Protocol (CQP)</h3>
                <p>Instead of sending raw Expression Trees or using a heavy text-based protocol like GraphQL, ConduitNet will use a <strong>Structured Query Object (SQO)</strong> pattern. This is a lightweight, binary-serializable intermediate representation of a query.</p>

                <h4>2.1. The Wire Format (MessagePack)</h4>
                <p>The query is serialized into a compact binary structure.</p>
                <pre><code class="language-csharp">[MessagePackObject]
public class ConduitQuery
{
    [Key(0)]
    public List&lt;QueryFilter&gt; Filters { get; set; } = new();
    // ... (truncated for brevity)
}</code></pre>

                <hr />

                <!-- 3. C# Client -->
                <h3>3. C# Client Implementation (The "Magic")</h3>
                <p>We implement a custom <code>IQueryProvider</code>. This allows C# clients to write standard LINQ, which ConduitNet translates into <code>ConduitQuery</code> objects at runtime.</p>

                <h4>Usage</h4>
                <pre><code class="language-csharp">// Client code looks like standard LINQ
var users = await userService.Users
    .Where(u => u.Age > 18 && u.Role == "Admin")
    .OrderByDescending(u => u.LastLogin)
    .Skip(10)
    .Take(20)
    .ToListAsync();</code></pre>

                <hr />

                <!-- 4. TypeScript Client -->
                <h3>4. TypeScript Client Implementation</h3>
                <p>TypeScript does not have LINQ. While libraries like <code>linq.ts</code> exist, they operate on in-memory arrays. We need a <strong>Fluent Query Builder</strong> that constructs our <code>ConduitQuery</code> object.</p>

                <h4>Proposed TS Syntax</h4>
                <pre><code class="language-typescript">// Generated or Generic Builder
const query = db.users
    .where(u => u.age.gt(18))
    .where(u => u.role.eq('Admin'))
    .orderByDescending(u => u.lastLogin)
    .skip(10)
    .take(20);

// Executes: sends MessagePack binary of the query structure
const users = await query.toListAsync();</code></pre>

                <hr />

                <!-- 5. Server-Side -->
                <h3>5. Server-Side Execution</h3>
                <p>The server receives the <code>ConduitQuery</code> and applies it to the actual data source (Entity Framework, Memory, etc.).</p>
                <pre><code class="language-csharp">public async Task&lt;List&lt;UserDto&gt;&gt; GetUsersAsync(ConduitQuery query)
{
    IQueryable&lt;User&gt; dbQuery = _dbContext.Users;
    // ... applies filters, sorts, paging
    return await dbQuery.ToListAsync();
}</code></pre>

                <hr />

                <!-- 6. Relationships -->
                <h3>6. Handling Relationships (The "Graph" in GraphQL)</h3>
                <p>To match GraphQL's ability to query related data (e.g., <code>users { posts { title } }</code>), CQP uses the <code>Includes</code> property.</p>

                <pre><code class="language-typescript">// TypeScript Example: Get Users and their recent active Posts
const query = db.users
    .where(u => u.age.gt(18))
    .include(u => u.posts, q => q
        .where(p => p.active.isTrue())
        .orderByDescending(p => p.createdDate)
        .take(5)
    );</code></pre>

                <hr />

                <!-- 7. Federation -->
                <h3>7. Distributed Federation (Microservices)</h3>
                <p>In a microservices architecture, <code>Users</code> and <code>Posts</code> might live in different services. CQP handles this via <strong>Query Stitching</strong>.</p>
                <p>This allows the client to treat the distributed system as a single cohesive graph, similar to <strong>Apollo Federation</strong>.</p>

                <hr />

                <!-- 8. Advanced -->
                <h3>8. Advanced Capabilities</h3>
                
                <h4>8.1 Reverse Navigation (Any/All)</h4>
                <p>To support queries like "Users who have active posts" (<code>u.Posts.Any(p => p.Active)</code>), CQP supports collection operators.</p>

                <h4>8.2 Partial Results (Resilience)</h4>
                <p>In a distributed system, one downstream service might fail. Instead of failing the entire request, CQP returns a <strong>Partial Result</strong>.</p>

                <hr />

                <!-- 9. Aggregations -->
                <h3>9. Aggregations & Grouping</h3>
                <p>Mature applications need to summarize data. CQP supports SQL-like grouping and aggregation.</p>
                <pre><code class="language-typescript">// TypeScript: Count users by Role
db.users
    .groupBy(u => u.role)
    .select(g => ({
        role: g.key,
        count: g.count()
    }))
    .toListAsync();</code></pre>

                <hr />

                <!-- 10. Live Queries -->
                <h3>10. Real-time Subscriptions (Live Queries)</h3>
                <p>Since ConduitNet runs over WebSockets, we can offer <strong>Live Queries</strong>. The server monitors the data source and pushes <strong>Deltas</strong> when data matching the query changes.</p>
                <pre><code class="language-typescript">// TypeScript: Watch for new high-priority tickets
const subscription = db.tickets
    .where(t => t.priority.eq('High'))
    .watch((delta) => {
        if (delta.type === 'Added') console.log('New Ticket:', delta.item);
        if (delta.type === 'Modified') console.log('Ticket Updated:', delta.item);
    });</code></pre>
            </div>
        `;
        if ((window as any).Prism) {
            (window as any).Prism.highlightAllUnder(this);
        }
    }
}
customElements.define('doc-architecture', DocArchitecture);
