import { routeRequest } from './router';
import { generateDailyChallenge } from './seeds';

export interface Env {
  DB: D1Database;
  SEED_HMAC_KEY: string;
  GOOGLE_CLIENT_ID: string;
  JWT_SECRET: string;
  CHALLENGE_GAME_TYPES?: string;
}

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    return routeRequest(request, env);
  },

  async scheduled(event: ScheduledEvent, env: Env): Promise<void> {
    if (event.cron === '0 0 * * *') {
      await generateDailyChallenge(env);
    }
  },
};
