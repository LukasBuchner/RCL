import React from "react";
import { useQuery } from "@apollo/client";
import { useTranslation } from "react-i18next";
import { AnimatePresence } from "framer-motion";
import { useManagementCRUD } from "../../hooks/useManagementCRUD";
import { MODAL_CONFIGS } from "../../hooks/useRouterModal";
import { MotionButton, MotionEmptyState } from "../motion";
import {
  CheckboxList,
  FormColumn,
  FormField,
  FormRow,
  FormSection,
  ManagementCard,
  ManagementContainer,
  ManagementHeader,
  RelatedItemsList,
} from "./common";
import { UnifiedModal } from "../common/UnifiedModal";
import "./styles/management.css";
import {
  AgentInput,
  CreateAgentDocument,
  CreateAgentInput,
  DeleteAgentDocument,
  GetAgentsDocument,
  GetAgentsQuery,
  GetSkillsDocument,
  SkillFieldsFragment,
  UpdateAgentDocument,
  UpdateAgentInput,
} from "../../__generated__/graphql";

interface AgentFormData {
  id: string;
  name: string;
  representativeColor: string;
  skillIds: string[];
}

// Type for the agent data returned by the query
type AgentWithSkills = {
  __typename?: "Agent";
  id: string;
  name: string;
  representativeColor: string;
  skills: Array<SkillFieldsFragment | null>;
};

const AgentManagement: React.FC = () => {
  const { t } = useTranslation();

  // Additional query for skills (for the modal)
  const {
    data: skillsData,
    loading: skillsLoading,
    error: skillsError,
  } = useQuery(GetSkillsDocument);

  const managementConfig = {
    documents: {
      get: GetAgentsDocument,
      create: CreateAgentDocument,
      update: UpdateAgentDocument,
      delete: DeleteAgentDocument,
    },
    modalConfig: MODAL_CONFIGS.AGENT,
    dataAccessor: (data: GetAgentsQuery | undefined) => data?.agents,
    entityFinder: (agents: AgentWithSkills[] | undefined, id: string) =>
      agents?.find((agent) => agent.id === id),
    getInitialFormData: (): AgentFormData => ({
      id: "",
      name: "",
      representativeColor: "#0066cc",
      skillIds: [],
    }),
    mapToFormData: (agent: AgentWithSkills): AgentFormData => ({
      id: agent.id,
      name: agent.name,
      representativeColor: agent.representativeColor,
      skillIds: agent.skills?.map((s) => s?.id).filter(Boolean) || [],
    }),
    mapToCreateInput: (formData: AgentFormData): CreateAgentInput => ({
      agentInput: {
        id: formData.id,
        name: formData.name,
        representativeColor: formData.representativeColor,
        skillIds: formData.skillIds,
      } as AgentInput,
    }),
    mapToUpdateInput: (
      formData: AgentFormData,
      entityId: string,
    ): UpdateAgentInput => ({
      id: entityId,
      agentInput: {
        id: formData.id,
        name: formData.name,
        representativeColor: formData.representativeColor,
        skillIds: formData.skillIds,
      } as AgentInput,
    }),
    validateForm: (formData: AgentFormData) => !!formData.name.trim(),
    i18nKeys: {
      componentName: "AgentManagement",
      operations: {
        get: "GetAgents",
        create: "CreateAgent",
        update: "UpdateAgent",
        delete: "DeleteAgent",
      },
      messages: {
        deleteConfirm: "agents.deleteConfirm",
        failedToCreate: "agents.failedToCreate",
        failedToUpdate: "agents.failedToUpdate",
        failedToDelete: "agents.failedToDelete",
      },
    },
    paths: {
      create: "/management/agents/create",
      edit: (id: string) => `/management/agents/${id}/edit`,
    },
  };

  const {
    entities: agents,
    queryLoading,
    queryError,
    isMutating,
    formData,
    updateField,
    toggleArrayItem,
    isModalOpen,
    isEditing,
    handleSubmit,
    handleDelete,
    handleCloseModal,
    handleModalExited,
    navigateToCreate,
    navigateToEdit,
    refetch,
    isValid,
  } = useManagementCRUD<
    AgentWithSkills,
    AgentFormData,
    CreateAgentInput,
    UpdateAgentInput,
    GetAgentsQuery | undefined
  >(managementConfig);

  // Prepare skills for checkbox list
  const skillItems =
    skillsData?.skills?.map((skill) => ({
      id: skill.id,
      label: skill.name,
      description: skill.description,
    })) || [];

  const headerContent = (
    <ManagementHeader
      icon="bi-people-fill"
      title={t("agents.title")}
      count={agents?.length}
      addButtonText={t("agents.addAgent")}
      onAddClick={navigateToCreate}
      addButtonDisabled={queryLoading}
    />
  );

  const emptyStateContent = (
    <MotionEmptyState
      icon="bi-people"
      title={t("agents.noAgentsDefined")}
      description={t("agents.createFirstAgent")}
      buttonText={t("agents.createFirstButton")}
      onButtonClick={navigateToCreate}
    />
  );

  return (
    <>
      <ManagementContainer
        loading={queryLoading}
        error={queryError}
        isMutating={isMutating}
        errorTitle={t("agents.loadingError")}
        errorMessage={t("agents.loadingErrorDescription")}
        onRetry={refetch}
        header={headerContent}
        showEmptyState={agents?.length === 0}
        emptyState={emptyStateContent}
        gridClassName="agents-grid flex-grow-1"
        skeletonVariant="agent"
        skeletonCount={3}
      >
        <AnimatePresence mode="popLayout">
          {agents?.map((agent, index) => (
            <ManagementCard key={agent.id} index={index}>
              {/* Agent Header */}
              <div className="agent-header p-3 border-bottom">
                <div className="d-flex align-items-center justify-content-between">
                  <div className="d-flex align-items-center">
                    <div
                      className="agent-color-indicator rounded-circle me-3"
                      style={{
                        width: "32px",
                        height: "32px",
                        backgroundColor: agent.representativeColor,
                        border: "3px solid var(--app-white)",
                        boxShadow: "var(--app-shadow)",
                      }}
                    />
                    <div>
                      <h6 className="mb-0 fw-semibold">{agent.name}</h6>
                      <small className="text-muted">
                        {agent.representativeColor}
                      </small>
                    </div>
                  </div>
                  <div className="d-flex gap-1">
                    <MotionButton
                      variant="outline-primary"
                      size="sm"
                      onClick={() => navigateToEdit(agent.id)}
                      className="d-flex align-items-center"
                    >
                      <i className="bi bi-pencil"></i>
                    </MotionButton>
                    <MotionButton
                      variant="outline-danger"
                      size="sm"
                      onClick={() => handleDelete(agent.id)}
                      className="d-flex align-items-center"
                    >
                      <i className="bi bi-trash"></i>
                    </MotionButton>
                  </div>
                </div>
              </div>

              {/* Skills Section */}
              <div className="agent-skills p-3">
                <RelatedItemsList
                  icon="bi-tools"
                  label={t("modals.skills")}
                  count={agent.skills?.filter((skill) => !!skill).length || 0}
                  items={
                    agent.skills
                      ?.filter((skill) => !!skill)
                      .map((skill) => ({
                        id: skill!.id,
                        name: skill!.name,
                      })) || []
                  }
                  emptyText={t("modals.noSkillsAssigned")}
                  badgeVariant="info"
                />
              </div>
            </ManagementCard>
          ))}
        </AnimatePresence>
      </ManagementContainer>

      <UnifiedModal
        show={isModalOpen}
        onHide={handleCloseModal}
        onExited={handleModalExited}
        title={isEditing ? t("modals.editAgent") : t("modals.addAgent")}
        icon="bi-person"
        onSubmit={handleSubmit}
        isValid={isValid}
        isEditing={isEditing}
        loading={isMutating}
        submitText={
          isEditing ? t("modals.updateAgent") : t("modals.createAgent")
        }
      >
        <FormSection>
          <FormRow>
            <FormColumn md={6}>
              <FormField
                label={t("modals.name")}
                value={formData.name}
                onChange={(value) => updateField("name", value)}
                required
              />
            </FormColumn>
            <FormColumn md={6}>
              <FormField
                label={t("modals.representativeColor")}
                type="color"
                value={formData.representativeColor}
                onChange={(value) => updateField("representativeColor", value)}
                required
              />
            </FormColumn>
          </FormRow>
        </FormSection>

        <CheckboxList
          label={t("modals.skills")}
          items={skillItems}
          selectedIds={formData.skillIds}
          onToggle={(skillId) => toggleArrayItem("skillIds", skillId)}
          loading={skillsLoading}
          error={skillsError}
          emptyText={t("modals.noSkillsAvailable")}
          errorTitle={t("modals.loadingSkillsError")}
          errorDescription={t("modals.loadingSkillsErrorDescription")}
          onRetry={() => window.location.reload()}
        />
      </UnifiedModal>
    </>
  );
};

export default AgentManagement;
