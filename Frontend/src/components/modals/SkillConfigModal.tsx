// SkillConfigModal.tsx – full file (synced selects + live properties, no SET_SKILL_PROPERTIES)
import React, { useEffect, useMemo, useReducer } from "react";
import { Form } from "react-bootstrap";
import { UnifiedModal } from "../common/UnifiedModal";
import { MODAL_CONFIGS, useRouterModal } from "../../hooks/useRouterModal";
import { v4 as uuidv4 } from "uuid";
import { produce } from "immer";
import { useQuery } from "@apollo/client";
import { useError } from "../../hooks";
import { LoadingOverlay } from "../loading";
import { useLocation } from "react-router-dom";
import { useTranslation } from "react-i18next";
import {
  AgentFieldsFragment,
  GetAgentsBySkillIdDocument,
  GetAgentsBySkillIdQuery,
  GetAgentsDocument,
  GetAgentsQuery,
  GetNodeByIdDocument,
  GetNodeByIdQuery,
  GetSkillByIdDocument,
  GetSkillByIdQuery,
  GetSkillsByAgentIdDocument,
  GetSkillsByAgentIdQuery,
  GetSkillsDocument,
  GetSkillsQuery,
  PropertyFieldsFragment,
  SkillExecutionNodeFieldsFragment,
  SkillFieldsFragment,
} from "../../__generated__/graphql";
import { createNodeCommand, updateNodeCommand, useFlowUndo } from "../../undo";
import { SkillExecutionNode } from "../../types/nodeTypes";
import PropertyInput from "../common/PropertyInput";
import { createLogger } from "../../utils/logger";

const log = createLogger("SkillConfigModal");

// State passed through location.state from navigation
interface LocationState {
  position?: { x: number; y: number };
  parentId?: string;
}

/**********************************************************************
 * Helper – blank fragment for "create" mode
 *********************************************************************/
const blankFragment = (
  position: { x: number; y: number } = { x: 0, y: 0 },
  parentId?: string,
): SkillExecutionNodeFieldsFragment => ({
  __typename: "SkillExecutionNode",
  id: uuidv4(),
  parentId: parentId ?? null,
  extent: parentId ? "parent" : null,
  width: null,
  height: 50,
  selectable: true,
  selected: false,
  draggable: true,
  dragging: false,
  hidden: false,
  position: { __typename: "NodePosition", ...position },
  skillExecutionTask: {
    __typename: "SkillExecutionTask",
    name: "",
    description: "",
    startTime: 0,
    duration: 200,
    isExecuting: false,
    agent: {
      __typename: "Agent",
      id: "",
      name: "",
      representativeColor: "",
    },
    skill: {
      __typename: "Skill",
      id: "",
      name: "",
      description: "",
      properties: [],
      agents: [],
    },
  },
});

/**********************************************************************
 * Reducer actions
 *********************************************************************/
type Action =
  | {
      type: "OPEN_CREATE";
      position: { x: number; y: number };
      parentId?: string;
    }
  | { type: "OPEN_EDIT"; fragment: SkillExecutionNodeFieldsFragment }
  | { type: "CLOSE" }
  | {
      type: "SET_FIELD";
      key: "name" | "description" | "duration";
      value: string | number;
    }
  | { type: "SET_AGENT"; agent: AgentFieldsFragment | null }
  | { type: "SET_SKILL"; skill: SkillFieldsFragment | null }
  | { type: "SET_PROP"; prop: PropertyFieldsFragment };

/**********************************************************************
 * Reducer – Immer
 *********************************************************************/
function reducer(
  state: SkillExecutionNodeFieldsFragment,
  action: Action,
): SkillExecutionNodeFieldsFragment {
  return produce(state, (draft) => {
    switch (action.type) {
      case "OPEN_CREATE":
        return blankFragment(action.position, action.parentId);
      case "OPEN_EDIT":
        return action.fragment;
      case "CLOSE":
        return blankFragment();
      case "SET_FIELD": {
        if (action.key === "name")
          draft.skillExecutionTask.name = action.value as string;
        if (action.key === "description")
          draft.skillExecutionTask.description = action.value as string;
        if (action.key === "duration")
          draft.skillExecutionTask.duration = action.value as number;
        return;
      }
      case "SET_AGENT": {
        draft.skillExecutionTask.agent =
          action.agent ?? blankFragment().skillExecutionTask.agent;
        return;
      }
      case "SET_SKILL": {
        const newSkill =
          action.skill ?? blankFragment().skillExecutionTask.skill;
        const existingProperties = draft.skillExecutionTask.skill.properties;

        // Preserve existing property values when setting a new skill
        if (newSkill && newSkill.properties) {
          const skillWithPreservedProperties = {
            ...newSkill,
            properties: newSkill.properties.map((templateProp) => {
              const existingProp = existingProperties.find(
                (p) => p.name === templateProp.name,
              );
              if (existingProp) {
                return existingProp;
              } else {
                return templateProp;
              }
            }),
          };
          draft.skillExecutionTask.skill = skillWithPreservedProperties;
        } else {
          draft.skillExecutionTask.skill = newSkill;
        }
        return;
      }
      case "SET_PROP": {
        const idx = draft.skillExecutionTask.skill.properties.findIndex(
          (p) => p.name === action.prop.name,
        );
        if (idx === -1)
          draft.skillExecutionTask.skill.properties.push(action.prop);
        else draft.skillExecutionTask.skill.properties[idx] = action.prop;
        return;
      }
      default:
        return;
    }
  });
}

/**********************************************************************
 * Component
 *********************************************************************/
const SkillConfigModal: React.FC = () => {
  const { t } = useTranslation();

  // Get router state and location state
  const location = useLocation();
  const locationState = location.state as LocationState | null;
  const { isModalOpen, isEditing, entityId, closeModal } = useRouterModal(
    MODAL_CONFIGS.SKILL_CONFIG,
  );
  // Both create and update flow through the undo manager so they are
  // undoable end-to-end. Loading flags stay `false` because dispatching a
  // command is synchronous — the persister handles network I/O on its own
  // serialized queue.
  const { manager: undoManager } = useFlowUndo();
  const createLoading = false;
  const updateLoading = false;
  const [node, dispatch] = useReducer(reducer, blankFragment());
  const { addError } = useError();

  // ---------------------- Queries ----------------------
  const { data: agentsData, loading: agentsLoading } =
    useQuery<GetAgentsQuery>(GetAgentsDocument);

  const { data: skillsData, loading: skillsLoading } =
    useQuery<GetSkillsQuery>(GetSkillsDocument);
  const { data: agentSkillsData, loading: agentSkillsLoading } =
    useQuery<GetSkillsByAgentIdQuery>(GetSkillsByAgentIdDocument, {
      variables: { agentId: node.skillExecutionTask.agent.id as string },
      skip: !node.skillExecutionTask.agent.id,
      fetchPolicy: "no-cache",
    });

  const { data: skillAgentsData, loading: skillAgentsLoading } =
    useQuery<GetAgentsBySkillIdQuery>(GetAgentsBySkillIdDocument, {
      variables: { skillId: node.skillExecutionTask.skill.id as string },
      skip: !node.skillExecutionTask.skill.id,
      fetchPolicy: "no-cache",
    });

  const { data: nodeData } = useQuery<GetNodeByIdQuery>(GetNodeByIdDocument, {
    variables: { id: entityId || "" },
    skip: !entityId,
    fetchPolicy: "no-cache",
  });

  // fetch full skill (with properties) whenever a skill is selected
  const { data: selectedSkillData } = useQuery<GetSkillByIdQuery>(
    GetSkillByIdDocument,
    {
      variables: { skillId: node.skillExecutionTask.skill.id as string },
      skip: !node.skillExecutionTask.skill.id,
      fetchPolicy: "no-cache",
    },
  );

  // ---------------------- Derived lists ----------------------
  const filteredAgents: (AgentFieldsFragment | null)[] =
    useMemo((): (AgentFieldsFragment | null)[] => {
      if (node.skillExecutionTask.skill.id) {
        return skillAgentsData?.skillById?.agents ?? [];
      }
      return agentsData?.agents ?? [];
    }, [node.skillExecutionTask.skill.id, skillAgentsData, agentsData]);

  const filteredSkills: (SkillFieldsFragment | null)[] =
    useMemo((): (SkillFieldsFragment | null)[] => {
      if (node.skillExecutionTask.agent.id) {
        return agentSkillsData?.agentById?.skills ?? [];
      }
      return skillsData?.skills ?? [];
    }, [node.skillExecutionTask.agent.id, agentSkillsData, skillsData]);

  // ---------------------- Modal lifecycle sync ----------------------
  useEffect(() => {
    if (!isModalOpen) return;

    if (
      isEditing &&
      entityId &&
      nodeData?.nodeById &&
      nodeData.nodeById.__typename === "SkillExecutionNode"
    ) {
      dispatch({ type: "OPEN_EDIT", fragment: nodeData.nodeById });
    } else if (!isEditing) {
      dispatch({
        type: "OPEN_CREATE",
        position: locationState?.position ?? { x: 0, y: 0 },
        parentId: locationState?.parentId,
      });
    }
  }, [isModalOpen, isEditing, entityId, nodeData, locationState]);

  // hydrate reducer with full skill (incl. properties) once loaded
  useEffect(() => {
    if (selectedSkillData?.skillById) {
      const fullSkill = {
        ...selectedSkillData.skillById,
        properties: selectedSkillData.skillById.properties.map(
          (p: PropertyFieldsFragment): PropertyFieldsFragment => ({
            ...p,
          }),
        ),
      } as SkillFieldsFragment;
      dispatch({ type: "SET_SKILL", skill: fullSkill });
    }
  }, [selectedSkillData]);

  // clear incompatible agent after skill selection
  useEffect(() => {
    if (!node.skillExecutionTask.skill.id) return;
    if (
      skillAgentsData &&
      node.skillExecutionTask.agent.id &&
      !skillAgentsData.skillById?.agents.some(
        (a: AgentFieldsFragment | null) =>
          a?.id === node.skillExecutionTask.agent.id,
      )
    ) {
      dispatch({ type: "SET_AGENT", agent: null });
    }
  }, [
    skillAgentsData,
    node.skillExecutionTask.agent.id,
    node.skillExecutionTask.skill.id,
  ]);

  // clear incompatible skill after agent selection
  useEffect(() => {
    if (!node.skillExecutionTask.agent.id) return;
    if (
      agentSkillsData &&
      node.skillExecutionTask.skill.id &&
      !agentSkillsData.agentById?.skills.some(
        (s: SkillFieldsFragment | null) =>
          s?.id === node.skillExecutionTask.skill.id,
      )
    ) {
      dispatch({ type: "SET_SKILL", skill: null });
    }
  }, [
    agentSkillsData,
    node.skillExecutionTask.agent.id,
    node.skillExecutionTask.skill.id,
  ]);

  const handleClose = () => {
    // Don't close modal if a mutation is in progress
    if (createLoading || updateLoading) {
      return;
    }
    dispatch({ type: "CLOSE" });
    closeModal();
  };

  // ---------------------- Field handlers ----------------------
  const handleAgentChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const agent = filteredAgents.find((a) => a?.id === e.target.value) ?? null;
    dispatch({ type: "SET_AGENT", agent: agent as AgentFieldsFragment | null });
  };

  const handleSkillChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const skill = filteredSkills.find((s) => s?.id === e.target.value) ?? null;
    dispatch({ type: "SET_SKILL", skill: skill as SkillFieldsFragment | null });
  };

  const handlePropertyChange = (fragment: PropertyFieldsFragment) => {
    dispatch({ type: "SET_PROP", prop: fragment });
  };

  // ---------------------- Submit ----------------------
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      const { id, position, parentId, skillExecutionTask: task } = node;

      // Validate required fields
      if (!task.name.trim()) {
        addError({
          message: t("errors.nameRequired"),
          severity: "warning",
        });
        return;
      }
      if (!task.agent.id) {
        addError({
          message: t("errors.agentSelectionRequired"),
          severity: "warning",
        });
        return;
      }
      if (!task.skill.id) {
        addError({
          message: t("errors.skillSelectionRequired"),
          severity: "warning",
        });
        return;
      }
      const agent = filteredAgents.find(
        (a) => a?.id === task.agent.id,
      )! as AgentFieldsFragment;
      const skill = filteredSkills.find((s) => s?.id === task.skill.id)!;
      const skillWithProperties = {
        ...skill,
        properties: task.skill.properties,
      } as SkillFieldsFragment;

      if (isEditing && entityId) {
        // ------------------ EDIT ------------------
        if (
          !nodeData?.nodeById ||
          nodeData.nodeById.__typename !== "SkillExecutionNode"
        ) {
          log.error("Node not found or not a skill execution node");
          return;
        }

        // Build `before` from the server-side fragment and `after` with the
        // user's form edits applied. Dispatch through the undo manager so
        // edits are reversible.
        const serverNode = nodeData.nodeById;
        const serverSkill = serverNode.skillExecutionTask.skill;
        const serverAgent = serverNode.skillExecutionTask.agent;
        const before: SkillExecutionNode = {
          id: serverNode.id,
          type: "skillExecutionNode",
          position: {
            x: serverNode.position.x,
            y: serverNode.position.y,
          },
          parentId: serverNode.parentId ?? undefined,
          extent: serverNode.extent === "parent" ? "parent" : undefined,
          data: {
            name: serverNode.skillExecutionTask.name,
            description: serverNode.skillExecutionTask.description ?? "",
            startTime: serverNode.skillExecutionTask.startTime,
            duration: serverNode.skillExecutionTask.duration,
            isExecuting: serverNode.skillExecutionTask.isExecuting ?? false,
            skill: serverSkill,
            agent: serverAgent,
          },
        };
        const after: SkillExecutionNode = {
          ...before,
          data: {
            ...before.data,
            name: task.name,
            description: task.description ?? "",
            duration: task.duration,
            skill: skillWithProperties,
            agent,
          },
        };
        undoManager.dispatch(updateNodeCommand(before, after));
      } else {
        // ------------------ CREATE ------------------
        // Dispatch through the undo manager so a freshly created skill
        // execution node can be undone.
        const newNode: SkillExecutionNode = {
          id,
          type: "skillExecutionNode",
          position,
          parentId: parentId ?? undefined,
          extent: parentId ? "parent" : undefined,
          data: {
            name: task.name,
            description: task.description ?? "",
            startTime: 0,
            duration: task.duration,
            isExecuting: false,
            skill: skillWithProperties,
            agent,
          },
        };
        undoManager.dispatch(createNodeCommand(newNode));
      }

      handleClose();
    } catch (err) {
      log.error(
        `Failed to ${isEditing ? "update" : "create"} skill execution node:`,
        err,
      );
      addError({
        message: isEditing
          ? t("errors.failedToUpdateSkillExecution")
          : t("errors.failedToCreateSkillExecution"),
        severity: "error",
        retry: () => handleSubmit(e),
      });
    }
  };

  /* ------------------------------------------------------------------ */
  /* Render helpers                                                    */
  /* ------------------------------------------------------------------ */
  const renderSkillProperties = () => {
    const props = node.skillExecutionTask.skill.properties;
    if (
      node.skillExecutionTask.skill.id &&
      (skillsLoading || agentSkillsLoading)
    ) {
      return (
        <Form.Group className="mb-3">
          <Form.Label>
            <i className="bi bi-gear me-2" /> {t("modals.skillProperties")}
            <span className="ms-2 text-muted">
              <i className="bi bi-hourglass-split" />{" "}
              {t("modals.loadingSkillProperties")}
            </span>
          </Form.Label>
          <div className="d-flex justify-content-center my-3">
            <div className="spinner-border text-primary" role="status" />
          </div>
        </Form.Group>
      );
    }

    if (!props.length) {
      return (
        <Form.Group className="mb-3">
          <Form.Label>
            <i className="bi bi-gear me-2" /> {t("modals.skillProperties")}
          </Form.Label>
          <p className="text-muted">{t("modals.noConfigurableProperties")}</p>
        </Form.Group>
      );
    }

    return (
      <Form.Group className="mb-3">
        <Form.Label>
          <i className="bi bi-gear me-2" /> {t("modals.skillProperties")}
        </Form.Label>
        {props.map(
          (prop) =>
            prop && (
              <PropertyInput
                key={prop.name}
                prop={prop}
                handlePropertyChange={handlePropertyChange}
              />
            ),
        )}
      </Form.Group>
    );
  };

  /* ------------------------------------------------------------------ */
  /* JSX                                                               */
  /* ------------------------------------------------------------------ */
  log.traceLazy(() => ["render", { isModalOpen, isEditing, entityId }]);

  return (
    <UnifiedModal
      show={isModalOpen}
      onHide={handleClose}
      title={
        isEditing
          ? t("modals.editSkillExecution")
          : t("modals.configureSkillExecution")
      }
      icon="bi-lightning-charge"
      onSubmit={handleSubmit}
      isValid={
        !!node.skillExecutionTask.name.trim() &&
        !!node.skillExecutionTask.agent.id &&
        !!node.skillExecutionTask.skill.id
      }
      isEditing={isEditing}
      submitText={
        isEditing
          ? t("modals.updateSkillExecution")
          : t("modals.createSkillExecution")
      }
    >
      <LoadingOverlay
        show={createLoading || updateLoading}
        text={
          isEditing
            ? t("modals.updatingSkillExecution")
            : t("modals.creatingSkillExecution")
        }
      />
      {/* Name */}
      <Form.Group className="mb-3">
        <Form.Label>
          <i className="bi bi-tag me-2" /> {t("modals.name")}
        </Form.Label>
        <Form.Control
          type="text"
          value={node.skillExecutionTask.name}
          placeholder={t("modals.enterName")}
          onChange={(e) =>
            dispatch({
              type: "SET_FIELD",
              key: "name",
              value: e.target.value,
            })
          }
          autoFocus
        />
      </Form.Group>

      {/* Description */}
      <Form.Group className="mb-3">
        <Form.Label>
          <i className="bi bi-card-text me-2" /> {t("modals.description")}
        </Form.Label>
        <Form.Control
          as="textarea"
          rows={3}
          value={node.skillExecutionTask.description ?? ""}
          placeholder={t("modals.enterDescription")}
          onChange={(e) =>
            dispatch({
              type: "SET_FIELD",
              key: "description",
              value: e.target.value,
            })
          }
        />
      </Form.Group>

      {/* Duration */}
      <Form.Group className="mb-3">
        <Form.Label>
          <i className="bi bi-clock me-2" /> {t("modals.duration")}
        </Form.Label>
        <Form.Control
          type="number"
          min={0}
          value={node.skillExecutionTask.duration}
          placeholder={t("modals.enterDuration")}
          onChange={(e) =>
            dispatch({
              type: "SET_FIELD",
              key: "duration",
              value: parseInt(e.target.value) || 200,
            })
          }
        />
        <Form.Text className="text-muted">
          {t("modals.skillExecutionDurationDescription")}
        </Form.Text>
      </Form.Group>

      {/* Agent */}
      <Form.Group className="mb-3">
        <Form.Label>
          <i className="bi bi-person me-2" /> {t("modals.agent")}
          {agentsLoading && (
            <span className="ms-2 text-muted">
              <i className="bi bi-hourglass-split" />{" "}
              {t("modals.loadingAgents")}
            </span>
          )}
          {skillAgentsLoading && node.skillExecutionTask.skill.id && (
            <span className="ms-2 text-muted">
              <i className="bi bi-hourglass-split" />{" "}
              {t("modals.loadingCompatibleAgents")}
            </span>
          )}
        </Form.Label>
        <Form.Select
          value={node.skillExecutionTask.agent.id ?? ""}
          disabled={agentsLoading || skillAgentsLoading}
          onChange={handleAgentChange}
        >
          <option value="">{t("modals.selectAgent")}</option>
          {filteredAgents.map(
            (a) =>
              a && (
                <option key={a.id} value={a.id}>
                  {a.name}
                </option>
              ),
          )}
        </Form.Select>
      </Form.Group>

      {/* Skill */}
      <Form.Group className="mb-3">
        <Form.Label>
          <i className="bi bi-lightning-charge me-2" /> {t("modals.skill")}
          {skillsLoading && (
            <span className="ms-2 text-muted">
              <i className="bi bi-hourglass-split" />{" "}
              {t("modals.loadingSkills")}
            </span>
          )}
          {agentSkillsLoading && node.skillExecutionTask.agent.id && (
            <span className="ms-2 text-muted">
              <i className="bi bi-hourglass-split" />{" "}
              {t("modals.loadingCompatibleSkills")}
            </span>
          )}
        </Form.Label>
        <Form.Select
          value={node.skillExecutionTask.skill.id ?? ""}
          disabled={skillsLoading || agentSkillsLoading}
          onChange={handleSkillChange}
        >
          <option value="">{t("modals.selectSkill")}</option>
          {filteredSkills.map(
            (s) =>
              s && (
                <option key={s.id} value={s.id}>
                  {s.name}
                </option>
              ),
          )}
        </Form.Select>
        {node.skillExecutionTask.agent.id &&
          !node.skillExecutionTask.skill.id && (
            <Form.Text className="text-muted">
              {t("modals.selectCompatibleSkill")}
            </Form.Text>
          )}
        {node.skillExecutionTask.skill.id &&
          !node.skillExecutionTask.agent.id && (
            <Form.Text className="text-muted">
              {t("modals.selectCompatibleAgent")}
            </Form.Text>
          )}
      </Form.Group>

      {/* Dynamic skill properties */}
      {node.skillExecutionTask.skill.id && renderSkillProperties()}
    </UnifiedModal>
  );
};

export default SkillConfigModal;
