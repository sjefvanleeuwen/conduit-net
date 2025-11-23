# Bare Metal Hosting Strategy: The "Thin Linux" Approach

## 1. Philosophy
We reject the complexity of Kubernetes and container orchestrators for this architecture. Instead, we leverage the robust, battle-tested primitives built directly into the Linux kernel and init system (`systemd`).

**The Goal:** A "Just Enough OS" layer that does nothing but boot the kernel, start the network, and launch the ConduitNet runtime.

## 2. The OS Choice: Debian 12 (Bookworm) Minimal
While Alpine Linux is smaller, **Debian 12 Minimal** is chosen for:
1.  **glibc Compatibility**: Native .NET debugging and profiling tools work best with glibc.
2.  **Driver Support**: Better hardware compatibility for diverse bare-metal hardware.
3.  **Stability**: The "Universal Operating System" is rock solid.

### Minimal Footprint
A stripped-down Debian install consumes < 100MB RAM and < 1GB Disk, leaving maximum resources for the application.

---

## 3. The "Seed" State (Pre-Installation)
The `ConduitNet.Deployer` expects a server in a "Seed State". This is the absolute minimum configuration required before the deployer takes over.

### Requirements
1.  **SSH Access**: Root or sudo-enabled user with SSH key authentication.
2.  **Network**: Static IP or DHCP reservation.
3.  **Dependencies**:
    *   `openssh-server`
    *   `libicu-dev` (Required for .NET Globalization)
    *   `curl` (For health checks/bootstrapping)

### Cloud-Init / Preseed Example
If provisioning via PXE or Cloud-Init, this is the config:

```yaml
packages:
  - openssh-server
  - libicu72
  - curl

users:
  - name: conduit_admin
    groups: sudo
    shell: /bin/bash
    ssh_authorized_keys:
      - ssh-rsa AAAAB3Nza... (Deployer Public Key)
```

---

## 4. Systemd Architecture (The "Orchestrator")
We use `systemd` not just to start services, but to isolate them.

### 4.1. The Slice (Resource Partitioning)
We create a dedicated **Slice** for all Conduit services. This prevents a memory leak in one service from crashing the OS (SSH/Kernel).

**File:** `/etc/systemd/system/conduit.slice`
```ini
[Unit]
Description=ConduitNet Application Slice
Documentation=man:systemd.slice

[Slice]
# Reserve 10% of memory for the OS/Kernel
MemoryHigh=90%
MemoryMax=95%
# Fair queuing for CPU
CPUAccounting=true
IOAccounting=true
```

### 4.2. The Service Units
Each Conduit Node runs as a separate service within this slice.

**File:** `/etc/systemd/system/conduit-node@.service`
```ini
[Unit]
Description=ConduitNet Node Service (%i)
After=network.target conduit.slice

[Service]
Type=notify
User=conduit
Group=conduit
WorkingDirectory=/opt/conduit/%i
ExecStart=/opt/conduit/%i/ConduitNode
Slice=conduit.slice

# Isolation & Security
ProtectSystem=strict
ReadWritePaths=/var/log/conduit /var/lib/conduit
PrivateTmp=true
NoNewPrivileges=true

# Resource Limits per Service
MemoryHigh=512M
MemoryMax=600M
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
```

---

## 5. The Deployment Core (`ConduitNet.Deployer`)
The deployer acts as the "Controller". It does not run on the nodes; it runs on an admin machine (or CI/CD agent) and pushes state to the nodes.

### Workflow
1.  **Connect**: Deployer connects to Target Node via SSH (`conduit_admin`).
2.  **Prepare**:
    *   Ensures user `conduit` exists.
    *   Ensures directories `/opt/conduit` and `/var/lib/conduit` exist.
3.  **Push**:
    *   Uploads **Self-Contained** .NET binaries (Single File, Trimmed).
    *   *Note: Self-contained means no .NET Runtime installation is needed on the host.*
4.  **Configure**:
    *   Generates/Updates `systemd` unit files based on the topology.
    *   Writes `appsettings.json` with environment-specific config.
5.  **Activate**:
    *   `systemctl daemon-reload`
    *   `systemctl restart conduit-node@api1`

## 6. Monitoring & Logs
Since we don't have sidecars (Prometheus exporters, Fluentd), we use the native streams.

*   **Logs**: `journald`. The Deployer can pull logs via `journalctl -u conduit-node@* -o json`.
*   **Metrics**: The Conduit Node exposes a lightweight `/metrics` endpoint (or pushes via UDP) that an external collector can scrape.
*   **Health**: `systemd` hardware watchdog integration.

## 7. Architecture Summary
| Layer | Technology | Responsibility |
| :--- | :--- | :--- |
| **Hardware** | Bare Metal | Raw Compute |
| **OS** | Debian 12 Minimal | Kernel, Network, SSH |
| **Orchestration** | systemd | Lifecycle, Resource Limits, Isolation |
| **Runtime** | .NET 9 (Self-Contained) | Application Logic |
| **Control Plane** | ConduitNet.Deployer | Provisioning, Updates, Config |

## 8. Comparative Analysis: Overhead & Complexity

We compare the **Thin Linux** approach against industry-standard containerization strategies.

### 8.1. Resource Overhead (Per Node)

| Metric | **Thin Linux (Systemd)** | **Docker (Bare Metal)** | **K8s (Bare Metal)** | **K8s on VMs** |
| :--- | :--- | :--- | :--- | :--- |
| **Idle RAM Usage** | **~90 MB** | ~400 MB (Daemon + containerd) | ~1.5 GB (Kubelet, Proxy, Etcd) | ~4 GB (Hypervisor + Guest OS + K8s) |
| **Disk Footprint** | **< 1 GB** | ~3 GB (Images + OverlayFS) | ~5 GB | ~20 GB |
| **CPU Overhead** | **Near Zero** | Low (Namespace overhead) | Medium (Control plane polling) | High (Virtualization tax) |
| **Network Latency** | **Native** | Low (Bridge/NAT) | Medium (CNI / Overlay) | High (vSwitch + Overlay) |
| **Cold Boot Time** | **< 10s** | ~15s | ~60s+ | ~120s+ |

### 8.2. Operational Complexity (Cognitive Load)

| Feature | **Thin Linux** | **Kubernetes** |
| :--- | :--- | :--- |
| **Networking** | Standard Linux Networking (IP, DNS, `/etc/hosts`). Easy to debug with `ping`, `netstat`. | CNI Plugins (Calico/Flannel), Ingress Controllers, Service Meshes, CoreDNS. Hard to debug. |
| **Storage** | Standard Filesystem (`ext4`, `xfs`). | PVs, PVCs, StorageClasses, CSI Drivers. |
| **Security** | Standard Linux Permissions (User/Group/Mode) + Systemd Sandboxing. | RBAC, PodSecurityPolicies, OPA Gatekeeper, Service Accounts. |
| **Updates** | `apt update` + Binary Swap. | Cluster Upgrades, Node Draining, Helm Chart migrations. |

---

## 9. Strategic Evaluation (Pros & Cons)

### ✅ Pros (Why we chose this)
1.  **Maximum Performance Density**: By removing the Hypervisor (5-15% overhead) and the Container Orchestrator (Agent overhead), we can pack 20-30% more actual application workload onto the same hardware.
2.  **Predictable Latency**: No overlay networks (VXLAN/Geneve) or double-NATing. Packets hit the NIC and go straight to the socket. Critical for high-throughput RPC.
3.  **Debuggability**: If a service fails, you can SSH in and use standard tools (`htop`, `strace`, `gdb`, `perf`). No "exec-ing" into ephemeral containers that lack tools.
4.  **Simplicity**: The entire "orchestrator" is a 20-line `systemd` unit file. There is no "magic" happening in the background.
5.  **Cost**: Lower hardware requirements mean cheaper servers or fewer servers for the same throughput.

### ❌ Cons (Trade-offs accepted)
1.  **No Dynamic Scheduling**: We cannot simply say "run 5 replicas somewhere". We must explicitly map services to nodes (Static Topology).
2.  **Slower Bin-Packing**: Kubernetes is excellent at squeezing mixed workloads (batch jobs + web servers) onto nodes dynamically. We have to plan capacity manually.
3.  **Dependency Management**: We must ensure the host OS libraries (glibc, openssl) match the .NET build requirements (mitigated by Self-Contained builds).
4.  **No "Cluster-Level" Self-Healing**: If a physical node dies, `systemd` cannot move the workload to another node automatically. The `ConduitNet.Deployer` or a load balancer must handle failover.

---

## 10. Implementation Roadmap

To realize this "Thin Linux" strategy, we need to build specific capabilities into the `ConduitNet` ecosystem.

### Phase 1: The Control Plane (`ConduitNet.Deployer`)
We need a CLI tool that acts as the "Puppet Master".

*   **Dependencies**: Add `SSH.NET` to `ConduitNet.Deployer.csproj`.
*   **Topology Definition**: Create a `topology.json` schema to define which nodes run which services.
    ```json
    {
      "nodes": [
        { "ip": "192.168.1.10", "role": "api-server", "services": ["Api1", "Api2"] },
        { "ip": "192.168.1.11", "role": "worker", "services": ["ConduitNode"] }
      ]
    }
    ```
*   **Provisioning Logic**: Implement `SshProvisioner` class:
    1.  `Connect(ip, key)`
    2.  `EnsureUser("conduit")`
    3.  `EnsureDirectories("/opt/conduit")`
    4.  `UploadFile(localPath, remotePath)`

### Phase 2: Build Pipeline Integration
We must automate the creation of the "Artifacts" that get deployed.

*   **Build Script**: Create `build-deploy-artifacts.ps1`.
*   **Command**:
    ```powershell
    dotnet publish ./ConduitNet/Api1/Api1.csproj `
      -c Release `
      -r linux-x64 `
      --self-contained true `
      -p:PublishSingleFile=true `
      -p:PublishTrimmed=true `
      -o ./artifacts/Api1
    ```
*   **Outcome**: A single executable binary `Api1` (approx 40-60MB) that runs without external dependencies.

### Phase 3: Runtime Systemd Integration
The .NET application needs to "know" it is running under systemd to handle lifecycle events correctly.

*   **NuGet Package**: Install `Microsoft.Extensions.Hosting.Systemd` into `ConduitNet.Core`.
*   **Code Change**: Update `Program.cs` / `Host.CreateDefaultBuilder`:
    ```csharp
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSystemd() // <--- Enables Notify and Journald logging
            .ConfigureServices(...)
    ```
*   **Health Checks**: Implement `IHostedLifecycleService` to send `sd_notify("READY=1")` when the application is fully initialized.

### Phase 4: Observability & Maintenance
*   **Log Aggregation**: The `Deployer` needs a `tail` command:
    *   `conduit-deploy logs --node 192.168.1.10 --service Api1 --follow`
    *   Implementation: SSH exec `journalctl -u conduit-node@Api1 -f -o json`.
*   **Updates**: Implement "Rolling Update" logic in the Deployer:
    1.  Stop Service A on Node 1.
    2.  Swap Binary.
    3.  Start Service A.
    4.  Wait for Health Check (HTTP 200).
    5.  Proceed to Node 2.

---

## 11. Mitigating the Trade-offs: "Rolling Our Own" Solutions

We acknowledge the cons of leaving Kubernetes, but we can mitigate them with targeted, simpler solutions that fit our specific architecture.

### 11.1. Addressing "No Dynamic Scheduling"
**The Problem:** We can't just throw a container at the cluster and let it land anywhere.
**Our Solution: Deterministic Placement via `ConduitNet.Deployer`**
Instead of random scheduling, we implement **Calculated Placement**.
1.  **Capacity Awareness**: The Deployer reads a `nodes.json` file defining the physical RAM/CPU of each host.
2.  **Validation**: When you define a topology, the Deployer calculates the sum of `MemoryHigh` for all services on a node.
3.  **Guardrails**: If `Sum(Services) > Node_RAM * 0.9`, the deployment is rejected before it even starts.
*Result: We lose "magic" placement but gain guaranteed stability. We know exactly where things run.*

### 11.2. Addressing "Slower Bin-Packing"
**The Problem:** We might under-utilize servers because we don't dynamically squeeze workloads.
**Our Solution: Systemd Slices & Over-provisioning**
1.  **Guaranteed vs. Burstable**: We configure `systemd` with `MemoryHigh` (Throttle point) and `MemoryMax` (Kill point).
2.  **Shared Slices**: We can group low-priority background services into a `batch.slice` with lower CPU weight (`CPUWeight=50`) compared to the `api.slice` (`CPUWeight=200`).
*Result: The Linux Kernel handles the "squeezing" for us at the process level, which is more efficient than K8s handling it at the pod level.*

### 11.3. Addressing "Dependency Management"
**The Problem:** Updates to the host OS might break the app (e.g., newer OpenSSL).
**Our Solution: The "Immutable Artifact" & Pre-Flight Checks**
1.  **Self-Contained**: As mentioned, we bundle the .NET Runtime.
2.  **Pre-Flight Validator**: The `ConduitNet.Deployer` runs a script on connection:
    ```bash
    # Verify glibc version >= 2.36
    ldd --version | grep -q "2.3[6-9]" || exit 1
    # Verify libicu exists
    dpkg -s libicu72 > /dev/null || exit 1
    ```
*Result: We fail fast at deployment time if the host is incompatible, rather than crashing at runtime.*

### 11.4. Addressing "No Cluster-Level Self-Healing"
**The Problem:** If a node motherboard dies, the workload doesn't move.
**Our Solution: N+1 Redundancy & Client-Side Failover**
In bare metal, you cannot "move" a workload instantly (booting a server takes minutes). We rely on **Architecture, not Orchestration**.
1.  **Smart Clients**: The `ConduitNet.Client` (and the `ConduitProxy`) is aware of the cluster topology.
2.  **Active Health Checking**: The client maintains a list of healthy nodes. If Node A stops responding (TCP Timeout), the client **immediately** retries the request on Node B.
3.  **The "Spare" Strategy**: We keep one node powered on but empty (or running low-priority jobs). If a primary node dies, the Admin runs `conduit-deploy promote --spare 192.168.1.50 --replace 192.168.1.10`.
*Result: Zero-downtime for users (due to client retries) while giving Ops time to manually replace the hardware.*



