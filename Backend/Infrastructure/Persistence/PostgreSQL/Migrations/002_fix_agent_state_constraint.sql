-- Fix agents.state CHECK constraint to match the C# AgentState enum.
-- The original constraint allowed ('Registered', 'Active', 'Idle', 'Unavailable')
-- but the domain model uses Inactive, Lost, and Decommissioned instead.

ALTER TABLE agents DROP CONSTRAINT IF EXISTS agents_state_check;
ALTER TABLE agents
    ADD CONSTRAINT agents_state_check
        CHECK (state IN ('Registered', 'Active', 'Inactive', 'Lost', 'Decommissioned'));

-- Migrate any rows that used old state names
UPDATE agents
SET state = 'Inactive'
WHERE state = 'Idle';
UPDATE agents
SET state = 'Lost'
WHERE state = 'Unavailable';
