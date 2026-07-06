import React, { useState } from "react";
import { Col, Collapse, Form, Row } from "react-bootstrap";
import { useTranslation } from "react-i18next";
import { useQuery } from "@apollo/client";
import {
  BindingMode,
  GetPositionTagsDocument,
  GetSceneObjectsDocument,
  PropertyDirection,
  PropertyInput,
} from "../../__generated__/graphql";
import { GetProcedureVariablesDocument } from "../../graphql/variables";
import { MotionButton, MotionCard } from "../motion";

interface PropertyEditorProps {
  properties: PropertyInput[];
  onChange: (properties: PropertyInput[]) => void;
  disabled?: boolean;
  procedureId?: string;
}

export const PropertyEditor: React.FC<PropertyEditorProps> = ({
  properties,
  onChange,
  disabled = false,
  procedureId,
}) => {
  const { t } = useTranslation();
  const [expandedBindings, setExpandedBindings] = useState<{
    [key: number]: boolean;
  }>({});

  // Fetch scene objects and position tags for dropdowns
  const { data: sceneObjectsData } = useQuery(GetSceneObjectsDocument);
  const { data: positionTagsData } = useQuery(GetPositionTagsDocument);

  // Fetch procedure variables for binding
  const { data: variablesData } = useQuery(GetProcedureVariablesDocument, {
    variables: { procedureId: procedureId || "" },
    skip: !procedureId,
  });
  const propertyTypes = [
    {
      key: "stringProperty",
      label: "String",
      defaultValue: { stringProperty: { value: "" } },
    },
    {
      key: "numberProperty",
      label: "Number",
      defaultValue: { numberProperty: { value: 0 } },
    },
    {
      key: "booleanProperty",
      label: "Boolean",
      defaultValue: { booleanProperty: { value: false } },
    },
    {
      key: "positionProperty",
      label: "Position",
      defaultValue: {
        positionProperty: {
          value: { x: 0, y: 0, z: 0, alpha: 0, beta: 0, gamma: 0 },
        },
      },
    },
    {
      key: "positionTagProperty",
      label: "Position Tag",
      defaultValue: {
        positionTagProperty: {
          value: {
            id: "",
            tag: "",
            position: { x: 0, y: 0, z: 0, alpha: 0, beta: 0, gamma: 0 },
          },
        },
      },
    },
    {
      key: "sceneObjectProperty",
      label: "Scene Object",
      defaultValue: {
        sceneObjectProperty: {
          value: {
            id: "",
            name: "",
            position: { x: 0, y: 0, z: 0, alpha: 0, beta: 0, gamma: 0 },
          },
        },
      },
    },
  ];

  const addProperty = () => {
    const newProperty: PropertyInput = {
      name: "",
      direction: PropertyDirection.InputOutput,
      value: propertyTypes[0].defaultValue,
    };
    onChange([...properties, newProperty]);
  };

  const removeProperty = (index: number) => {
    onChange(properties.filter((_, i) => i !== index));
  };

  const updatePropertyName = (index: number, name: string) => {
    const updated = [...properties];
    updated[index] = { ...updated[index], name };
    onChange(updated);
  };

  const updatePropertyDirection = (
    index: number,
    direction: PropertyDirection,
  ) => {
    const updated = [...properties];
    updated[index] = { ...updated[index], direction };
    onChange(updated);
  };

  const toggleBindingExpansion = (index: number) => {
    setExpandedBindings((prev) => ({
      ...prev,
      [index]: !prev[index],
    }));
  };

  const updatePropertyBinding = (
    index: number,
    binding: {
      variableName: string;
      mode: BindingMode;
      transformExpression?: string;
    } | null,
  ) => {
    const updated = [...properties];
    updated[index] = {
      ...updated[index],
      binding: binding
        ? {
            variableName: binding.variableName,
            mode: binding.mode,
            transformExpression: binding.transformExpression || undefined,
          }
        : undefined,
    };
    onChange(updated);
  };

  const updatePropertyType = (index: number, typeKey: string) => {
    const typeConfig = propertyTypes.find((t) => t.key === typeKey);
    if (!typeConfig) return;

    const updated = [...properties];
    updated[index] = {
      ...updated[index],
      value: typeConfig.defaultValue,
    };
    onChange(updated);
  };

  const updatePropertyValue = (index: number, field: string, value: string) => {
    const updated = [...properties];
    const currentProp = updated[index];

    if (currentProp.value.stringProperty) {
      updated[index] = {
        ...currentProp,
        value: { stringProperty: { value } },
      };
    } else if (currentProp.value.numberProperty) {
      updated[index] = {
        ...currentProp,
        value: { numberProperty: { value: parseFloat(value) || 0 } },
      };
    } else if (currentProp.value.booleanProperty) {
      updated[index] = {
        ...currentProp,
        value: { booleanProperty: { value: value === "true" } },
      };
    } else if (currentProp.value.positionProperty) {
      const currentPos = currentProp.value.positionProperty.value;
      updated[index] = {
        ...currentProp,
        value: {
          positionProperty: {
            value: {
              ...currentPos,
              [field]: parseFloat(value) || 0,
            },
          },
        },
      };
    } else if (currentProp.value.positionTagProperty) {
      if (field === "selectedTag") {
        const selectedTag = JSON.parse(value);
        updated[index] = {
          ...currentProp,
          value: {
            positionTagProperty: {
              value: selectedTag,
            },
          },
        };
      }
    } else if (currentProp.value.sceneObjectProperty) {
      if (field === "selectedObject") {
        const selectedObject = JSON.parse(value);
        updated[index] = {
          ...currentProp,
          value: {
            sceneObjectProperty: {
              value: selectedObject,
            },
          },
        };
      }
    }
    onChange(updated);
  };

  const getPropertyType = (prop: PropertyInput): string => {
    if (prop.value.stringProperty) return "stringProperty";
    if (prop.value.numberProperty) return "numberProperty";
    if (prop.value.booleanProperty) return "booleanProperty";
    if (prop.value.positionProperty) return "positionProperty";
    if (prop.value.positionTagProperty) return "positionTagProperty";
    if (prop.value.sceneObjectProperty) return "sceneObjectProperty";
    return "stringProperty";
  };

  const renderValueInputs = (prop: PropertyInput, index: number) => {
    const type = getPropertyType(prop);

    switch (type) {
      case "stringProperty":
        return (
          <Form.Control
            type="text"
            placeholder={t("properties.propertyValue")}
            value={prop.value.stringProperty?.value || ""}
            onChange={(e) =>
              updatePropertyValue(index, "value", e.target.value)
            }
            disabled={disabled}
          />
        );

      case "numberProperty":
        return (
          <Form.Control
            type="number"
            placeholder={t("properties.numberValue")}
            value={prop.value.numberProperty?.value || 0}
            onChange={(e) =>
              updatePropertyValue(index, "value", e.target.value)
            }
            disabled={disabled}
          />
        );

      case "booleanProperty":
        return (
          <Form.Select
            value={prop.value.booleanProperty?.value ? "true" : "false"}
            onChange={(e) =>
              updatePropertyValue(index, "value", e.target.value)
            }
            disabled={disabled}
          >
            <option value="false">False</option>
            <option value="true">True</option>
          </Form.Select>
        );

      case "positionProperty": {
        const pos = prop.value.positionProperty?.value;
        return (
          <div className="position-editor">
            <Row className="g-1 mb-1">
              <Col xs={2}>
                <div className="input-group input-group-sm">
                  <span className="input-group-text bg-primary text-white">
                    X
                  </span>
                  <Form.Control
                    type="number"
                    step="0.01"
                    value={pos?.x || 0}
                    onChange={(e) =>
                      updatePropertyValue(index, "x", e.target.value)
                    }
                    disabled={disabled}
                    className="text-center"
                  />
                </div>
              </Col>
              <Col xs={2}>
                <div className="input-group input-group-sm">
                  <span className="input-group-text bg-success text-white">
                    Y
                  </span>
                  <Form.Control
                    type="number"
                    step="0.01"
                    value={pos?.y || 0}
                    onChange={(e) =>
                      updatePropertyValue(index, "y", e.target.value)
                    }
                    disabled={disabled}
                    className="text-center"
                  />
                </div>
              </Col>
              <Col xs={2}>
                <div className="input-group input-group-sm">
                  <span className="input-group-text bg-info text-white">Z</span>
                  <Form.Control
                    type="number"
                    step="0.01"
                    value={pos?.z || 0}
                    onChange={(e) =>
                      updatePropertyValue(index, "z", e.target.value)
                    }
                    disabled={disabled}
                    className="text-center"
                  />
                </div>
              </Col>
              <Col xs={2}>
                <div className="input-group input-group-sm">
                  <span className="input-group-text bg-warning text-dark">
                    α
                  </span>
                  <Form.Control
                    type="number"
                    step="0.1"
                    value={pos?.alpha || 0}
                    onChange={(e) =>
                      updatePropertyValue(index, "alpha", e.target.value)
                    }
                    disabled={disabled}
                    className="text-center"
                  />
                </div>
              </Col>
              <Col xs={2}>
                <div className="input-group input-group-sm">
                  <span className="input-group-text bg-danger text-white">
                    β
                  </span>
                  <Form.Control
                    type="number"
                    step="0.1"
                    value={pos?.beta || 0}
                    onChange={(e) =>
                      updatePropertyValue(index, "beta", e.target.value)
                    }
                    disabled={disabled}
                    className="text-center"
                  />
                </div>
              </Col>
              <Col xs={2}>
                <div className="input-group input-group-sm">
                  <span className="input-group-text bg-secondary text-white">
                    γ
                  </span>
                  <Form.Control
                    type="number"
                    step="0.1"
                    value={pos?.gamma || 0}
                    onChange={(e) =>
                      updatePropertyValue(index, "gamma", e.target.value)
                    }
                    disabled={disabled}
                    className="text-center"
                  />
                </div>
              </Col>
            </Row>
          </div>
        );
      }

      case "positionTagProperty": {
        const posTag = prop.value.positionTagProperty?.value;
        return (
          <Form.Select
            value={posTag?.id || ""}
            onChange={(e) => {
              const selectedTag = positionTagsData?.positionTags?.find(
                (tag) => tag.id === e.target.value,
              );
              if (selectedTag) {
                updatePropertyValue(
                  index,
                  "selectedTag",
                  JSON.stringify(selectedTag),
                );
              }
            }}
            disabled={disabled}
            size="sm"
          >
            <option value="">{t("properties.selectPositionTag")}</option>
            {positionTagsData?.positionTags?.map((tag) => (
              <option key={tag.id} value={tag.id}>
                📍 {tag.tag} ({tag.position.x.toFixed(1)},{" "}
                {tag.position.y.toFixed(1)}, {tag.position.z.toFixed(1)})
              </option>
            ))}
          </Form.Select>
        );
      }

      case "sceneObjectProperty": {
        const sceneObj = prop.value.sceneObjectProperty?.value;
        return (
          <Form.Select
            value={sceneObj?.id || ""}
            onChange={(e) => {
              const selectedObject = sceneObjectsData?.sceneObjects?.find(
                (obj) => obj.id === e.target.value,
              );
              if (selectedObject) {
                updatePropertyValue(
                  index,
                  "selectedObject",
                  JSON.stringify(selectedObject),
                );
              }
            }}
            disabled={disabled}
            size="sm"
          >
            <option value="">{t("properties.selectSceneObject")}</option>
            {sceneObjectsData?.sceneObjects?.map((obj) => (
              <option key={obj.id} value={obj.id}>
                🎯 {obj.name} ({obj.position.x.toFixed(1)},{" "}
                {obj.position.y.toFixed(1)}, {obj.position.z.toFixed(1)})
              </option>
            ))}
          </Form.Select>
        );
      }

      default:
        return null;
    }
  };

  return (
    <Form.Group className="mb-3">
      {/* Header */}
      <div className="d-flex justify-content-between align-items-center mb-2 p-2 bg-light rounded border">
        <div className="d-flex align-items-center">
          <i className="bi bi-gear-fill text-primary me-2"></i>
          <Form.Label className="mb-0 fw-semibold">
            {t("properties.title")}
          </Form.Label>
          {properties.length > 0 && (
            <span className="badge bg-primary ms-2">{properties.length}</span>
          )}
        </div>
        <MotionButton
          variant="primary"
          size="sm"
          type="button"
          onClick={addProperty}
          disabled={disabled}
        >
          <i className="bi bi-plus me-1"></i> {t("properties.add")}
        </MotionButton>
      </div>

      {/* Properties List */}
      <div className="properties-container">
        {properties.map((property, index) => (
          <MotionCard
            key={index}
            interaction="property"
            className="mb-2 border rounded shadow-sm bg-white"
          >
            {/* Property Header */}
            <div className="property-header p-2 bg-gradient">
              <Row className="align-items-center g-2 mb-2">
                <Col md={5}>
                  <Form.Control
                    type="text"
                    placeholder={t("properties.propertyName")}
                    value={property.name}
                    onChange={(e) => updatePropertyName(index, e.target.value)}
                    disabled={disabled}
                    size="sm"
                  />
                </Col>
                <Col md={4}>
                  <Form.Select
                    value={getPropertyType(property)}
                    onChange={(e) => updatePropertyType(index, e.target.value)}
                    disabled={disabled}
                    size="sm"
                  >
                    {propertyTypes.map((type) => (
                      <option key={type.key} value={type.key}>
                        {type.label}
                      </option>
                    ))}
                  </Form.Select>
                </Col>
                <Col md={3} className="text-end">
                  <MotionButton
                    variant="outline-danger"
                    size="sm"
                    type="button"
                    onClick={() => removeProperty(index)}
                    disabled={disabled}
                  >
                    <i className="bi bi-trash"></i>
                  </MotionButton>
                </Col>
              </Row>
              <Row className="align-items-center g-2">
                <Col md={6}>
                  <Form.Label className="mb-1 small text-muted">
                    {t("properties.direction")}
                  </Form.Label>
                  <Form.Select
                    value={property.direction}
                    onChange={(e) =>
                      updatePropertyDirection(
                        index,
                        e.target.value as PropertyDirection,
                      )
                    }
                    disabled={disabled}
                    size="sm"
                  >
                    <option value={PropertyDirection.Input}>
                      {t("properties.directionInput")}
                    </option>
                    <option value={PropertyDirection.Output}>
                      {t("properties.directionOutput")}
                    </option>
                    <option value={PropertyDirection.InputOutput}>
                      {t("properties.directionInputOutput")}
                    </option>
                  </Form.Select>
                </Col>
                <Col md={6}>
                  <Form.Label className="mb-1 small text-muted">
                    {t("properties.variableBinding")}
                  </Form.Label>
                  <div className="d-flex gap-2">
                    <MotionButton
                      variant={
                        property.binding ? "success" : "outline-secondary"
                      }
                      size="sm"
                      type="button"
                      onClick={() => toggleBindingExpansion(index)}
                      disabled={disabled}
                      className="flex-grow-1"
                    >
                      <i
                        className={`bi bi-${property.binding ? "link-45deg" : "plus-circle"} me-1`}
                      ></i>
                      {property.binding
                        ? t("properties.boundTo", {
                            name: property.binding.variableName,
                          })
                        : t("properties.bindToVariable")}
                      <i
                        className={`bi bi-chevron-${expandedBindings[index] ? "up" : "down"} ms-1`}
                      ></i>
                    </MotionButton>
                    {property.binding && (
                      <MotionButton
                        variant="outline-danger"
                        size="sm"
                        type="button"
                        onClick={() => updatePropertyBinding(index, null)}
                        disabled={disabled}
                      >
                        <i className="bi bi-x"></i>
                      </MotionButton>
                    )}
                  </div>
                </Col>
              </Row>
            </div>

            {/* Variable Binding Section */}
            <Collapse in={expandedBindings[index]}>
              <div className="binding-section p-2 bg-light border-top">
                <Row className="g-2">
                  <Col md={6}>
                    <Form.Label className="mb-1 small">
                      {t("properties.variableName")}
                    </Form.Label>
                    <Form.Select
                      value={property.binding?.variableName || ""}
                      onChange={(e) =>
                        updatePropertyBinding(index, {
                          variableName: e.target.value,
                          mode: property.binding?.mode || BindingMode.Read,
                          transformExpression:
                            property.binding?.transformExpression || undefined,
                        })
                      }
                      disabled={disabled}
                      size="sm"
                    >
                      <option value="">{t("conditions.selectVariable")}</option>
                      {variablesData?.procedureById?.variables?.map(
                        (variable: {
                          name: string;
                          type: { __typename?: string };
                        }) => (
                          <option key={variable.name} value={variable.name}>
                            {variable.name} (
                            {variable.type.__typename?.replace("Type", "")})
                          </option>
                        ),
                      )}
                    </Form.Select>
                  </Col>
                  <Col md={6}>
                    <Form.Label className="mb-1 small">
                      {t("properties.bindingMode")}
                    </Form.Label>
                    <Form.Select
                      value={property.binding?.mode || BindingMode.Read}
                      onChange={(e) =>
                        updatePropertyBinding(index, {
                          variableName: property.binding?.variableName || "",
                          mode: e.target.value as BindingMode,
                          transformExpression:
                            property.binding?.transformExpression || undefined,
                        })
                      }
                      disabled={disabled || !property.binding?.variableName}
                      size="sm"
                    >
                      <option value={BindingMode.Read}>
                        {t("properties.bindingRead")}
                      </option>
                      <option value={BindingMode.Write}>
                        {t("properties.bindingWrite")}
                      </option>
                      <option value={BindingMode.ReadWrite}>
                        {t("properties.bindingReadWrite")}
                      </option>
                    </Form.Select>
                  </Col>
                  <Col md={12}>
                    <Form.Label className="mb-1 small">
                      {t("properties.transformExpression")}
                    </Form.Label>
                    <Form.Control
                      type="text"
                      placeholder={t(
                        "properties.transformExpressionPlaceholder",
                      )}
                      value={property.binding?.transformExpression || ""}
                      onChange={(e) =>
                        updatePropertyBinding(index, {
                          variableName: property.binding?.variableName || "",
                          mode: property.binding?.mode || BindingMode.Read,
                          transformExpression: e.target.value || undefined,
                        })
                      }
                      disabled={disabled || !property.binding?.variableName}
                      size="sm"
                    />
                    <Form.Text className="text-muted small">
                      {t("properties.transformExpressionHelp")}
                    </Form.Text>
                  </Col>
                </Row>
              </div>
            </Collapse>

            {/* Property Value */}
            <div className="property-value p-2">
              <div className="value-input-container">
                {renderValueInputs(property, index)}
              </div>
            </div>
          </MotionCard>
        ))}
      </div>

      {/* Empty State */}
      {properties.length === 0 && (
        <div className="empty-state text-center p-3 border rounded bg-light">
          <div className="mb-2">
            <i className="bi bi-inbox h3 text-muted"></i>
          </div>
          <small className="text-muted">
            {t("properties.noPropertiesDefined")}
          </small>
        </div>
      )}

      <style>{`
        .property-header {
          background: var(--app-gradient-primary) !important;
        }
        
        .bg-gradient {
          background: var(--app-gradient-primary) !important;
        }
        
        .form-floating > .form-control:focus,
        .form-floating > .form-select:focus {
          border-color: var(--app-primary);
          box-shadow: 0 0 0 0.25rem var(--app-focus-shadow-primary);
        }
        
        .value-input-container {
          background: var(--app-surface);
          border-radius: 6px;
          padding: 8px;
          border: 1px solid var(--app-border);
        }
        
        .empty-state {
          background: var(--app-gradient-light) !important;
          border: 2px dashed var(--app-border) !important;
        }
        
        .properties-container {
          max-height: 400px;
          overflow-y: auto;
        }
        
        .properties-container::-webkit-scrollbar {
          width: 6px;
        }
        
        .properties-container::-webkit-scrollbar-track {
          background: var(--app-scrollbar-track);
          border-radius: 3px;
        }
        
        .properties-container::-webkit-scrollbar-thumb {
          background: var(--app-scrollbar-thumb);
          border-radius: 3px;
        }
        
        .properties-container::-webkit-scrollbar-thumb:hover {
          background: var(--app-scrollbar-thumb-hover);
        }
        
        .custom-select {
          border: 2px solid var(--app-border);
          border-radius: 8px;
          font-size: 0.9rem;
        }
        
        .custom-select:focus {
          border-color: var(--app-primary);
          box-shadow: 0 0 0 0.25rem var(--app-focus-shadow-primary);
        }
        
        
        .position-editor .input-group-text {
          font-weight: 600;
          min-width: 35px;
          justify-content: center;
        }
        
        .position-editor .form-control {
          font-weight: 500;
        }
        
        .input-group:hover .input-group-text {
          transform: scale(1.05);
          transition: transform 0.1s ease;
        }
      `}</style>
    </Form.Group>
  );
};
