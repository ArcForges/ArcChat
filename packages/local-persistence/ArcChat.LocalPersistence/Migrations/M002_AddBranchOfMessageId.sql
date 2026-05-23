ALTER TABLE Message ADD COLUMN BranchOfMessageId TEXT NULL;

CREATE INDEX IF NOT EXISTS IX_Message_Conversation_Branch
  ON Message (ConversationId, BranchOfMessageId, Ordinal);
