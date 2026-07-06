import React from "react";
import { useQuery } from "@apollo/client";
import { useTranslation } from "react-i18next";
import { AnimatePresence } from "framer-motion";
import { useManagementCRUD } from "../../hooks/useManagementCRUD";
import { MODAL_CONFIGS } from "../../hooks/useRouterModal";
import { MotionEmptyState, MotionButton } from "../motion";
import {
  ManagementContainer,
  ManagementHeader,
  ManagementCard,
  RelatedItemsList,
  FormSection,
  FormField,
  FormRow,
  FormColumn,
  CheckboxList,
} from "./common";
import { UnifiedModal } from "../common/UnifiedModal";
import { PropertyEditor } from "../common/PropertyEditor";
import "./styles/management.css";
import {
  CreateSkillDocument,
  CreateSkillInput,
  DeleteSkillDocument,
  GetAgentsDocument,
  GetSkillsDocument,
  SkillInput,
  PropertyInput,
  UpdateSkillDocument,
  UpdateSkillInput,
  GetSkillsQuery,
  PropertyFieldsFragment,
  AgentFieldsFragment,
} from "../../__generated__/graphql";
import { mapPropertyFieldsFragmentToPropertyInput } from "../../types/mapping/property/mapPropertyFieldsFragmentToPropertyInput.ts";

interface SkillFormData {
  id: string;
  name: string;
  description: string;
  agentIds: string[];
  properties: PropertyInput[];
}

// Type for the skill data returned by the query
type SkillWithAgents = {
  __typename?: "Skill";
  id: string;
  name: string;
  description: string;
  properties: Array<PropertyFieldsFragment>;
  agents: Array<AgentFieldsFragment | null>;
};

const SkillManagement: React.FC = () => {
  const { t } = useTranslation();

  // Additional query for agents (for the modal)
  const {
    data: agentsData,
    loading: agentsLoading,
    error: agentsError,
  } = useQuery(GetAgentsDocument);

  const managementConfig = {
    documents: {
      get: GetSkillsDocument,
      create: CreateSkillDocument,
      update: UpdateSkillDocument,
      delete: DeleteSkillDocument,
    },
    modalConfig: MODAL_CONFIGS.SKILL,
    dataAccessor: (data: GetSkillsQuery | undefined) => data?.skills,
    entityFinder: (skills: SkillWithAgents[] | undefined, id: string) =>
      skills?.find((skill) => skill.id === id),
    getInitialFormData: (): SkillFormData => ({
      id: "",
      name: "",
      description: "",
      agentIds: [],
      properties: [],
    }),
    mapToFormData: (skill: SkillWithAgents): SkillFormData => ({
      id: skill.id,
      name: skill.name,
      description: skill.description,
      agentIds: skill.agents?.map((agent) => agent?.id).filter(Boolean) || [],
      properties: (skill.properties || []).map((prop) =>
        mapPropertyFieldsFragmentToPropertyInput(prop),
      ),
    }),
    mapToCreateInput: (formData: SkillFormData): CreateSkillInput => {
      // Helper to recursively remove __typename from objects
      const cleanObject = (obj: unknown): unknown => {
        if (obj === null || typeof obj !== "object") return obj;
        if (Array.isArray(obj)) return obj.map(cleanObject);
        const cleaned: Record<string, unknown> = {};
        for (const [key, value] of Object.entries(
          obj as Record<string, unknown>,
        )) {
          if (key !== "__typename") {
            cleaned[key] = cleanObject(value);
          }
        }
        return cleaned;
      };

      const input = {
        skillInput: {
          id: formData.id,
          name: formData.name,
          description: formData.description,
          agentIds: formData.agentIds,
          properties: formData.properties,
        } as SkillInput,
      };

      return cleanObject(input) as CreateSkillInput;
    },
    mapToUpdateInput: (
      formData: SkillFormData,
      entityId: string,
    ): UpdateSkillInput => {
      // Helper to recursively remove __typename from objects
      const cleanObject = (obj: unknown): unknown => {
        if (obj === null || typeof obj !== "object") return obj;
        if (Array.isArray(obj)) return obj.map(cleanObject);
        const cleaned: Record<string, unknown> = {};
        for (const [key, value] of Object.entries(
          obj as Record<string, unknown>,
        )) {
          if (key !== "__typename") {
            cleaned[key] = cleanObject(value);
          }
        }
        return cleaned;
      };

      const input = {
        id: entityId,
        skillInput: {
          id: formData.id,
          name: formData.name,
          description: formData.description,
          agentIds: formData.agentIds,
          properties: formData.properties,
        } as SkillInput,
      };

      return cleanObject(input) as UpdateSkillInput;
    },
    validateForm: (formData: SkillFormData) =>
      !!formData.name.trim() && !!formData.description.trim(),
    i18nKeys: {
      componentName: "SkillManagement",
      operations: {
        get: "GetSkills",
        create: "CreateSkill",
        update: "UpdateSkill",
        delete: "DeleteSkill",
      },
      messages: {
        deleteConfirm: "skills.deleteConfirm",
        failedToCreate: "skills.failedToCreate",
        failedToUpdate: "skills.failedToUpdate",
        failedToDelete: "skills.failedToDelete",
      },
    },
    paths: {
      create: "/management/skills/create",
      edit: (id: string) => `/management/skills/${id}/edit`,
    },
  };

  const {
    entities: skills,
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
    SkillWithAgents,
    SkillFormData,
    CreateSkillInput,
    UpdateSkillInput,
    GetSkillsQuery | undefined
  >(managementConfig);

  // Prepare agents for checkbox list
  const agentItems =
    agentsData?.agents?.map((agent) => ({
      id: agent.id,
      label: agent.name,
      description: undefined, // Agents don't have descriptions in the checkbox
    })) || [];

  const handlePropertiesChange = (newProperties: PropertyInput[]) => {
    updateField("properties", newProperties);
  };

  const headerContent = (
    <ManagementHeader
      icon="bi-tools"
      title={t("skills.title")}
      count={skills?.length}
      addButtonText={t("skills.addSkill")}
      onAddClick={navigateToCreate}
      addButtonDisabled={queryLoading}
    />
  );

  const emptyStateContent = (
    <MotionEmptyState
      icon="bi-lightning"
      title={t("skills.noSkillsDefined")}
      description={t("skills.createFirstSkill")}
      buttonText={t("skills.createFirstButton")}
      onButtonClick={navigateToCreate}
    />
  );

  return (
    <>
      <ManagementContainer
        loading={queryLoading}
        error={queryError}
        isMutating={isMutating}
        errorTitle={t("skills.loadingError")}
        errorMessage={t("skills.loadingErrorDescription")}
        onRetry={refetch}
        header={headerContent}
        showEmptyState={skills?.length === 0}
        emptyState={emptyStateContent}
        gridClassName="skills-grid flex-grow-1"
        skeletonVariant="skill"
        skeletonCount={3}
      >
        <AnimatePresence mode="popLayout">
          {skills?.map((skill, index) => (
            <ManagementCard key={skill.id} index={index}>
              {/* Skill Header */}
              <div className="skill-header p-3 border-bottom">
                <div className="d-flex align-items-center justify-content-between">
                  <div className="flex-grow-1">
                    <h6 className="mb-1 fw-semibold">{skill.name}</h6>
                    <p className="mb-0 text-muted small">{skill.description}</p>
                  </div>
                  <div className="d-flex gap-1">
                    <MotionButton
                      variant="outline-primary"
                      size="sm"
                      onClick={() => navigateToEdit(skill.id)}
                      className="d-flex align-items-center"
                    >
                      <i className="bi bi-pencil"></i>
                    </MotionButton>
                    <MotionButton
                      variant="outline-danger"
                      size="sm"
                      onClick={() => handleDelete(skill.id)}
                      className="d-flex align-items-center"
                    >
                      <i className="bi bi-trash"></i>
                    </MotionButton>
                  </div>
                </div>
              </div>

              {/* Properties and Agents */}
              <div className="skill-details p-3">
                <div className="row g-3">
                  <div className="col-md-6">
                    <RelatedItemsList
                      icon="bi-gear"
                      label={t("skills.properties")}
                      count={skill.properties?.length || 0}
                      items={[]} // Properties are complex objects, just show count
                      badgeVariant="secondary"
                    />
                  </div>
                  <div className="col-md-6">
                    <RelatedItemsList
                      icon="bi-people"
                      label={t("modals.agents")}
                      count={
                        skill.agents?.filter((agent) => !!agent).length || 0
                      }
                      items={
                        skill.agents
                          ?.filter((agent) => !!agent)
                          .map((agent) => ({
                            id: agent!.id,
                            name: agent!.name,
                          })) || []
                      }
                      badgeVariant="info"
                    />
                  </div>
                </div>
              </div>
            </ManagementCard>
          ))}
        </AnimatePresence>
      </ManagementContainer>

      <UnifiedModal
        show={isModalOpen}
        onHide={handleCloseModal}
        onExited={handleModalExited}
        title={isEditing ? t("skills.editSkill") : t("skills.addSkill")}
        icon="bi-lightning-charge"
        onSubmit={handleSubmit}
        isValid={isValid}
        isEditing={isEditing}
        loading={isMutating}
        submitText={
          isEditing ? t("skills.updateSkill") : t("skills.createSkill")
        }
      >
        <FormSection>
          <FormRow>
            <FormColumn md={6}>
              <FormField
                label={t("skills.skillName")}
                value={formData.name}
                onChange={(value) => updateField("name", value)}
                required
              />
            </FormColumn>
            <FormColumn md={6}>
              <FormField
                label={t("modals.description")}
                value={formData.description}
                onChange={(value) => updateField("description", value)}
                required
              />
            </FormColumn>
          </FormRow>
        </FormSection>

        <CheckboxList
          label={t("skills.agentAssignments")}
          items={agentItems}
          selectedIds={formData.agentIds}
          onToggle={(agentId) => toggleArrayItem("agentIds", agentId)}
          loading={agentsLoading}
          error={agentsError}
          emptyText={t("skills.noAgentsAvailable")}
          errorTitle={t("errors.failedToLoadAgents")}
          errorDescription={t("modals.loadingSkillsErrorDescription")}
          onRetry={() => window.location.reload()}
          maxHeight="150px"
        />

        <PropertyEditor
          properties={formData.properties}
          onChange={handlePropertiesChange}
        />
      </UnifiedModal>
    </>
  );
};

export default SkillManagement;
