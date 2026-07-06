<div align="center">

<img src="docs/vrobocoop-logo-minimal.svg" alt="VRoboCoop Logo" width="160"/>

# VRoboCoop - Robot Collaboration Language (RCL)

### Multi-Project Workspace for Robotics, Visualization & Orchestration

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-18-61DAFB?logo=react)](https://reactjs.org/)
[![GraphQL](https://img.shields.io/badge/GraphQL-API-E10098?logo=graphql)](https://graphql.org/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql)](https://www.postgresql.org/)

[Architecture](docs/architecture.md) • [Deployment](docs/deployment.md) • [Path Configuration](docs/path-configuration.md)

</div>

---

## Overview

VRoboCoop is a multi-project workspace for orchestrating, visualizing, and executing robotic procedures across real and simulated agents. 
This repository presents the Robot Collaboration Language (RCL), a sub-part of the full project.

---

## Project Structure

| Project     | Description                                                                                                                                                                               | Documentation                              |
| ----------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------ |
| **Magnus**  | React/TypeScript frontend with GraphQL subscriptions for flow visualization, procedure orchestration, router-node configuration, variable management, and i18n (EN/DE/ES/PL).             | [`Frontend/README.md`](Frontend/README.md) |
| **Freydis** | .NET 10 GraphQL API backend with reactive scheduling pipeline, router-based conditional branching, variable-driven execution, LP-optimized task scheduling, and multi-agent coordination. | [`Backend/README.md`](Backend/README.md)   |

---

## Quick Start

- Frontend dev server → [`Frontend/README.md`](Frontend/README.md#-quick-start)
- Backend API → [`Backend/README.md`](Backend/README.md#-quick-start)

---

## Typical Development Workflow

```bash
# Terminal 1 — PostgreSQL
docker start postgres

# Terminal 2 — Freydis backend
cd Backend
dotnet run --project GraphQLServer/GraphQLServer.csproj

# Terminal 3 — Magnus frontend
cd Frontend
npm run dev

```

Access:

- Magnus: `http://localhost:5173`
- Freydis GraphQL: `http://localhost:5095/graphql`

---

## Key Capabilities

For feature detail on any of these, follow the link to the owning sub-project.

- **Procedure orchestration** — visual flow editor, drag-and-drop nodes, dependency edges with cycle detection ([Magnus](Frontend/README.md)).
- **Router nodes and variables** — conditional branches, expression selectors, typed procedure-scoped variables ([Magnus](Frontend/README.md), [Freydis](Backend/README.md)).
- **Reactive execution pipeline** — Rx.NET-based execution with real-time GraphQL subscriptions and two-phase completion ([Freydis](Backend/README.md)).
- **LP-optimized scheduling** — SCC decomposition, constrained groups for SS/FF coupling, adaptive duration estimation — formally verified in Lean 4 ([Freydis](Backend/README.md), [Sunstone](Sunstone/README.md)).
- **Multi-agent coordination** — Dummy, KUKA iiwa, Digital Twin, and Hybrid modes through a unified agent factory ([Freydis](Backend/README.md)).
- **Internationalization** — English, German, Spanish, Polish ([Magnus](Frontend/README.md)).

---

## Architecture & Cross-Cutting Documentation

- [`docs/architecture.md`](docs/architecture.md) — system-wide components, protocols, and data flow
- [`docs/deployment.md`](docs/deployment.md) — Docker, environment configuration, deployment topology
- [`docs/path-configuration.md`](docs/path-configuration.md) — template-based machine-specific paths
- [`Backend/docs/README.md`](Backend/docs/README.md) — backend layer-by-layer deep dives

---

## Contributing

- **Main branch:** `main`
- **Issues:** [GitHub Issues](../../issues)
- **Documentation index:** [`docs/`](docs/)

---

## Acknowledgments

This work has been conducted within the project **VRoboCoop**, which received funding from the European Union's IBW/EFRE and Joint Transition Fund, granted and managed by the *Amt der Oberösterreichischen Landesregierung, Abteilung Wirtschaft*.

---

<div align="center">

**Made with ❤️ by the VRoboCoop Team**

[⬆ Back to Top](#vrobocoop)

</div>
