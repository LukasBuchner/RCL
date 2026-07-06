import {
  PropertyInput,
  PropertyTypeInput,
  PropertyDirection,
} from "../__generated__/graphql";

// Type for the property structure returned from GraphQL queries
export interface PropertyFromQuery {
  __typename?: "Property";
  name: string;
  direction?: string;
  value: {
    __typename:
      | "BooleanValue"
      | "NumberValue"
      | "StringValue"
      | "PositionValue"
      | "PositionTagValue"
      | "SceneObjectValue";
    boolValue?: boolean;
    numberValue?: number;
    stringValue?: string;
    positionValue?: {
      x: number;
      y: number;
      z: number;
      alpha: number;
      beta: number;
      gamma: number;
    };
    positionTagValue?: {
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
    };
    sceneObjectValue?: {
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
    };
    type?: {
      typeName: string;
    };
  };
}

/**
 * Converts a property from the GraphQL query result format to the input format
 * expected by mutations
 */
export function mapPropertyFromQueryToInput(
  property: PropertyFromQuery,
): PropertyInput {
  const { name, direction, value } = property;

  let propertyTypeInput: PropertyTypeInput;

  switch (value.__typename) {
    case "BooleanValue":
      propertyTypeInput =
        value.boolValue !== undefined
          ? { booleanProperty: { value: value.boolValue } }
          : { stringProperty: { value: "" } };
      break;

    case "NumberValue":
      propertyTypeInput =
        value.numberValue !== undefined
          ? { numberProperty: { value: value.numberValue } }
          : { stringProperty: { value: "" } };
      break;

    case "StringValue":
      propertyTypeInput =
        value.stringValue !== undefined
          ? { stringProperty: { value: value.stringValue } }
          : { stringProperty: { value: "" } };
      break;

    case "PositionValue":
      propertyTypeInput = value.positionValue
        ? { positionProperty: { value: value.positionValue } }
        : { stringProperty: { value: "" } };
      break;

    case "PositionTagValue":
      propertyTypeInput = value.positionTagValue
        ? {
            positionTagProperty: {
              value: {
                id: value.positionTagValue.id,
                tag: value.positionTagValue.tag,
                position: value.positionTagValue.position,
              },
            },
          }
        : { stringProperty: { value: "" } };
      break;

    case "SceneObjectValue":
      propertyTypeInput = value.sceneObjectValue
        ? {
            sceneObjectProperty: {
              value: {
                id: value.sceneObjectValue.id,
                name: value.sceneObjectValue.name,
                position: value.sceneObjectValue.position,
              },
            },
          }
        : { stringProperty: { value: "" } };
      break;

    default:
      propertyTypeInput = { stringProperty: { value: "" } };
  }

  return {
    name,
    direction:
      (direction as PropertyDirection) || PropertyDirection.InputOutput,
    value: propertyTypeInput,
  };
}

/**
 * Converts an array of properties from query format to input format
 */
export function mapPropertiesFromQueryToInput(
  properties: PropertyFromQuery[],
): PropertyInput[] {
  return properties.map(mapPropertyFromQueryToInput);
}
