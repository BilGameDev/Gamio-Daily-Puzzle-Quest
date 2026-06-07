# Gamio

A daily puzzle game collection for mobile. Each day brings three randomly selected puzzles with a shared seed — solve them, compete on the leaderboard, and keep your streak alive.

## Games

- **Arrows** — Slide tiles in the direction they point
- **Hitori** — Eliminate duplicate numbers from rows and columns
- **Kings** — Place one king per row, column, and region (no two may touch)
- **LineConnect** — Connect matching endpoints with unbroken paths
- **Pipes** — Rotate pipes to complete a closed loop
- **Shikaku** — Divide the grid into rectangles matching their clue numbers
- **Sudoku** — Classic 9×9 number placement
- **WordGrid** — Guess the word from coloured feedback
- **WordSearch** — Find all hidden words in the grid

## Architecture

### Client (Unity)

The app uses a service-locator pattern via `GamioAppContext` for loose coupling. Each puzzle game follows the same structure: a **Game** class (implements `IGame`), a **GridController** for input/validation, a **GridUI** for visuals, and a **SettingsSO** ScriptableObject for per-difficulty config (Easy/Medium/Hard).

Scenes are loaded through `SceneLoader` with a fade overlay. Popups use a shared base class (`SlideUpPopup` or `PopupUI`) loaded from `Resources/Popups/`.

Key packages: DOTween (tweening), NiceVibrations (haptics), UniTask (async), TextMeshPro (UI), LeanGUI (toggles), UnlimitedScrollUI (lists), Google AdMob (ads).

### Backend (Cloudflare Workers)

A single TypeScript worker (`backend/src/index.ts`) with a D1 SQLite database handles all API requests.

**API endpoints:**
- `POST /api/auth/verify` — Google Sign-In token exchange
- `GET /api/seeds` — Fetch today's 3 daily challenges with per-slot seed data
- `POST /api/completions` — Submit a solved challenge time
- `DELETE /api/completions` — Reset today's completions
- `GET /api/streaks` — Fetch current streak (with `effective` validation for gaps)
- `GET /api/leaderboard` — Per-slot rankings and my rank
- `GET /api/leaderboard/today` — All 3 today's leaderboards at once

**Daily flow:** A CRON trigger at midnight UTC calls the seed generator, which uses HMAC(date, slot) to deterministically produce 3 seeds — one per puzzle slot. Every user sees the same seeds on the same day. Completing any single puzzle earns the daily streak; streak expires if a day is missed.

## Development

Open the project in **Unity 6000.3.9f1** with the URP 2D template.

For the backend:

```bash
cd backend
npm install
npm run dev      # local dev with wrangler
npm run migrate  # apply D1 migrations
npm run deploy   # deploy to Cloudflare Workers
```

Environment secrets (set via `wrangler secret put`): `SEED_HMAC_KEY`, `GOOGLE_CLIENT_ID`, `JWT_SECRET`, `CHALLENGE_GAME_TYPES`.
