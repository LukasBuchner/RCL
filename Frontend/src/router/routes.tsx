import { RouteObject, Navigate } from "react-router-dom";
import RootLayout from "../layouts/RootLayout";
import Flow from "../components/Flow";
import ManagementLayout from "../layouts/ManagementLayout";
import AgentManagement from "../components/management/AgentManagement";
import SkillManagement from "../components/management/SkillManagement";
import PositionTagManagement from "../components/management/PositionTagManagement";
import SceneObjectManagement from "../components/management/SceneObjectManagement";
import VariableManagement from "../components/management/VariableManagement";
import SettingsLayout from "../layouts/SettingsLayout";
import GeneralSettings from "../components/settings/GeneralSettings";
import GraphQLSettings from "../components/settings/GraphQLSettings";
import AppearanceSettings from "../components/settings/AppearanceSettings";
import HelpLayout from "../layouts/HelpLayout";
import GettingStarted from "../components/help/GettingStarted";
import FlowEditorHelp from "../components/help/FlowEditorHelp";
import ManagementHelp from "../components/help/ManagementHelp";
import TroubleshootingHelp from "../components/help/TroubleshootingHelp";
import RouteError from "../components/error/RouteError";

export const routes: RouteObject[] = [
  {
    path: "/",
    element: <RootLayout />,
    errorElement: <RouteError type="root" />,
    children: [
      {
        index: true,
        element: <Flow />,
      },
      {
        path: "flow",
        element: <Navigate to="/" replace />,
      },
      {
        path: "skill/create",
        element: <Flow />,
      },
      {
        path: "skill/:nodeId/edit",
        element: <Flow />,
      },
      {
        path: "task/create",
        element: <Flow />,
      },
      {
        path: "task/:nodeId/edit",
        element: <Flow />,
      },
      {
        path: "router/create",
        element: <Flow />,
      },
      {
        path: "router/:nodeId/edit",
        element: <Flow />,
      },
      {
        path: "management",
        element: <ManagementLayout />,
        errorElement: <RouteError type="management" />,
        children: [
          {
            index: true,
            element: <AgentManagement />,
          },
          {
            path: "agents",
            element: <AgentManagement />,
          },
          {
            path: "agents/create",
            element: <AgentManagement />,
          },
          {
            path: "agents/:agentId/edit",
            element: <AgentManagement />,
          },
          {
            path: "skills",
            element: <SkillManagement />,
          },
          {
            path: "skills/create",
            element: <SkillManagement />,
          },
          {
            path: "skills/:skillId/edit",
            element: <SkillManagement />,
          },
          {
            path: "position-tags",
            element: <PositionTagManagement />,
          },
          {
            path: "position-tags/create",
            element: <PositionTagManagement />,
          },
          {
            path: "position-tags/:positionTagId/edit",
            element: <PositionTagManagement />,
          },
          {
            path: "scene-objects",
            element: <SceneObjectManagement />,
          },
          {
            path: "scene-objects/create",
            element: <SceneObjectManagement />,
          },
          {
            path: "scene-objects/:sceneObjectId/edit",
            element: <SceneObjectManagement />,
          },
          {
            path: "variables",
            element: <VariableManagement />,
          },
        ],
      },
      {
        path: "settings",
        element: <SettingsLayout />,
        children: [
          {
            index: true,
            element: <GeneralSettings />,
          },
          {
            path: "general",
            element: <GeneralSettings />,
          },
          {
            path: "graphql",
            element: <GraphQLSettings />,
          },
          {
            path: "appearance",
            element: <AppearanceSettings />,
          },
        ],
      },
      {
        path: "help",
        element: <HelpLayout />,
        children: [
          {
            index: true,
            element: <GettingStarted />,
          },
          {
            path: "getting-started",
            element: <GettingStarted />,
          },
          {
            path: "flow-editor",
            element: <FlowEditorHelp />,
          },
          {
            path: "management",
            element: <ManagementHelp />,
          },
          {
            path: "troubleshooting",
            element: <TroubleshootingHelp />,
          },
        ],
      },
    ],
  },
];
