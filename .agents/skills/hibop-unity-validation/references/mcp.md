# When the editor is open: use Unity MCP

Prefer MCP over batchmode for local validation while the editor is open.

Before acting, inspect the available Unity resources/state, especially:

```text
mcpforunity://custom-tools
mcpforunity://instances
mcpforunity://editor/state
mcpforunity://project/info
```

If multiple Unity instances are connected, explicitly select the HiBoP instance
before running commands.

Run the narrowest relevant EditMode or PlayMode tests. After starting a test job,
poll it to completion rather than assuming the initial call is the result.

Before and after the run, inspect console errors. A passing test result does not
make new Unity console errors irrelevant.

If the Unity MCP tools are unexpectedly unavailable after MCP configuration was
changed, a Codex restart may be required. The expected local MCP endpoint for this
project is:

```toml
[mcp_servers.unityMCP]
url = "http://127.0.0.1:8080/mcp"
```

