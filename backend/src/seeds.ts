import { Env } from './index';
import { jsonResponse, errorResponse } from './utils';

const DEFAULT_GAME_TYPES = ['Sudoku', 'Hitori', 'Pipes', 'Shikaku', 'Kings', 'LineConnect', 'WordGrid', 'WordSearch', 'Arrows'];

function getGameTypes(env: Env): string[] {
  const raw = env.CHALLENGE_GAME_TYPES;
  if (!raw) return DEFAULT_GAME_TYPES;
  return raw.split(',').map(s => s.trim()).filter(Boolean);
}

export async function generateDailyChallenge(env: Env): Promise<void> {
  const today = new Date().toISOString().split('T')[0];
  const seed = await computeSeed(today, env.SEED_HMAC_KEY);
  const gameTypes = getGameTypes(env);
  const gameType = gameTypes[simpleHash(today) % gameTypes.length];

  await env.DB.prepare(
    `INSERT OR IGNORE INTO daily_challenges (date, seed, game_type)
     VALUES (?, ?, ?)`
  ).bind(today, seed, gameType).run();
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

  // Get or create today's challenge
  let challenge = await env.DB.prepare(
    'SELECT id, seed, game_type FROM daily_challenges WHERE date = ?'
  ).bind(today).first<{ id: number; seed: string; game_type: string }>();

  if (!challenge) {
    await generateDailyChallenge(env);
    challenge = await env.DB.prepare(
      'SELECT id, seed, game_type FROM daily_challenges WHERE date = ?'
    ).bind(today).first<{ id: number; seed: string; game_type: string }>();

    if (!challenge) {
      return errorResponse(500, 'Failed to create daily challenge');
    }
  }

  // Check if user already completed today's challenge
  const userChallenge = await env.DB.prepare(
    'SELECT time_seconds FROM user_challenges WHERE user_id = ? AND challenge_id = ?'
  ).bind(userId, challenge.id).first<{ time_seconds: number }>();

  const streak = await env.DB.prepare(
    'SELECT current_streak, longest_streak FROM streaks WHERE user_id = ?'
  ).bind(userId).first<{ current_streak: number; longest_streak: number }>();

  return jsonResponse({
    date: today,
    seedId: challenge.id,
    seed: challenge.seed,
    gameType: challenge.game_type,
    dailyCompleted: !!userChallenge,
    totalTimeSeconds: userChallenge?.time_seconds || null,
    streak: {
      current: streak?.current_streak || 0,
      longest: streak?.longest_streak || 0,
    },
  });
}

export function handleGetConfig(env: Env): Response {
  return jsonResponse({
    gameTypes: getGameTypes(env),
  });
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
