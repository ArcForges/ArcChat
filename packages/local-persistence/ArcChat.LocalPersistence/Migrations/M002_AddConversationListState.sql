CREATE TABLE IF NOT EXISTS ConversationListState (
  ConversationId TEXT NOT NULL PRIMARY KEY,
  IsPinned INTEGER NOT NULL DEFAULT 0,
  IsArchived INTEGER NOT NULL DEFAULT 0,
  SortOrder INTEGER NOT NULL DEFAULT 0,
  UnreadCount INTEGER NOT NULL DEFAULT 0,
  UpdatedAt INTEGER NOT NULL DEFAULT 0,
  FOREIGN KEY (ConversationId) REFERENCES Conversation(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_ConversationListState_VisibleOrder
  ON ConversationListState (IsArchived, IsPinned, SortOrder, UpdatedAt);

INSERT OR IGNORE INTO ConversationListState (
  ConversationId,
  IsPinned,
  IsArchived,
  SortOrder,
  UnreadCount,
  UpdatedAt
)
SELECT
  Conversation.Id,
  0,
  0,
  (
    SELECT COUNT(*)
    FROM Conversation AS Earlier
    WHERE Earlier.UpdatedAt > Conversation.UpdatedAt
       OR (Earlier.UpdatedAt = Conversation.UpdatedAt AND Earlier.Id < Conversation.Id)
  ),
  0,
  Conversation.UpdatedAt
FROM Conversation;
