# Codex Notes For HiBoP

## Unity MCP Workflow

This Unity project is expected to be driven through MCP for Unity when Unity is open.

Before using Unity tools, discover the Unity MCP tools if they are not already visible:

```text
tool_search: unity MCP run_tests get_test_job editor state instances
```

The expected MCP server is:

```toml
[mcp_servers.unityMCP]
url = "http://127.0.0.1:8080/mcp"
```

If the Unity MCP package/configuration was just added, Codex may need to be restarted before the `mcp__unityMCP` tools appear.

### Resource-First Checks

Always read Unity resources before acting:

```text
mcpforunity://custom-tools
mcpforunity://instances
mcpforunity://editor/state
mcpforunity://project/info
```

If multiple Unity instances are connected, use:

```text
set_active_instance(instance="HiBoP@...")
```

For this project, the expected Unity instance is named `HiBoP` and currently uses Unity `6000.5.1f1`.

### Running Tests

Use Unity MCP instead of launching Unity batchmode when the editor is open.

Typical EditMode serialization run:

```text
run_tests(
  mode="EditMode",
  assembly_names=["HBP.Serialization.Tests"],
  include_failed_tests=true,
  include_details=false,
  init_timeout=30000
)
```

Then poll:

```text
get_test_job(
  job_id="<job_id>",
  wait_timeout=60,
  include_failed_tests=true,
  include_details=false
)
```

Before and after the run, check the console:

```text
read_console(action="get", types=["error"], count="10", format="detailed", include_stacktrace=true)
```

### Preferred Tools

Prefer MCP for Unity when the editor is open:

```text
mcp__unityMCP.run_tests
mcp__unityMCP.get_test_job
mcp__unityMCP.read_console
```

Batchmode Unity test scripts are only a fallback for CI or closed-editor runs, not the preferred local workflow.
