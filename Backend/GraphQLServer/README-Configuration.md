# Configuration Files

## File Structure

```
GraphQLServer/
├── Config/
│   ├── scene-config.json           # Scene entities (positions, objects)
│   └── dummy-agents-config.json    # Dummy agents for testing
└── appsettings.Development.json    # Application settings
```

## Scene Configuration (`Config/scene-config.json`)

**Purpose**: Defines the environment/scene entities that exist independently of agents.

Contains:

- **PositionTags**: Named locations in the scene (e.g., "HomePosition", "WorkStation")
- **SceneObjects**: Physical objects in the environment (e.g., tools, parts, fixtures)

**Source of Truth**: This file is the authoritative source for scene entities. On startup, these are synced to the
database where all agents (real and dummy) can access them.

**When to use**: When adding new positions or objects to your robotic environment.

## Agent Configuration (`Config/dummy-agents-config.json`)

**Purpose**: Defines dummy agents for testing and development.

Contains:

- **SkillDefinitions**: Shared skill definitions that multiple agents can reference
- **Agents**: Individual dummy agent configurations with their capabilities

**Note**: Position tags and scene objects are **NOT** in this file. They are loaded from the database by the
`DummyAgentFactory` via the `ISceneEntityProvider` interface. This ensures the scene-config.json remains the single
source of truth.

**When to use**: When configuring dummy agents for testing.

## KUKA Agent Configuration (`Config/kuka-agents-config.json`)

**Purpose**: Defines KUKA iiwa 14 agents for integration with OPC UA servers.

Contains:

- **Agents**: KUKA iiwa 14 agent configurations with:
    - `id`: Unique identifier for the agent
    - `name`: Display name for the agent
    - `opcUaEndpoint`: OPC UA server endpoint URL (e.g., `opc.tcp://localhost:4840/kuka/iiwa14`)
    - `description`: Human-readable description
    - `maxConcurrentExecutions`: Maximum number of concurrent skill executions (typically 1 for robots)
    - `connectionTimeout`: Connection timeout in milliseconds
    - `skills`: Array of skill references with:
        - `skillDefinitionId`: Reference to a skill definition from `skills-config.json`
        - `canExecuteAdaptively`: Whether the skill supports adaptive execution (typically false for KUKA)
        - `nominalDuration`: Expected execution time in seconds

**Note**: Like dummy agents, KUKA agents load position tags and scene objects from the database via
`ISceneEntityProvider`.

**When to use**: When configuring KUKA robots connected via OPC UA.

## Real Agents

Real agents will:

1. Discover scene entities (position tags, scene objects) from the database
2. Register themselves via the discovery endpoint
3. Report their capabilities to the system

They do not use configuration files - everything is dynamic.

## Service Architecture

### Initialization Services (`Services/Initialization/`)

All initialization-related services are organized in a dedicated folder:

- **ISceneInitializationService** / **SceneInitializationService**: Loads scene entities from config and syncs to
  database
- **IAgentStartupService** / **AgentStartupService**: Manages agent initialization
- **InitializationHostedService**: Background service that orchestrates startup (scene first, then agents)

### Scene Entity Provider

The `ISceneEntityProvider` interface (defined in Agents project, implemented in Application layer) provides access to
scene entities from the database:

- **Purpose**: Allows agent factories to load position tags and scene objects from the database instead of from config
  files
- **Benefits**: Maintains single source of truth for scene entities, proper separation of concerns
- **Implementation**: `SceneEntityProvider` in `Application/Services/AgentCoordination/`

## Agent Types

Each agent type has its own sub-configuration with an independent `Enabled` flag, allowing any
combination of agent types to run simultaneously.

| Sub-config    | Default | Rationale                                    |
|---------------|---------|----------------------------------------------|
| `DummyAgents` | `true`  | Dev default — most common during development |
| `KukaAgents`  | `false` | Requires hardware, opt-in                    |
| `RealAgents`  | `false` | Not yet implemented, opt-in                  |
| `DigitalTwin` | `true`  | Always available for VR connections          |

### Dummy Agents (Default)

```json
{
  "Agents": {
    "DummyAgents": {
      "Enabled": true,
      "ConfigurationFile": "Config/dummy-agents-config.json",
      "AutoLoad": true
    }
  }
}
```

- Uses simulated dummy agents for development and testing
- No physical hardware required
- Fast execution times for rapid iteration

### KUKA Agents

```json
{
  "Agents": {
    "DummyAgents": {
      "Enabled": false
    },
    "KukaAgents": {
      "Enabled": true,
      "ConfigurationFile": "Config/kuka-agents-config.json",
      "AutoLoad": true
    }
  }
}
```

- Loads KUKA iiwa 14 agents
- Connects to real robots via OPC UA
- Requires OPC UA servers to be running

### Mixed (Dummy + KUKA)

```json
{
  "Agents": {
    "DummyAgents": {
      "Enabled": true,
      "ConfigurationFile": "Config/dummy-agents-config.json",
      "AutoLoad": true
    },
    "KukaAgents": {
      "Enabled": true,
      "ConfigurationFile": "Config/kuka-agents-config.json",
      "AutoLoad": true
    }
  }
}
```

- Loads both dummy agents and KUKA agents simultaneously
- Useful for mixed environments (some real robots, some simulated)
- Enables gradual migration from simulation to production

### Real Agents (Placeholder)

```json
{
  "Agents": {
    "DummyAgents": {
      "Enabled": false
    },
    "RealAgents": {
      "Enabled": true,
      "DiscoveryEndpoint": "http://discovery-server:5000",
      "AutoStart": true
    }
  }
}
```

- Reserved for future dynamic agent discovery
- Not yet implemented

### Digital Twin

```json
{
  "Agents": {
    "DigitalTwin": {
      "Enabled": true,
      "NominalDurationSeconds": 5.0,
      "PingIntervalSeconds": 10.0,
      "EstimateTimeoutSeconds": 2.0
    }
  }
}
```

- Accepts VR Digital Twin connections via WebSocket (`/ws/digital-twin`)
- Enabled by default — set `Enabled: false` to disable the endpoint entirely