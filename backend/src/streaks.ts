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

  const effective = getEffectiveStreak(streak?.current_streak ?? 0, streak?.last_completed_date ?? null);
  const endTime = getStreakEndTime();

  return jsonResponse({
    current: streak?.current_streak ?? 0,
    longest: streak?.longest_streak ?? 0,
    effective,
    endTime,
    lastCompletedDate: streak?.last_completed_date ?? null,
    recentCompletions: recentCompletions.results,
    completionDates: Array.from(completionDates).sort().reverse(),
  });
}

function getYesterday(dateStr: string): string {
  const date = new Date(dateStr + 'T00:00:00Z');
  date.setUTCDate(date.getUTCDate() - 1);
  return date.toISOString().split('T')[0];
}

function getEffectiveStreak(currentStreak: number, lastCompletedDate: string | null): number {
  if (!lastCompletedDate) return 0;
  const today = new Date().toISOString().split('T')[0];
  const yesterday = getYesterday(today);
  if (lastCompletedDate === today || lastCompletedDate === yesterday) {
    return currentStreak;
  }
  return 0;
}

function getStreakEndTime(): string {
  const now = new Date();
  now.setUTCHours(23, 59, 59, 999);
  return now.toISOString();
}
