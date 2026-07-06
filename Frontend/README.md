<div align="center">

<img src="../docs/vrobocoop-logo-minimal.svg" alt="VRoboCoop Logo" width="160"/>

# Magnus

### React frontend for designing and monitoring robotic procedures

[![React](https://img.shields.io/badge/React-18-61DAFB?logo=react)](https://reactjs.org/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5-3178C6?logo=typescript)](https://www.typescriptlang.org/)
[![Vite](https://img.shields.io/badge/Vite-5-646CFF?logo=vite)](https://vitejs.dev/)
[![GraphQL](https://img.shields.io/badge/GraphQL-Apollo_Client-E10098?logo=graphql)](https://www.apollographql.com/)
[![React Flow](https://img.shields.io/badge/React_Flow-Flow_Builder-FF0072)](https://reactflow.dev/)

</div>

---

## Overview

Magnus is the React/TypeScript web frontend for the VRoboCoop workspace. It provides a drag-and-drop flow editor for authoring robotic procedures, real-time execution monitoring via GraphQL subscriptions, router-based conditional branching, and a procedure-scoped variable system. It connects to the Freydis backend for persistence, scheduling, and execution state.

## Prerequisites

- **Node.js 22+** and npm — download from [nodejs.org](https://nodejs.org/) or install via your distro's package manager.
- **Freydis backend** reachable at `http://localhost:5095/graphql` — see [`../Backend/README.md`](../Backend/README.md).

<details>
<summary><b>Linux (Ubuntu/Debian) Node.js install</b></summary>

```bash
curl -fsSL https://deb.nodesource.com/setup_22.x | sudo -E bash -
sudo apt-get install -y nodejs
node --version && npm --version
```

</details>

## Quick Start

```bash
cd Frontend
npm install
npm run graphql-codegen   # requires Freydis running
npm run dev
```

Access the dev server at `http://localhost:5173`.

> [!IMPORTANT]
> Generated files under `src/__generated__/` are gitignored. Re-run `npm run graphql-codegen` after cloning and whenever the Freydis GraphQL schema changes.

## Key Features

- **Visual flow builder** — React Flow canvas with drag-and-drop task, skill-execution, and router nodes, plus dependency edges.
- **Real-time subscriptions** — GraphQL subscriptions for node, edge, and execution events; optimistic UI with rollback.
- **Router nodes** — conditional branches with expression-based selectors, design-time preview, and arbitrarily nested routers.
- **Variable management** — procedure-scoped typed variables with auto-creation from skill outputs and a full CRUD editor.
- **Internationalisation** — English, German, Spanish, Polish locales with in-app selection.
- **Code generation** — GraphQL Code Generator produces TypeScript types from the Freydis schema automatically.

## Commands

| Command | Purpose |
|---|---|
| `npm run dev` | Start the Vite dev server with hot module replacement |
| `npm run build` | Produce a production build in `dist/` |
| `npm run lint` | Run ESLint over the source tree |
| `npm run graphql-codegen` | Regenerate TypeScript types from the Freydis GraphQL schema |
| `npm run generate-possible-types` | Generate Apollo Client union/interface types |
| `npx tsc --noEmit` | Type-check without emitting files |

## Project Structure

```
Frontend/
├── src/
│   ├── __generated__/   # Auto-generated GraphQL types (gitignored)
│   ├── components/      # React components
│   ├── graphql/         # .graphql operations
│   ├── hooks/           # Custom hooks
│   ├── utils/           # Utilities
│   ├── App.tsx          # Root component
│   └── main.tsx         # Entry point
├── public/              # Static assets
├── codegen.ts           # GraphQL Code Generator config
├── vite.config.ts       # Vite config
└── package.json         # Dependencies and scripts
```

## Related Documentation

- [Root README](../README.md) — workspace overview
- [Freydis backend](../Backend/README.md) — the GraphQL API this frontend consumes
- [GraphQL Operations](../Backend/GraphQLServer/docs/graphql-operations.md) — queries, mutations, and subscriptions reference

---

<div align="center">

**Made with ❤️ by the VRoboCoop Team**

[⬆ Back to Top](#magnus)

</div>
