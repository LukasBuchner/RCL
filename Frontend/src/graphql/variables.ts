import { gql } from "@apollo/client";

export const GetProcedureVariablesDocument = gql`
  query GetProcedureVariables($procedureId: UUID!) {
    procedureById(id: $procedureId) {
      id
      name
      variables {
        name
        type {
          __typename
          ... on BooleanType {
            typeName
          }
          ... on NumberType {
            typeName
          }
          ... on StringType {
            typeName
          }
          ... on PositionType {
            typeName
          }
          ... on PositionTagType {
            typeName
          }
          ... on SceneObjectType {
            typeName
          }
          ... on EnumType {
            typeName
            allowedValues
          }
          ... on ListType {
            typeName
            elementType {
              __typename
              ... on BooleanType {
                typeName
              }
              ... on NumberType {
                typeName
              }
              ... on StringType {
                typeName
              }
              ... on PositionType {
                typeName
              }
              ... on PositionTagType {
                typeName
              }
              ... on SceneObjectType {
                typeName
              }
              ... on EnumType {
                typeName
                allowedValues
              }
            }
          }
        }
        defaultValue
        scope
        source
        description
        isReadOnly
      }
    }
  }
`;

export const AddVariableToProcedureDocument = gql`
  mutation AddVariableToProcedure(
    $procedureId: UUID!
    $variable: VariableDefinitionInput!
  ) {
    addVariableToProcedure(procedureId: $procedureId, variable: $variable) {
      id
      name
      variables {
        name
        type {
          __typename
          ... on BooleanType {
            typeName
          }
          ... on NumberType {
            typeName
          }
          ... on StringType {
            typeName
          }
          ... on PositionType {
            typeName
          }
          ... on PositionTagType {
            typeName
          }
          ... on SceneObjectType {
            typeName
          }
          ... on EnumType {
            typeName
            allowedValues
          }
          ... on ListType {
            typeName
            elementType {
              __typename
              ... on BooleanType {
                typeName
              }
              ... on NumberType {
                typeName
              }
              ... on StringType {
                typeName
              }
              ... on PositionType {
                typeName
              }
              ... on PositionTagType {
                typeName
              }
              ... on SceneObjectType {
                typeName
              }
              ... on EnumType {
                typeName
                allowedValues
              }
            }
          }
        }
        defaultValue
        scope
        source
        description
        isReadOnly
      }
    }
  }
`;

export const UpdateProcedureVariableDocument = gql`
  mutation UpdateProcedureVariable(
    $procedureId: UUID!
    $variableName: String!
    $variable: VariableDefinitionInput!
  ) {
    updateProcedureVariable(
      procedureId: $procedureId
      variableName: $variableName
      variable: $variable
    ) {
      id
      name
      variables {
        name
        type {
          __typename
          ... on BooleanType {
            typeName
          }
          ... on NumberType {
            typeName
          }
          ... on StringType {
            typeName
          }
          ... on PositionType {
            typeName
          }
          ... on PositionTagType {
            typeName
          }
          ... on SceneObjectType {
            typeName
          }
          ... on EnumType {
            typeName
            allowedValues
          }
          ... on ListType {
            typeName
            elementType {
              __typename
              ... on BooleanType {
                typeName
              }
              ... on NumberType {
                typeName
              }
              ... on StringType {
                typeName
              }
              ... on PositionType {
                typeName
              }
              ... on PositionTagType {
                typeName
              }
              ... on SceneObjectType {
                typeName
              }
              ... on EnumType {
                typeName
                allowedValues
              }
            }
          }
        }
        defaultValue
        scope
        source
        description
        isReadOnly
      }
    }
  }
`;

export const RemoveProcedureVariableDocument = gql`
  mutation RemoveProcedureVariable(
    $procedureId: UUID!
    $variableName: String!
  ) {
    removeProcedureVariable(
      procedureId: $procedureId
      variableName: $variableName
    ) {
      id
      name
      variables {
        name
        type {
          __typename
          ... on BooleanType {
            typeName
          }
          ... on NumberType {
            typeName
          }
          ... on StringType {
            typeName
          }
          ... on PositionType {
            typeName
          }
          ... on PositionTagType {
            typeName
          }
          ... on SceneObjectType {
            typeName
          }
          ... on EnumType {
            typeName
            allowedValues
          }
          ... on ListType {
            typeName
            elementType {
              __typename
              ... on BooleanType {
                typeName
              }
              ... on NumberType {
                typeName
              }
              ... on StringType {
                typeName
              }
              ... on PositionType {
                typeName
              }
              ... on PositionTagType {
                typeName
              }
              ... on SceneObjectType {
                typeName
              }
              ... on EnumType {
                typeName
                allowedValues
              }
            }
          }
        }
        defaultValue
        scope
        source
        description
        isReadOnly
      }
    }
  }
`;
