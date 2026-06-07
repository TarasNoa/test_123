# Deprecated: browser-mcp-server (Node lane)

As of **O2.5 MCP Consolidation**, Libr4 routes `browser.smoke` and `browser.auth` through **Obscura native** (`IObscuraMcpBridge` → `IAgentObscuraTool`).

## Migration

Set in `AutonomousAppGeneration:AgentIntegration:Mcp:BrowserLane`:

```json
"BrowserLane": {
  "Provider": "Obscura"
}
```

Remove `ServerProfiles:browser-lane` (Node `browser-mcp-server`) from host configuration.

## Legacy fallback

To temporarily use the Node MCP server, set `"Provider": "Node"` and restore a `browser-lane` server profile pointing at `browser-mcp-server/server.js`.

This path is deprecated and will be removed in a later release.
