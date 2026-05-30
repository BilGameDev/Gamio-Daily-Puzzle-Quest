var __defProp = Object.defineProperty;
var __name = (target, value) => __defProp(target, "name", { value, configurable: true });

// src/auth.ts
var cachedCerts = null;
var certsExpiry = 0;
async function fetchGoogleCerts() {
  if (cachedCerts && Date.now() / 1e3 < certsExpiry) {
    return cachedCerts;
  }
  const resp = await fetch("https://www.googleapis.com/oauth2/v3/certs");
  const data = await resp.json();
  const keys = {};
  for (const key of data.keys) {
    const jwk = {
      kty: key.kty,
      n: key.n,
      e: key.e,
      alg: "RS256"
    };
    const cryptoKey = await crypto.subtle.importKey(
      "jwk",
      jwk,
      { name: "RSASSA-PKCS1-v1_5", hash: "SHA-256" },
      false,
      ["verify"]
    );
    keys[key.kid] = cryptoKey;
  }
  cachedCerts = keys;
  certsExpiry = Math.floor(Date.now() / 1e3) + 3600;
  return keys;
}
__name(fetchGoogleCerts, "fetchGoogleCerts");
async function verifyGoogleToken(idToken, clientId) {
  try {
    const parts = idToken.split(".");
    if (parts.length !== 3) return null;
    const header = JSON.parse(atob(parts[0]));
    const kid = header.kid;
    const keys = await fetchGoogleCerts();
    const key = keys[kid];
    if (!key) return null;
    const data = new TextEncoder().encode(`${parts[0]}.${parts[1]}`);
    const sig = base64urlToBytes(parts[2]);
    const valid = await crypto.subtle.verify("RSASSA-PKCS1-v1_5", key, sig, data);
    if (!valid) return null;
    const payload = JSON.parse(atob(parts[1]));
    if (payload.aud !== clientId) return null;
    if (payload.exp < Math.floor(Date.now() / 1e3)) return null;
    return payload;
  } catch (err) {
    console.error("[auth] verifyGoogleToken error:", err);
    return null;
  }
}
__name(verifyGoogleToken, "verifyGoogleToken");
function base64urlToBytes(str) {
  str = str.replace(/-/g, "+").replace(/_/g, "/");
  while (str.length % 4) str += "=";
  const binary = atob(str);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
  return bytes.buffer;
}
__name(base64urlToBytes, "base64urlToBytes");

// src/utils.ts
function jsonResponse(data, status = 200) {
  return new Response(JSON.stringify(data), {
    status,
    headers: {
      "Content-Type": "application/json",
      "Access-Control-Allow-Origin": "*"
    }
  });
}
__name(jsonResponse, "jsonResponse");
function errorResponse(status, message) {
  return jsonResponse({ error: message }, status);
}
__name(errorResponse, "errorResponse");
function unauthorizedResponse() {
  return errorResponse(401, "Unauthorized");
}
__name(unauthorizedResponse, "unauthorizedResponse");
async function createSessionToken(userId, secret) {
  const encoder = new TextEncoder();
  const issuedAt = Math.floor(Date.now() / 1e3);
  const expiresAt = issuedAt + 86400 * 30;
  const payload = `${userId}:${issuedAt}:${expiresAt}`;
  const key = await crypto.subtle.importKey(
    "raw",
    encoder.encode(secret),
    { name: "HMAC", hash: "SHA-256" },
    false,
    ["sign"]
  );
  const sig = await crypto.subtle.sign("HMAC", key, encoder.encode(payload));
  const mac = Array.from(new Uint8Array(sig)).map((b) => b.toString(16).padStart(2, "0")).join("");
  return `${base64urlEncode(payload)}.${mac}`;
}
__name(createSessionToken, "createSessionToken");
async function verifySessionToken(token, secret) {
  try {
    const parts = token.split(".");
    if (parts.length !== 2) return null;
    const payloadBytes = base64urlDecode(parts[0]);
    const payload = new TextDecoder().decode(payloadBytes);
    const providedMac = parts[1];
    const key = await crypto.subtle.importKey(
      "raw",
      new TextEncoder().encode(secret),
      { name: "HMAC", hash: "SHA-256" },
      false,
      ["sign"]
    );
    const sig = await crypto.subtle.sign("HMAC", key, new TextEncoder().encode(payload));
    const expectedMac = Array.from(new Uint8Array(sig)).map((b) => b.toString(16).padStart(2, "0")).join("");
    if (providedMac !== expectedMac) return null;
    const [userId, _issuedAt, expiresAtStr] = payload.split(":");
    const expiresAt = parseInt(expiresAtStr);
    if (Date.now() / 1e3 > expiresAt) return null;
    return { userId, expiresAt };
  } catch {
    return null;
  }
}
__name(verifySessionToken, "verifySessionToken");
async function getAuthUser(authHeader, jwtSecret, db) {
  if (!authHeader.startsWith("Bearer ")) return null;
  const token = authHeader.slice(7);
  const session = await verifySessionToken(token, jwtSecret);
  if (!session) return null;
  const user = await db.prepare("SELECT id FROM users WHERE id = ?").bind(session.userId).first();
  return user || null;
}
__name(getAuthUser, "getAuthUser");
function base64urlEncode(str) {
  return btoa(str).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}
__name(base64urlEncode, "base64urlEncode");
function base64urlDecode(str) {
  str = str.replace(/-/g, "+").replace(/_/g, "/");
  while (str.length % 4) str += "=";
  const binary = atob(str);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) {
    bytes[i] = binary.charCodeAt(i);
  }
  return bytes;
}
__name(base64urlDecode, "base64urlDecode");

// src/seeds.ts
var DEFAULT_GAME_TYPES = ["Sudoku", "Hitori", "Pipes", "Shikaku", "Kings", "LineConnect", "WordGrid", "WordSearch", "Arrows"];
var DEFAULT_PUZZLE_COUNT = 4;
function getGameTypes(env) {
  const raw = env.CHALLENGE_GAME_TYPES;
  if (!raw) return DEFAULT_GAME_TYPES;
  return raw.split(",").map((s) => s.trim()).filter(Boolean);
}
__name(getGameTypes, "getGameTypes");
function getPuzzleCount(env) {
  const raw = env.CHALLENGE_PUZZLE_COUNT;
  if (!raw) return DEFAULT_PUZZLE_COUNT;
  const n = parseInt(raw, 10);
  return isNaN(n) || n < 1 ? DEFAULT_PUZZLE_COUNT : n;
}
__name(getPuzzleCount, "getPuzzleCount");
async function generateDailySeeds(env) {
  const today = (/* @__PURE__ */ new Date()).toISOString().split("T")[0];
  const seeds = await computeSeeds(today, env.SEED_HMAC_KEY);
  await env.DB.prepare(
    `INSERT OR IGNORE INTO daily_seeds (date, seed_a, seed_b, seed_c)
     VALUES (?, ?, ?, ?)`
  ).bind(today, seeds[0], seeds[1], seeds[2]).run();
}
__name(generateDailySeeds, "generateDailySeeds");
async function computeSeeds(date, hmacKey) {
  const encoder = new TextEncoder();
  const key = await crypto.subtle.importKey(
    "raw",
    encoder.encode(hmacKey),
    { name: "HMAC", hash: "SHA-256" },
    false,
    ["sign"]
  );
  const seeds = [];
  for (let i = 0; i < 3; i++) {
    const data = `${date}-${i}`;
    const sig = await crypto.subtle.sign("HMAC", key, encoder.encode(data));
    const hash = Array.from(new Uint8Array(sig)).map((b) => b.toString(16).padStart(2, "0")).join("");
    seeds.push(hash.substring(0, 16));
  }
  return seeds;
}
__name(computeSeeds, "computeSeeds");
async function handleGetSeeds(userId, env) {
  const today = (/* @__PURE__ */ new Date()).toISOString().split("T")[0];
  let seedRow = await env.DB.prepare(
    "SELECT id, seed_a, seed_b, seed_c FROM daily_seeds WHERE date = ?"
  ).bind(today).first();
  if (!seedRow) {
    await generateDailySeeds(env);
    seedRow = await env.DB.prepare(
      "SELECT id, seed_a, seed_b, seed_c FROM daily_seeds WHERE date = ?"
    ).bind(today).first();
    if (!seedRow) {
      return errorResponse(500, "Failed to create daily seeds");
    }
  }
  let assignment = await env.DB.prepare(
    `SELECT id, seed_index FROM daily_assignments
     WHERE user_id = ? AND seed_id = ?`
  ).bind(userId, seedRow.id).first();
  let seedIndex;
  let assignmentId;
  if (assignment) {
    seedIndex = assignment.seed_index;
    assignmentId = assignment.id;
  } else {
    const hash = simpleHash(userId + today);
    seedIndex = hash % 3;
    const result = await env.DB.prepare(
      `INSERT INTO daily_assignments (user_id, seed_id, seed_index)
       VALUES (?, ?, ?)`
    ).bind(userId, seedRow.id, seedIndex).run();
    assignmentId = Number(result.meta.last_row_id);
  }
  const seed = seedIndex === 0 ? seedRow.seed_a : seedIndex === 1 ? seedRow.seed_b : seedRow.seed_c;
  const completions = await env.DB.prepare(
    `SELECT puzzle_index, game_type, time_seconds FROM puzzle_completions
     WHERE assignment_id = ?
     ORDER BY puzzle_index`
  ).bind(assignmentId).all();
  const dailyDone = await env.DB.prepare(
    "SELECT id, total_time_seconds FROM daily_completions WHERE assignment_id = ?"
  ).bind(assignmentId).first();
  const streak = await env.DB.prepare(
    "SELECT current_streak, longest_streak FROM streaks WHERE user_id = ?"
  ).bind(userId).first();
  const gameTypes = getGameTypes(env);
  const puzzleCount = getPuzzleCount(env);
  const assignedGames = pickGamesForSeed(seed, puzzleCount, gameTypes);
  const completedPuzzles = new Map(completions.results.map((c) => [c.puzzle_index, c]));
  const games = assignedGames.map((gameType, idx) => {
    const done = completedPuzzles.get(idx);
    return {
      puzzleIndex: idx,
      gameType,
      completed: !!done,
      timeSeconds: done ? done.time_seconds : null
    };
  });
  return jsonResponse({
    date: today,
    seedId: seedRow.id,
    seed,
    seedIndex,
    assignmentId,
    games,
    dailyCompleted: !!dailyDone,
    totalTimeSeconds: dailyDone ? dailyDone.total_time_seconds : null,
    streak: {
      current: streak?.current_streak || 0,
      longest: streak?.longest_streak || 0
    }
  });
}
__name(handleGetSeeds, "handleGetSeeds");
function handleGetConfig(env) {
  return jsonResponse({
    gameTypes: getGameTypes(env),
    puzzleCount: getPuzzleCount(env)
  });
}
__name(handleGetConfig, "handleGetConfig");
function simpleHash(str) {
  let hash = 0;
  for (let i = 0; i < str.length; i++) {
    const chr = str.charCodeAt(i);
    hash = (hash << 5) - hash + chr;
    hash |= 0;
  }
  return Math.abs(hash);
}
__name(simpleHash, "simpleHash");
function pickGamesForSeed(seed, count, gameTypes) {
  const shuffled = [...gameTypes];
  let seedNum = 0;
  for (let i = 0; i < seed.length; i++) {
    seedNum = seedNum * 31 + seed.charCodeAt(i) >>> 0;
  }
  for (let i = shuffled.length - 1; i > 0; i--) {
    seedNum = seedNum * 1103515245 + 12345 >>> 0;
    const j = seedNum % (i + 1);
    [shuffled[i], shuffled[j]] = [shuffled[j], shuffled[i]];
  }
  return shuffled.slice(0, count);
}
__name(pickGamesForSeed, "pickGamesForSeed");

// src/completions.ts
async function handleRecordPuzzle(userId, request, env) {
  const body = await request.json();
  const { assignmentId, puzzleIndex, gameType, timeSeconds } = body;
  if (assignmentId == null || puzzleIndex == null || !gameType || timeSeconds == null) {
    return errorResponse(400, "Missing required fields: assignmentId, puzzleIndex, gameType, timeSeconds");
  }
  if (timeSeconds < 1) {
    return errorResponse(400, "Invalid time");
  }
  const assignment = await env.DB.prepare(
    "SELECT id FROM daily_assignments WHERE id = ? AND user_id = ?"
  ).bind(assignmentId, userId).first();
  if (!assignment) {
    return errorResponse(403, "Assignment not found or does not belong to user");
  }
  const existing = await env.DB.prepare(
    "SELECT id FROM puzzle_completions WHERE assignment_id = ? AND puzzle_index = ?"
  ).bind(assignmentId, puzzleIndex).first();
  if (existing) {
    return errorResponse(409, "Puzzle already completed");
  }
  await env.DB.prepare(
    `INSERT INTO puzzle_completions (user_id, assignment_id, puzzle_index, game_type, time_seconds)
     VALUES (?, ?, ?, ?, ?)`
  ).bind(userId, assignmentId, puzzleIndex, gameType, timeSeconds).run();
  return jsonResponse({ success: true });
}
__name(handleRecordPuzzle, "handleRecordPuzzle");
async function handleSubmitDaily(userId, request, env) {
  const body = await request.json();
  const { assignmentId, puzzles } = body;
  if (assignmentId == null || !puzzles || !Array.isArray(puzzles) || puzzles.length === 0) {
    return errorResponse(400, "Missing required fields: assignmentId, puzzles[]");
  }
  const assignment = await env.DB.prepare(
    `SELECT da.id, da.seed_id, ds.date FROM daily_assignments da
     JOIN daily_seeds ds ON ds.id = da.seed_id
     WHERE da.id = ? AND da.user_id = ?`
  ).bind(assignmentId, userId).first();
  if (!assignment) {
    return errorResponse(403, "Assignment not found");
  }
  let totalTime = 0;
  const inserted = [];
  for (const puzzle of puzzles) {
    const { puzzleIndex, gameType, timeSeconds } = puzzle;
    if (puzzleIndex == null || !gameType || timeSeconds == null || timeSeconds <= 0) {
      continue;
    }
    const existing = await env.DB.prepare(
      "SELECT id FROM puzzle_completions WHERE assignment_id = ? AND puzzle_index = ?"
    ).bind(assignmentId, puzzleIndex).first();
    if (!existing) {
      await env.DB.prepare(
        `INSERT INTO puzzle_completions (user_id, assignment_id, puzzle_index, game_type, time_seconds)
         VALUES (?, ?, ?, ?, ?)`
      ).bind(userId, assignmentId, puzzleIndex, gameType, timeSeconds).run();
    }
    totalTime += timeSeconds;
    inserted.push({ puzzleIndex, gameType, timeSeconds });
  }
  const existingDaily = await env.DB.prepare(
    "SELECT id FROM daily_completions WHERE assignment_id = ?"
  ).bind(assignmentId).first();
  if (existingDaily) {
    return jsonResponse({
      success: true,
      alreadyCompleted: true,
      totalTimeSeconds: totalTime,
      puzzlesSynced: inserted.length
    });
  }
  let streak = await env.DB.prepare(
    "SELECT current_streak, longest_streak, last_completed_date FROM streaks WHERE user_id = ?"
  ).first();
  let newStreak = 1;
  if (streak) {
    const lastDate = streak.last_completed_date;
    const today = assignment.date;
    const yesterday = getYesterday(today);
    if (lastDate === yesterday) {
      newStreak = streak.current_streak + 1;
    } else if (lastDate === today) {
      newStreak = streak.current_streak;
    } else {
      newStreak = 1;
    }
  }
  const longestStreak = Math.max(
    newStreak,
    streak?.current_streak || 0,
    streak?.longest_streak || 0
  );
  await env.DB.prepare(
    `INSERT INTO streaks (user_id, current_streak, longest_streak, last_completed_date, updated_at)
     VALUES (?, ?, ?, ?, ?)
     ON CONFLICT(user_id) DO UPDATE SET
       current_streak = excluded.current_streak,
       longest_streak = excluded.longest_streak,
       last_completed_date = excluded.last_completed_date,
       updated_at = excluded.updated_at`
  ).bind(userId, newStreak, longestStreak, assignment.date, now()).run();
  await env.DB.prepare(
    `INSERT OR IGNORE INTO daily_completions
       (user_id, assignment_id, seed_id, total_time_seconds, streak_count, completed_at)
     VALUES (?, ?, ?, ?, ?, ?)`
  ).bind(userId, assignmentId, assignment.seed_id, totalTime, newStreak, now()).run();
  return jsonResponse({
    success: true,
    alreadyCompleted: false,
    totalTimeSeconds: totalTime,
    streak: {
      current: newStreak,
      longest: longestStreak
    },
    puzzlesSynced: inserted.length
  });
}
__name(handleSubmitDaily, "handleSubmitDaily");
async function handleDeleteCompletions(userId, env) {
  const today = (/* @__PURE__ */ new Date()).toISOString().split("T")[0];
  const seedRow = await env.DB.prepare(
    "SELECT id FROM daily_seeds WHERE date = ?"
  ).bind(today).first();
  if (!seedRow) {
    return jsonResponse({ success: true, deleted: false, reason: "No seed for today" });
  }
  const assignment = await env.DB.prepare(
    "SELECT id FROM daily_assignments WHERE user_id = ? AND seed_id = ?"
  ).bind(userId, seedRow.id).first();
  if (!assignment) {
    return jsonResponse({ success: true, deleted: false, reason: "No assignment for today" });
  }
  await env.DB.prepare(
    "DELETE FROM puzzle_completions WHERE assignment_id = ?"
  ).bind(assignment.id).run();
  await env.DB.prepare(
    "DELETE FROM daily_completions WHERE assignment_id = ?"
  ).bind(assignment.id).run();
  return jsonResponse({ success: true, deleted: true });
}
__name(handleDeleteCompletions, "handleDeleteCompletions");
function now() {
  return (/* @__PURE__ */ new Date()).toISOString().replace("T", " ").split(".")[0];
}
__name(now, "now");
function getYesterday(dateStr) {
  const date = /* @__PURE__ */ new Date(dateStr + "T00:00:00Z");
  date.setUTCDate(date.getUTCDate() - 1);
  return date.toISOString().split("T")[0];
}
__name(getYesterday, "getYesterday");

// src/streaks.ts
async function handleGetStreaks(userId, env) {
  const streak = await env.DB.prepare(
    "SELECT current_streak, longest_streak, last_completed_date FROM streaks WHERE user_id = ?"
  ).bind(userId).first();
  const recentCompletions = await env.DB.prepare(
    `SELECT ds.date, dc.total_time_seconds
     FROM daily_completions dc
     JOIN daily_seeds ds ON ds.id = dc.seed_id
     WHERE dc.user_id = ?
     ORDER BY ds.date DESC
     LIMIT 30`
  ).bind(userId).all();
  const completionDates = new Set(recentCompletions.results.map((r) => r.date));
  return jsonResponse({
    current: streak?.current_streak || 0,
    longest: streak?.longest_streak || 0,
    lastCompletedDate: streak?.last_completed_date || null,
    recentCompletions: recentCompletions.results,
    completionDates: Array.from(completionDates).sort().reverse()
  });
}
__name(handleGetStreaks, "handleGetStreaks");

// src/leaderboard.ts
async function handleGetLeaderboard(seedId, env) {
  const entries = await env.DB.prepare(
    `SELECT
       u.id AS userId,
       COALESCE(NULLIF(u.username, ''), u.display_name) AS displayName,
       u.avatar_url AS avatarUrl,
       dc.total_time_seconds AS timeSeconds,
       dc.streak_count AS streakCount,
       dc.completed_at AS completedAt
     FROM daily_completions dc
     JOIN users u ON u.id = dc.user_id
     WHERE dc.seed_id = ?
     ORDER BY dc.total_time_seconds ASC
     LIMIT 100`
  ).bind(seedId).all();
  const ranked = entries.results.map((entry, idx) => ({
    rank: idx + 1,
    ...entry
  }));
  return jsonResponse({
    seedId,
    totalParticipants: entries.results.length,
    entries: ranked
  });
}
__name(handleGetLeaderboard, "handleGetLeaderboard");
async function handleGetMyRank(userId, env) {
  const today = (/* @__PURE__ */ new Date()).toISOString().split("T")[0];
  const seeds = await env.DB.prepare(
    "SELECT id, seed_a, seed_b, seed_c FROM daily_seeds WHERE date = ?"
  ).bind(today).all();
  const results = [];
  for (const seed of seeds.results) {
    const myEntry = await env.DB.prepare(
      `SELECT dc.total_time_seconds, dc.streak_count, dc.completed_at
       FROM daily_completions dc
       WHERE dc.user_id = ? AND dc.seed_id = ?`
    ).bind(userId, seed.id).first();
    if (!myEntry) continue;
    const rankResult = await env.DB.prepare(
      `SELECT COUNT(*) as count FROM daily_completions
       WHERE seed_id = ? AND total_time_seconds < ?`
    ).bind(seed.id, myEntry.total_time_seconds).first();
    const rank = (rankResult?.count || 0) + 1;
    const totalResult = await env.DB.prepare(
      "SELECT COUNT(*) as count FROM daily_completions WHERE seed_id = ?"
    ).bind(seed.id).first();
    results.push({
      seedId: seed.id,
      rank,
      totalParticipants: totalResult?.count || 0,
      timeSeconds: myEntry.total_time_seconds,
      streakCount: myEntry.streak_count,
      completedAt: myEntry.completed_at
    });
  }
  return jsonResponse({
    userId,
    rankings: results
  });
}
__name(handleGetMyRank, "handleGetMyRank");

// src/router.ts
async function routeRequest(request, env) {
  const url = new URL(request.url);
  const path = url.pathname;
  const method = request.method;
  try {
    if (method === "OPTIONS") {
      return new Response(null, {
        headers: {
          "Access-Control-Allow-Origin": "*",
          "Access-Control-Allow-Methods": "GET, POST, DELETE, OPTIONS",
          "Access-Control-Allow-Headers": "Content-Type, Authorization",
          "Access-Control-Max-Age": "86400"
        }
      });
    }
    if (method === "POST" && path === "/api/auth/verify") {
      const body = await request.json();
      if (!body.idToken) return errorResponse(400, "idToken required");
      const payload = await verifyGoogleToken(body.idToken, env.GOOGLE_CLIENT_ID);
      if (!payload) return errorResponse(401, "Invalid Google token");
      const { sub, email, name, picture } = payload;
      const userId = `user_${sub.substring(0, 12)}`;
      const displayName = name || "Player";
      await env.DB.prepare(
        `INSERT INTO users (id, google_sub, email, display_name, username, avatar_url)
         VALUES (?, ?, ?, ?, ?, ?)
         ON CONFLICT(google_sub) DO UPDATE SET
           display_name = excluded.display_name,
           avatar_url = excluded.avatar_url,
           updated_at = datetime('now')`
      ).bind(userId, sub, email, displayName, displayName, picture || null).run();
      const user2 = await env.DB.prepare(
        "SELECT username FROM users WHERE id = ?"
      ).bind(userId).first();
      const token = await createSessionToken(userId, env.JWT_SECRET);
      return jsonResponse({
        userId,
        sessionToken: token,
        displayName,
        username: user2?.username || displayName,
        email,
        avatarUrl: picture || null
      });
    }
    if (method === "GET" && path === "/api/config") {
      return handleGetConfig(env);
    }
    const authHeader = request.headers.get("Authorization") || "";
    const user = await getAuthUser(authHeader, env.JWT_SECRET, env.DB);
    if (!user) return unauthorizedResponse();
    if (method === "GET" && path === "/api/seeds") {
      return handleGetSeeds(user.id, env);
    }
    if (method === "POST" && path === "/api/puzzles/complete") {
      return handleRecordPuzzle(user.id, request, env);
    }
    if (method === "POST" && path === "/api/daily/submit") {
      return handleSubmitDaily(user.id, request, env);
    }
    if (method === "POST" && path === "/api/daily/sync") {
      return handleSubmitDaily(user.id, request, env);
    }
    if (method === "POST" && path === "/api/users/username") {
      const body = await request.json();
      const { username } = body;
      if (!username || typeof username !== "string" || username.length < 1 || username.length > 24) {
        return errorResponse(400, "Username must be 1-24 characters");
      }
      await env.DB.prepare(
        `UPDATE users SET username = ?, updated_at = datetime('now') WHERE id = ?`
      ).bind(username.trim(), user.id).run();
      return jsonResponse({ success: true, username: username.trim() });
    }
    if (method === "DELETE" && path === "/api/user/completions") {
      return handleDeleteCompletions(user.id, env);
    }
    if (method === "GET" && path === "/api/streaks") {
      return handleGetStreaks(user.id, env);
    }
    const leaderboardMatch = path.match(/^\/api\/leaderboard\/(\d+)$/);
    if (method === "GET" && leaderboardMatch) {
      return handleGetLeaderboard(parseInt(leaderboardMatch[1]), env);
    }
    if (method === "GET" && path === "/api/leaderboard/me") {
      return handleGetMyRank(user.id, env);
    }
    return errorResponse(404, "Not found");
  } catch (err) {
    console.error(`[gamio-api] ${err.stack || err.message}`);
    return errorResponse(500, err.message || "Internal error");
  }
}
__name(routeRequest, "routeRequest");

// src/index.ts
var index_default = {
  async fetch(request, env) {
    return routeRequest(request, env);
  },
  async scheduled(event, env) {
    if (event.cron === "0 0 * * *") {
      await generateDailySeeds(env);
    }
  }
};
export {
  index_default as default
};
//# sourceMappingURL=index.js.map
