import React, { useEffect, useState } from "react";
import { Form, InputGroup } from "react-bootstrap";
import { useTranslation } from "react-i18next";
import {
  GetPositionTagsDocument,
  GetPositionTagsQuery,
  GetSceneObjectsDocument,
  GetSceneObjectsQuery,
  PropertyFieldsFragment,
} from "../../__generated__/graphql";
import { useQuery } from "@apollo/client";
import PositionInput from "./PositionInput";

interface PropertyInputProps {
  prop: PropertyFieldsFragment;
  handlePropertyChange: (
    propertyFieldsFragment: PropertyFieldsFragment,
  ) => void;
}

const PropertyInput: React.FC<PropertyInputProps> = ({
  prop,
  handlePropertyChange,
}) => {
  const { t } = useTranslation();
  const value = prop.value;

  // State for different property types
  const [stringValue, setStringValue] = useState("");
  const [numberValue, setNumberValue] = useState(0);
  const [booleanValue, setBooleanValue] = useState(false);
  const [positionValue, setPositionValue] = useState({
    x: 0,
    y: 0,
    z: 0,
    alpha: 0,
    beta: 0,
    gamma: 0,
  });
  const [positionTagValue, setPositionTagValue] = useState({
    id: "",
    tag: "",
    position: { x: 0, y: 0, z: 0, alpha: 0, beta: 0, gamma: 0 },
  });
  const [sceneObjectValue, setSceneObjectValue] = useState({
    id: "",
    name: "",
    position: { x: 0, y: 0, z: 0, alpha: 0, beta: 0, gamma: 0 },
  });

  // Update local state when props change
  useEffect(() => {
    if (value.__typename === "StringValue") {
      setStringValue(value.stringValue);
    }
    if (value.__typename === "NumberValue") {
      setNumberValue(value.numberValue);
    }
    if (value.__typename === "BooleanValue") {
      setBooleanValue(value.boolValue);
    }
    if (value.__typename === "PositionValue") {
      setPositionValue(value.positionValue);
    }
    if (value.__typename === "PositionTagValue") {
      setPositionTagValue(value.positionTagValue);
    }
    if (value.__typename === "SceneObjectValue") {
      setSceneObjectValue(value.sceneObjectValue);
    }
  }, [value, prop.name]);

  const {
    data: sceneObjectsData,
    loading: sceneObjectsLoading,
    error: sceneObjectsError,
  } = useQuery<GetSceneObjectsQuery>(GetSceneObjectsDocument);

  // Fetch position tags
  const {
    data: positionTagsData,
    loading: positionTagsLoading,
    error: positionTagsError,
  } = useQuery<GetPositionTagsQuery>(GetPositionTagsDocument);

  const isStringProperty = value.__typename === "StringValue";
  const isNumberProperty = value.__typename === "NumberValue";
  const isBooleanProperty = value.__typename === "BooleanValue";
  const isPositionProperty = value.__typename === "PositionValue";
  const isPositionTagProperty = value.__typename === "PositionTagValue";
  const isSceneObjectProperty = value.__typename === "SceneObjectValue";

  // Handler for Boolean property
  if (isBooleanProperty) {
    return (
      <Form.Check
        type="switch"
        id={`switch_${prop.name}`}
        label={prop.name}
        checked={booleanValue}
        onChange={(e) => {
          const newValue = e.target.checked;
          setBooleanValue(newValue);
          handlePropertyChange({
            __typename: "Property",
            name: prop.name,
            direction: prop.direction,
            value: {
              __typename: "BooleanValue",
              boolValue: newValue,
              type: { __typename: "BooleanType", typeName: "Boolean" },
            },
          });
        }}
        aria-label={prop.name}
        className="mb-3"
      />
    );
  }

  // Handler for Number property
  if (isNumberProperty) {
    return (
      <InputGroup className="mb-3">
        <InputGroup.Text>
          <i className="bi bi-123" aria-hidden="true"></i>
        </InputGroup.Text>
        <InputGroup.Text>{prop.name}</InputGroup.Text>
        <Form.Control
          type="number"
          value={numberValue}
          onChange={(e) => {
            const numVal = parseFloat(e.target.value) || 0;
            setNumberValue(numVal);
            handlePropertyChange({
              __typename: "Property",
              name: prop.name,
              direction: prop.direction,
              value: {
                __typename: "NumberValue",
                numberValue: numVal,
                type: { __typename: "NumberType", typeName: "Number" },
              },
            });
          }}
          placeholder={t("properties.enterProperty", { name: prop.name })}
          aria-label={prop.name}
        />
      </InputGroup>
    );
  }

  // Handler for String property
  if (isStringProperty) {
    return (
      <InputGroup className="mb-3">
        <InputGroup.Text>
          <i className="bi bi-fonts" aria-hidden="true"></i>
        </InputGroup.Text>
        <InputGroup.Text>{prop.name}</InputGroup.Text>
        <Form.Control
          type="text"
          value={stringValue}
          onChange={(e) => {
            const newValue = e.target.value;
            setStringValue(newValue);
            handlePropertyChange({
              __typename: "Property",
              name: prop.name,
              direction: prop.direction,
              value: {
                __typename: "StringValue",
                stringValue: newValue,
                type: { __typename: "StringType", typeName: "String" },
              },
            });
          }}
          placeholder={t("properties.enterProperty", { name: prop.name })}
          aria-label={prop.name}
        />
      </InputGroup>
    );
  }

  // Handler for Position property
  if (isPositionProperty) {
    return (
      <Form.Group className="mb-3">
        <Form.Label>{prop.name}</Form.Label>
        <PositionInput
          position={positionValue}
          onChange={(newPosition) => {
            setPositionValue(newPosition);
            handlePropertyChange({
              __typename: "Property",
              name: prop.name,
              direction: prop.direction,
              value: {
                __typename: "PositionValue",
                positionValue: newPosition,
                type: { __typename: "PositionType", typeName: "Position" },
              },
            });
          }}
        />
      </Form.Group>
    );
  }

  // Handler for PositionTag property
  if (isPositionTagProperty) {
    return (
      <Form.Group className="mb-3">
        <Form.Label>{prop.name}</Form.Label>
        {positionTagsLoading ? (
          <Form.Control
            type="text"
            placeholder={t("loading.loadingPositionTags")}
            disabled
          />
        ) : positionTagsError ? (
          <Form.Control
            type="text"
            placeholder={t("errors.errorLoadingPositionTags")}
            disabled
          />
        ) : (
          <Form.Select
            value={positionTagValue.id || ""}
            onChange={(e) => {
              const selectedPositionTag = positionTagsData?.positionTags.find(
                (tag) => tag.id === e.target.value,
              );
              if (selectedPositionTag) {
                const newValue = {
                  id: selectedPositionTag.id,
                  tag: selectedPositionTag.tag,
                  position: selectedPositionTag.position,
                };
                setPositionTagValue(newValue);
                handlePropertyChange({
                  __typename: "Property",
                  name: prop.name,
                  direction: prop.direction,
                  value: {
                    __typename: "PositionTagValue",
                    positionTagValue: newValue,
                    type: {
                      __typename: "PositionTagType",
                      typeName: "PositionTag",
                    },
                  },
                });
              }
            }}
          >
            <option value="">{t("properties.selectPositionTag")}</option>
            {positionTagsData?.positionTags.map((tag) => (
              <option key={tag.id} value={tag.id}>
                {tag.tag}
              </option>
            ))}
          </Form.Select>
        )}
      </Form.Group>
    );
  }

  // Handler for SceneObject property
  if (isSceneObjectProperty) {
    return (
      <Form.Group className="mb-3">
        <Form.Label>{prop.name}</Form.Label>
        {sceneObjectsLoading ? (
          <Form.Control
            type="text"
            placeholder={t("loading.loadingSceneObjects")}
            disabled
          />
        ) : sceneObjectsError ? (
          <Form.Control
            type="text"
            placeholder={t("errors.errorLoadingSceneObjects")}
            disabled
          />
        ) : (
          <Form.Select
            value={sceneObjectValue.id || ""}
            onChange={(e) => {
              const selectedSceneObject = sceneObjectsData?.sceneObjects.find(
                (obj) => obj.id === e.target.value,
              );
              if (selectedSceneObject) {
                const newValue = {
                  id: selectedSceneObject.id,
                  name: selectedSceneObject.name,
                  position: selectedSceneObject.position,
                };
                setSceneObjectValue(newValue);
                handlePropertyChange({
                  __typename: "Property",
                  name: prop.name,
                  direction: prop.direction,
                  value: {
                    __typename: "SceneObjectValue",
                    sceneObjectValue: newValue,
                    type: {
                      __typename: "SceneObjectType",
                      typeName: "SceneObject",
                    },
                  },
                });
              }
            }}
          >
            <option value="">{t("properties.selectSceneObject")}</option>
            {sceneObjectsData?.sceneObjects.map((obj) => (
              <option key={obj.id} value={obj.id}>
                {obj.name}
              </option>
            ))}
          </Form.Select>
        )}
      </Form.Group>
    );
  }

  // Fallback: treat as a string property
  return (
    <InputGroup className="mb-3">
      <InputGroup.Text>
        <i className="bi bi-textarea-t" aria-hidden="true"></i>
      </InputGroup.Text>
      <InputGroup.Text>{prop.name}</InputGroup.Text>
      <Form.Control
        type="text"
        value={stringValue}
        onChange={(e) => {
          const newValue = e.target.value;
          setStringValue(newValue);
          handlePropertyChange({
            __typename: "Property",
            name: prop.name,
            direction: prop.direction,
            value: {
              __typename: "StringValue",
              stringValue: newValue,
              type: { __typename: "StringType", typeName: "String" },
            },
          });
        }}
        placeholder={t("properties.enterProperty", { name: prop.name })}
        aria-label={prop.name}
      />
    </InputGroup>
  );
};

export default PropertyInput;
