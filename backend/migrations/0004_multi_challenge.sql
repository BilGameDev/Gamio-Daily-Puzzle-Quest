-- Multi-slot daily challenges (3 games per day, slot 0/1/2)

DROP TABLE IF EXISTS user_challenges;
DROP TABLE IF EXISTS daily_challenges;

CREATE TABLE IF NOT EXISTS daily_challenges (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  date TEXT NOT NULL,
  slot INTEGER NOT NULL CHECK(slot IN (0, 1, 2)),
  seed TEXT NOT NULL,
  game_type TEXT NOT NULL,
  created_at TEXT NOT NULL DEFAULT (datetime('now')),
  UNIQUE(date, slot)
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
