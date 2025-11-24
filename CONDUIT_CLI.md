# Conduit CLI (`cn`) Design

The Conduit CLI (`cn`) is the unified command-line interface for managing the entire Conduit ecosystem, from development to deployment. It follows a modern, hierarchical command structure similar to the Azure CLI (`az`).

## 1. Core Principles

*   **Unified Tool**: A single binary (`cn`) replaces multiple standalone tools.
*   **Noun-Verb Structure**: Commands are organized by resource (Group) then action (Command).
    *   Format: `cn <group> [subgroup] <command> [arguments] [flags]`
    *   Example: `cn registry publish`
*   **Output Formats**: Supports multiple output formats for human readability or machine parsing.
    *   `--output table` (Default): Human-readable ASCII table.
    *   `--output json`: Minified JSON for piping to `jq`.
    *   `--output tsv`: Tab-separated values.
*   **Idempotency**: Commands should be idempotent where possible.

## 2. Command Groups

### `co registry` (alias: `reg`)
Manages packages and interactions with the Conduit Service Registry.

*   `co registry login <url>`: Authenticate with a registry.
*   `co registry publish`: Build and push the current project as a `.cnp`.
*   `co registry install <package>`: Download and extract a package.
*   `co registry list`: Search or list packages.
*   `co registry show <package>`: Show details/metadata for a package.

### `co node`
Manages local or remote Conduit Nodes.

*   `co node start`: Start a node in the current directory.
*   `co node stop`: Stop a running node.
*   `co node status`: Check health/metrics of a node.
*   `co node connect <address>`: Connect CLI to a remote node for management.

### `co service` (alias: `svc`)
Manages running services within a node.

*   `co service list`: List running services.
*   `co service logs <service-id>`: Stream logs from a specific service.
*   `co service restart <service-id>`: Restart a specific service.

### `co deploy`
Manages deployment to bare-metal infrastructure.

*   `co deploy init`: Create a deployment configuration.
*   `co deploy apply`: Apply the configuration to target servers (SSH).

### `co config`
Manages CLI configuration.

*   `co config set <key> <value>`
*   `co config get <key>`

## 3. Global Flags

*   `--verbose`: Enable debug logging.
*   `--output, -o`: Output format (`json`, `table`, `tsv`).
*   `--help, -h`: Show help message.
*   `--version, -v`: Show CLI version.

## 4. Implementation Details

*   **Language**: C# (Native AOT) or Go (for zero-dependency distribution). Given the ecosystem is .NET, C# Native AOT is preferred.
*   **Autocomplete**: Shell completion scripts for PowerShell, Bash, and Zsh.
