import { Env } from './index';
import { jsonResponse } from './utils';

export async function handleGetLeaderboard(challengeId: number, env: Env): Promise<Response> {
  const entries = await env.DB.prepare(
    `SELECT
       u.id AS userId,
       COALESCE(NULLIF(u.username, ''), u.display_name) AS displayName,
       u.avatar_url AS avatarUrl,
       uc.time_seconds AS timeSeconds,
       0 AS streakCount,
       uc.completed_at AS completedAt
     FROM user_challenges uc
     JOIN users u ON u.id = uc.user_id
     WHERE uc.challenge_id = ?
     ORDER BY uc.time_seconds ASC
     LIMIT 100`
  ).bind(challengeId).all<{
    userId: string;
    displayName: string;
    avatarUrl: string | null;
    timeSeconds: number;
    streakCount: number;
    completedAt: string;
  }>();

  const ranked = entries.results.map((entry, idx) => ({
    rank: idx + 1,
    ...entry,
  }));

  return jsonResponse({
    seedId: challengeId,
    totalParticipants: entries.results.length,
    entries: ranked,
  });
}

export async function handleGetMyRank(userId: string, env: Env): Promise<Response> {
  const today = new Date().toISOString().split('T')[0];

  const challenge = await env.DB.prepare(
    'SELECT id FROM daily_challenges WHERE date = ?'
  ).bind(today).first<{ id: number }>();

  if (!challenge) {
    return jsonResponse({ userId, rankings: [] });
  }

  const myEntry = await env.DB.prepare(
    `SELECT time_seconds, completed_at
     FROM user_challenges
     WHERE user_id = ? AND challenge_id = ?`
  ).bind(userId, challenge.id).first<{
    time_seconds: number;
    completed_at: string;
  }>();

  if (!myEntry) {
    return jsonResponse({ userId, rankings: [] });
  }

  const rankResult = await env.DB.prepare(
    `SELECT COUNT(*) as count FROM user_challenges
     WHERE challenge_id = ? AND time_seconds < ?`
  ).bind(challenge.id, myEntry.time_seconds).first<{ count: number }>();

  const rank = (rankResult?.count || 0) + 1;

  const totalResult = await env.DB.prepare(
    'SELECT COUNT(*) as count FROM user_challenges WHERE challenge_id = ?'
  ).bind(challenge.id).first<{ count: number }>();

  return jsonResponse({
    userId,
    rankings: [{
      seedId: challenge.id,
      rank,
      totalParticipants: totalResult?.count || 0,
      timeSeconds: myEntry.time_seconds,
      streakCount: 0,
      completedAt: myEntry.completed_at,
    }],
  });
}
