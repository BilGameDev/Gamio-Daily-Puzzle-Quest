import { Env } from './index';
import { verifyGoogleToken } from './auth';
import { handleGetSeeds, handleGetConfig } from './seeds';
import { handleSubmitDaily, handleDeleteCompletions } from './completions';
import { handleGetStreaks } from './streaks';
import { handleGetLeaderboard, handleGetMyRank, handleGetTodayLeaderboards } from './leaderboard';
import {
  jsonResponse, errorResponse, unauthorizedResponse,
  getAuthUser, createSessionToken,
} from './utils';

export async function routeRequest(request: Request, env: Env): Promise<Response> {
  const url = new URL(request.url);
  const path = url.pathname;
  const method = request.method;

  try {
    // CORS preflight
    if (method === 'OPTIONS') {
      return new Response(null, {
        headers: {
          'Access-Control-Allow-Origin': '*',
          'Access-Control-Allow-Methods': 'GET, POST, DELETE, OPTIONS',
          'Access-Control-Allow-Headers': 'Content-Type, Authorization',
          'Access-Control-Max-Age': '86400',
        },
      });
    }

    // POST /api/auth/verify — Google token verification
    if (method === 'POST' && path === '/api/auth/verify') {
      const body: any = await request.json();
      if (!body.idToken) return errorResponse(400, 'idToken required');

      const payload = await verifyGoogleToken(body.idToken, env.GOOGLE_CLIENT_ID);
      if (!payload) return errorResponse(401, 'Invalid Google token');

      const { sub, email, name, picture } = payload;
      const userId = `user_${sub.substring(0, 12)}`;

      const displayName = name || 'Player';
      await env.DB.prepare(
        `INSERT INTO users (id, google_sub, email, display_name, username, avatar_url)
         VALUES (?, ?, ?, ?, ?, ?)
         ON CONFLICT(google_sub) DO UPDATE SET
           display_name = excluded.display_name,
           avatar_url = excluded.avatar_url,
           updated_at = datetime('now')`
      ).bind(userId, sub, email, displayName, displayName, picture || null).run();

      const user = await env.DB.prepare(
        'SELECT username FROM users WHERE id = ?'
      ).bind(userId).first<{ username: string }>();

      const token = await createSessionToken(userId, env.JWT_SECRET);

      return jsonResponse({
        userId,
        sessionToken: token,
        displayName,
        username: user?.username || displayName,
        email,
        avatarUrl: picture || null,
      });
    }

    // GET /api/config — get challenge config (no auth)
    if (method === 'GET' && path === '/api/config') {
      return handleGetConfig(env);
    }

    // Authenticated endpoints below
    const authHeader = request.headers.get('Authorization') || '';
    const user = await getAuthUser(authHeader, env.JWT_SECRET, env.DB);
    if (!user) return unauthorizedResponse();

    // GET /api/seeds — get today's challenge
    if (method === 'GET' && path === '/api/seeds') {
      return handleGetSeeds(user.id, env);
    }

    // POST /api/daily/submit — submit daily challenge completion
    if (method === 'POST' && path === '/api/daily/submit') {
      return handleSubmitDaily(user.id, request, env);
    }

    // POST /api/daily/sync — sync offline-queued completion
    if (method === 'POST' && path === '/api/daily/sync') {
      return handleSubmitDaily(user.id, request, env);
    }

    // POST /api/users/username — update display name
    if (method === 'POST' && path === '/api/users/username') {
      const body: any = await request.json();
      const { username } = body;
      if (!username || typeof username !== 'string' || username.length < 1 || username.length > 24) {
        return errorResponse(400, 'Username must be 1-24 characters');
      }
      await env.DB.prepare(
        `UPDATE users SET username = ?, updated_at = datetime('now') WHERE id = ?`
      ).bind(username.trim(), user.id).run();
      return jsonResponse({ success: true, username: username.trim() });
    }

    // DELETE /api/user/completions — reset today's completion (for testing)
    if (method === 'DELETE' && path === '/api/user/completions') {
      return handleDeleteCompletions(user.id, env);
    }

    // GET /api/streaks — get user streaks
    if (method === 'GET' && path === '/api/streaks') {
      return handleGetStreaks(user.id, env);
    }

    // GET /api/leaderboard/today — all 3 today's leaderboards
    if (method === 'GET' && path === '/api/leaderboard/today') {
      return handleGetTodayLeaderboards(env);
    }

    // GET /api/leaderboard/:challengeId — get leaderboard for a challenge
    const leaderboardMatch = path.match(/^\/api\/leaderboard\/(\d+)$/);
    if (method === 'GET' && leaderboardMatch) {
      return handleGetLeaderboard(parseInt(leaderboardMatch[1]), env);
    }

    // GET /api/leaderboard/me — get my rank on today's challenge
    if (method === 'GET' && path === '/api/leaderboard/me') {
      return handleGetMyRank(user.id, env);
    }

    return errorResponse(404, 'Not found');
  } catch (err: any) {
    console.error(`[gamio-api] ${err.stack || err.message}`);
    return errorResponse(500, err.message || 'Internal error');
  }
}
