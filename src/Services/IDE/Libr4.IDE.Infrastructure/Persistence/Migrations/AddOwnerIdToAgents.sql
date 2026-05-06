-- Add OwnerId column to Agents table for JWT ownership validation
-- This ensures users can only access their own agents via SignalR

ALTER TABLE "Agents"
ADD COLUMN "OwnerId" varchar(255) NOT NULL DEFAULT '';

-- Add index on OwnerId for faster ownership checks
CREATE INDEX IX_Agents_OwnerId ON "Agents"("OwnerId");

-- Update existing records with a default OwnerId (temporary, should be updated by application)
UPDATE "Agents" SET "OwnerId" = 'system-user' WHERE "OwnerId" = '';
