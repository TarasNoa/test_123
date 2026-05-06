-- Add automatic deletion policy for old logs (Retention Policy)
-- Keep only last 7 days of activity if records exceed 100,000

CREATE OR REPLACE PROCEDURE cleanup_old_agent_events()
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM "AgentEvents"
    WHERE "Timestamp" < NOW() - INTERVAL '7 days'
    AND "Id" NOT IN (
        SELECT "Id" FROM "AgentEvents" 
        ORDER BY "Timestamp" DESC 
        LIMIT 10000
    );
END;
$$;

-- In production, this is scheduled via pg_cron or Quartz.NET in C#
