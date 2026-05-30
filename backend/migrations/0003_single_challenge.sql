-- Single daily challenge (one game per day) replacing multi-game system

CREATE TABLE IF NOT EXISTS daily_challenges (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  date TEXT UNIQUE NOT NULL,
  seed TEXT NOT NULL,
  game_type TEXT NOT NULL,
  created_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS user_challenges (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  user_id TEXT NOT NULL REFERENCES users(id),
  challenge_id INTEGER NOT NULL REFERENCES daily_challenges(id),
  time_seconds REAL NOT NULL CHECK(time_seconds > 0),
  completed_at TEXT NOT NULL DEFAULT (datetime('now')),
  UNIQUE(user_id, challenge_id)
);

CREATE INDEX IF NOT EXISTS idx_user_challenges_challenge_time
  ON user_challenges(challenge_id, time_seconds);

CREATE INDEX IF NOT EXISTS idx_user_challenges_user
  ON user_challenges(user_id);

DROP TABLE IF EXISTS puzzle_completions;
DROP TABLE IF EXISTS daily_completions;
DROP TABLE IF EXISTS daily_assignments;
DROP TABLE IF EXISTS daily_seeds;
