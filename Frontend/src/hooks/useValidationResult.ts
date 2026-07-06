import { useMemo, useState } from "react";
import { useSubscription } from "@apollo/client";
import {
  AgentSerializationViolation,
  OnProcedureValidationChangedDocument,
  OnProcedureValidationChangedSubscription,
} from "../__generated__/graphql";
import { useProcedure } from "../contexts/ProcedureContext";

export interface ValidationResult {
  violations: AgentSerializationViolation[];
  warningNodeIds: Set<string>;
}

/**
 * Subscribes to the live procedure validation stream and exposes the current
 * agent serialization violations together with a derived set of node IDs that
 * should be highlighted as warnings in the flow canvas.
 *
 * The subscription is skipped when no procedure is loaded, matching the same
 * guard pattern used by the node and edge change subscriptions in Flow.tsx.
 *
 * Results are UX-only and may be up to ~1 second stale; the hard execution
 * gate lives server-side in ExecutionOrchestrator.
 */
export const useValidationResult = (): ValidationResult => {
  const { loadedProcedure } = useProcedure();
  const [violations, setViolations] = useState<AgentSerializationViolation[]>(
    [],
  );

  useSubscription<OnProcedureValidationChangedSubscription>(
    OnProcedureValidationChangedDocument,
    {
      skip: !loadedProcedure,
      onData: ({ data: { data } }) => {
        if (data?.procedureValidationChanged) {
          setViolations(
            data.procedureValidationChanged.agentSerializationViolations,
          );
        }
      },
    },
  );

  const warningNodeIds = useMemo(
    () =>
      new Set(
        violations.flatMap((v) =>
          v.unserializedSkills.map((s) => String(s.nodeId)),
        ),
      ),
    [violations],
  );

  return { violations, warningNodeIds };
};
