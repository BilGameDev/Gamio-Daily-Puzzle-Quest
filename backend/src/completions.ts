import { Env } from './index';
import { jsonResponse, errorResponse } from './utils';

export async function handleSubmitDaily(
  userId: string, request: Request, env: Env
): Promise<Response> {
  const body: any = await request.json();
  const { challengeId, timeSeconds } = body;

  if (challengeId == null || timeSeconds == null) {
    return errorResponse(400, 'Missing required fields: challengeId, timeSeconds');
  }

  if (timeSeconds < 1) {
    return errorResponse(400, 'Invalid time');
  }

  // Validate the challenge exists
  const challenge = await env.DB.prepare(
    'SELECT id, date FROM daily_challenges WHERE id = ?'
  ).bind(challengeId).first<{ id: number; date: string }>();

  if (!challenge) {
    return errorResponse(404, 'Challenge not found');
  }

  // Check if already completed
  const existing = await env.DB.prepare(
    'SELECT id FROM user_challenges WHERE user_id = ? AND challenge_id = ?'
  ).bind(userId, challengeId).first();

  if (existing) {
    return jsonResponse({
      success: true,
      alreadyCompleted: true,
      totalTimeSeconds: timeSeconds,
    });
  }

  // Insert user challenge completion
  await env.DB.prepare(
    `INSERT INTO user_challenges (user_id, challenge_id, time_seconds)
     VALUES (?, ?, ?)`
  ).bind(userId, challengeId, timeSeconds).run();

  // Update streak
  let streak = await env.DB.prepare(
    'SELECT current_streak, longest_streak, last_completed_date FROM streaks WHERE user_id = ?'
  ).bind(userId).first<{ current_streak: number; longest_streak: number; last_completed_date: string }>();

  let newStreak = 1;
  const today = challenge.date;
  const yesterday = getYesterday(today);

  if (streak) {
    const lastDate = streak.last_completed_date;
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
  ).bind(userId, newStreak, longestStreak, today, now()).run();

  return jsonResponse({
    success: true,
    alreadyCompleted: false,
    totalTimeSeconds: timeSeconds,
    streak: {
      current: newStreak,
      longest: longestStreak,
    },
  });
}

export async function handleDeleteCompletions(userId: string, env: Env): Promise<Response> {
  const today = new Date().toISOString().split('T')[0];

  const challenge = await env.DB.prepare(
    'SELECT id FROM daily_challenges WHERE date = ?'
  ).bind(today).first<{ id: number }>();

  if (!challenge) {
    return jsonResponse({ success: true, deleted: false, reason: 'No challenge for today' });
  }

  await env.DB.prepare(
    'DELETE FROM user_challenges WHERE user_id = ? AND challenge_id = ?'
  ).bind(userId, challenge.id).run();

  return jsonResponse({ success: true, deleted: true });
}

function now(): string {
  return new Date().toISOString().replace('T', ' ').split('.')[0];
}

function getYesterday(dateStr: string): string {
  const date = new Date(dateStr + 'T00:00:00Z');
  date.setUTCDate(date.getUTCDate() - 1);
  return date.toISOString().split('T')[0];
}
