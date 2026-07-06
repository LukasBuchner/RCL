import {
  BooleanPropertyInput,
  NumberPropertyInput,
  PositionInput,
  PositionPropertyInput,
  PositionTagInput,
  PositionTagPropertyInput,
  PropertyFieldsFragment,
  PropertyTypeInput,
  SceneObjectPropertyInput,
  StringPropertyInput,
} from "../../../__generated__/graphql";

// Helper function to recursively remove __typename from objects
const removeTypename = (obj: unknown): unknown => {
  if (obj === null || typeof obj !== "object") {
    return obj;
  }
  if (Array.isArray(obj)) {
    return obj.map(removeTypename);
  }
  const cleaned: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(obj as Record<string, unknown>)) {
    if (key !== "__typename") {
      cleaned[key] = removeTypename(value);
    }
  }
  return cleaned;
};

export const mapPropertyFieldsFragmentValueToPropertyTypeInput = (
  value: PropertyFieldsFragment["value"],
): PropertyTypeInput => {
  switch (value.__typename) {
    case "BooleanValue":
      return {
        booleanProperty: { value: value.boolValue } as BooleanPropertyInput,
      };

    case "NumberValue":
      return {
        numberProperty: { value: value.numberValue } as NumberPropertyInput,
      };

    case "StringValue":
      return {
        stringProperty: { value: value.stringValue } as StringPropertyInput,
      };

    case "PositionValue":
      return {
        positionProperty: {
          value: removeTypename({
            alpha: value.positionValue.alpha,
            beta: value.positionValue.beta,
            gamma: value.positionValue.gamma,
            x: value.positionValue.x,
            y: value.positionValue.y,
            z: value.positionValue.z,
          }) as PositionInput,
        } as PositionPropertyInput,
      };

    case "PositionTagValue":
      return {
        positionTagProperty: {
          value: removeTypename({
            id: value.positionTagValue.id,
            position: {
              alpha: value.positionTagValue.position.alpha,
              beta: value.positionTagValue.position.beta,
              gamma: value.positionTagValue.position.gamma,
              x: value.positionTagValue.position.x,
              y: value.positionTagValue.position.y,
              z: value.positionTagValue.position.z,
            },
            tag: value.positionTagValue.tag,
          }) as PositionTagInput,
        } as PositionTagPropertyInput,
      };

    case "SceneObjectValue":
      return {
        sceneObjectProperty: {
          value: removeTypename({
            id: value.sceneObjectValue.id,
            name: value.sceneObjectValue.name,
            position: {
              alpha: value.sceneObjectValue.position.alpha,
              beta: value.sceneObjectValue.position.beta,
              gamma: value.sceneObjectValue.position.gamma,
              x: value.sceneObjectValue.position.x,
              y: value.sceneObjectValue.position.y,
              z: value.sceneObjectValue.position.z,
            },
          }),
        } as SceneObjectPropertyInput,
      };

    default:
      /* satisfies exhaustive check – never reached */
      return {
        stringProperty: { value: "Error Default value: Type was not set" },
      };
  }
};
