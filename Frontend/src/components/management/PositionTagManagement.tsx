import React from "react";
import { useTranslation } from "react-i18next";
import { AnimatePresence } from "framer-motion";
import { useManagementCRUD } from "../../hooks/useManagementCRUD";
import { MODAL_CONFIGS } from "../../hooks/useRouterModal";
import { MotionEmptyState } from "../motion";
import {
  FormColumn,
  FormField,
  FormRow,
  FormSection,
  ManagementCard,
  ManagementContainer,
  ManagementHeader,
  PositionFormFields,
} from "./common";
import { UnifiedModal } from "../common/UnifiedModal";
import "./styles/management.css";
import {
  CreatePositionTagDocument,
  CreatePositionTagInput,
  DeletePositionTagDocument,
  GetPositionTagsDocument,
  PositionTagInput,
  UpdatePositionTagDocument,
  UpdatePositionTagInput,
  GetPositionTagsQuery,
  PositionTag,
} from "../../__generated__/graphql";

interface PositionTagFormData {
  id: string;
  tag: string;
  position: {
    x: number;
    y: number;
    z: number;
    alpha: number;
    beta: number;
    gamma: number;
  };
}

const PositionTagManagement: React.FC = () => {
  const { t } = useTranslation();

  const managementConfig = {
    documents: {
      get: GetPositionTagsDocument,
      create: CreatePositionTagDocument,
      update: UpdatePositionTagDocument,
      delete: DeletePositionTagDocument,
    },
    modalConfig: MODAL_CONFIGS.POSITION_TAG,
    dataAccessor: (data: GetPositionTagsQuery | undefined) =>
      data?.positionTags,
    entityFinder: (positionTags: PositionTag[] | undefined, id: string) =>
      positionTags?.find((tag) => tag.id === id),
    getInitialFormData: (): PositionTagFormData => ({
      id: "",
      tag: "",
      position: {
        x: 0,
        y: 0,
        z: 0,
        alpha: 0,
        beta: 0,
        gamma: 0,
      },
    }),
    mapToFormData: (positionTag: PositionTag): PositionTagFormData => ({
      id: positionTag.id,
      tag: positionTag.tag,
      position: {
        x: positionTag.position.x,
        y: positionTag.position.y,
        z: positionTag.position.z,
        alpha: positionTag.position.alpha,
        beta: positionTag.position.beta,
        gamma: positionTag.position.gamma,
      },
    }),
    mapToCreateInput: (
      formData: PositionTagFormData,
    ): CreatePositionTagInput => ({
      positionTag: {
        id: formData.id,
        tag: formData.tag,
        position: formData.position,
      } as PositionTagInput,
    }),
    mapToUpdateInput: (
      formData: PositionTagFormData,
    ): UpdatePositionTagInput => ({
      positionTag: {
        id: formData.id,
        tag: formData.tag,
        position: formData.position,
      } as PositionTagInput,
    }),
    validateForm: (formData: PositionTagFormData) => !!formData.tag.trim(),
    i18nKeys: {
      componentName: "PositionTagManagement",
      operations: {
        get: "GetPositionTags",
        create: "CreatePositionTag",
        update: "UpdatePositionTag",
        delete: "DeletePositionTag",
      },
      messages: {
        deleteConfirm: "positionTags.deleteConfirm",
        failedToCreate: "positionTags.failedToCreate",
        failedToUpdate: "positionTags.failedToUpdate",
        failedToDelete: "positionTags.failedToDelete",
      },
    },
    paths: {
      create: "/management/position-tags/create",
      edit: (id: string) => `/management/position-tags/${id}/edit`,
    },
  };

  const {
    entities: positionTags,
    queryLoading,
    queryError,
    isMutating,
    formData,
    updateField,
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
  } = useManagementCRUD(managementConfig);

  const updatePosition = (
    field: keyof PositionTagFormData["position"],
    value: number,
  ) => {
    updateField("position", {
      ...(formData.position || {
        x: 0,
        y: 0,
        z: 0,
        alpha: 0,
        beta: 0,
        gamma: 0,
      }),
      [field]: value,
    });
  };

  const headerContent = (
    <ManagementHeader
      icon="bi-geo-alt-fill"
      title={t("positionTags.title")}
      count={positionTags?.length}
      addButtonText={t("positionTags.addTag")}
      onAddClick={navigateToCreate}
      addButtonDisabled={queryLoading}
    />
  );

  const emptyStateContent = (
    <MotionEmptyState
      icon="bi-geo-alt"
      title={t("positionTags.noTagsDefined")}
      description={t("positionTags.createFirstTag")}
      buttonText={t("positionTags.createFirstButton")}
      onButtonClick={navigateToCreate}
    />
  );

  return (
    <>
      <ManagementContainer
        loading={queryLoading}
        error={queryError}
        isMutating={isMutating}
        errorTitle={t("positionTags.loadingError")}
        errorMessage={t("positionTags.loadingErrorDescription")}
        onRetry={refetch}
        header={headerContent}
        showEmptyState={positionTags?.length === 0}
        emptyState={emptyStateContent}
        gridClassName="position-tags-grid flex-grow-1"
        skeletonVariant="position"
        skeletonCount={3}
      >
        <AnimatePresence mode="popLayout">
          {positionTags?.map((tag, index) => (
            <ManagementCard key={tag.id} index={index}>
              {/* Tag Header */}
              <div className="tag-header p-3 border-bottom">
                <div className="d-flex align-items-center justify-content-between">
                  <div className="d-flex align-items-center">
                    <div className="tag-indicator rounded-circle me-3 d-flex align-items-center justify-content-center">
                      <i className="bi bi-geo-alt-fill text-warning"></i>
                    </div>
                    <div>
                      <h6 className="mb-0 fw-semibold">{tag.tag}</h6>
                      <small className="text-muted">
                        {t("positionTags.title")}
                      </small>
                    </div>
                  </div>
                  <div className="d-flex gap-1">
                    <button
                      className="btn btn-outline-primary btn-sm d-flex align-items-center"
                      onClick={() => navigateToEdit(tag.id)}
                    >
                      <i className="bi bi-pencil"></i>
                    </button>
                    <button
                      className="btn btn-outline-danger btn-sm d-flex align-items-center"
                      onClick={() => handleDelete(tag.id)}
                    >
                      <i className="bi bi-trash"></i>
                    </button>
                  </div>
                </div>
              </div>

              {/* Position Details */}
              <div className="tag-details p-3">
                <div className="row g-3">
                  <div className="col-md-6">
                    <div className="d-flex align-items-center mb-2">
                      <i className="bi bi-arrows-move text-secondary me-2"></i>
                      <small className="text-muted fw-medium">
                        {t("positionTags.position")}
                      </small>
                    </div>
                    <div className="position-display">
                      <div className="d-flex gap-2">
                        <span className="badge bg-primary">
                          X: {tag.position.x.toFixed(1)}
                        </span>
                        <span className="badge bg-success">
                          Y: {tag.position.y.toFixed(1)}
                        </span>
                        <span className="badge bg-info">
                          Z: {tag.position.z.toFixed(1)}
                        </span>
                      </div>
                    </div>
                  </div>
                  <div className="col-md-6">
                    <div className="d-flex align-items-center mb-2">
                      <i className="bi bi-arrow-clockwise text-secondary me-2"></i>
                      <small className="text-muted fw-medium">
                        {t("positionTags.rotation")}
                      </small>
                    </div>
                    <div className="rotation-display">
                      <div className="d-flex gap-2">
                        <span className="badge bg-warning text-dark">
                          α: {tag.position.alpha.toFixed(1)}°
                        </span>
                        <span className="badge bg-danger">
                          β: {tag.position.beta.toFixed(1)}°
                        </span>
                        <span className="badge bg-secondary">
                          γ: {tag.position.gamma.toFixed(1)}°
                        </span>
                      </div>
                    </div>
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
        title={isEditing ? t("positionTags.editTag") : t("positionTags.addTag")}
        icon="bi-geo-alt-fill"
        onSubmit={handleSubmit}
        isValid={isValid}
        isEditing={isEditing}
        loading={isMutating}
        submitText={
          isEditing ? t("positionTags.updateTag") : t("positionTags.createTag")
        }
      >
        <FormSection>
          <FormRow>
            <FormColumn md={12}>
              <FormField
                label={t("positionTags.tagName")}
                value={formData.tag}
                onChange={(value) => updateField("tag", value)}
                required
              />
            </FormColumn>
          </FormRow>
        </FormSection>

        <PositionFormFields
          position={formData.position}
          onPositionChange={updatePosition}
          coordinateLabels={{
            x: t("positionTags.coordinateX"),
            y: t("positionTags.coordinateY"),
            z: t("positionTags.coordinateZ"),
          }}
          angleLabels={{
            alpha: t("positionTags.angleAlpha"),
            beta: t("positionTags.angleBeta"),
            gamma: t("positionTags.angleGamma"),
          }}
        />
      </UnifiedModal>
    </>
  );
};

export default PositionTagManagement;
