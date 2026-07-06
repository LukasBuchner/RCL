// GraphQL ValueType union types
export type ValueTypeGraphQL =
  | { __typename: "BooleanType"; typeName: string }
  | { __typename: "NumberType"; typeName: string }
  | { __typename: "StringType"; typeName: string }
  | { __typename: "PositionType"; typeName: string }
  | { __typename: "PositionTagType"; typeName: string }
  | { __typename: "SceneObjectType"; typeName: string }
  | { __typename: "EnumType"; typeName: string; allowedValues: string[] }
  | { __typename: "ListType"; typeName: string; elementType: ValueTypeGraphQL };

// Simplified type for display/UI
export type ValueType =
  | "String"
  | "Number"
  | "Boolean"
  | "Position"
  | "PositionTag"
  | "SceneObject"
  | "Enum"
  | "List";

export type VariableScope = "PROCEDURE" | "TASK" | "GLOBAL";

export type VariableSource =
  | "USER_DEFINED"
  | "SKILL_OUTPUT"
  | "AGENT_STATE"
  | "SYSTEM";

// GraphQL response type
export interface VariableDefinitionGraphQL {
  name: string;
  type: ValueTypeGraphQL;
  defaultValue?: string | null;
  scope: VariableScope;
  source: VariableSource;
  description?: string | null;
  isReadOnly: boolean;
}

// UI/Display type (with simplified type field)
export interface VariableDefinition {
  name: string;
  type: ValueType;
  defaultValue?: string;
  scope: VariableScope;
  source: VariableSource;
  description?: string;
  isReadOnly: boolean;
  allowedValues?: string[]; // For enum types, list of allowed values
  elementType?: ValueType; // For list types
}

export interface VariableDefinitionInput {
  name: string;
  type: ValueType;
  defaultValue?: string;
  scope?: VariableScope;
  source?: VariableSource;
  description?: string;
  isReadOnly?: boolean;
}

/**
 * Converts a GraphQL ValueType to a simplified display string.
 */
export function convertValueTypeToDisplay(
  graphqlType: ValueTypeGraphQL,
): ValueType {
  switch (graphqlType.__typename) {
    case "BooleanType":
      return "Boolean";
    case "NumberType":
      return "Number";
    case "StringType":
      return "String";
    case "PositionType":
      return "Position";
    case "PositionTagType":
      return "PositionTag";
    case "SceneObjectType":
      return "SceneObject";
    case "EnumType":
      return "Enum";
    case "ListType":
      return "List";
    default:
      return "String"; // fallback
  }
}

/**
 * Converts a GraphQL VariableDefinition to the UI format.
 */
export function convertVariableFromGraphQL(
  graphqlVar: VariableDefinitionGraphQL,
): VariableDefinition {
  const variable: VariableDefinition = {
    name: graphqlVar.name,
    type: convertValueTypeToDisplay(graphqlVar.type),
    defaultValue: graphqlVar.defaultValue ?? undefined,
    scope: graphqlVar.scope,
    source: graphqlVar.source,
    description: graphqlVar.description ?? undefined,
    isReadOnly: graphqlVar.isReadOnly,
  };

  // Add enum-specific fields
  if (graphqlVar.type.__typename === "EnumType") {
    variable.allowedValues = graphqlVar.type.allowedValues;
  }

  // Add list-specific fields
  if (
    graphqlVar.type.__typename === "ListType" &&
    graphqlVar.type.elementType
  ) {
    variable.elementType = convertValueTypeToDisplay(
      graphqlVar.type.elementType,
    );
  }

  return variable;
}

/**
 * GraphQL input type for ValueType (oneOf discriminated union).
 */
export type ValueTypeInputGraphQL =
  | { boolean: { dummy?: boolean } }
  | { number: { dummy?: boolean } }
  | { string: { dummy?: boolean } }
  | { position: { dummy?: boolean } }
  | { positionTag: { dummy?: boolean } }
  | { sceneObject: { dummy?: boolean } }
  | { enum: { allowedValues: string[] } }
  | { list: { elementType: ValueTypeInputGraphQL } };

/**
 * Converts a UI ValueType to GraphQL input format.
 */
export function convertValueTypeToGraphQLInput(
  type: ValueType,
  options?: { allowedValues?: string[]; elementType?: ValueType },
): ValueTypeInputGraphQL {
  switch (type) {
    case "Boolean":
      return { boolean: { dummy: true } };
    case "Number":
      return { number: { dummy: true } };
    case "String":
      return { string: { dummy: true } };
    case "Position":
      return { position: { dummy: true } };
    case "PositionTag":
      return { positionTag: { dummy: true } };
    case "SceneObject":
      return { sceneObject: { dummy: true } };
    case "Enum":
      return { enum: { allowedValues: options?.allowedValues || [] } };
    case "List":
      if (!options?.elementType) {
        throw new Error("List type requires elementType");
      }
      return {
        list: {
          elementType: convertValueTypeToGraphQLInput(options.elementType),
        },
      };
    default:
      return { string: { dummy: true } }; // fallback
  }
}

/**
 * GraphQL input type for VariableDefinition.
 */
export interface VariableDefinitionInputGraphQL {
  name: string;
  type: ValueTypeInputGraphQL;
  defaultValue?: string;
  scope?: VariableScope;
  source?: VariableSource;
  description?: string;
  isReadOnly?: boolean;
}

/**
 * Converts a UI VariableDefinitionInput to GraphQL input format.
 */
export function convertVariableToGraphQLInput(
  variable: VariableDefinitionInput & {
    allowedValues?: string[];
    elementType?: ValueType;
  },
): VariableDefinitionInputGraphQL {
  return {
    name: variable.name,
    type: convertValueTypeToGraphQLInput(variable.type, {
      allowedValues: variable.allowedValues,
      elementType: variable.elementType,
    }),
    defaultValue: variable.defaultValue,
    scope: variable.scope,
    source: variable.source,
    description: variable.description,
    isReadOnly: variable.isReadOnly,
  };
}
