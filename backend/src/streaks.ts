import { Env } from './index';
import { jsonResponse } from './utils';

export async function handleGetStreaks(userId: string, env: Env): Promise<Response> {
  const streak = await env.DB.prepare(
    'SELECT current_streak, longest_streak, last_completed_date FROM streaks WHERE user_id = ?'
  ).bind(userId).first<{
    current_streak: number;
    longest_streak: number;
    last_completed_date: string | null;
  }>();

  const recentCompletions = await env.DB.prepare(
    `SELECT dc.date, uc.time_seconds AS total_time_seconds
     FROM user_challenges uc
     JOIN daily_challenges dc ON dc.id = uc.challenge_id
     WHERE uc.user_id = ?
     ORDER BY dc.date DESC
     LIMIT 30`
  ).bind(userId).all<{ date: string; total_time_seconds: number }>();

  const completionDates = new Set(recentCompletions.results.map(r => r.date));

  return jsonResponse({
    current: streak?.current_streak || 0,
    longest: streak?.longest_streak || 0,
    lastCompletedDate: streak?.last_completed_date || null,
    recentCompletions: recentCompletions.results,
    completionDates: Array.from(completionDates).sort().reverse(),
  });
}
