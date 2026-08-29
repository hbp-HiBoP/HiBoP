const MAX_BODY_SIZE = 24 * 1024;
const RATE_LIMIT_WINDOW_MS = 60 * 1000;

const FIELD_CONFIGURATION = new Map([
  ["👤 Name", true],
  ["📧 Email", true],
  ["📝 Description", false],
  ["🖥️ System", true],
  ["⚙️ Hardware", true],
  ["🖼️ Display", true],
]);

const REQUIRED_FIELDS = [
  "📝 Description",
  "🖥️ System",
  "⚙️ Hardware",
  "🖼️ Display",
];

export default {
  fetch: handleRequest,
};

export class StrictRateLimiter {
  constructor(state) {
    this.storage = state.storage;
  }

  async fetch(request) {
    if (request.method !== "POST") {
      return new Response(null, { status: 405 });
    }

    const requestedLimit = Number.parseInt(
      request.headers.get("X-Rate-Limit") ?? "",
      10,
    );
    if (!Number.isInteger(requestedLimit) || requestedLimit < 1) {
      return new Response(null, { status: 400 });
    }

    const allowed = await consumeSlidingWindow(
      this.storage,
      Date.now(),
      requestedLimit,
      RATE_LIMIT_WINDOW_MS,
    );
    return new Response(null, { status: allowed ? 204 : 429 });
  }
}

export async function handleRequest(request, env) {
  try {
    const url = new URL(request.url);
    if (url.pathname !== "/report") {
      return jsonResponse("not_found", 404);
    }

    if (request.method !== "POST") {
      return jsonResponse("method_not_allowed", 405);
    }

    const contentType = request.headers.get("Content-Type") ?? "";
    if (!contentType.toLowerCase().startsWith("application/json")) {
      return jsonResponse("invalid_content_type", 415);
    }

    const installationId =
      request.headers.get("X-HiBoP-Installation") ?? "";
    if (!/^[a-f0-9]{32}$/i.test(installationId)) {
      return jsonResponse("invalid_installation", 400);
    }

    const body = await request.arrayBuffer();
    if (body.byteLength === 0 || body.byteLength > MAX_BODY_SIZE) {
      return jsonResponse("invalid_body_size", 413);
    }

    let input;
    try {
      input = JSON.parse(new TextDecoder().decode(body));
    } catch {
      return jsonResponse("invalid_json", 400);
    }

    const discordPayload = normalizePayload(input);
    if (discordPayload === null) {
      return jsonResponse("invalid_report", 400);
    }

    const clientIp = request.headers.get("CF-Connecting-IP") ?? "unknown";
    const limitsAllowRequest = await checkRateLimits(
      env,
      installationId.toLowerCase(),
      clientIp,
    );
    if (!limitsAllowRequest) {
      return jsonResponse("rate_limited", 429);
    }

    if (!isDiscordWebhookUrl(env.DISCORD_WEBHOOK_URL)) {
      return jsonResponse("relay_not_configured", 503);
    }

    const webhookUrl = new URL(env.DISCORD_WEBHOOK_URL);
    webhookUrl.searchParams.set("wait", "true");

    const discordResponse = await fetch(webhookUrl, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(discordPayload),
    });

    if (!discordResponse.ok) {
      return jsonResponse("discord_unavailable", 502);
    }

    return new Response(null, {
      status: 204,
      headers: {
        "Cache-Control": "no-store",
      },
    });
  } catch {
    return jsonResponse("relay_unavailable", 503);
  }
}

export function normalizePayload(input) {
  if (
    input === null ||
    typeof input !== "object" ||
    !Array.isArray(input.embeds) ||
    input.embeds.length !== 1
  ) {
    return null;
  }

  const source = input.embeds[0];
  if (
    source === null ||
    typeof source !== "object" ||
    typeof source.title !== "string" ||
    !/^\[Bug Report\] \d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$/.test(
      source.title,
    ) ||
    !isValidText(source.description, 4096) ||
    !Array.isArray(source.fields) ||
    source.fields.length < REQUIRED_FIELDS.length ||
    source.fields.length > FIELD_CONFIGURATION.size
  ) {
    return null;
  }

  const fields = [];
  const seenFields = new Set();

  for (const field of source.fields) {
    if (
      field === null ||
      typeof field !== "object" ||
      typeof field.name !== "string" ||
      !FIELD_CONFIGURATION.has(field.name) ||
      seenFields.has(field.name) ||
      !isValidText(field.value, 1024)
    ) {
      return null;
    }

    seenFields.add(field.name);
    fields.push({
      name: field.name,
      value: field.value,
      inline: FIELD_CONFIGURATION.get(field.name),
    });
  }

  if (REQUIRED_FIELDS.some((name) => !seenFields.has(name))) {
    return null;
  }

  return {
    allowed_mentions: {
      parse: [],
    },
    embeds: [
      {
        title: source.title,
        description: source.description,
        color: 15158332,
        timestamp: new Date().toISOString(),
        fields,
      },
    ],
  };
}

export async function checkRateLimits(env, installationId, clientIp) {
  // The edge bindings absorb floods cheaply, but are intentionally permissive
  // and eventually consistent. Durable Objects enforce the exact limits below.
  const installationLimit = await env.INSTALLATION_RATE_LIMITER.limit({
    key: installationId,
  });
  if (!installationLimit.success) {
    return false;
  }

  const ipLimit = await env.IP_RATE_LIMITER.limit({
    key: clientIp,
  });
  if (!ipLimit.success) {
    return false;
  }

  const globalLimit = await env.GLOBAL_RATE_LIMITER.limit({
    key: "all-reports",
  });
  if (!globalLimit.success) {
    return false;
  }

  const strictLimits = [
    [`installation:${installationId}`, 1],
    [`ip:${clientIp}`, 3],
    ["global:all-reports", 20],
  ];

  for (const [key, limit] of strictLimits) {
    const limiter = env.STRICT_RATE_LIMITERS.getByName(key);
    const response = await limiter.fetch("https://rate-limiter.internal/check", {
      method: "POST",
      headers: {
        "X-Rate-Limit": limit.toString(),
      },
    });
    if (response.status !== 204) {
      return false;
    }
  }

  return true;
}

export async function consumeSlidingWindow(storage, now, limit, windowMs) {
  return storage.transaction(async (transaction) => {
    const cutoff = now - windowMs;
    const storedTimestamps = (await transaction.get("timestamps")) ?? [];
    const activeTimestamps = storedTimestamps.filter(
      (timestamp) => timestamp > cutoff,
    );

    if (activeTimestamps.length >= limit) {
      if (activeTimestamps.length !== storedTimestamps.length) {
        await transaction.put("timestamps", activeTimestamps);
      }
      return false;
    }

    activeTimestamps.push(now);
    await transaction.put("timestamps", activeTimestamps);
    return true;
  });
}

export function isDiscordWebhookUrl(value) {
  if (typeof value !== "string") {
    return false;
  }

  try {
    const url = new URL(value);
    return (
      url.protocol === "https:" &&
      url.hostname === "discord.com" &&
      /^\/api(?:\/v\d+)?\/webhooks\/\d+\/[A-Za-z0-9._-]+$/.test(
        url.pathname,
      )
    );
  } catch {
    return false;
  }
}

function isValidText(value, maxLength) {
  return (
    typeof value === "string" &&
    value.length > 0 &&
    value.length <= maxLength
  );
}

function jsonResponse(error, status) {
  return new Response(JSON.stringify({ error }), {
    status,
    headers: {
      "Content-Type": "application/json",
      "Cache-Control": "no-store",
    },
  });
}
