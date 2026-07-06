# Application Services

> Per-group documentation for every folder under `Backend/Application/Services/` — what each group does, how it connects
> to the others, and where it sits in the design-time and execution pipelines.

## Overview

The Application layer is organized into service groups, one folder per concern. This index links the deep-dive doc for
each group. For the layer-level picture (pipeline flow, architectural patterns, the headline interfaces), start with the
[Application Layer README](../README.md).

## Service Groups

| Group                 | Folder                        | Role                                                                                                                 | Doc                                            |
|-----------------------|-------------------------------|----------------------------------------------------------------------------------------------------------------------|------------------------------------------------|
| **Execution**         | `Services/Execution/`         | The runtime execution pipeline: orchestration, triggering, coordination, rescheduling, state, monitoring, validation | [execution.md](execution.md)                   |
| **Scheduling**        | `Services/Scheduling/`        | Timing calculation over OR-Tools, graph building, duration providers, branch filtering, positioning                  | [scheduling.md](scheduling.md)                 |
| **EntityManagement**  | `Services/EntityManagement/`  | Procedure-scoped CRUD for every entity type with reactive notifications                                              | [entity-management.md](entity-management.md)   |
| **AgentCoordination** | `Services/AgentCoordination/` | Agent registration, capability analysis, skill mapping, and skill synchronization                                    | [agent-coordination.md](agent-coordination.md) |
| **Branching**         | `Services/Branching/`         | Router branch selection with priority short-circuit logic                                                            | [branching.md](branching.md)                   |
| **Variables**         | `Services/Variables/`         | Runtime variable-context resolution for branching and data flow                                                      | [variables.md](variables.md)                   |
| **Properties**        | `Services/Properties/`        | Binding skill input/output properties to variable values                                                             | [properties.md](properties.md)                 |
| **Expressions**       | `Services/Expressions/`       | Evaluating router condition expressions                                                                              | [expressions.md](expressions.md)               |
| **Common**            | `Services/Common/`            | Cross-cutting infrastructure: procedure context, reactive change tracking, platform utilities                        | [common.md](common.md)                         |
| **UI**                | `Services/UI/`                | Node height, width, and X/Y positioning and visibility for the visual graph                                          | [ui.md](ui.md)                                 |

## Related Documentation

- [Application Layer](../README.md) — Service categories, pipeline flow, and architectural patterns
- [Execution Pipeline](../../../docs/execution-pipeline.md) — End-to-end runtime walkthrough
- [Glossary](../../../docs/glossary.md) — Term definitions
- [Documentation Hub](../../../docs/README.md) — Back to the index
