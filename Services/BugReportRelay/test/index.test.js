import assert from "node:assert/strict";
import test from "node:test";

import {
  checkRateLimits,
  consumeSlidingWindow,
  isDiscordWebhookUrl,
  normalizePayload,
  StrictRateLimiter,
} from "../src/index.js";

function createValidPayload() {
  return {
    allowed_mentions: {
      parse: ["everyone"],
    },
    embeds: [
      {
        title: "[Bug Report] 2026-08-29 12:34:56",
        description: "```\ndiagnostic\n```",
        color: 0,
        timestamp: "invalid",
        fields: [
          {
            name: "📝 Description",
            value: "A description",
            inline: true,
          },
          {
            name: "🖥️ System",
            value: "A system",
            inline: false,
          },
          {
            name: "⚙️ Hardware",
            value: "Hardware",
            inline: false,
          },
          {
            name: "🖼️ Display",
            value: "Display",
            inline: false,
          },
        ],
      },
    ],
  };
}

test("normalizePayload accepts the HiBoP report shape and forces safe Discord options", () => {
  const result = normalizePayload(createValidPayload());

  assert.notEqual(result, null);
  assert.deepEqual(result.allowed_mentions, { parse: [] });
  assert.equal(result.embeds[0].color, 15158332);
  assert.equal(result.embeds[0].fields[0].inline, false);
  assert.equal(result.embeds[0].fields[1].inline, true);
});

test("normalizePayload rejects unknown and duplicate fields", () => {
  const unknownFieldPayload = createValidPayload();
  unknownFieldPayload.embeds[0].fields[0].name = "Unexpected";
  assert.equal(normalizePayload(unknownFieldPayload), null);

  const duplicateFieldPayload = createValidPayload();
  duplicateFieldPayload.embeds[0].fields[1].name = "📝 Description";
  assert.equal(normalizePayload(duplicateFieldPayload), null);
});

test("normalizePayload rejects malformed titles and oversized values", () => {
  const malformedTitlePayload = createValidPayload();
  malformedTitlePayload.embeds[0].title = "Anything";
  assert.equal(normalizePayload(malformedTitlePayload), null);

  const oversizedValuePayload = createValidPayload();
  oversizedValuePayload.embeds[0].fields[0].value = "x".repeat(1025);
  assert.equal(normalizePayload(oversizedValuePayload), null);
});

test("checkRateLimits stops at the first rejected limiter", async () => {
  const calls = [];
  const env = {
    INSTALLATION_RATE_LIMITER: createLimiter("installation", true, calls),
    IP_RATE_LIMITER: createLimiter("ip", false, calls),
    GLOBAL_RATE_LIMITER: createLimiter("global", true, calls),
  };

  assert.equal(await checkRateLimits(env, "install", "ip"), false);
  assert.deepEqual(calls, [
    ["installation", "install"],
    ["ip", "ip"],
  ]);
});

test("checkRateLimits applies all strict limits after the edge limits", async () => {
  const calls = [];
  const env = {
    INSTALLATION_RATE_LIMITER: createLimiter("edge-installation", true, calls),
    IP_RATE_LIMITER: createLimiter("edge-ip", true, calls),
    GLOBAL_RATE_LIMITER: createLimiter("edge-global", true, calls),
    STRICT_RATE_LIMITERS: {
      getByName(name) {
        return {
          async fetch(_url, options) {
            calls.push([
              name,
              Number.parseInt(options.headers["X-Rate-Limit"], 10),
            ]);
            return new Response(null, { status: 204 });
          },
        };
      },
    },
  };

  assert.equal(await checkRateLimits(env, "install", "ip"), true);
  assert.deepEqual(calls, [
    ["edge-installation", "install"],
    ["edge-ip", "ip"],
    ["edge-global", "all-reports"],
    ["installation:install", 1],
    ["ip:ip", 3],
    ["global:all-reports", 20],
  ]);
});

test("consumeSlidingWindow enforces an exact rolling limit", async () => {
  const storage = createStorage();

  assert.equal(await consumeSlidingWindow(storage, 1_000, 1, 60_000), true);
  assert.equal(await consumeSlidingWindow(storage, 1_001, 1, 60_000), false);
  assert.equal(await consumeSlidingWindow(storage, 61_001, 1, 60_000), true);
});

test("StrictRateLimiter rejects the second immediate request", async () => {
  const limiter = new StrictRateLimiter({ storage: createStorage() });
  const request = new Request("https://rate-limiter.internal/check", {
    method: "POST",
    headers: { "X-Rate-Limit": "1" },
  });

  assert.equal((await limiter.fetch(request)).status, 204);
  assert.equal((await limiter.fetch(request)).status, 429);
});

test("isDiscordWebhookUrl accepts only Discord webhook endpoints", () => {
  assert.equal(
    isDiscordWebhookUrl(
      "https://discord.com/api/webhooks/123456/token-value",
    ),
    true,
  );
  assert.equal(
    isDiscordWebhookUrl(
      "https://example.com/api/webhooks/123456/token-value",
    ),
    false,
  );
  assert.equal(isDiscordWebhookUrl("not a URL"), false);
});

function createLimiter(name, success, calls) {
  return {
    async limit({ key }) {
      calls.push([name, key]);
      return { success };
    },
  };
}

function createStorage() {
  const values = new Map();
  return {
    async transaction(action) {
      return action({
        async get(key) {
          return values.get(key);
        },
        async put(key, value) {
          values.set(key, structuredClone(value));
        },
      });
    },
  };
}
