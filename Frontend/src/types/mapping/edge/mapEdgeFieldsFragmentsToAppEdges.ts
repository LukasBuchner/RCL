import { DependencyEdgeFieldsFragment } from "../../../__generated__/graphql";
import { Edge } from "@xyflow/react";

export function mapEdgeFieldsFragmentsToAppEdges(
  fragments: DependencyEdgeFieldsFragment[],
): Edge[] {
  return fragments.map((fragment) => {
    return {
      id: fragment.id, // Ensure `fragment.id` matches the `id` type in `Edge`
      source: fragment.sourceId, // Maps directly to `source`
      target: fragment.targetId, // Maps directly to `target`
      sourceHandle: fragment.sourceHandle ?? null, // Handles optional `sourceHandle`
      targetHandle: fragment.targetHandle ?? null, // Handles optional `targetHandle`
      type: "dependencyEdge",
    };
  });
}
