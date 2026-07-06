-- Migration: 001_initial_schema
-- Description: Initial PostgreSQL schema migrated from MongoDB collections
-- Date: 2026-03-09

BEGIN;

-- ============================================================================
-- AGENTS
-- ============================================================================
CREATE TABLE agents
(
    id                   UUID PRIMARY KEY,
    name                 TEXT NOT NULL,
    skill_ids            UUID[],
    representative_color TEXT NOT NULL,
    state                TEXT NOT NULL DEFAULT 'Registered'
        CHECK (state IN ('Registered', 'Active', 'Inactive', 'Lost', 'Decommissioned')),
    last_seen_utc        TIMESTAMPTZ,
    metadata             JSONB
);

-- ============================================================================
-- SKILLS
-- ============================================================================
CREATE TABLE skills
(
    id          UUID PRIMARY KEY,
    name        TEXT  NOT NULL,
    description TEXT  NOT NULL,
    properties  JSONB NOT NULL
);

-- ============================================================================
-- SCENE OBJECTS
-- ============================================================================
CREATE TABLE scene_objects
(
    id       UUID PRIMARY KEY,
    name     TEXT  NOT NULL,
    position JSONB NOT NULL
);

-- ============================================================================
-- POSITION TAGS
-- ============================================================================
CREATE TABLE position_tags
(
    id       UUID PRIMARY KEY,
    tag      TEXT  NOT NULL,
    position JSONB NOT NULL
);

-- ============================================================================
-- PROCEDURES (must be created before nodes and dependency_edges)
-- ============================================================================
CREATE TABLE procedures
(
    id                  UUID PRIMARY KEY,
    name                TEXT        NOT NULL,
    description         TEXT,
    created_at_utc      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_updated_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    root_node_ids       UUID[] NOT NULL DEFAULT '{}',
    variables           JSONB       NOT NULL DEFAULT '[]',
    is_loaded           BOOLEAN     NOT NULL DEFAULT FALSE,
    last_loaded_utc     TIMESTAMPTZ
);

-- ============================================================================
-- NODES (discriminator pattern for TaskNode, SkillExecutionNode, RouterNode)
-- ============================================================================
CREATE TABLE nodes
(
    id           UUID PRIMARY KEY,
    procedure_id UUID  NOT NULL REFERENCES procedures (id) ON DELETE CASCADE,
    node_type    TEXT  NOT NULL
        CHECK (node_type IN ('TaskNode', 'SkillExecutionNode', 'RouterNode')),
    position     JSONB NOT NULL,
    parent_id    UUID,
    extent       TEXT,
    width        DOUBLE PRECISION,
    height       DOUBLE PRECISION,
    selectable   BOOLEAN,
    selected     BOOLEAN,
    draggable    BOOLEAN,
    dragging     BOOLEAN,
    hidden       BOOLEAN,
    data         JSONB NOT NULL
);

-- ============================================================================
-- DEPENDENCY EDGES
-- ============================================================================
CREATE TABLE dependency_edges
(
    id            UUID PRIMARY KEY,
    procedure_id  UUID NOT NULL REFERENCES procedures (id) ON DELETE CASCADE,
    source_id     UUID NOT NULL,
    target_id     UUID NOT NULL,
    source_handle TEXT,
    target_handle TEXT
);

-- ============================================================================
-- INDEXES
-- ============================================================================

-- nodes
CREATE INDEX idx_nodes_procedure_id ON nodes (procedure_id);
CREATE INDEX idx_nodes_parent_id ON nodes (parent_id);

-- dependency_edges
CREATE INDEX idx_dependency_edges_procedure_id ON dependency_edges (procedure_id);
CREATE INDEX idx_dependency_edges_source_id ON dependency_edges (source_id);
CREATE INDEX idx_dependency_edges_target_id ON dependency_edges (target_id);

COMMIT;
