-- Add editable username field to users table
ALTER TABLE users ADD COLUMN username TEXT NOT NULL DEFAULT '';
