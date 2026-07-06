import { Node } from "@xyflow/react";
import {
  AgentFieldsFragment,
  SkillFieldsFragment,
} from "../__generated__/graphql.ts";

// Base data common to Task and SkillExecution nodes
export type BaseNodeData = {
  duration: number;
  width?: number; // Calculated width from backend based on duration * timeToPixelScale
  startTime?: number; // Optional start time for execution timing
  markedForCut?: boolean;
  markedForCopy?: boolean;
  isExecuting?: boolean; // Optional because not all nodes might have this
  progress?: number; // Progress from 0 to 100 as float (e.g., 45.5)
  hideHandles?: boolean;
};

export type PlayheadData = {
  position: number;
  markedForCut?: boolean;
  markedForCopy?: boolean;
};

export type TaskBasicData = {
  name: string;
  description: string;
  startTime: number;
  duration: number;
  width?: number; // Calculated width from backend based on duration * timeToPixelScale
  isExecuting: boolean;
  progress?: number; // Progress from 0 to 100 as float (e.g., 45.5)
  markedForCut?: boolean;
  markedForCopy?: boolean;
  isRouterChild?: boolean;
};

export type SkillExecutionBasicData = TaskBasicData & {
  skill: SkillFieldsFragment;
  agent: AgentFieldsFragment;
};

export type RouterBasicData = {
  name: string;
  description: string;
  selector: {
    __typename: string;
    expression: string;
  };
  branches: Array<{
    name: string;
    condition?: string;
    priority: number;
    targetNodeId: string;
  }>;
  selectedBranch?: string;
  manuallySelectedBranch?: string;
  duration: number;
  width?: number; // Calculated width from backend based on duration * timeToPixelScale
  startTime?: number;
  markedForCut?: boolean;
  markedForCopy?: boolean;
  isExecuting?: boolean;
  progress?: number;
};

export type TaskNode = Node<TaskBasicData, "taskNode">;
export type SkillExecutionNode = Node<
  SkillExecutionBasicData,
  "skillExecutionNode"
>;
export type PlayheadNode = Node<PlayheadData, "playheadNode">;
export type RouterNode = Node<RouterBasicData, "routerNode">;
export type AppNode = TaskNode | SkillExecutionNode | PlayheadNode | RouterNode;
