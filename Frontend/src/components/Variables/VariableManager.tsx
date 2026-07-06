import React, { useMemo, useState } from "react";
import { useMutation, useQuery } from "@apollo/client";
import { useTranslation } from "react-i18next";
import VariableList from "./VariableList";
import VariableEditor from "./VariableEditor";
import {
  convertVariableFromGraphQL,
  convertVariableToGraphQLInput,
  ValueType,
  VariableDefinition,
  VariableDefinitionGraphQL,
  VariableDefinitionInput,
} from "./types";
import {
  AddVariableToProcedureDocument,
  GetProcedureVariablesDocument,
  RemoveProcedureVariableDocument,
  UpdateProcedureVariableDocument,
} from "../../graphql/variables";
import { ManagementContainer, ManagementHeader } from "../management/common";
import { MotionEmptyState } from "../motion";
import { createLogger } from "../../utils/logger";

const log = createLogger("Variables");

export interface VariableManagerProps {
  procedureId: string;
}

const VariableManager: React.FC<VariableManagerProps> = ({ procedureId }) => {
  const { t } = useTranslation();
  const [isEditorOpen, setIsEditorOpen] = useState(false);
  const [editingVariable, setEditingVariable] = useState<
    VariableDefinition | undefined
  >(undefined);
  const [isMutating, setIsMutating] = useState(false);

  const {
    loading,
    error: queryError,
    data,
    refetch,
  } = useQuery(GetProcedureVariablesDocument, {
    variables: { procedureId },
    skip: !procedureId,
  });

  const [addVariableToProcedure] = useMutation(AddVariableToProcedureDocument, {
    refetchQueries: [
      { query: GetProcedureVariablesDocument, variables: { procedureId } },
    ],
  });

  const [updateProcedureVariable] = useMutation(
    UpdateProcedureVariableDocument,
    {
      refetchQueries: [
        { query: GetProcedureVariablesDocument, variables: { procedureId } },
      ],
    },
  );

  const [removeProcedureVariable] = useMutation(
    RemoveProcedureVariableDocument,
    {
      refetchQueries: [
        { query: GetProcedureVariablesDocument, variables: { procedureId } },
      ],
    },
  );

  const handleAdd = () => {
    setEditingVariable(undefined);
    setIsEditorOpen(true);
  };

  const handleEdit = (variable: VariableDefinition) => {
    setEditingVariable(variable);
    setIsEditorOpen(true);
  };

  const handleSave = async (
    variable: VariableDefinitionInput & {
      allowedValues?: string[];
      elementType?: ValueType;
    },
  ) => {
    try {
      setIsMutating(true);
      // Convert UI format to GraphQL input format
      const graphqlVariable = convertVariableToGraphQLInput(variable);

      if (editingVariable) {
        await updateProcedureVariable({
          variables: {
            procedureId,
            variableName: editingVariable.name,
            variable: graphqlVariable,
          },
        });
      } else {
        await addVariableToProcedure({
          variables: {
            procedureId,
            variable: graphqlVariable,
          },
        });
      }
      setIsEditorOpen(false);
      setEditingVariable(undefined);
    } catch (err) {
      log.error("Failed to save variable:", err);
      throw err;
    } finally {
      setIsMutating(false);
    }
  };

  const handleDelete = async (variableName: string) => {
    try {
      setIsMutating(true);
      await removeProcedureVariable({
        variables: {
          procedureId,
          variableName,
        },
      });
    } catch (err) {
      log.error("Failed to delete variable:", err);
      throw err;
    } finally {
      setIsMutating(false);
    }
  };

  const handleCloseEditor = () => {
    setIsEditorOpen(false);
    setEditingVariable(undefined);
  };

  // Convert GraphQL variables to UI format
  const variables = useMemo(() => {
    const graphqlVariables =
      (data?.procedureById?.variables as VariableDefinitionGraphQL[]) || [];
    return graphqlVariables.map(convertVariableFromGraphQL);
  }, [data?.procedureById?.variables]);
  const existingVariableNames = variables.map(
    (v: VariableDefinition) => v.name,
  );

  const headerContent = (
    <ManagementHeader
      icon="bi-code-square"
      title={t("variables.title")}
      count={variables?.length}
      addButtonText={t("variables.addVariable")}
      onAddClick={handleAdd}
      addButtonDisabled={loading}
    />
  );

  const emptyStateContent = (
    <MotionEmptyState
      icon="bi-braces"
      title={t("variables.noVariablesDefined")}
      description={t("variables.createFirstVariable")}
      buttonText={t("variables.createFirstButton")}
      onButtonClick={handleAdd}
    />
  );

  return (
    <>
      <ManagementContainer
        loading={loading}
        error={queryError}
        isMutating={isMutating}
        mutatingText={
          editingVariable
            ? t("variables.updatingVariable")
            : t("variables.creatingVariable")
        }
        errorTitle={t("variables.loadingError")}
        errorMessage={t("variables.loadingErrorDescription")}
        onRetry={refetch}
        header={headerContent}
        showEmptyState={variables?.length === 0}
        emptyState={emptyStateContent}
        containerClassName="position-relative h-100"
      >
        <VariableList
          variables={variables}
          onEdit={handleEdit}
          onDelete={handleDelete}
          onAdd={handleAdd}
        />
      </ManagementContainer>

      <VariableEditor
        isOpen={isEditorOpen}
        onClose={handleCloseEditor}
        onSave={handleSave}
        editingVariable={editingVariable}
        existingVariableNames={existingVariableNames}
      />
    </>
  );
};

export default VariableManager;
