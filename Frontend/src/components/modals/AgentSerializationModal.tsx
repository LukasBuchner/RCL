import React from "react";
import { UnifiedModal } from "../common/UnifiedModal";
import { MotionButton } from "../motion";
import type { AgentSerializationViolation } from "../../__generated__/graphql";

interface AgentSerializationModalProps {
  show: boolean;
  violations: AgentSerializationViolation[];
  onClose: () => void;
}

/**
 * Displays agent serialization violations that prevent procedure execution.
 *
 * Each violation groups the skills assigned to a single agent that could
 * run concurrently because no finish-to-start chain exists between them.
 * The modal explains the problem and lists three concrete ways to fix it.
 */
export const AgentSerializationModal: React.FC<
  AgentSerializationModalProps
> = ({ show, violations, onClose }) => {
  const footer = (
    <MotionButton variant="primary" onClick={onClose}>
      <i className="bi bi-pencil-square me-2" />
      Go to editor
    </MotionButton>
  );

  return (
    <UnifiedModal
      show={show}
      onHide={onClose}
      title="Cannot execute: skills could run at the same time"
      icon="bi-exclamation-triangle-fill"
      size="lg"
      centered
      customFooter={footer}
    >
      <p className="text-muted mb-3">
        The following agents have skills that may execute simultaneously. Each
        agent can only run one skill at a time.
      </p>

      {violations.map((violation) => (
        <div key={String(violation.agentId)} className="mb-4">
          <h6 className="d-flex align-items-center gap-2 mb-2">
            <i
              className="bi bi-exclamation-triangle-fill"
              style={{ color: "var(--bs-warning)" }}
              aria-hidden="true"
            />
            {violation.agentName}
          </h6>
          <ul className="mb-0 ps-3">
            {violation.unserializedSkills.map((skill) => (
              <li key={String(skill.nodeId)}>{skill.skillName}</li>
            ))}
          </ul>
        </div>
      ))}

      <hr />

      <div>
        <p className="fw-semibold mb-2">How to fix (any of these):</p>
        <ol className="mb-0">
          <li>
            Add <strong>finish-to-start</strong> connections between the skills
            to define their execution order.
          </li>
          <li>
            Assign one or more skills to a <strong>different agent</strong>.
          </li>
          <li>
            Move skills into <strong>different router branches</strong> so only
            one branch runs at a time.
          </li>
        </ol>
      </div>
    </UnifiedModal>
  );
};
