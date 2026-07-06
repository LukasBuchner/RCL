import { DependencyEdgeInput } from "../../../__generated__/graphql";
import { Edge } from "@xyflow/react";

export function mapEdgeToDependencyEdgeInput(edge: Edge): DependencyEdgeInput {
  return {
    id: edge.id as string, // Ensure `edge.id` is a UUID string
    sourceHandle: edge.sourceHandle ?? null,
    sourceId: edge.source as string, // Ensure `edge.source` is a UUID string
    targetHandle: edge.targetHandle ?? null,
    targetId: edge.target as string, // Ensure `edge.target` is a UUID string
  };
}
