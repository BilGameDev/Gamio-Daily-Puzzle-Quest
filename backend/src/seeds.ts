import { Env } from './index';
import { jsonResponse, errorResponse } from './utils';

const DEFAULT_GAME_TYPES = ['Sudoku', 'Hitori', 'Pipes', 'Shikaku', 'Kings', 'LineConnect', 'WordGrid', 'WordSearch', 'Arrows'];

function getGameTypes(env: Env): string[] {
  const raw = env.CHALLENGE_GAME_TYPES;
  if (!raw) return DEFAULT_GAME_TYPES;
  return raw.split(',').map(s => s.trim()).filter(Boolean);
}

const SLOTS = [0, 1, 2];

export async function generateDailyChallenge(env: Env): Promise<void> {
  const today = new Date().toISOString().split('T')[0];
  const gameTypes = getGameTypes(env);
  const selectedTypes = new Set<string>();

  for (const slot of SLOTS) {
    const seed = await computeSeed(`${today}:${slot}`, env.SEED_HMAC_KEY);

    const available = gameTypes.filter(t => !selectedTypes.has(t));
    const pool = available.length > 0 ? available : gameTypes;
    const gameType = pool[simpleHash(seed) % pool.length];
    selectedTypes.add(gameType);

    await env.DB.prepare(
      `INSERT OR IGNORE INTO daily_challenges (date, slot, seed, game_type)
       VALUES (?, ?, ?, ?)`
    ).bind(today, slot, seed, gameType).run();
  }
}

async function computeSeed(date: string, hmacKey: string): Promise<string> {
  const encoder = new TextEncoder();
  const key = await crypto.subtle.importKey(
    'raw', encoder.encode(hmacKey),
    { name: 'HMAC', hash: 'SHA-256' }, false, ['sign']
  );

  const sig = await crypto.subtle.sign('HMAC', key, encoder.encode(date));
  const hash = Array.from(new Uint8Array(sig))
    .map(b => b.toString(16).padStart(2, '0')).join('');
  return hash.substring(0, 16);
}

export async function handleGetSeeds(userId: string, env: Env): Promise<Response> {
  const today = new Date().toISOString().split('T')[0];

  let challenges = await env.DB.prepare(
    'SELECT id, seed, game_type FROM daily_challenges WHERE date = ? ORDER BY slot ASC'
  ).bind(today).all<{ id: number; seed: string; game_type: string }>();

  if (!challenges.results.length) {
    await generateDailyChallenge(env);
    challenges = await env.DB.prepare(
      'SELECT id, seed, game_type FROM daily_challenges WHERE date = ? ORDER BY slot ASC'
    ).bind(today).all<{ id: number; seed: string; game_type: string }>();

    if (!challenges.results.length) {
      return errorResponse(500, 'Failed to create daily challenges');
    }
  }

  const ids = challenges.results.map(c => c.id);
  const placeholders = ids.map(() => '?').join(',');

  const userChallenges = await env.DB.prepare(
    `SELECT challenge_id, time_seconds FROM user_challenges
     WHERE user_id = ? AND challenge_id IN (${placeholders})`
  ).bind(userId, ...ids).all<{ challenge_id: number; time_seconds: number }>();

  const completedIds = new Set(userChallenges.results.map(uc => uc.challenge_id));
  const times = new Map(userChallenges.results.map(uc => [uc.challenge_id, uc.time_seconds]));

  const streak = await env.DB.prepare(
    'SELECT current_streak, longest_streak, last_completed_date FROM streaks WHERE user_id = ?'
  ).bind(userId).first<{ current_streak: number; longest_streak: number; last_completed_date: string }>();

  const effective = getEffectiveStreak(streak?.current_streak ?? 0, streak?.last_completed_date ?? null);
  const endTime = getStreakEndTime();

  return jsonResponse({
    date: today,
    challenges: challenges.results.map(c => ({
      seedId: c.id,
      seed: c.seed,
      gameType: c.game_type,
      completed: completedIds.has(c.id),
      totalTimeSeconds: times.get(c.id) ?? null,
    })),
    dailyCompleted: userChallenges.results.length > 0,
    streak: {
      current: streak?.current_streak ?? 0,
      longest: streak?.longest_streak ?? 0,
      effective,
      endTime,
    },
  });
}

export function handleGetConfig(env: Env): Response {
  return jsonResponse({
    gameTypes: getGameTypes(env),
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

function simpleHash(str: string): number {
  let hash = 0;
  for (let i = 0; i < str.length; i++) {
    const chr = str.charCodeAt(i);
    hash = ((hash << 5) - hash) + chr;
    hash |= 0;
  }
  return Math.abs(hash);
}
