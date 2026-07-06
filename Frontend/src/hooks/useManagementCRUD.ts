import { useCallback, useEffect, useRef, useState } from "react";
import { useMutation, useQuery, DocumentNode } from "@apollo/client";
import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router-dom";
import { v4 as uuidv4 } from "uuid";
import { useApolloError, useError } from "../hooks";
import { MODAL_CONFIGS, useRouterModal } from "./useRouterModal";
import { useManagementModalStore } from "../stores/managementModalStore";
import { createLogger } from "../utils/logger";

const log = createLogger("ManagementCRUD");

export interface ManagementCRUDConfig<
  TData,
  TFormData,
  TCreateInput,
  TUpdateInput,
  TQueryData = unknown,
> {
  // GraphQL documents
  documents: {
    get: DocumentNode;
    create: DocumentNode;
    update: DocumentNode;
    delete: DocumentNode;
  };

  // Modal configuration
  modalConfig: (typeof MODAL_CONFIGS)[keyof typeof MODAL_CONFIGS];

  // Data accessors and transformers
  dataAccessor: (data: TQueryData) => TData[] | undefined;
  entityFinder: (data: TData[] | undefined, id: string) => TData | undefined;

  // Form data management
  getInitialFormData: () => TFormData;
  mapToFormData: (entity: TData) => TFormData;
  mapToCreateInput: (formData: TFormData) => TCreateInput;
  mapToUpdateInput: (formData: TFormData, entityId: string) => TUpdateInput;

  // Validation
  validateForm: (formData: TFormData) => boolean;

  // i18n configuration
  i18nKeys: {
    componentName: string;
    operations: {
      get: string;
      create: string;
      update: string;
      delete: string;
    };
    messages: {
      deleteConfirm: string;
      failedToCreate: string;
      failedToUpdate: string;
      failedToDelete: string;
    };
  };

  // Navigation paths
  paths: {
    create: string;
    edit: (id: string) => string;
  };
}

export function useManagementCRUD<
  TData,
  TFormData,
  TCreateInput,
  TUpdateInput,
  TQueryData = unknown,
>(
  config: ManagementCRUDConfig<
    TData,
    TFormData,
    TCreateInput,
    TUpdateInput,
    TQueryData
  >,
) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { addError } = useError();

  // Modal state — zustand store is the source of truth for visibility,
  // while useRouterModal provides URL parsing (isEditing, entityId) and navigation.
  const {
    isOpen,
    open: openModal,
    close: closeModalStore,
  } = useManagementModalStore();
  const {
    isModalOpen: urlModalOpen,
    isEditing,
    entityId,
    closeModal: urlCloseModal,
  } = useRouterModal(config.modalConfig);

  // Guard: when the user explicitly closes the modal, the URL hasn't
  // changed yet (navigation is deferred to onExited). Without this ref
  // the sync effect below would immediately reopen the modal.
  const isClosingRef = useRef(false);

  // Sync URL → store for deep linking and initial page load.
  // Skipped while a user-initiated close is in progress.
  useEffect(() => {
    if (urlModalOpen && !isOpen && !isClosingRef.current) {
      openModal();
    }
  }, [urlModalOpen, isOpen, openModal]);

  // Reset store when component unmounts (e.g. navigating away from management)
  useEffect(() => {
    return () => closeModalStore();
  }, [closeModalStore]);

  // Form state
  const [formData, setFormData] = useState<TFormData>(
    config.getInitialFormData(),
  );

  // GraphQL operations
  const {
    data: queryData,
    loading: queryLoading,
    error: queryError,
    refetch,
  } = useQuery(config.documents.get);

  const [createMutation, { loading: createLoading }] = useMutation(
    config.documents.create,
  );
  const [updateMutation, { loading: updateLoading }] = useMutation(
    config.documents.update,
  );
  const [deleteMutation, { loading: deleteLoading }] = useMutation(
    config.documents.delete,
  );

  // Error handling
  useApolloError(queryError, {
    componentName: config.i18nKeys.componentName,
    operation: config.i18nKeys.operations.get,
  });

  // Derived state
  const entities = config.dataAccessor(queryData) || [];
  const editingEntity = entityId
    ? config.entityFinder(entities, entityId)
    : undefined;
  const isMutating = createLoading || updateLoading || deleteLoading;

  // Modal lifecycle effect
  useEffect(() => {
    if (!isOpen) return;

    if (isEditing && editingEntity) {
      setFormData(config.mapToFormData(editingEntity));
    } else if (!isEditing) {
      setFormData({
        ...config.getInitialFormData(),
        id: uuidv4(),
      } as TFormData);
    }
  }, [isOpen, isEditing, editingEntity, config]);

  // Form handlers
  const updateField = <K extends keyof TFormData>(
    field: K,
    value: TFormData[K],
  ) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
  };

  const updateNestedField = <
    K extends keyof TFormData,
    NK extends keyof TFormData[K],
  >(
    parentKey: K,
    nestedKey: NK,
    value: TFormData[K][NK],
  ) => {
    setFormData((prev) => ({
      ...prev,
      [parentKey]: {
        ...prev[parentKey],
        [nestedKey]: value,
      },
    }));
  };

  const toggleArrayItem = <K extends keyof TFormData>(
    arrayKey: K,
    item: string,
  ) => {
    setFormData((prev) => {
      const array = prev[arrayKey] as unknown as string[];
      return {
        ...prev,
        [arrayKey]: array.includes(item)
          ? array.filter((id) => id !== item)
          : [...array, item],
      } as TFormData;
    });
  };

  const resetForm = () => {
    setFormData(config.getInitialFormData());
  };

  const handleCloseModal = () => {
    if (createLoading || updateLoading) return;
    resetForm();
    isClosingRef.current = true;
    closeModalStore(); // Modal gets show={false}, exit animation starts
    // Navigation deferred to handleModalExited — called by Modal's onExited
    // after the exit animation completes and portal is removed from the DOM.
  };

  const handleModalExited = useCallback(() => {
    isClosingRef.current = false;
    urlCloseModal();
  }, [urlCloseModal]);

  // CRUD operations
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!config.validateForm(formData)) {
      return;
    }

    try {
      if (isEditing && entityId && editingEntity) {
        const updateInput = config.mapToUpdateInput(formData, entityId);
        await updateMutation({ variables: { input: updateInput } });
      } else {
        const createInput = config.mapToCreateInput(formData);
        await createMutation({ variables: { input: createInput } });
      }

      handleCloseModal();
      refetch();
    } catch (error) {
      log.error(`${config.i18nKeys.componentName} save error:`, error);
      addError({
        message: isEditing
          ? t(config.i18nKeys.messages.failedToUpdate)
          : t(config.i18nKeys.messages.failedToCreate),
        severity: "error",
        retry: () => handleSubmit(e),
      });
    }
  };

  const handleDelete = async (id: string) => {
    if (window.confirm(t(config.i18nKeys.messages.deleteConfirm))) {
      try {
        await deleteMutation({ variables: { input: { id } } });
        refetch();
      } catch (error) {
        log.error(`${config.i18nKeys.componentName} delete error:`, error);
        addError({
          message: t(config.i18nKeys.messages.failedToDelete),
          severity: "error",
        });
      }
    }
  };

  // Navigation helpers
  const navigateToCreate = () => {
    openModal();
    navigate(config.paths.create);
  };
  const navigateToEdit = (id: string) => {
    openModal();
    navigate(config.paths.edit(id));
  };

  return {
    // Data
    entities,
    editingEntity,

    // Loading states
    queryLoading,
    queryError,
    isMutating,
    createLoading,
    updateLoading,
    deleteLoading,

    // Form state
    formData,
    setFormData,
    updateField,
    updateNestedField,
    toggleArrayItem,
    resetForm,

    // Modal state — driven by zustand store for reliable show/hide
    isModalOpen: isOpen,
    isEditing,
    entityId,

    // Actions
    handleSubmit,
    handleDelete,
    handleCloseModal,
    handleModalExited,
    navigateToCreate,
    navigateToEdit,
    refetch,

    // Validation
    isValid: config.validateForm(formData),
  };
}
