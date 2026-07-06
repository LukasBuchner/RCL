import {
  Property,
  PropertyInput,
  PropertyTypeInput,
} from "../../../__generated__/graphql";

/**
 * Maps Property to PropertyInput for mutations
 */
export const mapPropertyToPropertyInput = (
  property: Property,
): PropertyInput => {
  return {
    name: property.name,
    direction: property.direction,
    value: mapPropertyTypeToPropertyTypeInput(property.value),
  };
};

/**
 * Maps PropertyType to PropertyTypeInput
 */
const mapPropertyTypeToPropertyTypeInput = (
  value: Property["value"],
): PropertyTypeInput => {
  switch (value.__typename) {
    case "BooleanValue":
      return {
        booleanProperty: {
          value: value.boolValue,
        },
      };
    case "NumberValue":
      return {
        numberProperty: {
          value: value.numberValue,
        },
      };
    case "StringValue":
      return {
        stringProperty: {
          value: value.stringValue,
        },
      };
    case "PositionValue":
      return {
        positionProperty: {
          value: {
            x: value.positionValue.x,
            y: value.positionValue.y,
            z: value.positionValue.z,
            alpha: value.positionValue.alpha,
            beta: value.positionValue.beta,
            gamma: value.positionValue.gamma,
          },
        },
      };
    case "PositionTagValue":
      return {
        positionTagProperty: {
          value: {
            id: value.positionTagValue.id,
            tag: value.positionTagValue.tag,
            position: {
              x: value.positionTagValue.position.x,
              y: value.positionTagValue.position.y,
              z: value.positionTagValue.position.z,
              alpha: value.positionTagValue.position.alpha,
              beta: value.positionTagValue.position.beta,
              gamma: value.positionTagValue.position.gamma,
            },
          },
        },
      };
    case "SceneObjectValue":
      return {
        sceneObjectProperty: {
          value: {
            id: value.sceneObjectValue.id,
            name: value.sceneObjectValue.name,
            position: {
              x: value.sceneObjectValue.position.x,
              y: value.sceneObjectValue.position.y,
              z: value.sceneObjectValue.position.z,
              alpha: value.sceneObjectValue.position.alpha,
              beta: value.sceneObjectValue.position.beta,
              gamma: value.sceneObjectValue.position.gamma,
            },
          },
        },
      };
    default:
      throw new Error(`Unknown property type: ${value.__typename}`);
  }
};
