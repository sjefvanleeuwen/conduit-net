export class DocCqp extends HTMLElement {
    connectedCallback() {
        this.innerHTML = `
            <div class="doc-jumbotron" style="background-image: url('/images/background6.png');">
                <h2>Conduit Query Protocol Manual</h2>
                <p>The complete guide to writing distributed queries.</p>
            </div>
            <div class="doc-content">
                
                <!-- 1. Introduction -->
                <h3 id="introduction">1. Introduction</h3>
                <p>The Conduit Query Protocol (CQP) is a type-safe, distributed query language designed to expose <code>IQueryable&lt;T&gt;</code> over WebSockets. It allows clients (TypeScript, C#, etc.) to filter, sort, page, and aggregate data residing on remote servers without writing custom endpoints for every requirement.</p>
                <p>This manual focuses on the <strong>TypeScript Client</strong> usage, but the concepts apply equally to the C# LINQ provider.</p>

                <hr />

                <!-- 2. Basic Querying -->
                <h3 id="basic-querying">2. Basic Querying</h3>
                <p>ConduitNet uses a <strong>Fluent API</strong> that mimics LINQ. To begin, define a <code>ConduitContext</code> that maps your remote collections.</p>
                <pre><code class="language-typescript">import { ConduitContext, DbSet } from '@conduit/client';
import { User } from './dtos';

class AppDbContext extends ConduitContext {
    public users = new DbSet&lt;User&gt;('Users');
    public products = new DbSet&lt;Product&gt;('Products');
}

const db = new AppDbContext();

// Select all users
const users = await db.users.toListAsync();</code></pre>

                <hr />

                <!-- 3. Filtering -->
                <h3 id="filtering">3. Filtering</h3>
                <p>Filtering uses type-safe arrow functions. The builder uses a proxy to capture property access and operators, ensuring you never have to write magic strings.</p>

                <pre><code class="language-typescript">// Simple filter
const adults = await db.users
    .where(u => u.age.gt(18))
    .toListAsync();

// Chaining (Implicit AND)
const adminUsers = await db.users
    .where(u => u.role.eq('Admin'))
    .where(u => u.active.isTrue())
    .toListAsync();</code></pre>

                <h4>Supported Operators</h4>
                <div class="table-wrapper">
                    <table>
                        <thead>
                            <tr>
                                <th>Operator</th>
                                <th>Description</th>
                                <th>Example</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr><td><code>.eq(val)</code></td><td>Equals</td><td><code>u.age.eq(18)</code></td></tr>
                            <tr><td><code>.neq(val)</code></td><td>Not Equals</td><td><code>u.role.neq('Guest')</code></td></tr>
                            <tr><td><code>.gt(val)</code></td><td>Greater Than</td><td><code>u.score.gt(100)</code></td></tr>
                            <tr><td><code>.lt(val)</code></td><td>Less Than</td><td><code>u.price.lt(50)</code></td></tr>
                            <tr><td><code>.gte(val)</code></td><td>Greater/Equal</td><td><code>u.date.gte('2023-01-01')</code></td></tr>
                            <tr><td><code>.lte(val)</code></td><td>Less/Equal</td><td><code>u.rank.lte(10)</code></td></tr>
                            <tr><td><code>.contains(val)</code></td><td>String Contains</td><td><code>u.name.contains('John')</code></td></tr>
                            <tr><td><code>.startsWith(val)</code></td><td>String Starts With</td><td><code>u.code.startsWith('A')</code></td></tr>
                            <tr><td><code>.in(list)</code></td><td>In List</td><td><code>u.id.in([1, 2, 3])</code></td></tr>
                        </tbody>
                    </table>
                </div>

                <h4>Logical Groups (OR)</h4>
                <p>To use <code>OR</code> logic, use the <code>.or()</code> operator within the expression.</p>
                <pre><code class="language-typescript">// (Role == Admin OR Role == Mod) AND Active == true
db.users
    .where(u => u.role.eq('Admin').or(u.role.eq('Moderator')))
    .where(u => u.active.isTrue())
    .toListAsync();</code></pre>

                <hr />

                <!-- 4. Sorting & Paging -->
                <h3 id="sorting-paging">4. Sorting & Paging</h3>
                <p>Use <code>orderBy</code>, <code>orderByDescending</code>, <code>thenBy</code>, and <code>thenByDescending</code> with property selectors.</p>
                <pre><code class="language-typescript">// Get page 2 (items 21-40), sorted by Name then Date
db.products
    .orderBy(p => p.name)
    .thenByDescending(p => p.createdDate)
    .skip(20)
    .take(20)
    .toListAsync();</code></pre>

                <hr />

                <!-- 5. Projections -->
                <h3 id="projections">5. Projections</h3>
                <p>To fetch only specific fields, use <code>.select()</code>. This returns a partial object.</p>
                <pre><code class="language-typescript">// Only fetch Id and Email
const users = await db.users
    .select(u => ({ id: u.id, email: u.email }))
    .toListAsync();</code></pre>

                <hr />

                <!-- 6. Relationships -->
                <h3 id="relationships">6. Relationships (Includes)</h3>
                <p>Fetch related data using <code>.include()</code>. You can chain a sub-query builder to filter or sort the related collection.</p>
                <pre><code class="language-typescript">// Get Users with their Posts, and those Posts' Comments
db.users
    .include(u => u.posts, q => q
        .where(p => p.active.isTrue()) // Filter related posts
        .orderByDescending(p => p.date)
        .take(5)
        .include(p => p.comments, c => c
            .take(3)
        )
    )
    .toListAsync();</code></pre>

                <h4>Reverse Navigation (Any/All)</h4>
                <p>Filter parents based on properties of their children using <code>.any()</code> or <code>.all()</code>.</p>
                <pre><code class="language-typescript">// Users who have ANY post with > 100 likes
db.users
    .where(u => u.posts.any(p => p.likes.gt(100)))
    .toListAsync();</code></pre>

                <hr />

                <!-- 7. Aggregations -->
                <h3 id="aggregations">7. Aggregations</h3>
                <p>Perform server-side calculations using <code>.groupBy()</code> and aggregation methods.</p>
                <pre><code class="language-typescript">// Count users by Country
const stats = await db.users
    .groupBy(u => u.country)
    .select(g => ({
        country: g.key,
        totalUsers: g.count(),
        avgAge: g.average(u => u.age)
    }))
    .toListAsync();</code></pre>

                <hr />

                <!-- 8. Real-time -->
                <h3 id="real-time">8. Real-time Queries</h3>
                <p>Turn any query into a live subscription using <code>.watch()</code>.</p>
                <pre><code class="language-typescript">const sub = db.orders
    .where(o => o.status.eq('New'))
    .watch(delta => {
        if (delta.type === 'Added') {
            alert(\`New Order: \${delta.item.id}\`);
        }
    });

// Later...
sub.unsubscribe();</code></pre>

                <hr />

                <!-- 9. Extensibility -->
                <h3 id="extensibility">9. Extensibility</h3>
                <p>Pass custom options to the server provider.</p>
                <pre><code class="language-typescript">db.logs
    .withOption('SearchStrategy', 'Fuzzy')
    .toListAsync();</code></pre>
            </div>
        `;
        if ((window as any).Prism) {
            (window as any).Prism.highlightAllUnder(this);
        }
    }
}
customElements.define('doc-cqp', DocCqp);
