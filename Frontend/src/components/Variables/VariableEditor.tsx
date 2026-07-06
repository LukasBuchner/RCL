import React, { useEffect, useState } from "react";
import { Alert, Form } from "react-bootstrap";
import { useQuery } from "@apollo/client";
import { useTranslation } from "react-i18next";
import { UnifiedModal } from "../common/UnifiedModal";
import VariableTypeSelector from "./VariableTypeSelector";
import {
  ValueType,
  VariableDefinition,
  VariableDefinitionInput,
} from "./types";
import {
  GetPositionTagsDocument,
  GetSceneObjectsDocument,
} from "../../__generated__/graphql";

export interface VariableEditorProps {
  isOpen: boolean;
  onClose: () => void;
  onSave: (
    variable: VariableDefinitionInput & {
      allowedValues?: string[];
      elementType?: ValueType;
    },
  ) => Promise<void>;
  editingVariable?: VariableDefinition;
  existingVariableNames: string[];
}

const VariableEditor: React.FC<VariableEditorProps> = ({
  isOpen,
  onClose,
  onSave,
  editingVariable,
  existingVariableNames,
}) => {
  const { t } = useTranslation();
  const isEditMode = !!editingVariable;

  const [name, setName] = useState("");
  const [type, setType] = useState<ValueType>("String");
  const [defaultValue, setDefaultValue] = useState("");
  const [description, setDescription] = useState("");
  const [error, setError] = useState("");
  const [isSaving, setIsSaving] = useState(false);

  // Fetch scene objects and position tags for dropdowns
  const { data: sceneObjectsData } = useQuery(GetSceneObjectsDocument);
  const { data: positionTagsData } = useQuery(GetPositionTagsDocument);

  const sceneObjects = sceneObjectsData?.sceneObjects || [];
  const positionTags = positionTagsData?.positionTags || [];

  useEffect(() => {
    if (editingVariable) {
      setName(editingVariable.name);
      setType(editingVariable.type);
      setDefaultValue(editingVariable.defaultValue || "");
      setDescription(editingVariable.description || "");
    } else {
      setName("");
      setType("String");
      setDefaultValue("");
      setDescription("");
    }
    setError("");
  }, [editingVariable, isOpen]);

  const validateName = (name: string): string | null => {
    if (!name.trim()) {
      return t("errors.nameRequired");
    }

    if (!/^[a-zA-Z_][a-zA-Z0-9_]*$/.test(name)) {
      return t("variables.invalidName");
    }

    if (!isEditMode && existingVariableNames.includes(name)) {
      return t("variables.duplicateName");
    }

    return null;
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");

    const nameError = validateName(name);
    if (nameError) {
      setError(nameError);
      return;
    }

    setIsSaving(true);

    try {
      const variableInput: VariableDefinitionInput & {
        allowedValues?: string[];
        elementType?: ValueType;
      } = {
        name: name.trim(),
        type,
        scope: "PROCEDURE",
        source: "USER_DEFINED",
        description: description.trim() || undefined,
        isReadOnly: false,
      };

      if (defaultValue.trim()) {
        variableInput.defaultValue = defaultValue.trim();
      }

      await onSave(variableInput);
      onClose();
    } catch {
      setError(t("variables.failedToSave"));
    } finally {
      setIsSaving(false);
    }
  };

  const renderDefaultValueInput = () => {
    switch (type) {
      case "Number":
        return (
          <Form.Control
            type="number"
            value={defaultValue}
            onChange={(e) => setDefaultValue(e.target.value)}
            placeholder={t("variables.enterDefaultValue")}
          />
        );
      case "Boolean":
        return (
          <Form.Select
            value={defaultValue}
            onChange={(e) => setDefaultValue(e.target.value)}
          >
            <option value="">{t("variables.selectValue")}</option>
            <option value="true">true</option>
            <option value="false">false</option>
          </Form.Select>
        );
      case "PositionTag":
        return (
          <Form.Select
            value={defaultValue}
            onChange={(e) => setDefaultValue(e.target.value)}
          >
            <option value="">{t("variables.selectPositionTag")}</option>
            {positionTags.map(
              (tag: {
                id: string;
                tag: string;
                position: { x: number; y: number; z: number };
              }) => (
                <option key={tag.id} value={tag.id}>
                  {tag.tag} ({tag.position.x.toFixed(2)},{" "}
                  {tag.position.y.toFixed(2)}, {tag.position.z.toFixed(2)})
                </option>
              ),
            )}
          </Form.Select>
        );
      case "SceneObject":
        return (
          <Form.Select
            value={defaultValue}
            onChange={(e) => setDefaultValue(e.target.value)}
          >
            <option value="">{t("variables.selectSceneObject")}</option>
            {sceneObjects.map((obj: { id: string; name: string }) => (
              <option key={obj.id} value={obj.id}>
                {obj.name}
              </option>
            ))}
          </Form.Select>
        );
      case "Position":
        return (
          <Form.Control
            type="text"
            value={defaultValue}
            onChange={(e) => setDefaultValue(e.target.value)}
            placeholder={t("variables.enterPositionJson")}
          />
        );
      case "String":
      default:
        return (
          <Form.Control
            type="text"
            value={defaultValue}
            onChange={(e) => setDefaultValue(e.target.value)}
            placeholder={t("variables.enterDefaultValue")}
          />
        );
    }
  };

  const getDefaultValueHelpText = () => {
    switch (type) {
      case "PositionTag":
        return t("variables.helpPositionTag");
      case "SceneObject":
        return t("variables.helpSceneObject");
      case "Position":
        return t("variables.helpPosition");
      case "Boolean":
        return t("variables.helpBoolean");
      case "Number":
        return t("variables.helpNumber");
      default:
        return t("variables.helpDefaultValue");
    }
  };

  return (
    <UnifiedModal
      show={isOpen}
      onHide={onClose}
      title={
        isEditMode ? t("variables.editVariable") : t("variables.addVariable")
      }
      icon={isEditMode ? "bi-pencil-square" : "bi-plus-circle"}
      size="lg"
      onSubmit={handleSave}
      isValid={!error && !!name}
      isEditing={isEditMode}
      loading={isSaving}
    >
      {error && (
        <Alert variant="danger" className="mb-3">
          {error}
        </Alert>
      )}

      <Form.Group className="mb-3" controlId="variable-name">
        <Form.Label>{t("variables.name")}</Form.Label>
        <Form.Control
          type="text"
          value={name}
          onChange={(e) => setName(e.target.value)}
          disabled={isEditMode}
          placeholder={t("variables.enterVariableName")}
        />
        <Form.Text className="text-muted">{t("variables.nameHelp")}</Form.Text>
      </Form.Group>

      <Form.Group className="mb-3" controlId="variable-type">
        <Form.Label>{t("variables.type")}</Form.Label>
        <VariableTypeSelector
          value={type}
          onChange={(newType) => {
            setType(newType);
            setDefaultValue(""); // Reset default value when type changes
          }}
          disabled={false}
        />
      </Form.Group>

      <Form.Group className="mb-3" controlId="variable-default-value">
        <Form.Label>{t("variables.defaultValue")}</Form.Label>
        {renderDefaultValueInput()}
        <Form.Text className="text-muted">
          {getDefaultValueHelpText()}
        </Form.Text>
      </Form.Group>

      <Form.Group className="mb-3" controlId="variable-description">
        <Form.Label>{t("variables.description")}</Form.Label>
        <Form.Control
          as="textarea"
          rows={3}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder={t("variables.enterDescription")}
        />
      </Form.Group>
    </UnifiedModal>
  );
};

export default VariableEditor;
