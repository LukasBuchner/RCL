# Magnus Frontend Documentation

> Index of client-side architectural docs and cross-links to backend topics the frontend consumes.

## Frontend-owned

- [State Architecture](state-architecture.md) — store layout, hooks, data flow through Apollo and Zustand
- [Error and Loading Patterns](error-loading-patterns.md) — error boundary conventions, loading UI, GraphQL error plumbing

## Cross-cutting topics

The frontend consumes backend-defined contracts for validation, execution, and the GraphQL schema. Shared topic docs live with the backend:

- [Agent Serialization — Client UX](../../../Backend/docs/agent-serialization/client-ux.md) — how `useExecutionErrorHandlers.ts`, `BaseNode.tsx`, and `AgentSerializationModal` render backend violations, and the error-code registry pattern
- [GraphQL Operations](../../../Backend/GraphQLServer/docs/graphql-operations.md) — schema and subscription reference

## Related

- [Root README](../../../README.md)
- [Backend Documentation Hub](../../../Backend/docs/README.md)
