import { Edge } from "@xyflow/react";
import { SceneObject } from "../__generated__/graphql.ts";

export type FinishToStart = Edge & {
  dependencyType: "FinishToStart";
};
export type StartToStart = Edge & {
  dependencyType: "StartToStart";
};
export type FinishToFinish = Edge & {
  dependencyType: "FinishToFinish";
};
export type StartToFinish = Edge & {
  dependencyType: "StartToFinish";
};

export type DependencyType =
  | "FinishToStart"
  | "StartToStart"
  | "FinishToFinish"
  | "StartToFinish";

export type Dependency =
  | FinishToStart
  | StartToStart
  | FinishToFinish
  | StartToFinish;

export type Environment = {
  id: string;
  name: string;
  objects: SceneObject[];
};
