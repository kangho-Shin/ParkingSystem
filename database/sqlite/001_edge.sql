CREATE TABLE IF NOT EXISTS outbox_message (
    event_id TEXT NOT NULL PRIMARY KEY,
    event_type TEXT NOT NULL DEFAULT 'Entry',
    payload_json TEXT NOT NULL,
    state INTEGER NOT NULL DEFAULT 0,
    retry_count INTEGER NOT NULL DEFAULT 0,
    next_attempt_at_utc TEXT NOT NULL,
    created_at_utc TEXT NOT NULL,
    completed_at_utc TEXT NULL
);
CREATE INDEX IF NOT EXISTS ix_outbox_pending ON outbox_message(state,next_attempt_at_utc,created_at_utc);
