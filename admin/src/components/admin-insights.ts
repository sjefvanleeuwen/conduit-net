import * as d3 from 'd3';
import { ConduitService } from '../services/ConduitService';
import { ITelemetryCollector, NodeInfo, TraceSpanDto, mapTraceSpanDto } from '../contracts';

interface GraphNode extends d3.SimulationNodeDatum {
    id: string;
    label: string;
    type: string;
    address: string;
    requestCount: number;
    avgDuration: number;
    errorCount: number;
}

interface GraphLink extends d3.SimulationLinkDatum<GraphNode> {
    source: string | GraphNode;
    target: string | GraphNode;
    count: number;
    avgDuration: number;
}

export class AdminInsights extends HTMLElement {
    private telemetryService: ITelemetryCollector | null = null;
    private svg: d3.Selection<SVGSVGElement, unknown, null, undefined> | null = null;
    private zoomLayer: d3.Selection<SVGGElement, unknown, null, undefined> | null = null;
    private simulation: d3.Simulation<GraphNode, GraphLink> | null = null;
    
    private nodes: GraphNode[] = [];
    private links: GraphLink[] = [];
    
    private refreshInterval: number | null = null;

    connectedCallback() {
        // Fill the parent container completely, ignoring its padding
        this.style.display = 'block';
        this.style.margin = '-20px'; // Counteract .content-wrapper padding
        this.style.width = 'calc(100% + 40px)';
        this.style.height = 'calc(100% + 40px)';
        this.style.overflow = 'hidden';
        this.style.position = 'relative';

        this.innerHTML = `
            <div id="graph-container" style="width: 100%; height: 100%; background: #1e1e1e; cursor: grab;">
                <div style="position: absolute; top: 20px; right: 20px; color: #666; font-family: monospace; font-size: 12px; pointer-events: none; z-index: 10;">
                    <span id="status">Connecting...</span>
                </div>
                <div style="position: absolute; bottom: 20px; left: 20px; color: #444; font-size: 12px; pointer-events: none; z-index: 10;">
                    <i class="fa fa-hand-paper"></i> Pan & Zoom Enabled
                </div>
            </div>
        `;

        this.initGraph();
        this.startDataLoop();
    }

    disconnectedCallback() {
        if (this.refreshInterval) {
            clearInterval(this.refreshInterval);
        }
        if (this.simulation) {
            this.simulation.stop();
        }
    }

    initGraph() {
        const container = this.querySelector('#graph-container') as HTMLElement;
        const rect = container.getBoundingClientRect();
        const width = rect.width;
        const height = rect.height;

        this.svg = d3.select(container).append('svg')
            .attr('width', '100%')
            .attr('height', '100%')
            .attr('viewBox', [0, 0, width, height]);

        // Zoom behavior
        const zoom = d3.zoom<SVGSVGElement, unknown>()
            .scaleExtent([0.1, 4])
            .on("zoom", (event) => {
                this.zoomLayer?.attr("transform", event.transform);
                container.style.cursor = "grabbing";
            })
            .on("end", () => {
                container.style.cursor = "grab";
            });

        this.svg.call(zoom)
            .on("dblclick.zoom", null);

        // Create the container for graph elements (that will be zoomed/panned)
        this.zoomLayer = this.svg.append("g").attr("class", "zoom-layer");

        // Define arrow marker
        const defs = this.svg.append("defs");
        defs.append("marker")
            .attr("id", "arrowhead")
            .attr("viewBox", "0 -5 10 10")
            .attr("refX", 115) // Adjusted for card width (220/2 + margin)
            .attr("refY", 0)
            .attr("markerWidth", 6)
            .attr("markerHeight", 6)
            .attr("orient", "auto")
            .append("path")
            .attr("d", "M0,-5L10,0L0,5")
            .attr("fill", "#666");

        this.simulation = d3.forceSimulation<GraphNode, GraphLink>()
            .force("link", d3.forceLink<GraphNode, GraphLink>().id(d => d.id).distance(300))
            .force("charge", d3.forceManyBody().strength(-1500))
            .force("x", d3.forceX(width / 2).strength(0.08))
            .force("y", d3.forceY(height / 2).strength(0.08))
            .force("collide", d3.forceCollide().radius(130))
            .velocityDecay(0.6);
    }

    async startDataLoop() {
        try {
            this.telemetryService = await ConduitService.getTelemetryService();
            await this.refreshData();
            this.refreshInterval = window.setInterval(() => this.refreshData(), 3000);
        } catch (err) {
            console.error("Failed to init insights", err);
            const status = this.querySelector('#status');
            if (status) status.textContent = "Failed to connect to Telemetry";
        }
    }

    async refreshData() {
        const status = this.querySelector('#status');
        if (status) status.textContent = "Updating...";

        try {
            // 1. Discover Nodes
            const serviceTypes = ['IUserService', 'IAclService', 'ITelemetryCollector', 'IConduitDirectory'];
            const allNodesMap = new Map<string, NodeInfo & { type: string }>();

            for (const svc of serviceTypes) {
                try {
                    const nodes = await ConduitService.discover(svc);
                    for (const node of nodes) {
                        if (!allNodesMap.has(node.Id)) {
                            allNodesMap.set(node.Id, { ...node, type: svc });
                        } else {
                            const existing = allNodesMap.get(node.Id)!;
                            if (!existing.type.includes(svc)) {
                                existing.type += `, ${svc}`;
                            }
                        }
                    }
                } catch (e) {
                    console.warn(`Failed to discover ${svc}`, e);
                }
            }

            // 2. Get Telemetry
            let spans: TraceSpanDto[] = [];
            if (this.telemetryService) {
                const rawSpans = await this.telemetryService.GetRecentSpansAsync() as any[];
                spans = rawSpans.map(mapTraceSpanDto);
            }

            // 3. Calculate Node Stats
            const nodeStats = new Map<string, { count: number, durationSum: number, errors: number }>();
            
            // Helper to get stats object
            const getStats = (id: string) => {
                if (!nodeStats.has(id)) nodeStats.set(id, { count: 0, durationSum: 0, errors: 0 });
                return nodeStats.get(id)!;
            };

            // Index spans by SpanId for parent lookup
            const spanMap = new Map<string, TraceSpanDto>();
            spans.forEach(s => spanMap.set(s.SpanId, s));

            // 4. Build Links based on ParentSpanId
            const linkMap = new Map<string, { count: number, durationSum: number }>();

            for (const span of spans) {
                // Node Stats
                const nodeId = span.Tags['service.instance.id'] || span.ServiceName;
                const stats = getStats(nodeId);
                stats.count++;
                stats.durationSum += span.Duration;
                // Simple error detection (if duration > 1000ms or explicit tag)
                if (span.Duration > 1000) stats.errors++; 

                // Link Stats
                if (span.ParentSpanId && spanMap.has(span.ParentSpanId)) {
                    const parent = spanMap.get(span.ParentSpanId)!;
                    const sourceId = parent.Tags['service.instance.id'] || parent.ServiceName;
                    const targetId = nodeId;

                    if (sourceId !== targetId) {
                        const key = `${sourceId}->${targetId}`;
                        if (!linkMap.has(key)) linkMap.set(key, { count: 0, durationSum: 0 });
                        const lStats = linkMap.get(key)!;
                        lStats.count++;
                        lStats.durationSum += span.Duration; // Duration of the child call
                    }
                }
            }

            // 5. Update Graph Nodes
            const newNodes: GraphNode[] = Array.from(allNodesMap.values()).map(n => {
                const s = nodeStats.get(n.Id) || { count: 0, durationSum: 0, errors: 0 };
                return {
                    id: n.Id,
                    label: n.Services.join(', ') || n.type,
                    type: n.type,
                    address: n.Address,
                    requestCount: s.count,
                    avgDuration: s.count > 0 ? s.durationSum / s.count : 0,
                    errorCount: s.errors,
                    x: this.nodes.find(xn => xn.id === n.Id)?.x,
                    y: this.nodes.find(xn => xn.id === n.Id)?.y
                };
            });

            // 6. Update Graph Links
            const newLinks: GraphLink[] = [];
            linkMap.forEach((stats, key) => {
                const [source, target] = key.split('->');
                // Only add link if both nodes exist in our discovery map
                if (allNodesMap.has(source) && allNodesMap.has(target)) {
                    newLinks.push({
                        source,
                        target,
                        count: stats.count,
                        avgDuration: stats.count > 0 ? stats.durationSum / stats.count : 0
                    });
                }
            });

            // Fallback: If no traffic links, show structural links to Directory (so nodes aren't floating)
            if (newLinks.length === 0) {
                const directoryNodes = newNodes.filter(n => n.type.includes('IConduitDirectory'));
                for (const dir of directoryNodes) {
                    for (const node of newNodes) {
                        if (node.id !== dir.id) {
                            newLinks.push({ 
                                source: node.id, 
                                target: dir.id,
                                count: 0,
                                avgDuration: 0
                            });
                        }
                    }
                }
            }

            this.nodes = newNodes;
            this.links = newLinks;
            this.updateGraph();
            
            if (status) status.textContent = `Live: ${spans.length} spans / 5s`;

        } catch (err) {
            console.error("Error refreshing data", err);
            if (status) status.textContent = "Error updating";
        }
    }

    updateGraph() {
        if (!this.zoomLayer || !this.simulation) return;

        // --- LINKS ---
        const linkSelection = this.zoomLayer.selectAll<SVGLineElement, GraphLink>(".link-group")
            .data(this.links, d => (d.source as any).id + "-" + (d.target as any).id);

        const linkEnter = linkSelection.enter().append("g")
            .attr("class", "link-group");

        linkEnter.append("line")
            .attr("class", "link")
            .attr("stroke", "#666")
            .attr("stroke-width", 2)
            .attr("marker-end", "url(#arrowhead)");

        // Link Labels (Stats)
        linkEnter.append("text")
            .attr("class", "link-label")
            .attr("text-anchor", "middle")
            .attr("dy", -5)
            .attr("fill", "#aaa")
            .style("font-size", "10px");

        const linkUpdate = linkEnter.merge(linkSelection as any);
        
        linkUpdate.select("text")
            .text((d: GraphLink) => d.count > 0 ? `${d.count} req | ${d.avgDuration.toFixed(1)}ms` : "");

        linkSelection.exit().remove();

        // --- NODES ---
        const nodeSelection = this.zoomLayer.selectAll<SVGGElement, GraphNode>(".node")
            .data(this.nodes, d => d.id);

        const nodeEnter = nodeSelection.enter().append("g")
            .attr("class", "node")
            .call(d3.drag<SVGGElement, GraphNode>()
                .on("start", (event, d) => {
                    if (!event.active) this.simulation!.alphaTarget(0.3).restart();
                    d.fx = d.x;
                    d.fy = d.y;
                })
                .on("drag", (event, d) => {
                    d.fx = event.x;
                    d.fy = event.y;
                })
                .on("end", (event, d) => {
                    if (!event.active) this.simulation!.alphaTarget(0);
                    d.fx = null;
                    d.fy = null;
                }));

        // Use foreignObject to render HTML Card
        const fo = nodeEnter.append("foreignObject")
            .attr("width", 220)
            .attr("height", 100)
            .attr("x", -110)
            .attr("y", -50);

        fo.append("xhtml:div")
            .style("width", "100%")
            .style("height", "100%")
            .style("background", "#2d2d2d")
            .style("border", "1px solid #555")
            .style("border-radius", "4px")
            .style("display", "flex")
            .style("flex-direction", "column")
            .style("padding", "8px")
            .style("box-sizing", "border-box")
            .style("color", "white")
            .style("font-family", "sans-serif")
            .style("font-size", "12px")
            .style("overflow", "hidden")
            .style("box-shadow", "0 4px 6px rgba(0,0,0,0.3)")
            .html(d => this.getNodeHtml(d));

        const nodeUpdate = nodeEnter.merge(nodeSelection);
        
        // Update HTML content
        nodeUpdate.select("foreignObject").select("div")
            .style("border-color", d => d.errorCount > 0 ? "#ff4757" : "#555")
            .html(d => this.getNodeHtml(d));

        nodeSelection.exit().remove();

        // Restart simulation
        this.simulation.nodes(this.nodes);
        (this.simulation.force("link") as d3.ForceLink<GraphNode, GraphLink>).links(this.links);
        this.simulation.alpha(0.3).restart();

        this.simulation.on("tick", () => {
            linkUpdate.select("line")
                .attr("x1", d => (d.source as GraphNode).x!)
                .attr("y1", d => (d.source as GraphNode).y!)
                .attr("x2", d => (d.target as GraphNode).x!)
                .attr("y2", d => (d.target as GraphNode).y!);

            linkUpdate.select("text")
                .attr("x", d => ((d.source as GraphNode).x! + (d.target as GraphNode).x!) / 2)
                .attr("y", d => ((d.source as GraphNode).y! + (d.target as GraphNode).y!) / 2);

            nodeUpdate.attr("transform", d => `translate(${d.x},${d.y})`);
        });
    }

    getNodeHtml(d: GraphNode): string {
        const icon = this.getIcon(d.type);
        const color = this.getColor(d.type);
        const shortId = d.id.substring(0, 8);
        
        return `
            <div style="display: flex; align-items: center; margin-bottom: 6px;">
                <div style="width: 28px; height: 28px; background: ${color}; border-radius: 4px; display: flex; align-items: center; justify-content: center; margin-right: 10px; flex-shrink: 0;">
                    <i class="${icon}" style="color: white; font-size: 14px;"></i>
                </div>
                <div style="flex: 1; overflow: hidden;">
                    <div style="font-weight: bold; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; font-size: 13px;">${d.label}</div>
                    <div style="font-size: 10px; color: #aaa; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">${d.address}</div>
                </div>
            </div>
            <div style="font-size: 10px; color: #666; font-family: monospace; margin-bottom: 6px;">ID: ${shortId}...</div>
            <div style="display: flex; justify-content: space-between; margin-top: auto; border-top: 1px solid #444; padding-top: 6px;">
                <span style="color: #ccc;">${d.requestCount} reqs</span>
                <span style="color: ${d.avgDuration > 500 ? '#ff4757' : '#ccc'};">${d.avgDuration.toFixed(0)}ms avg</span>
            </div>
        `;
    }

    getIcon(type: string): string {
        if (type.includes('Directory')) return 'fa fa-sitemap';
        if (type.includes('User')) return 'fa fa-user';
        if (type.includes('Acl')) return 'fa fa-lock';
        if (type.includes('Telemetry')) return 'fa fa-chart-line';
        return 'fa fa-server';
    }

    getColor(type: string): string {
        if (type.includes('Directory')) return '#ff9f43';
        if (type.includes('User')) return '#54a0ff';
        if (type.includes('Acl')) return '#ee5253';
        if (type.includes('Telemetry')) return '#1dd1a1';
        return '#8395a7';
    }
}

customElements.define('admin-insights', AdminInsights);
