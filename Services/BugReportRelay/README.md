# HiBoP bug report relay

This Cloudflare Worker validates and rate-limits bug reports before forwarding
them to the private Discord webhook. Strict counters are stored in SQLite-backed
Durable Objects, which are included in the Cloudflare free plan. The webhook URL
must never be stored in this repository or in the HiBoP client.

## Local verification

```powershell
pnpm install
pnpm test
pnpm exec wrangler deploy --dry-run
```

## Deployment

Authenticate once, then deploy:

```powershell
pnpm exec wrangler login
pnpm exec wrangler deploy
```

Copy the resulting `workers.dev` URL into `BUG_REPORT_RELAY_URL` in
`BugReporterWindow.cs`, with `/report` appended.

Configure the Discord webhook interactively after deployment:

```powershell
pnpm exec wrangler secret put DISCORD_WEBHOOK_URL
```

Use `pnpm exec wrangler secret list` to confirm that the binding exists without
displaying its value.

## Limits

- One report per installation every 60 seconds.
- Three reports per source IP every 60 seconds.
- Twenty reports globally every 60 seconds.

The installation identifier is not a credential. It provides a stable key for
ordinary clients, while the IP and aggregate limits contain trivial identifier
rotation.

The built-in edge limiters absorb floods. Because those counters are
intentionally approximate, each accepted request is also checked against a
strongly consistent Durable Object before Discord is contacted.
