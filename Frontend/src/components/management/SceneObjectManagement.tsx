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
  CreateSceneObjectDocument,
  CreateSceneObjectInput,
  DeleteSceneObjectDocument,
  GetSceneObjectsDocument,
  SceneObjectInput,
  UpdateSceneObjectDocument,
  UpdateSceneObjectInput,
  GetSceneObjectsQuery,
  SceneObject,
} from "../../__generated__/graphql";

interface SceneObjectFormData {
  id: string;
  name: string;
  position: {
    x: number;
    y: number;
    z: number;
    alpha: number;
    beta: number;
    gamma: number;
  };
}

const SceneObjectManagement: React.FC = () => {
  const { t } = useTranslation();

  const managementConfig = {
    documents: {
      get: GetSceneObjectsDocument,
      create: CreateSceneObjectDocument,
      update: UpdateSceneObjectDocument,
      delete: DeleteSceneObjectDocument,
    },
    modalConfig: MODAL_CONFIGS.SCENE_OBJECT,
    dataAccessor: (data: GetSceneObjectsQuery | undefined) =>
      data?.sceneObjects,
    entityFinder: (sceneObjects: SceneObject[] | undefined, id: string) =>
      sceneObjects?.find((obj) => obj.id === id),
    getInitialFormData: (): SceneObjectFormData => ({
      id: "",
      name: "",
      position: {
        x: 0,
        y: 0,
        z: 0,
        alpha: 0,
        beta: 0,
        gamma: 0,
      },
    }),
    mapToFormData: (sceneObject: SceneObject): SceneObjectFormData => ({
      id: sceneObject.id,
      name: sceneObject.name,
      position: {
        x: sceneObject.position.x,
        y: sceneObject.position.y,
        z: sceneObject.position.z,
        alpha: sceneObject.position.alpha,
        beta: sceneObject.position.beta,
        gamma: sceneObject.position.gamma,
      },
    }),
    mapToCreateInput: (
      formData: SceneObjectFormData,
    ): CreateSceneObjectInput => ({
      sceneObject: {
        id: formData.id,
        name: formData.name,
        position: formData.position,
      } as SceneObjectInput,
    }),
    mapToUpdateInput: (
      formData: SceneObjectFormData,
    ): UpdateSceneObjectInput => ({
      sceneObject: {
        id: formData.id,
        name: formData.name,
        position: formData.position,
      } as SceneObjectInput,
    }),
    validateForm: (formData: SceneObjectFormData) => !!formData.name.trim(),
    i18nKeys: {
      componentName: "SceneObjectManagement",
      operations: {
        get: "GetSceneObjects",
        create: "CreateSceneObject",
        update: "UpdateSceneObject",
        delete: "DeleteSceneObject",
      },
      messages: {
        deleteConfirm: "sceneObjects.deleteConfirm",
        failedToCreate: "sceneObjects.failedToCreate",
        failedToUpdate: "sceneObjects.failedToUpdate",
        failedToDelete: "sceneObjects.failedToDelete",
      },
    },
    paths: {
      create: "/management/scene-objects/create",
      edit: (id: string) => `/management/scene-objects/${id}/edit`,
    },
  };

  const {
    entities: sceneObjects,
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
    field: keyof SceneObjectFormData["position"],
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
      icon="bi-box-seam"
      title={t("sceneObjects.title")}
      count={sceneObjects?.length}
      addButtonText={t("sceneObjects.addObject")}
      onAddClick={navigateToCreate}
      addButtonDisabled={queryLoading}
    />
  );

  const emptyStateContent = (
    <MotionEmptyState
      icon="bi-box"
      title={t("sceneObjects.noObjectsDefined")}
      description={t("sceneObjects.createFirstObject")}
      buttonText={t("sceneObjects.createFirstButton")}
      onButtonClick={navigateToCreate}
    />
  );

  return (
    <>
      <ManagementContainer
        loading={queryLoading}
        error={queryError}
        isMutating={isMutating}
        errorTitle={t("sceneObjects.loadingError")}
        errorMessage={t("sceneObjects.loadingErrorDescription")}
        onRetry={refetch}
        header={headerContent}
        showEmptyState={sceneObjects?.length === 0}
        emptyState={emptyStateContent}
        gridClassName="scene-objects-grid flex-grow-1"
        skeletonVariant="scene"
        skeletonCount={3}
      >
        <AnimatePresence mode="popLayout">
          {sceneObjects?.map((object, index) => (
            <ManagementCard key={object.id} index={index}>
              {/* Object Header */}
              <div className="object-header p-3 border-bottom">
                <div className="d-flex align-items-center justify-content-between">
                  <div className="d-flex align-items-center">
                    <div className="object-indicator rounded me-3 d-flex align-items-center justify-content-center">
                      <i className="bi bi-box-seam text-info"></i>
                    </div>
                    <div>
                      <h6 className="mb-0 fw-semibold">{object.name}</h6>
                      <small className="text-muted">
                        {t("sceneObjects.title")}
                      </small>
                    </div>
                  </div>
                  <div className="d-flex gap-1">
                    <button
                      className="btn btn-outline-primary btn-sm d-flex align-items-center"
                      onClick={() => navigateToEdit(object.id)}
                    >
                      <i className="bi bi-pencil"></i>
                    </button>
                    <button
                      className="btn btn-outline-danger btn-sm d-flex align-items-center"
                      onClick={() => handleDelete(object.id)}
                    >
                      <i className="bi bi-trash"></i>
                    </button>
                  </div>
                </div>
              </div>

              {/* Position Details */}
              <div className="object-details p-3">
                <div className="row g-3">
                  <div className="col-md-6">
                    <div className="d-flex align-items-center mb-2">
                      <i className="bi bi-arrows-move text-secondary me-2"></i>
                      <small className="text-muted fw-medium">
                        {t("sceneObjects.position")}
                      </small>
                    </div>
                    <div className="position-display">
                      <div className="d-flex gap-2">
                        <span className="badge bg-primary">
                          X: {object.position.x.toFixed(1)}
                        </span>
                        <span className="badge bg-success">
                          Y: {object.position.y.toFixed(1)}
                        </span>
                        <span className="badge bg-info">
                          Z: {object.position.z.toFixed(1)}
                        </span>
                      </div>
                    </div>
                  </div>
                  <div className="col-md-6">
                    <div className="d-flex align-items-center mb-2">
                      <i className="bi bi-arrow-clockwise text-secondary me-2"></i>
                      <small className="text-muted fw-medium">
                        {t("sceneObjects.rotation")}
                      </small>
                    </div>
                    <div className="rotation-display">
                      <div className="d-flex gap-2">
                        <span className="badge bg-warning text-dark">
                          α: {object.position.alpha.toFixed(1)}°
                        </span>
                        <span className="badge bg-danger">
                          β: {object.position.beta.toFixed(1)}°
                        </span>
                        <span className="badge bg-secondary">
                          γ: {object.position.gamma.toFixed(1)}°
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
        title={
          isEditing ? t("sceneObjects.editObject") : t("sceneObjects.addObject")
        }
        icon="bi-box-seam"
        onSubmit={handleSubmit}
        isValid={isValid}
        isEditing={isEditing}
        loading={isMutating}
        submitText={
          isEditing
            ? t("sceneObjects.updateObject")
            : t("sceneObjects.createObject")
        }
      >
        <FormSection>
          <FormRow>
            <FormColumn md={12}>
              <FormField
                label={t("sceneObjects.objectName")}
                value={formData.name}
                onChange={(value) => updateField("name", value)}
                required
              />
            </FormColumn>
          </FormRow>
        </FormSection>

        <PositionFormFields
          position={formData.position}
          onPositionChange={updatePosition}
        />
      </UnifiedModal>
    </>
  );
};

export default SceneObjectManagement;
