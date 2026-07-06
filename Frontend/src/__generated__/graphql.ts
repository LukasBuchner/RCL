/* eslint-disable */
import { TypedDocumentNode as DocumentNode } from "@graphql-typed-document-node/core";
export type Maybe<T> = T | null;
export type InputMaybe<T> = Maybe<T>;
export type Exact<T extends { [key: string]: unknown }> = {
  [K in keyof T]: T[K];
};
export type MakeOptional<T, K extends keyof T> = Omit<T, K> & {
  [SubKey in K]?: Maybe<T[SubKey]>;
};
export type MakeMaybe<T, K extends keyof T> = Omit<T, K> & {
  [SubKey in K]: Maybe<T[SubKey]>;
};
export type MakeEmpty<
  T extends { [key: string]: unknown },
  K extends keyof T,
> = { [_ in K]?: never };
export type Incremental<T> =
  | T
  | {
      [P in keyof T]?: P extends " $fragmentName" | "__typename" ? T[P] : never;
    };
/** All built-in and custom scalars, mapped to their actual values */
export type Scalars = {
  ID: { input: string; output: string };
  String: { input: string; output: string };
  Boolean: { input: boolean; output: boolean };
  Int: { input: number; output: number };
  Float: { input: number; output: number };
  /** The `DateTime` scalar represents an ISO-8601 compliant date time type. */
  DateTime: { input: any; output: any };
  UUID: { input: any; output: any };
};

export type Agent = {
  __typename?: "Agent";
  id: Scalars["UUID"]["output"];
  lastSeenUtc?: Maybe<Scalars["DateTime"]["output"]>;
  metadata?: Maybe<Array<KeyValuePairOfStringAndObject>>;
  name: Scalars["String"]["output"];
  representativeColor: Scalars["String"]["output"];
  skills: Array<Maybe<Skill>>;
  state: AgentState;
};

export type AgentFilterInput = {
  and?: InputMaybe<Array<AgentFilterInput>>;
  id?: InputMaybe<UuidOperationFilterInput>;
  lastSeenUtc?: InputMaybe<DateTimeOperationFilterInput>;
  name?: InputMaybe<StringOperationFilterInput>;
  or?: InputMaybe<Array<AgentFilterInput>>;
  representativeColor?: InputMaybe<StringOperationFilterInput>;
  skills?: InputMaybe<ListFilterInputTypeOfSkillFilterInput>;
  state?: InputMaybe<AgentStateOperationFilterInput>;
};

export type AgentHealthStatus = {
  __typename?: "AgentHealthStatus";
  activeExecutions: Scalars["Int"]["output"];
  additionalMetrics?: Maybe<Array<KeyValuePairOfStringAndObject>>;
  agentId: Scalars["UUID"]["output"];
  agentName: Scalars["String"]["output"];
  averageExecutionTimeSeconds?: Maybe<Scalars["Float"]["output"]>;
  cpuUsagePercent?: Maybe<Scalars["Float"]["output"]>;
  errorMessage?: Maybe<Scalars["String"]["output"]>;
  failedExecutions: Scalars["Int"]["output"];
  isAvailable: Scalars["Boolean"]["output"];
  isHealthy: Scalars["Boolean"]["output"];
  lastSeenUtc: Scalars["DateTime"]["output"];
  memoryUsageMb?: Maybe<Scalars["Float"]["output"]>;
  startedUtc: Scalars["DateTime"]["output"];
  statusMessage?: Maybe<Scalars["String"]["output"]>;
  totalExecutionsCompleted: Scalars["Int"]["output"];
};

export type AgentInput = {
  id: Scalars["UUID"]["input"];
  lastSeenUtc?: InputMaybe<Scalars["DateTime"]["input"]>;
  metadata?: InputMaybe<Array<KeyValuePairOfStringAndObjectInput>>;
  name: Scalars["String"]["input"];
  representativeColor: Scalars["String"]["input"];
  skillIds: Array<Scalars["UUID"]["input"]>;
  state?: InputMaybe<AgentState>;
};

export type AgentSerializationViolation = {
  __typename?: "AgentSerializationViolation";
  agentId: Scalars["UUID"]["output"];
  agentName: Scalars["String"]["output"];
  missingFsPairs: Array<SkillPair>;
  unserializedSkills: Array<UnserializedSkill>;
};

export type AgentSortInput = {
  id?: InputMaybe<SortEnumType>;
  lastSeenUtc?: InputMaybe<SortEnumType>;
  name?: InputMaybe<SortEnumType>;
  representativeColor?: InputMaybe<SortEnumType>;
  state?: InputMaybe<SortEnumType>;
};

export enum AgentState {
  Active = "ACTIVE",
  Decommissioned = "DECOMMISSIONED",
  Inactive = "INACTIVE",
  Lost = "LOST",
  Registered = "REGISTERED",
}

export type AgentStateOperationFilterInput = {
  eq?: InputMaybe<AgentState>;
  in?: InputMaybe<Array<InputMaybe<AgentState>>>;
  neq?: InputMaybe<AgentState>;
  nin?: InputMaybe<Array<InputMaybe<AgentState>>>;
};

export enum BindingMode {
  Read = "READ",
  ReadWrite = "READ_WRITE",
  Write = "WRITE",
}

export type BooleanOperationFilterInput = {
  eq?: InputMaybe<Scalars["Boolean"]["input"]>;
  neq?: InputMaybe<Scalars["Boolean"]["input"]>;
};

export type BooleanPropertyInput = {
  value: Scalars["Boolean"]["input"];
};

/** Boolean value type. */
export type BooleanType = {
  __typename?: "BooleanType";
  typeName: Scalars["String"]["output"];
};

/** Wrapper type for boolean property values */
export type BooleanValue = {
  __typename?: "BooleanValue";
  /** The actual boolean value */
  boolValue: Scalars["Boolean"]["output"];
  /** Type descriptor */
  type: BooleanType;
};

export type BooleanValueTypeInput = {
  dummy?: InputMaybe<Scalars["Boolean"]["input"]>;
};

export type ConditionalBranch = {
  __typename?: "ConditionalBranch";
  condition?: Maybe<Scalars["String"]["output"]>;
  name: Scalars["String"]["output"];
  priority: Scalars["Int"]["output"];
  targetNodeId?: Maybe<Scalars["UUID"]["output"]>;
};

export type ConditionalBranchInput = {
  condition?: InputMaybe<Scalars["String"]["input"]>;
  name: Scalars["String"]["input"];
  priority: Scalars["Int"]["input"];
  targetNodeId?: InputMaybe<Scalars["UUID"]["input"]>;
};

export type CreateAgentInput = {
  agentInput: AgentInput;
};

export type CreateAgentPayload = {
  __typename?: "CreateAgentPayload";
  agent?: Maybe<Agent>;
};

export type CreateDependencyEdgeInput = {
  dependencyEdge: DependencyEdgeInput;
};

export type CreateDependencyEdgePayload = {
  __typename?: "CreateDependencyEdgePayload";
  dependencyEdge?: Maybe<DependencyEdge>;
};

export type CreateNodeInput = {
  nodeInput: NodeInput;
};

export type CreateNodePayload = {
  __typename?: "CreateNodePayload";
  node?: Maybe<Node>;
};

export type CreatePositionTagInput = {
  positionTag: PositionTagInput;
};

export type CreatePositionTagPayload = {
  __typename?: "CreatePositionTagPayload";
  positionTag?: Maybe<PositionTag>;
};

export type CreateProcedureInput = {
  description?: InputMaybe<Scalars["String"]["input"]>;
  name: Scalars["String"]["input"];
};

export type CreateProcedurePayload = {
  __typename?: "CreateProcedurePayload";
  procedure?: Maybe<Procedure>;
};

export type CreateSceneObjectInput = {
  sceneObject: SceneObjectInput;
};

export type CreateSceneObjectPayload = {
  __typename?: "CreateSceneObjectPayload";
  sceneObject?: Maybe<SceneObject>;
};

export type CreateSkillInput = {
  skillInput: SkillInput;
};

export type CreateSkillPayload = {
  __typename?: "CreateSkillPayload";
  skill?: Maybe<Skill>;
};

export type DateTimeOperationFilterInput = {
  eq?: InputMaybe<Scalars["DateTime"]["input"]>;
  gt?: InputMaybe<Scalars["DateTime"]["input"]>;
  gte?: InputMaybe<Scalars["DateTime"]["input"]>;
  in?: InputMaybe<Array<InputMaybe<Scalars["DateTime"]["input"]>>>;
  lt?: InputMaybe<Scalars["DateTime"]["input"]>;
  lte?: InputMaybe<Scalars["DateTime"]["input"]>;
  neq?: InputMaybe<Scalars["DateTime"]["input"]>;
  ngt?: InputMaybe<Scalars["DateTime"]["input"]>;
  ngte?: InputMaybe<Scalars["DateTime"]["input"]>;
  nin?: InputMaybe<Array<InputMaybe<Scalars["DateTime"]["input"]>>>;
  nlt?: InputMaybe<Scalars["DateTime"]["input"]>;
  nlte?: InputMaybe<Scalars["DateTime"]["input"]>;
};

export type DeleteAgentInput = {
  id: Scalars["UUID"]["input"];
};

export type DeleteAgentPayload = {
  __typename?: "DeleteAgentPayload";
  boolean?: Maybe<Scalars["Boolean"]["output"]>;
};

export type DeleteDependencyEdgeInput = {
  id: Scalars["UUID"]["input"];
};

export type DeleteDependencyEdgePayload = {
  __typename?: "DeleteDependencyEdgePayload";
  boolean?: Maybe<Scalars["Boolean"]["output"]>;
};

export type DeleteNodeInput = {
  id: Scalars["UUID"]["input"];
};

export type DeleteNodePayload = {
  __typename?: "DeleteNodePayload";
  boolean?: Maybe<Scalars["Boolean"]["output"]>;
};

export type DeletePositionTagInput = {
  id: Scalars["UUID"]["input"];
};

export type DeletePositionTagPayload = {
  __typename?: "DeletePositionTagPayload";
  boolean?: Maybe<Scalars["Boolean"]["output"]>;
};

export type DeleteProcedureInput = {
  id: Scalars["UUID"]["input"];
};

export type DeleteProcedurePayload = {
  __typename?: "DeleteProcedurePayload";
  boolean?: Maybe<Scalars["Boolean"]["output"]>;
};

export type DeleteSceneObjectInput = {
  id: Scalars["UUID"]["input"];
};

export type DeleteSceneObjectPayload = {
  __typename?: "DeleteSceneObjectPayload";
  boolean?: Maybe<Scalars["Boolean"]["output"]>;
};

export type DeleteSkillInput = {
  id: Scalars["UUID"]["input"];
};

export type DeleteSkillPayload = {
  __typename?: "DeleteSkillPayload";
  boolean?: Maybe<Scalars["Boolean"]["output"]>;
};

export type DependencyEdge = {
  __typename?: "DependencyEdge";
  id: Scalars["UUID"]["output"];
  procedureId: Scalars["UUID"]["output"];
  sourceHandle?: Maybe<Scalars["String"]["output"]>;
  sourceId: Scalars["UUID"]["output"];
  targetHandle?: Maybe<Scalars["String"]["output"]>;
  targetId: Scalars["UUID"]["output"];
};

export type DependencyEdgeFilterInput = {
  and?: InputMaybe<Array<DependencyEdgeFilterInput>>;
  id?: InputMaybe<UuidOperationFilterInput>;
  or?: InputMaybe<Array<DependencyEdgeFilterInput>>;
  sourceHandle?: InputMaybe<StringOperationFilterInput>;
  sourceId?: InputMaybe<UuidOperationFilterInput>;
  targetHandle?: InputMaybe<StringOperationFilterInput>;
  targetId?: InputMaybe<UuidOperationFilterInput>;
};

export type DependencyEdgeInput = {
  id: Scalars["UUID"]["input"];
  sourceHandle?: InputMaybe<Scalars["String"]["input"]>;
  sourceId: Scalars["UUID"]["input"];
  targetHandle?: InputMaybe<Scalars["String"]["input"]>;
  targetId: Scalars["UUID"]["input"];
};

export type DependencyEdgeSortInput = {
  id?: InputMaybe<SortEnumType>;
  sourceHandle?: InputMaybe<SortEnumType>;
  sourceId?: InputMaybe<SortEnumType>;
  targetHandle?: InputMaybe<SortEnumType>;
  targetId?: InputMaybe<SortEnumType>;
};

/** Enum value type with allowed values. */
export type EnumType = {
  __typename?: "EnumType";
  allowedValues: Array<Scalars["String"]["output"]>;
  typeName: Scalars["String"]["output"];
};

export type EnumValueTypeInput = {
  allowedValues: Array<Scalars["String"]["input"]>;
};

export type ExecutionTiming = {
  __typename?: "ExecutionTiming";
  currentTimeSeconds: Scalars["Float"]["output"];
  estimatedEndTimeUtc: Scalars["DateTime"]["output"];
  estimatedTotalDurationSeconds: Scalars["Float"]["output"];
  isRunning: Scalars["Boolean"]["output"];
  progressPercentage: Scalars["Float"]["output"];
  startTimeUtc: Scalars["DateTime"]["output"];
};

export type ExpressionSelector = {
  __typename?: "ExpressionSelector";
  expression: Scalars["String"]["output"];
};

export type ExpressionSelectorInput = {
  expression: Scalars["String"]["input"];
};

export type FloatOperationFilterInput = {
  eq?: InputMaybe<Scalars["Float"]["input"]>;
  gt?: InputMaybe<Scalars["Float"]["input"]>;
  gte?: InputMaybe<Scalars["Float"]["input"]>;
  in?: InputMaybe<Array<InputMaybe<Scalars["Float"]["input"]>>>;
  lt?: InputMaybe<Scalars["Float"]["input"]>;
  lte?: InputMaybe<Scalars["Float"]["input"]>;
  neq?: InputMaybe<Scalars["Float"]["input"]>;
  ngt?: InputMaybe<Scalars["Float"]["input"]>;
  ngte?: InputMaybe<Scalars["Float"]["input"]>;
  nin?: InputMaybe<Array<InputMaybe<Scalars["Float"]["input"]>>>;
  nlt?: InputMaybe<Scalars["Float"]["input"]>;
  nlte?: InputMaybe<Scalars["Float"]["input"]>;
};

export type KeyValuePairOfStringAndObject = {
  __typename?: "KeyValuePairOfStringAndObject";
  key: Scalars["String"]["output"];
  value?: Maybe<Scalars["String"]["output"]>;
};

export type KeyValuePairOfStringAndObjectInput = {
  key: Scalars["String"]["input"];
  value?: InputMaybe<Scalars["String"]["input"]>;
};

export type ListFilterInputTypeOfAgentFilterInput = {
  all?: InputMaybe<AgentFilterInput>;
  any?: InputMaybe<Scalars["Boolean"]["input"]>;
  none?: InputMaybe<AgentFilterInput>;
  some?: InputMaybe<AgentFilterInput>;
};

export type ListFilterInputTypeOfPropertyFilterInput = {
  all?: InputMaybe<PropertyFilterInput>;
  any?: InputMaybe<Scalars["Boolean"]["input"]>;
  none?: InputMaybe<PropertyFilterInput>;
  some?: InputMaybe<PropertyFilterInput>;
};

export type ListFilterInputTypeOfSkillFilterInput = {
  all?: InputMaybe<SkillFilterInput>;
  any?: InputMaybe<Scalars["Boolean"]["input"]>;
  none?: InputMaybe<SkillFilterInput>;
  some?: InputMaybe<SkillFilterInput>;
};

/** List value type. */
export type ListType = {
  __typename?: "ListType";
  elementType: ValueType;
  typeName: Scalars["String"]["output"];
};

export type ListValueTypeInput = {
  elementType: ValueTypeInput;
};

export type LoadProcedurePayload = {
  __typename?: "LoadProcedurePayload";
  procedure?: Maybe<Procedure>;
};

export type LoadedProcedureIdentity = {
  __typename?: "LoadedProcedureIdentity";
  id: Scalars["UUID"]["output"];
  name: Scalars["String"]["output"];
};

export type Mutation = {
  __typename?: "Mutation";
  /** Adds a variable to a procedure. */
  addVariableToProcedure: Procedure;
  /** Creates a new Agent based on the provided input. */
  createAgent: CreateAgentPayload;
  /** Creates a new DependencyEdge based on the provided input. */
  createDependencyEdge: CreateDependencyEdgePayload;
  /** Creates a new Node based on the provided input. */
  createNode: CreateNodePayload;
  /** Creates a new PositionTag based on the provided input. */
  createPositionTag: CreatePositionTagPayload;
  /** Creates a new procedure. */
  createProcedure: CreateProcedurePayload;
  /** Creates a new SceneObject based on the provided input. */
  createSceneObject: CreateSceneObjectPayload;
  /** Creates a new Skill based on the provided input. */
  createSkill: CreateSkillPayload;
  /** Deletes an Agent based on the ID. */
  deleteAgent: DeleteAgentPayload;
  /** Deletes a DependencyEdge based on the ID. */
  deleteDependencyEdge: DeleteDependencyEdgePayload;
  /** Deletes a Node by ID **together with its entire descendant tree** and any DependencyEdges that reference any of those nodes. */
  deleteNode: DeleteNodePayload;
  /** Deletes a PositionTag based on the ID. */
  deletePositionTag: DeletePositionTagPayload;
  /** Deletes a procedure and all associated entities (nodes, edges, variables). */
  deleteProcedure: DeleteProcedurePayload;
  /** Deletes a SceneObject based on the ID. */
  deleteSceneObject: DeleteSceneObjectPayload;
  /** Deletes a Skill based on the ID. */
  deleteSkill: DeleteSkillPayload;
  /** Loads a procedure, making it the currently active procedure. */
  loadProcedure: LoadProcedurePayload;
  /** Removes a variable from a procedure. */
  removeProcedureVariable: Procedure;
  /** Start a the loaded procedure. */
  startLoadedProcedure: StartLoadedProcedurePayload;
  /** Unloads the currently active procedure. */
  unloadProcedure: UnloadProcedurePayload;
  /** Updates an Agent based on the provided input. */
  updateAgent: UpdateAgentPayload;
  /** Updates a DependencyEdge based on the provided input. */
  updateDependencyEdge: UpdateDependencyEdgePayload;
  /** Updates a Node based on the provided input. */
  updateNode: UpdateNodePayload;
  /** Updates a PositionTag based on the provided input. */
  updatePositionTag: UpdatePositionTagPayload;
  /** Updates a variable in a procedure. */
  updateProcedureVariable: Procedure;
  /** Updates a SceneObject based on the provided input. */
  updateSceneObject: UpdateSceneObjectPayload;
  /** Updates a Skill based on the provided input. */
  updateSkill: UpdateSkillPayload;
};

export type MutationAddVariableToProcedureArgs = {
  procedureId: Scalars["UUID"]["input"];
  variable: VariableDefinitionInput;
};

export type MutationCreateAgentArgs = {
  input: CreateAgentInput;
};

export type MutationCreateDependencyEdgeArgs = {
  input: CreateDependencyEdgeInput;
};

export type MutationCreateNodeArgs = {
  input: CreateNodeInput;
};

export type MutationCreatePositionTagArgs = {
  input: CreatePositionTagInput;
};

export type MutationCreateProcedureArgs = {
  input: CreateProcedureInput;
};

export type MutationCreateSceneObjectArgs = {
  input: CreateSceneObjectInput;
};

export type MutationCreateSkillArgs = {
  input: CreateSkillInput;
};

export type MutationDeleteAgentArgs = {
  input: DeleteAgentInput;
};

export type MutationDeleteDependencyEdgeArgs = {
  input: DeleteDependencyEdgeInput;
};

export type MutationDeleteNodeArgs = {
  input: DeleteNodeInput;
};

export type MutationDeletePositionTagArgs = {
  input: DeletePositionTagInput;
};

export type MutationDeleteProcedureArgs = {
  input: DeleteProcedureInput;
};

export type MutationDeleteSceneObjectArgs = {
  input: DeleteSceneObjectInput;
};

export type MutationDeleteSkillArgs = {
  input: DeleteSkillInput;
};

export type MutationLoadProcedureArgs = {
  id: Scalars["UUID"]["input"];
};

export type MutationRemoveProcedureVariableArgs = {
  procedureId: Scalars["UUID"]["input"];
  variableName: Scalars["String"]["input"];
};

export type MutationUpdateAgentArgs = {
  input: UpdateAgentInput;
};

export type MutationUpdateDependencyEdgeArgs = {
  input: UpdateDependencyEdgeInput;
};

export type MutationUpdateNodeArgs = {
  input: UpdateNodeInput;
};

export type MutationUpdatePositionTagArgs = {
  input: UpdatePositionTagInput;
};

export type MutationUpdateProcedureVariableArgs = {
  procedureId: Scalars["UUID"]["input"];
  variable: VariableDefinitionInput;
  variableName: Scalars["String"]["input"];
};

export type MutationUpdateSceneObjectArgs = {
  input: UpdateSceneObjectInput;
};

export type MutationUpdateSkillArgs = {
  input: UpdateSkillInput;
};

export type Node = RouterNode | SkillExecutionNode | TaskNode;

export type NodeFilterInput = {
  and?: InputMaybe<Array<NodeFilterInput>>;
  draggable?: InputMaybe<BooleanOperationFilterInput>;
  dragging?: InputMaybe<BooleanOperationFilterInput>;
  extent?: InputMaybe<StringOperationFilterInput>;
  height?: InputMaybe<FloatOperationFilterInput>;
  hidden?: InputMaybe<BooleanOperationFilterInput>;
  id?: InputMaybe<UuidOperationFilterInput>;
  or?: InputMaybe<Array<NodeFilterInput>>;
  parentId?: InputMaybe<UuidOperationFilterInput>;
  position?: InputMaybe<NodePositionFilterInput>;
  selectable?: InputMaybe<BooleanOperationFilterInput>;
  selected?: InputMaybe<BooleanOperationFilterInput>;
  width?: InputMaybe<FloatOperationFilterInput>;
};

export type NodeInput =
  | {
      routerNode: RouterNodeInput;
      skillExecutionNode?: never;
      taskNode?: never;
    }
  | {
      routerNode?: never;
      skillExecutionNode: SkillExecutionNodeInput;
      taskNode?: never;
    }
  | { routerNode?: never; skillExecutionNode?: never; taskNode: TaskNodeInput };

export type NodePosition = {
  __typename?: "NodePosition";
  x: Scalars["Float"]["output"];
  y: Scalars["Float"]["output"];
};

export type NodePositionFilterInput = {
  and?: InputMaybe<Array<NodePositionFilterInput>>;
  or?: InputMaybe<Array<NodePositionFilterInput>>;
  x?: InputMaybe<FloatOperationFilterInput>;
  y?: InputMaybe<FloatOperationFilterInput>;
};

export type NodePositionInput = {
  x: Scalars["Float"]["input"];
  y: Scalars["Float"]["input"];
};

export type NumberPropertyInput = {
  value: Scalars["Float"]["input"];
};

/** Number value type. */
export type NumberType = {
  __typename?: "NumberType";
  typeName: Scalars["String"]["output"];
};

/** Wrapper type for number property values */
export type NumberValue = {
  __typename?: "NumberValue";
  /** The actual numeric value */
  numberValue: Scalars["Float"]["output"];
  /** Type descriptor */
  type: NumberType;
};

export type NumberValueTypeInput = {
  dummy?: InputMaybe<Scalars["Boolean"]["input"]>;
};

export type Position = {
  __typename?: "Position";
  alpha: Scalars["Float"]["output"];
  beta: Scalars["Float"]["output"];
  gamma: Scalars["Float"]["output"];
  x: Scalars["Float"]["output"];
  y: Scalars["Float"]["output"];
  z: Scalars["Float"]["output"];
};

export type PositionFilterInput = {
  alpha?: InputMaybe<FloatOperationFilterInput>;
  and?: InputMaybe<Array<PositionFilterInput>>;
  beta?: InputMaybe<FloatOperationFilterInput>;
  gamma?: InputMaybe<FloatOperationFilterInput>;
  or?: InputMaybe<Array<PositionFilterInput>>;
  x?: InputMaybe<FloatOperationFilterInput>;
  y?: InputMaybe<FloatOperationFilterInput>;
  z?: InputMaybe<FloatOperationFilterInput>;
};

export type PositionInput = {
  alpha: Scalars["Float"]["input"];
  beta: Scalars["Float"]["input"];
  gamma: Scalars["Float"]["input"];
  x: Scalars["Float"]["input"];
  y: Scalars["Float"]["input"];
  z: Scalars["Float"]["input"];
};

export type PositionPropertyInput = {
  value: PositionInput;
};

export type PositionSortInput = {
  alpha?: InputMaybe<SortEnumType>;
  beta?: InputMaybe<SortEnumType>;
  gamma?: InputMaybe<SortEnumType>;
  x?: InputMaybe<SortEnumType>;
  y?: InputMaybe<SortEnumType>;
  z?: InputMaybe<SortEnumType>;
};

export type PositionTag = {
  __typename?: "PositionTag";
  id: Scalars["UUID"]["output"];
  position: Position;
  tag: Scalars["String"]["output"];
};

export type PositionTagFilterInput = {
  and?: InputMaybe<Array<PositionTagFilterInput>>;
  id?: InputMaybe<UuidOperationFilterInput>;
  or?: InputMaybe<Array<PositionTagFilterInput>>;
  position?: InputMaybe<PositionFilterInput>;
  tag?: InputMaybe<StringOperationFilterInput>;
};

export type PositionTagInput = {
  id: Scalars["UUID"]["input"];
  position: PositionInput;
  tag: Scalars["String"]["input"];
};

export type PositionTagPropertyInput = {
  value: PositionTagInput;
};

export type PositionTagSortInput = {
  id?: InputMaybe<SortEnumType>;
  position?: InputMaybe<PositionSortInput>;
  tag?: InputMaybe<SortEnumType>;
};

/** Position tag value type. */
export type PositionTagType = {
  __typename?: "PositionTagType";
  typeName: Scalars["String"]["output"];
};

/** Wrapper type for position tag property values */
export type PositionTagValue = {
  __typename?: "PositionTagValue";
  /** The actual position tag reference */
  positionTagValue: PositionTag;
  /** Type descriptor */
  type: PositionTagType;
};

export type PositionTagValueTypeInput = {
  dummy?: InputMaybe<Scalars["Boolean"]["input"]>;
};

/** Position value type. */
export type PositionType = {
  __typename?: "PositionType";
  typeName: Scalars["String"]["output"];
};

/** Wrapper type for position property values */
export type PositionValue = {
  __typename?: "PositionValue";
  /** The actual position value */
  positionValue: Position;
  /** Type descriptor */
  type: PositionType;
};

export type PositionValueTypeInput = {
  dummy?: InputMaybe<Scalars["Boolean"]["input"]>;
};

/** Represents a procedure with its nodes, edges, and variables. */
export type Procedure = {
  __typename?: "Procedure";
  createdAtUtc: Scalars["DateTime"]["output"];
  description?: Maybe<Scalars["String"]["output"]>;
  id: Scalars["UUID"]["output"];
  isLoaded: Scalars["Boolean"]["output"];
  lastLoadedUtc?: Maybe<Scalars["DateTime"]["output"]>;
  lastUpdatedAtUtc: Scalars["DateTime"]["output"];
  name: Scalars["String"]["output"];
  rootNodeIds: Array<Scalars["UUID"]["output"]>;
  variables: Array<VariableDefinition>;
};

export type ProcedureValidationResult = {
  __typename?: "ProcedureValidationResult";
  agentSerializationViolations: Array<AgentSerializationViolation>;
  hasViolations: Scalars["Boolean"]["output"];
};

export type Property = {
  __typename?: "Property";
  binding?: Maybe<VariableBinding>;
  direction: PropertyDirection;
  name: Scalars["String"]["output"];
  value: PropertyValue;
};

export enum PropertyDirection {
  Input = "INPUT",
  InputOutput = "INPUT_OUTPUT",
  Output = "OUTPUT",
}

export type PropertyFilterInput = {
  and?: InputMaybe<Array<PropertyFilterInput>>;
  name?: InputMaybe<StringOperationFilterInput>;
  or?: InputMaybe<Array<PropertyFilterInput>>;
  value?: InputMaybe<PropertyTypeFilterInput>;
};

export type PropertyInput = {
  binding?: InputMaybe<VariableBindingInput>;
  direction: PropertyDirection;
  name: Scalars["String"]["input"];
  value: PropertyTypeInput;
};

export type PropertyTypeFilterInput = {
  and?: InputMaybe<Array<PropertyTypeFilterInput>>;
  or?: InputMaybe<Array<PropertyTypeFilterInput>>;
};

export type PropertyTypeInput =
  | {
      booleanProperty: BooleanPropertyInput;
      numberProperty?: never;
      positionProperty?: never;
      positionTagProperty?: never;
      sceneObjectProperty?: never;
      stringProperty?: never;
    }
  | {
      booleanProperty?: never;
      numberProperty: NumberPropertyInput;
      positionProperty?: never;
      positionTagProperty?: never;
      sceneObjectProperty?: never;
      stringProperty?: never;
    }
  | {
      booleanProperty?: never;
      numberProperty?: never;
      positionProperty: PositionPropertyInput;
      positionTagProperty?: never;
      sceneObjectProperty?: never;
      stringProperty?: never;
    }
  | {
      booleanProperty?: never;
      numberProperty?: never;
      positionProperty?: never;
      positionTagProperty: PositionTagPropertyInput;
      sceneObjectProperty?: never;
      stringProperty?: never;
    }
  | {
      booleanProperty?: never;
      numberProperty?: never;
      positionProperty?: never;
      positionTagProperty?: never;
      sceneObjectProperty: SceneObjectPropertyInput;
      stringProperty?: never;
    }
  | {
      booleanProperty?: never;
      numberProperty?: never;
      positionProperty?: never;
      positionTagProperty?: never;
      sceneObjectProperty?: never;
      stringProperty: StringPropertyInput;
    };

/** Union of all property value wrapper types */
export type PropertyValue =
  | BooleanValue
  | NumberValue
  | PositionTagValue
  | PositionValue
  | SceneObjectValue
  | StringValue;

export type Query = {
  __typename?: "Query";
  /** Gets a specific agent by its unique identifier. */
  agentById?: Maybe<Agent>;
  /** Gets all agents from the system with support for filtering and sorting. */
  agents: Array<Agent>;
  /** Gets a specific dependency edge by its unique identifier. */
  dependencyEdgeById?: Maybe<DependencyEdge>;
  /** Gets all dependency edges from the system with support for filtering and sorting. */
  dependencyEdges: Array<DependencyEdge>;
  /** Gets the currently loaded/active procedure, if any. */
  loadedProcedure?: Maybe<Procedure>;
  /** Gets a specific node by its unique identifier. */
  nodeById?: Maybe<Node>;
  /** Gets all nodes from the system with support for filtering. */
  nodes: Array<Node>;
  /** Gets a specific position tag by its unique identifier. */
  positionTagById?: Maybe<PositionTag>;
  /** Gets all position tags from the system with support for filtering and sorting. */
  positionTags: Array<PositionTag>;
  /** Gets a specific procedure by its unique identifier. */
  procedureById?: Maybe<Procedure>;
  /** Gets all procedures from the system. */
  procedures: Array<Procedure>;
  /** Gets a specific runtime agent by ID, combining domain and runtime information. */
  runtimeAgentById?: Maybe<RuntimeAgentInfo>;
  /** Gets a specific runtime agent by name, combining domain and runtime information. */
  runtimeAgentByName?: Maybe<RuntimeAgentInfo>;
  /** Gets all runtime agents, combining domain agent data with runtime status information. */
  runtimeAgents: Array<RuntimeAgentInfo>;
  /** Gets a specific scene object by its unique identifier. */
  sceneObjectById?: Maybe<SceneObject>;
  /** Gets all scene objects from the system with support for filtering and sorting. */
  sceneObjects: Array<SceneObject>;
  /** Gets the scheduling configuration including time-to-pixel scale and positioning settings. */
  schedulingConfiguration: SchedulingConfiguration;
  /** Gets a specific skill by its unique identifier. */
  skillById?: Maybe<Skill>;
  /** Gets all skills from the system with support for filtering and sorting. */
  skills: Array<Skill>;
};

export type QueryAgentByIdArgs = {
  id: Scalars["UUID"]["input"];
};

export type QueryAgentsArgs = {
  order?: InputMaybe<Array<AgentSortInput>>;
  where?: InputMaybe<AgentFilterInput>;
};

export type QueryDependencyEdgeByIdArgs = {
  id: Scalars["UUID"]["input"];
};

export type QueryDependencyEdgesArgs = {
  order?: InputMaybe<Array<DependencyEdgeSortInput>>;
  where?: InputMaybe<DependencyEdgeFilterInput>;
};

export type QueryNodeByIdArgs = {
  id: Scalars["UUID"]["input"];
};

export type QueryNodesArgs = {
  where?: InputMaybe<NodeFilterInput>;
};

export type QueryPositionTagByIdArgs = {
  id: Scalars["UUID"]["input"];
};

export type QueryPositionTagsArgs = {
  order?: InputMaybe<Array<PositionTagSortInput>>;
  where?: InputMaybe<PositionTagFilterInput>;
};

export type QueryProcedureByIdArgs = {
  id: Scalars["UUID"]["input"];
};

export type QueryRuntimeAgentByIdArgs = {
  agentId: Scalars["UUID"]["input"];
};

export type QueryRuntimeAgentByNameArgs = {
  agentName: Scalars["String"]["input"];
};

export type QuerySceneObjectByIdArgs = {
  id: Scalars["UUID"]["input"];
};

export type QuerySceneObjectsArgs = {
  order?: InputMaybe<Array<SceneObjectSortInput>>;
  where?: InputMaybe<SceneObjectFilterInput>;
};

export type QuerySkillByIdArgs = {
  id: Scalars["UUID"]["input"];
};

export type QuerySkillsArgs = {
  order?: InputMaybe<Array<SkillSortInput>>;
  where?: InputMaybe<SkillFilterInput>;
};

export type RouterNode = {
  __typename?: "RouterNode";
  draggable?: Maybe<Scalars["Boolean"]["output"]>;
  dragging?: Maybe<Scalars["Boolean"]["output"]>;
  extent?: Maybe<Scalars["String"]["output"]>;
  height?: Maybe<Scalars["Float"]["output"]>;
  hidden?: Maybe<Scalars["Boolean"]["output"]>;
  id: Scalars["UUID"]["output"];
  parentId?: Maybe<Scalars["UUID"]["output"]>;
  position: NodePosition;
  procedureId: Scalars["UUID"]["output"];
  routerTask: RouterTask;
  selectable?: Maybe<Scalars["Boolean"]["output"]>;
  selected?: Maybe<Scalars["Boolean"]["output"]>;
  width?: Maybe<Scalars["Float"]["output"]>;
};

export type RouterNodeInput = {
  draggable?: InputMaybe<Scalars["Boolean"]["input"]>;
  dragging?: InputMaybe<Scalars["Boolean"]["input"]>;
  extent?: InputMaybe<Scalars["String"]["input"]>;
  height?: InputMaybe<Scalars["Float"]["input"]>;
  hidden?: InputMaybe<Scalars["Boolean"]["input"]>;
  id: Scalars["UUID"]["input"];
  parentId?: InputMaybe<Scalars["UUID"]["input"]>;
  position: NodePositionInput;
  routerTaskInput: RouterTaskInput;
  selectable?: InputMaybe<Scalars["Boolean"]["input"]>;
  selected?: InputMaybe<Scalars["Boolean"]["input"]>;
  width?: InputMaybe<Scalars["Float"]["input"]>;
};

export type RouterTask = {
  __typename?: "RouterTask";
  branches: Array<ConditionalBranch>;
  description?: Maybe<Scalars["String"]["output"]>;
  duration: Scalars["Float"]["output"];
  finishTime?: Maybe<Scalars["Float"]["output"]>;
  isExecuting?: Maybe<Scalars["Boolean"]["output"]>;
  manuallySelectedBranch?: Maybe<Scalars["String"]["output"]>;
  name: Scalars["String"]["output"];
  progress?: Maybe<Scalars["Float"]["output"]>;
  selectedAtUtc?: Maybe<Scalars["DateTime"]["output"]>;
  selectedBranchName?: Maybe<Scalars["String"]["output"]>;
  selectedBranchTargetNodeId?: Maybe<Scalars["UUID"]["output"]>;
  selector: SelectorExpression;
  startTime: Scalars["Float"]["output"];
};

export type RouterTaskInput = {
  branches: Array<ConditionalBranchInput>;
  description?: InputMaybe<Scalars["String"]["input"]>;
  duration: Scalars["Float"]["input"];
  isExecuting?: InputMaybe<Scalars["Boolean"]["input"]>;
  manuallySelectedBranch?: InputMaybe<Scalars["String"]["input"]>;
  name: Scalars["String"]["input"];
  selector: SelectorExpressionInput;
  startTime: Scalars["Float"]["input"];
};

export type RuntimeAgentInfo = {
  __typename?: "RuntimeAgentInfo";
  agentType: Scalars["String"]["output"];
  availableSkills?: Maybe<Array<Skill>>;
  domainAgent?: Maybe<Agent>;
  healthStatus?: Maybe<AgentHealthStatus>;
  id: Scalars["UUID"]["output"];
  isActive: Scalars["Boolean"]["output"];
  lastSeen?: Maybe<Scalars["DateTime"]["output"]>;
  name: Scalars["String"]["output"];
  startedAt?: Maybe<Scalars["DateTime"]["output"]>;
};

export type SceneObject = {
  __typename?: "SceneObject";
  id: Scalars["UUID"]["output"];
  name: Scalars["String"]["output"];
  position: Position;
};

export type SceneObjectFilterInput = {
  and?: InputMaybe<Array<SceneObjectFilterInput>>;
  id?: InputMaybe<UuidOperationFilterInput>;
  name?: InputMaybe<StringOperationFilterInput>;
  or?: InputMaybe<Array<SceneObjectFilterInput>>;
  position?: InputMaybe<PositionFilterInput>;
};

export type SceneObjectInput = {
  id: Scalars["UUID"]["input"];
  name: Scalars["String"]["input"];
  position: PositionInput;
};

export type SceneObjectPropertyInput = {
  value: SceneObjectInput;
};

export type SceneObjectSortInput = {
  id?: InputMaybe<SortEnumType>;
  name?: InputMaybe<SortEnumType>;
  position?: InputMaybe<PositionSortInput>;
};

/** Scene object value type. */
export type SceneObjectType = {
  __typename?: "SceneObjectType";
  typeName: Scalars["String"]["output"];
};

/** Wrapper type for scene object property values */
export type SceneObjectValue = {
  __typename?: "SceneObjectValue";
  /** The actual scene object reference */
  sceneObjectValue: SceneObject;
  /** Type descriptor */
  type: SceneObjectType;
};

export type SceneObjectValueTypeInput = {
  dummy?: InputMaybe<Scalars["Boolean"]["input"]>;
};

export type SchedulingConfiguration = {
  __typename?: "SchedulingConfiguration";
  baseHeight: Scalars["Float"]["output"];
  baseYOffset: Scalars["Float"]["output"];
  containerBottomPadding: Scalars["Float"]["output"];
  containerTopPadding: Scalars["Float"]["output"];
  routerDropdownHeight: Scalars["Float"]["output"];
  siblingSpacing: Scalars["Float"]["output"];
  timeToPixelScale: Scalars["Float"]["output"];
};

export type SelectorExpression = ExpressionSelector | SimpleVariableSelector;

export type SelectorExpressionInput =
  | {
      expressionSelector: ExpressionSelectorInput;
      simpleVariableSelector?: never;
    }
  | {
      expressionSelector?: never;
      simpleVariableSelector: SimpleVariableSelectorInput;
    };

export type SimpleVariableSelector = {
  __typename?: "SimpleVariableSelector";
  expression: Scalars["String"]["output"];
};

export type SimpleVariableSelectorInput = {
  expression: Scalars["String"]["input"];
};

export type Skill = {
  __typename?: "Skill";
  agents: Array<Maybe<Agent>>;
  description: Scalars["String"]["output"];
  id: Scalars["UUID"]["output"];
  name: Scalars["String"]["output"];
  properties: Array<Property>;
};

export type SkillExecutionNode = {
  __typename?: "SkillExecutionNode";
  draggable?: Maybe<Scalars["Boolean"]["output"]>;
  dragging?: Maybe<Scalars["Boolean"]["output"]>;
  extent?: Maybe<Scalars["String"]["output"]>;
  height?: Maybe<Scalars["Float"]["output"]>;
  hidden?: Maybe<Scalars["Boolean"]["output"]>;
  id: Scalars["UUID"]["output"];
  parentId?: Maybe<Scalars["UUID"]["output"]>;
  position: NodePosition;
  procedureId: Scalars["UUID"]["output"];
  selectable?: Maybe<Scalars["Boolean"]["output"]>;
  selected?: Maybe<Scalars["Boolean"]["output"]>;
  skillExecutionTask: SkillExecutionTask;
  width?: Maybe<Scalars["Float"]["output"]>;
};

export type SkillExecutionNodeInput = {
  draggable?: InputMaybe<Scalars["Boolean"]["input"]>;
  dragging?: InputMaybe<Scalars["Boolean"]["input"]>;
  extent?: InputMaybe<Scalars["String"]["input"]>;
  height?: InputMaybe<Scalars["Float"]["input"]>;
  hidden?: InputMaybe<Scalars["Boolean"]["input"]>;
  id: Scalars["UUID"]["input"];
  parentId?: InputMaybe<Scalars["UUID"]["input"]>;
  position: NodePositionInput;
  selectable?: InputMaybe<Scalars["Boolean"]["input"]>;
  selected?: InputMaybe<Scalars["Boolean"]["input"]>;
  skillExecutionTask: SkillExecutionTaskInput;
  width?: InputMaybe<Scalars["Float"]["input"]>;
};

export type SkillExecutionTask = {
  __typename?: "SkillExecutionTask";
  agent: Agent;
  description?: Maybe<Scalars["String"]["output"]>;
  duration: Scalars["Float"]["output"];
  finishTime?: Maybe<Scalars["Float"]["output"]>;
  isExecuting?: Maybe<Scalars["Boolean"]["output"]>;
  name: Scalars["String"]["output"];
  progress?: Maybe<Scalars["Float"]["output"]>;
  skill: Skill;
  startTime: Scalars["Float"]["output"];
};

export type SkillExecutionTaskInput = {
  agentId: Scalars["UUID"]["input"];
  description?: InputMaybe<Scalars["String"]["input"]>;
  duration: Scalars["Float"]["input"];
  name: Scalars["String"]["input"];
  skill: SkillInput;
  startTime: Scalars["Float"]["input"];
};

export type SkillFilterInput = {
  agents?: InputMaybe<ListFilterInputTypeOfAgentFilterInput>;
  and?: InputMaybe<Array<SkillFilterInput>>;
  description?: InputMaybe<StringOperationFilterInput>;
  id?: InputMaybe<UuidOperationFilterInput>;
  name?: InputMaybe<StringOperationFilterInput>;
  or?: InputMaybe<Array<SkillFilterInput>>;
  properties?: InputMaybe<ListFilterInputTypeOfPropertyFilterInput>;
};

export type SkillInput = {
  agentIds?: InputMaybe<Array<Scalars["UUID"]["input"]>>;
  description: Scalars["String"]["input"];
  id: Scalars["UUID"]["input"];
  name: Scalars["String"]["input"];
  properties: Array<PropertyInput>;
};

export type SkillPair = {
  __typename?: "SkillPair";
  skillA: Scalars["UUID"]["output"];
  skillB: Scalars["UUID"]["output"];
};

export type SkillSortInput = {
  description?: InputMaybe<SortEnumType>;
  id?: InputMaybe<SortEnumType>;
  name?: InputMaybe<SortEnumType>;
};

export enum SortEnumType {
  Asc = "ASC",
  Desc = "DESC",
}

export type StartLoadedProcedurePayload = {
  __typename?: "StartLoadedProcedurePayload";
  boolean?: Maybe<Scalars["Boolean"]["output"]>;
};

export type StringOperationFilterInput = {
  and?: InputMaybe<Array<StringOperationFilterInput>>;
  contains?: InputMaybe<Scalars["String"]["input"]>;
  endsWith?: InputMaybe<Scalars["String"]["input"]>;
  eq?: InputMaybe<Scalars["String"]["input"]>;
  in?: InputMaybe<Array<InputMaybe<Scalars["String"]["input"]>>>;
  ncontains?: InputMaybe<Scalars["String"]["input"]>;
  nendsWith?: InputMaybe<Scalars["String"]["input"]>;
  neq?: InputMaybe<Scalars["String"]["input"]>;
  nin?: InputMaybe<Array<InputMaybe<Scalars["String"]["input"]>>>;
  nstartsWith?: InputMaybe<Scalars["String"]["input"]>;
  or?: InputMaybe<Array<StringOperationFilterInput>>;
  startsWith?: InputMaybe<Scalars["String"]["input"]>;
};

export type StringPropertyInput = {
  value: Scalars["String"]["input"];
};

/** String value type. */
export type StringType = {
  __typename?: "StringType";
  typeName: Scalars["String"]["output"];
};

/** Wrapper type for string property values */
export type StringValue = {
  __typename?: "StringValue";
  /** The actual string value */
  stringValue: Scalars["String"]["output"];
  /** Type descriptor */
  type: StringType;
};

export type StringValueTypeInput = {
  dummy?: InputMaybe<Scalars["Boolean"]["input"]>;
};

export type Subscription = {
  __typename?: "Subscription";
  agentsChanged: Array<Agent>;
  dependencyEdgesChanged: Array<DependencyEdge>;
  executionTimingChanged: ExecutionTiming;
  loadedProcedureIdentityChanged?: Maybe<LoadedProcedureIdentity>;
  nodesChanged: Array<Node>;
  positionTagsChanged: Array<PositionTag>;
  procedureValidationChanged: ProcedureValidationResult;
  procedureVariablesChanged: Array<VariableDefinition>;
  sceneObjectsChanged: Array<SceneObject>;
  skillsChanged: Array<Skill>;
};

export type Task = {
  __typename?: "Task";
  description?: Maybe<Scalars["String"]["output"]>;
  duration: Scalars["Float"]["output"];
  finishTime?: Maybe<Scalars["Float"]["output"]>;
  isExecuting?: Maybe<Scalars["Boolean"]["output"]>;
  name: Scalars["String"]["output"];
  progress?: Maybe<Scalars["Float"]["output"]>;
  startTime: Scalars["Float"]["output"];
};

export type TaskInput = {
  description?: InputMaybe<Scalars["String"]["input"]>;
  duration: Scalars["Float"]["input"];
  isExecuting?: InputMaybe<Scalars["Boolean"]["input"]>;
  name: Scalars["String"]["input"];
  startTime: Scalars["Float"]["input"];
};

export type TaskNode = {
  __typename?: "TaskNode";
  draggable?: Maybe<Scalars["Boolean"]["output"]>;
  dragging?: Maybe<Scalars["Boolean"]["output"]>;
  extent?: Maybe<Scalars["String"]["output"]>;
  height?: Maybe<Scalars["Float"]["output"]>;
  hidden?: Maybe<Scalars["Boolean"]["output"]>;
  id: Scalars["UUID"]["output"];
  parentId?: Maybe<Scalars["UUID"]["output"]>;
  position: NodePosition;
  procedureId: Scalars["UUID"]["output"];
  selectable?: Maybe<Scalars["Boolean"]["output"]>;
  selected?: Maybe<Scalars["Boolean"]["output"]>;
  task: Task;
  width?: Maybe<Scalars["Float"]["output"]>;
};

export type TaskNodeInput = {
  draggable?: InputMaybe<Scalars["Boolean"]["input"]>;
  dragging?: InputMaybe<Scalars["Boolean"]["input"]>;
  extent?: InputMaybe<Scalars["String"]["input"]>;
  height?: InputMaybe<Scalars["Float"]["input"]>;
  hidden?: InputMaybe<Scalars["Boolean"]["input"]>;
  id: Scalars["UUID"]["input"];
  parentId?: InputMaybe<Scalars["UUID"]["input"]>;
  position: NodePositionInput;
  selectable?: InputMaybe<Scalars["Boolean"]["input"]>;
  selected?: InputMaybe<Scalars["Boolean"]["input"]>;
  taskInput: TaskInput;
  width?: InputMaybe<Scalars["Float"]["input"]>;
};

export type UnloadProcedurePayload = {
  __typename?: "UnloadProcedurePayload";
  success: Scalars["Boolean"]["output"];
};

export type UnserializedSkill = {
  __typename?: "UnserializedSkill";
  nodeId: Scalars["UUID"]["output"];
  skillName: Scalars["String"]["output"];
};

export type UpdateAgentInput = {
  agentInput: AgentInput;
  id: Scalars["UUID"]["input"];
};

export type UpdateAgentPayload = {
  __typename?: "UpdateAgentPayload";
  agent?: Maybe<Agent>;
};

export type UpdateDependencyEdgeInput = {
  dependencyEdge: DependencyEdgeInput;
};

export type UpdateDependencyEdgePayload = {
  __typename?: "UpdateDependencyEdgePayload";
  boolean?: Maybe<Scalars["Boolean"]["output"]>;
};

export type UpdateNodeInput = {
  nodeInput: NodeInput;
};

export type UpdateNodePayload = {
  __typename?: "UpdateNodePayload";
  boolean?: Maybe<Scalars["Boolean"]["output"]>;
};

export type UpdatePositionTagInput = {
  positionTag: PositionTagInput;
};

export type UpdatePositionTagPayload = {
  __typename?: "UpdatePositionTagPayload";
  boolean?: Maybe<Scalars["Boolean"]["output"]>;
};

export type UpdateSceneObjectInput = {
  sceneObject: SceneObjectInput;
};

export type UpdateSceneObjectPayload = {
  __typename?: "UpdateSceneObjectPayload";
  boolean?: Maybe<Scalars["Boolean"]["output"]>;
};

export type UpdateSkillInput = {
  id: Scalars["UUID"]["input"];
  skillInput: SkillInput;
};

export type UpdateSkillPayload = {
  __typename?: "UpdateSkillPayload";
  skill?: Maybe<Skill>;
};

export type UuidOperationFilterInput = {
  eq?: InputMaybe<Scalars["UUID"]["input"]>;
  gt?: InputMaybe<Scalars["UUID"]["input"]>;
  gte?: InputMaybe<Scalars["UUID"]["input"]>;
  in?: InputMaybe<Array<InputMaybe<Scalars["UUID"]["input"]>>>;
  lt?: InputMaybe<Scalars["UUID"]["input"]>;
  lte?: InputMaybe<Scalars["UUID"]["input"]>;
  neq?: InputMaybe<Scalars["UUID"]["input"]>;
  ngt?: InputMaybe<Scalars["UUID"]["input"]>;
  ngte?: InputMaybe<Scalars["UUID"]["input"]>;
  nin?: InputMaybe<Array<InputMaybe<Scalars["UUID"]["input"]>>>;
  nlt?: InputMaybe<Scalars["UUID"]["input"]>;
  nlte?: InputMaybe<Scalars["UUID"]["input"]>;
};

/** Base type for all value types. */
export type ValueType =
  | BooleanType
  | EnumType
  | ListType
  | NumberType
  | PositionTagType
  | PositionType
  | SceneObjectType
  | StringType;

/** Input for value types. */
export type ValueTypeInput =
  | {
      boolean: BooleanValueTypeInput;
      enum?: never;
      list?: never;
      number?: never;
      position?: never;
      positionTag?: never;
      sceneObject?: never;
      string?: never;
    }
  | {
      boolean?: never;
      enum: EnumValueTypeInput;
      list?: never;
      number?: never;
      position?: never;
      positionTag?: never;
      sceneObject?: never;
      string?: never;
    }
  | {
      boolean?: never;
      enum?: never;
      list: ListValueTypeInput;
      number?: never;
      position?: never;
      positionTag?: never;
      sceneObject?: never;
      string?: never;
    }
  | {
      boolean?: never;
      enum?: never;
      list?: never;
      number: NumberValueTypeInput;
      position?: never;
      positionTag?: never;
      sceneObject?: never;
      string?: never;
    }
  | {
      boolean?: never;
      enum?: never;
      list?: never;
      number?: never;
      position: PositionValueTypeInput;
      positionTag?: never;
      sceneObject?: never;
      string?: never;
    }
  | {
      boolean?: never;
      enum?: never;
      list?: never;
      number?: never;
      position?: never;
      positionTag: PositionTagValueTypeInput;
      sceneObject?: never;
      string?: never;
    }
  | {
      boolean?: never;
      enum?: never;
      list?: never;
      number?: never;
      position?: never;
      positionTag?: never;
      sceneObject: SceneObjectValueTypeInput;
      string?: never;
    }
  | {
      boolean?: never;
      enum?: never;
      list?: never;
      number?: never;
      position?: never;
      positionTag?: never;
      sceneObject?: never;
      string: StringValueTypeInput;
    };

export type VariableBinding = {
  __typename?: "VariableBinding";
  mode: BindingMode;
  transformExpression?: Maybe<Scalars["String"]["output"]>;
  variableName: Scalars["String"]["output"];
};

export type VariableBindingInput = {
  mode: BindingMode;
  transformExpression?: InputMaybe<Scalars["String"]["input"]>;
  variableName: Scalars["String"]["input"];
};

/** Definition of a variable in a procedure. */
export type VariableDefinition = {
  __typename?: "VariableDefinition";
  defaultValue?: Maybe<Scalars["String"]["output"]>;
  description?: Maybe<Scalars["String"]["output"]>;
  isReadOnly: Scalars["Boolean"]["output"];
  name: Scalars["String"]["output"];
  scope: VariableScope;
  source: VariableSource;
  type: ValueType;
};

/** Input for creating or updating a variable definition. */
export type VariableDefinitionInput = {
  defaultValue?: InputMaybe<Scalars["String"]["input"]>;
  description?: InputMaybe<Scalars["String"]["input"]>;
  isReadOnly?: InputMaybe<Scalars["Boolean"]["input"]>;
  name: Scalars["String"]["input"];
  scope?: InputMaybe<VariableScope>;
  source?: InputMaybe<VariableSource>;
  type: ValueTypeInput;
};

/** Scope level where a variable is accessible. */
export enum VariableScope {
  Global = "GLOBAL",
  Procedure = "PROCEDURE",
  Task = "TASK",
}

/** Source of a variable's value. */
export enum VariableSource {
  AgentState = "AGENT_STATE",
  RuntimeComputed = "RUNTIME_COMPUTED",
  SensorData = "SENSOR_DATA",
  SkillOutput = "SKILL_OUTPUT",
  UserDefined = "USER_DEFINED",
}

export type GetAgentsQueryVariables = Exact<{ [key: string]: never }>;

export type GetAgentsQuery = {
  __typename?: "Query";
  agents: Array<{
    __typename?: "Agent";
    id: any;
    name: string;
    representativeColor: string;
    skills: Array<{
      __typename?: "Skill";
      id: any;
      name: string;
      description: string;
      properties: Array<{
        __typename?: "Property";
        name: string;
        direction: PropertyDirection;
        binding?: {
          __typename?: "VariableBinding";
          variableName: string;
          mode: BindingMode;
          transformExpression?: string | null;
        } | null;
        value:
          | {
              __typename: "BooleanValue";
              boolValue: boolean;
              type: { __typename?: "BooleanType"; typeName: string };
            }
          | {
              __typename: "NumberValue";
              numberValue: number;
              type: { __typename?: "NumberType"; typeName: string };
            }
          | {
              __typename: "PositionTagValue";
              positionTagValue: {
                __typename?: "PositionTag";
                id: any;
                tag: string;
                position: {
                  __typename?: "Position";
                  x: number;
                  y: number;
                  z: number;
                  alpha: number;
                  beta: number;
                  gamma: number;
                };
              };
              type: { __typename?: "PositionTagType"; typeName: string };
            }
          | {
              __typename: "PositionValue";
              positionValue: {
                __typename?: "Position";
                x: number;
                y: number;
                z: number;
                alpha: number;
                beta: number;
                gamma: number;
              };
              type: { __typename?: "PositionType"; typeName: string };
            }
          | {
              __typename: "SceneObjectValue";
              sceneObjectValue: {
                __typename?: "SceneObject";
                id: any;
                name: string;
                position: {
                  __typename?: "Position";
                  x: number;
                  y: number;
                  z: number;
                  alpha: number;
                  beta: number;
                  gamma: number;
                };
              };
              type: { __typename?: "SceneObjectType"; typeName: string };
            }
          | {
              __typename: "StringValue";
              stringValue: string;
              type: { __typename?: "StringType"; typeName: string };
            };
      }>;
      agents: Array<{
        __typename?: "Agent";
        id: any;
        name: string;
        representativeColor: string;
      } | null>;
    } | null>;
  }>;
};

export type GetAgentByIdQueryVariables = Exact<{
  id: Scalars["UUID"]["input"];
}>;

export type GetAgentByIdQuery = {
  __typename?: "Query";
  agentById?: {
    __typename?: "Agent";
    id: any;
    name: string;
    representativeColor: string;
    skills: Array<{
      __typename?: "Skill";
      id: any;
      name: string;
      description: string;
      properties: Array<{
        __typename?: "Property";
        name: string;
        direction: PropertyDirection;
        binding?: {
          __typename?: "VariableBinding";
          variableName: string;
          mode: BindingMode;
          transformExpression?: string | null;
        } | null;
        value:
          | {
              __typename: "BooleanValue";
              boolValue: boolean;
              type: { __typename?: "BooleanType"; typeName: string };
            }
          | {
              __typename: "NumberValue";
              numberValue: number;
              type: { __typename?: "NumberType"; typeName: string };
            }
          | {
              __typename: "PositionTagValue";
              positionTagValue: {
                __typename?: "PositionTag";
                id: any;
                tag: string;
                position: {
                  __typename?: "Position";
                  x: number;
                  y: number;
                  z: number;
                  alpha: number;
                  beta: number;
                  gamma: number;
                };
              };
              type: { __typename?: "PositionTagType"; typeName: string };
            }
          | {
              __typename: "PositionValue";
              positionValue: {
                __typename?: "Position";
                x: number;
                y: number;
                z: number;
                alpha: number;
                beta: number;
                gamma: number;
              };
              type: { __typename?: "PositionType"; typeName: string };
            }
          | {
              __typename: "SceneObjectValue";
              sceneObjectValue: {
                __typename?: "SceneObject";
                id: any;
                name: string;
                position: {
                  __typename?: "Position";
                  x: number;
                  y: number;
                  z: number;
                  alpha: number;
                  beta: number;
                  gamma: number;
                };
              };
              type: { __typename?: "SceneObjectType"; typeName: string };
            }
          | {
              __typename: "StringValue";
              stringValue: string;
              type: { __typename?: "StringType"; typeName: string };
            };
      }>;
      agents: Array<{
        __typename?: "Agent";
        id: any;
        name: string;
        representativeColor: string;
      } | null>;
    } | null>;
  } | null;
};

export type GetSkillsQueryVariables = Exact<{ [key: string]: never }>;

export type GetSkillsQuery = {
  __typename?: "Query";
  skills: Array<{
    __typename?: "Skill";
    id: any;
    name: string;
    description: string;
    properties: Array<{
      __typename?: "Property";
      name: string;
      direction: PropertyDirection;
      binding?: {
        __typename?: "VariableBinding";
        variableName: string;
        mode: BindingMode;
        transformExpression?: string | null;
      } | null;
      value:
        | {
            __typename: "BooleanValue";
            boolValue: boolean;
            type: { __typename?: "BooleanType"; typeName: string };
          }
        | {
            __typename: "NumberValue";
            numberValue: number;
            type: { __typename?: "NumberType"; typeName: string };
          }
        | {
            __typename: "PositionTagValue";
            positionTagValue: {
              __typename?: "PositionTag";
              id: any;
              tag: string;
              position: {
                __typename?: "Position";
                x: number;
                y: number;
                z: number;
                alpha: number;
                beta: number;
                gamma: number;
              };
            };
            type: { __typename?: "PositionTagType"; typeName: string };
          }
        | {
            __typename: "PositionValue";
            positionValue: {
              __typename?: "Position";
              x: number;
              y: number;
              z: number;
              alpha: number;
              beta: number;
              gamma: number;
            };
            type: { __typename?: "PositionType"; typeName: string };
          }
        | {
            __typename: "SceneObjectValue";
            sceneObjectValue: {
              __typename?: "SceneObject";
              id: any;
              name: string;
              position: {
                __typename?: "Position";
                x: number;
                y: number;
                z: number;
                alpha: number;
                beta: number;
                gamma: number;
              };
            };
            type: { __typename?: "SceneObjectType"; typeName: string };
          }
        | {
            __typename: "StringValue";
            stringValue: string;
            type: { __typename?: "StringType"; typeName: string };
          };
    }>;
    agents: Array<{
      __typename?: "Agent";
      id: any;
      name: string;
      representativeColor: string;
    } | null>;
  }>;
};

export type GetSkillByIdQueryVariables = Exact<{
  skillId: Scalars["UUID"]["input"];
}>;

export type GetSkillByIdQuery = {
  __typename?: "Query";
  skillById?: {
    __typename?: "Skill";
    id: any;
    name: string;
    description: string;
    properties: Array<{
      __typename?: "Property";
      name: string;
      direction: PropertyDirection;
      binding?: {
        __typename?: "VariableBinding";
        variableName: string;
        mode: BindingMode;
        transformExpression?: string | null;
      } | null;
      value:
        | {
            __typename: "BooleanValue";
            boolValue: boolean;
            type: { __typename?: "BooleanType"; typeName: string };
          }
        | {
            __typename: "NumberValue";
            numberValue: number;
            type: { __typename?: "NumberType"; typeName: string };
          }
        | {
            __typename: "PositionTagValue";
            positionTagValue: {
              __typename?: "PositionTag";
              id: any;
              tag: string;
              position: {
                __typename?: "Position";
                x: number;
                y: number;
                z: number;
                alpha: number;
                beta: number;
                gamma: number;
              };
            };
            type: { __typename?: "PositionTagType"; typeName: string };
          }
        | {
            __typename: "PositionValue";
            positionValue: {
              __typename?: "Position";
              x: number;
              y: number;
              z: number;
              alpha: number;
              beta: number;
              gamma: number;
            };
            type: { __typename?: "PositionType"; typeName: string };
          }
        | {
            __typename: "SceneObjectValue";
            sceneObjectValue: {
              __typename?: "SceneObject";
              id: any;
              name: string;
              position: {
                __typename?: "Position";
                x: number;
                y: number;
                z: number;
                alpha: number;
                beta: number;
                gamma: number;
              };
            };
            type: { __typename?: "SceneObjectType"; typeName: string };
          }
        | {
            __typename: "StringValue";
            stringValue: string;
            type: { __typename?: "StringType"; typeName: string };
          };
    }>;
    agents: Array<{
      __typename?: "Agent";
      id: any;
      name: string;
      representativeColor: string;
    } | null>;
  } | null;
};

export type GetSkillsByAgentIdQueryVariables = Exact<{
  agentId: Scalars["UUID"]["input"];
}>;

export type GetSkillsByAgentIdQuery = {
  __typename?: "Query";
  agentById?: {
    __typename?: "Agent";
    id: any;
    name: string;
    representativeColor: string;
    skills: Array<{
      __typename?: "Skill";
      id: any;
      name: string;
      description: string;
      properties: Array<{
        __typename?: "Property";
        name: string;
        direction: PropertyDirection;
        binding?: {
          __typename?: "VariableBinding";
          variableName: string;
          mode: BindingMode;
          transformExpression?: string | null;
        } | null;
        value:
          | {
              __typename: "BooleanValue";
              boolValue: boolean;
              type: { __typename?: "BooleanType"; typeName: string };
            }
          | {
              __typename: "NumberValue";
              numberValue: number;
              type: { __typename?: "NumberType"; typeName: string };
            }
          | {
              __typename: "PositionTagValue";
              positionTagValue: {
                __typename?: "PositionTag";
                id: any;
                tag: string;
                position: {
                  __typename?: "Position";
                  x: number;
                  y: number;
                  z: number;
                  alpha: number;
                  beta: number;
                  gamma: number;
                };
              };
              type: { __typename?: "PositionTagType"; typeName: string };
            }
          | {
              __typename: "PositionValue";
              positionValue: {
                __typename?: "Position";
                x: number;
                y: number;
                z: number;
                alpha: number;
                beta: number;
                gamma: number;
              };
              type: { __typename?: "PositionType"; typeName: string };
            }
          | {
              __typename: "SceneObjectValue";
              sceneObjectValue: {
                __typename?: "SceneObject";
                id: any;
                name: string;
                position: {
                  __typename?: "Position";
                  x: number;
                  y: number;
                  z: number;
                  alpha: number;
                  beta: number;
                  gamma: number;
                };
              };
              type: { __typename?: "SceneObjectType"; typeName: string };
            }
          | {
              __typename: "StringValue";
              stringValue: string;
              type: { __typename?: "StringType"; typeName: string };
            };
      }>;
      agents: Array<{
        __typename?: "Agent";
        id: any;
        name: string;
        representativeColor: string;
      } | null>;
    } | null>;
  } | null;
};

export type GetAgentsBySkillIdQueryVariables = Exact<{
  skillId: Scalars["UUID"]["input"];
}>;

export type GetAgentsBySkillIdQuery = {
  __typename?: "Query";
  skillById?: {
    __typename?: "Skill";
    id: any;
    name: string;
    description: string;
    agents: Array<{
      __typename?: "Agent";
      id: any;
      name: string;
      representativeColor: string;
    } | null>;
    properties: Array<{
      __typename?: "Property";
      name: string;
      direction: PropertyDirection;
      binding?: {
        __typename?: "VariableBinding";
        variableName: string;
        mode: BindingMode;
        transformExpression?: string | null;
      } | null;
      value:
        | {
            __typename: "BooleanValue";
            boolValue: boolean;
            type: { __typename?: "BooleanType"; typeName: string };
          }
        | {
            __typename: "NumberValue";
            numberValue: number;
            type: { __typename?: "NumberType"; typeName: string };
          }
        | {
            __typename: "PositionTagValue";
            positionTagValue: {
              __typename?: "PositionTag";
              id: any;
              tag: string;
              position: {
                __typename?: "Position";
                x: number;
                y: number;
                z: number;
                alpha: number;
                beta: number;
                gamma: number;
              };
            };
            type: { __typename?: "PositionTagType"; typeName: string };
          }
        | {
            __typename: "PositionValue";
            positionValue: {
              __typename?: "Position";
              x: number;
              y: number;
              z: number;
              alpha: number;
              beta: number;
              gamma: number;
            };
            type: { __typename?: "PositionType"; typeName: string };
          }
        | {
            __typename: "SceneObjectValue";
            sceneObjectValue: {
              __typename?: "SceneObject";
              id: any;
              name: string;
              position: {
                __typename?: "Position";
                x: number;
                y: number;
                z: number;
                alpha: number;
                beta: number;
                gamma: number;
              };
            };
            type: { __typename?: "SceneObjectType"; typeName: string };
          }
        | {
            __typename: "StringValue";
            stringValue: string;
            type: { __typename?: "StringType"; typeName: string };
          };
    }>;
  } | null;
};

export type CreateAgentMutationVariables = Exact<{
  input: CreateAgentInput;
}>;

export type CreateAgentMutation = {
  __typename?: "Mutation";
  createAgent: {
    __typename?: "CreateAgentPayload";
    agent?: {
      __typename?: "Agent";
      id: any;
      name: string;
      representativeColor: string;
    } | null;
  };
};

export type UpdateAgentMutationVariables = Exact<{
  input: UpdateAgentInput;
}>;

export type UpdateAgentMutation = {
  __typename?: "Mutation";
  updateAgent: {
    __typename?: "UpdateAgentPayload";
    agent?: {
      __typename?: "Agent";
      id: any;
      name: string;
      representativeColor: string;
    } | null;
  };
};

export type DeleteAgentMutationVariables = Exact<{
  input: DeleteAgentInput;
}>;

export type DeleteAgentMutation = {
  __typename?: "Mutation";
  deleteAgent: { __typename?: "DeleteAgentPayload"; boolean?: boolean | null };
};

export type CreateSkillMutationVariables = Exact<{
  input: CreateSkillInput;
}>;

export type CreateSkillMutation = {
  __typename?: "Mutation";
  createSkill: {
    __typename?: "CreateSkillPayload";
    skill?: {
      __typename?: "Skill";
      id: any;
      name: string;
      description: string;
      properties: Array<{
        __typename?: "Property";
        name: string;
        direction: PropertyDirection;
        binding?: {
          __typename?: "VariableBinding";
          variableName: string;
          mode: BindingMode;
          transformExpression?: string | null;
        } | null;
        value:
          | {
              __typename: "BooleanValue";
              boolValue: boolean;
              type: { __typename?: "BooleanType"; typeName: string };
            }
          | {
              __typename: "NumberValue";
              numberValue: number;
              type: { __typename?: "NumberType"; typeName: string };
            }
          | {
              __typename: "PositionTagValue";
              positionTagValue: {
                __typename?: "PositionTag";
                id: any;
                tag: string;
                position: {
                  __typename?: "Position";
                  x: number;
                  y: number;
                  z: number;
                  alpha: number;
                  beta: number;
                  gamma: number;
                };
              };
              type: { __typename?: "PositionTagType"; typeName: string };
            }
          | {
              __typename: "PositionValue";
              positionValue: {
                __typename?: "Position";
                x: number;
                y: number;
                z: number;
                alpha: number;
                beta: number;
                gamma: number;
              };
              type: { __typename?: "PositionType"; typeName: string };
            }
          | {
              __typename: "SceneObjectValue";
              sceneObjectValue: {
                __typename?: "SceneObject";
                id: any;
                name: string;
                position: {
                  __typename?: "Position";
                  x: number;
                  y: number;
                  z: number;
                  alpha: number;
                  beta: number;
                  gamma: number;
                };
              };
              type: { __typename?: "SceneObjectType"; typeName: string };
            }
          | {
              __typename: "StringValue";
              stringValue: string;
              type: { __typename?: "StringType"; typeName: string };
            };
      }>;
      agents: Array<{
        __typename?: "Agent";
        id: any;
        name: string;
        representativeColor: string;
      } | null>;
    } | null;
  };
};

export type UpdateSkillMutationVariables = Exact<{
  input: UpdateSkillInput;
}>;

export type UpdateSkillMutation = {
  __typename?: "Mutation";
  updateSkill: {
    __typename?: "UpdateSkillPayload";
    skill?: {
      __typename?: "Skill";
      id: any;
      name: string;
      description: string;
      properties: Array<{
        __typename?: "Property";
        name: string;
        direction: PropertyDirection;
        binding?: {
          __typename?: "VariableBinding";
          variableName: string;
          mode: BindingMode;
          transformExpression?: string | null;
        } | null;
        value:
          | {
              __typename: "BooleanValue";
              boolValue: boolean;
              type: { __typename?: "BooleanType"; typeName: string };
            }
          | {
              __typename: "NumberValue";
              numberValue: number;
              type: { __typename?: "NumberType"; typeName: string };
            }
          | {
              __typename: "PositionTagValue";
              positionTagValue: {
                __typename?: "PositionTag";
                id: any;
                tag: string;
                position: {
                  __typename?: "Position";
                  x: number;
                  y: number;
                  z: number;
                  alpha: number;
                  beta: number;
                  gamma: number;
                };
              };
              type: { __typename?: "PositionTagType"; typeName: string };
            }
          | {
              __typename: "PositionValue";
              positionValue: {
                __typename?: "Position";
                x: number;
                y: number;
                z: number;
                alpha: number;
                beta: number;
                gamma: number;
              };
              type: { __typename?: "PositionType"; typeName: string };
            }
          | {
              __typename: "SceneObjectValue";
              sceneObjectValue: {
                __typename?: "SceneObject";
                id: any;
                name: string;
                position: {
                  __typename?: "Position";
                  x: number;
                  y: number;
                  z: number;
                  alpha: number;
                  beta: number;
                  gamma: number;
                };
              };
              type: { __typename?: "SceneObjectType"; typeName: string };
            }
          | {
              __typename: "StringValue";
              stringValue: string;
              type: { __typename?: "StringType"; typeName: string };
            };
      }>;
      agents: Array<{
        __typename?: "Agent";
        id: any;
        name: string;
        representativeColor: string;
      } | null>;
    } | null;
  };
};

export type DeleteSkillMutationVariables = Exact<{
  input: DeleteSkillInput;
}>;

export type DeleteSkillMutation = {
  __typename?: "Mutation";
  deleteSkill: { __typename?: "DeleteSkillPayload"; boolean?: boolean | null };
};

export type GetSchedulingConfigurationQueryVariables = Exact<{
  [key: string]: never;
}>;

export type GetSchedulingConfigurationQuery = {
  __typename?: "Query";
  schedulingConfiguration: {
    __typename?: "SchedulingConfiguration";
    timeToPixelScale: number;
    baseYOffset: number;
    siblingSpacing: number;
    containerTopPadding: number;
    containerBottomPadding: number;
    baseHeight: number;
    routerDropdownHeight: number;
  };
};

export type GetDependencyEdgesQueryVariables = Exact<{
  where?: InputMaybe<DependencyEdgeFilterInput>;
  order?: InputMaybe<Array<DependencyEdgeSortInput> | DependencyEdgeSortInput>;
}>;

export type GetDependencyEdgesQuery = {
  __typename?: "Query";
  dependencyEdges: Array<{
    __typename?: "DependencyEdge";
    id: any;
    sourceId: any;
    targetId: any;
    sourceHandle?: string | null;
    targetHandle?: string | null;
  }>;
};

export type GetDependencyEdgeByIdQueryVariables = Exact<{
  id: Scalars["UUID"]["input"];
}>;

export type GetDependencyEdgeByIdQuery = {
  __typename?: "Query";
  dependencyEdgeById?: {
    __typename?: "DependencyEdge";
    id: any;
    sourceId: any;
    targetId: any;
    sourceHandle?: string | null;
    targetHandle?: string | null;
  } | null;
};

export type CreateDependencyEdgeMutationVariables = Exact<{
  input: CreateDependencyEdgeInput;
}>;

export type CreateDependencyEdgeMutation = {
  __typename?: "Mutation";
  createDependencyEdge: {
    __typename?: "CreateDependencyEdgePayload";
    dependencyEdge?: {
      __typename?: "DependencyEdge";
      id: any;
      sourceId: any;
      targetId: any;
      sourceHandle?: string | null;
      targetHandle?: string | null;
    } | null;
  };
};

export type UpdateDependencyEdgeMutationVariables = Exact<{
  input: UpdateDependencyEdgeInput;
}>;

export type UpdateDependencyEdgeMutation = {
  __typename?: "Mutation";
  updateDependencyEdge: {
    __typename?: "UpdateDependencyEdgePayload";
    boolean?: boolean | null;
  };
};

export type DeleteDependencyEdgeMutationVariables = Exact<{
  input: DeleteDependencyEdgeInput;
}>;

export type DeleteDependencyEdgeMutation = {
  __typename?: "Mutation";
  deleteDependencyEdge: {
    __typename?: "DeleteDependencyEdgePayload";
    boolean?: boolean | null;
  };
};

export type OnDependencyEdgesChangedSubscriptionVariables = Exact<{
  [key: string]: never;
}>;

export type OnDependencyEdgesChangedSubscription = {
  __typename?: "Subscription";
  dependencyEdgesChanged: Array<{
    __typename?: "DependencyEdge";
    id: any;
    sourceId: any;
    targetId: any;
    sourceHandle?: string | null;
    targetHandle?: string | null;
  }>;
};

export type AgentFieldsFragment = {
  __typename?: "Agent";
  id: any;
  name: string;
  representativeColor: string;
};

export type PropertyFieldsFragment = {
  __typename?: "Property";
  name: string;
  direction: PropertyDirection;
  binding?: {
    __typename?: "VariableBinding";
    variableName: string;
    mode: BindingMode;
    transformExpression?: string | null;
  } | null;
  value:
    | {
        __typename: "BooleanValue";
        boolValue: boolean;
        type: { __typename?: "BooleanType"; typeName: string };
      }
    | {
        __typename: "NumberValue";
        numberValue: number;
        type: { __typename?: "NumberType"; typeName: string };
      }
    | {
        __typename: "PositionTagValue";
        positionTagValue: {
          __typename?: "PositionTag";
          id: any;
          tag: string;
          position: {
            __typename?: "Position";
            x: number;
            y: number;
            z: number;
            alpha: number;
            beta: number;
            gamma: number;
          };
        };
        type: { __typename?: "PositionTagType"; typeName: string };
      }
    | {
        __typename: "PositionValue";
        positionValue: {
          __typename?: "Position";
          x: number;
          y: number;
          z: number;
          alpha: number;
          beta: number;
          gamma: number;
        };
        type: { __typename?: "PositionType"; typeName: string };
      }
    | {
        __typename: "SceneObjectValue";
        sceneObjectValue: {
          __typename?: "SceneObject";
          id: any;
          name: string;
          position: {
            __typename?: "Position";
            x: number;
            y: number;
            z: number;
            alpha: number;
            beta: number;
            gamma: number;
          };
        };
        type: { __typename?: "SceneObjectType"; typeName: string };
      }
    | {
        __typename: "StringValue";
        stringValue: string;
        type: { __typename?: "StringType"; typeName: string };
      };
};

export type SkillFieldsFragment = {
  __typename?: "Skill";
  id: any;
  name: string;
  description: string;
  properties: Array<{
    __typename?: "Property";
    name: string;
    direction: PropertyDirection;
    binding?: {
      __typename?: "VariableBinding";
      variableName: string;
      mode: BindingMode;
      transformExpression?: string | null;
    } | null;
    value:
      | {
          __typename: "BooleanValue";
          boolValue: boolean;
          type: { __typename?: "BooleanType"; typeName: string };
        }
      | {
          __typename: "NumberValue";
          numberValue: number;
          type: { __typename?: "NumberType"; typeName: string };
        }
      | {
          __typename: "PositionTagValue";
          positionTagValue: {
            __typename?: "PositionTag";
            id: any;
            tag: string;
            position: {
              __typename?: "Position";
              x: number;
              y: number;
              z: number;
              alpha: number;
              beta: number;
              gamma: number;
            };
          };
          type: { __typename?: "PositionTagType"; typeName: string };
        }
      | {
          __typename: "PositionValue";
          positionValue: {
            __typename?: "Position";
            x: number;
            y: number;
            z: number;
            alpha: number;
            beta: number;
            gamma: number;
          };
          type: { __typename?: "PositionType"; typeName: string };
        }
      | {
          __typename: "SceneObjectValue";
          sceneObjectValue: {
            __typename?: "SceneObject";
            id: any;
            name: string;
            position: {
              __typename?: "Position";
              x: number;
              y: number;
              z: number;
              alpha: number;
              beta: number;
              gamma: number;
            };
          };
          type: { __typename?: "SceneObjectType"; typeName: string };
        }
      | {
          __typename: "StringValue";
          stringValue: string;
          type: { __typename?: "StringType"; typeName: string };
        };
  }>;
  agents: Array<{
    __typename?: "Agent";
    id: any;
    name: string;
    representativeColor: string;
  } | null>;
};

export type NodePositionFieldsFragment = {
  __typename?: "NodePosition";
  x: number;
  y: number;
};

export type TaskFieldsFragment = {
  __typename?: "Task";
  name: string;
  description?: string | null;
  startTime: number;
  duration: number;
  isExecuting?: boolean | null;
  progress?: number | null;
};

export type SkillExecutionTaskFieldsFragment = {
  __typename?: "SkillExecutionTask";
  name: string;
  description?: string | null;
  startTime: number;
  duration: number;
  isExecuting?: boolean | null;
  progress?: number | null;
  skill: {
    __typename?: "Skill";
    id: any;
    name: string;
    description: string;
    properties: Array<{
      __typename?: "Property";
      name: string;
      direction: PropertyDirection;
      binding?: {
        __typename?: "VariableBinding";
        variableName: string;
        mode: BindingMode;
        transformExpression?: string | null;
      } | null;
      value:
        | {
            __typename: "BooleanValue";
            boolValue: boolean;
            type: { __typename?: "BooleanType"; typeName: string };
          }
        | {
            __typename: "NumberValue";
            numberValue: number;
            type: { __typename?: "NumberType"; typeName: string };
          }
        | {
            __typename: "PositionTagValue";
            positionTagValue: {
              __typename?: "PositionTag";
              id: any;
              tag: string;
              position: {
                __typename?: "Position";
                x: number;
                y: number;
                z: number;
                alpha: number;
                beta: number;
                gamma: number;
              };
            };
            type: { __typename?: "PositionTagType"; typeName: string };
          }
        | {
            __typename: "PositionValue";
            positionValue: {
              __typename?: "Position";
              x: number;
              y: number;
              z: number;
              alpha: number;
              beta: number;
              gamma: number;
            };
            type: { __typename?: "PositionType"; typeName: string };
          }
        | {
            __typename: "SceneObjectValue";
            sceneObjectValue: {
              __typename?: "SceneObject";
              id: any;
              name: string;
              position: {
                __typename?: "Position";
                x: number;
                y: number;
                z: number;
                alpha: number;
                beta: number;
                gamma: number;
              };
            };
            type: { __typename?: "SceneObjectType"; typeName: string };
          }
        | {
            __typename: "StringValue";
            stringValue: string;
            type: { __typename?: "StringType"; typeName: string };
          };
    }>;
    agents: Array<{
      __typename?: "Agent";
      id: any;
      name: string;
      representativeColor: string;
    } | null>;
  };
  agent: {
    __typename?: "Agent";
    id: any;
    name: string;
    representativeColor: string;
  };
};

export type TaskNodeFieldsFragment = {
  __typename?: "TaskNode";
  id: any;
  parentId?: any | null;
  extent?: string | null;
  width?: number | null;
  height?: number | null;
  selectable?: boolean | null;
  selected?: boolean | null;
  draggable?: boolean | null;
  dragging?: boolean | null;
  hidden?: boolean | null;
  position: { __typename?: "NodePosition"; x: number; y: number };
  task: {
    __typename?: "Task";
    name: string;
    description?: string | null;
    startTime: number;
    duration: number;
    isExecuting?: boolean | null;
    progress?: number | null;
  };
};

export type SkillExecutionNodeFieldsFragment = {
  __typename?: "SkillExecutionNode";
  id: any;
  parentId?: any | null;
  extent?: string | null;
  width?: number | null;
  height?: number | null;
  selectable?: boolean | null;
  selected?: boolean | null;
  draggable?: boolean | null;
  dragging?: boolean | null;
  hidden?: boolean | null;
  position: { __typename?: "NodePosition"; x: number; y: number };
  skillExecutionTask: {
    __typename?: "SkillExecutionTask";
    name: string;
    description?: string | null;
    startTime: number;
    duration: number;
    isExecuting?: boolean | null;
    progress?: number | null;
    skill: {
      __typename?: "Skill";
      id: any;
      name: string;
      description: string;
      properties: Array<{
        __typename?: "Property";
        name: string;
        direction: PropertyDirection;
        binding?: {
          __typename?: "VariableBinding";
          variableName: string;
          mode: BindingMode;
          transformExpression?: string | null;
        } | null;
        value:
          | {
              __typename: "BooleanValue";
              boolValue: boolean;
              type: { __typename?: "BooleanType"; typeName: string };
            }
          | {
              __typename: "NumberValue";
              numberValue: number;
              type: { __typename?: "NumberType"; typeName: string };
            }
          | {
              __typename: "PositionTagValue";
              positionTagValue: {
                __typename?: "PositionTag";
                id: any;
                tag: string;
                position: {
                  __typename?: "Position";
                  x: number;
                  y: number;
                  z: number;
                  alpha: number;
                  beta: number;
                  gamma: number;
                };
              };
              type: { __typename?: "PositionTagType"; typeName: string };
            }
          | {
              __typename: "PositionValue";
              positionValue: {
                __typename?: "Position";
                x: number;
                y: number;
                z: number;
                alpha: number;
                beta: number;
                gamma: number;
              };
              type: { __typename?: "PositionType"; typeName: string };
            }
          | {
              __typename: "SceneObjectValue";
              sceneObjectValue: {
                __typename?: "SceneObject";
                id: any;
                name: string;
                position: {
                  __typename?: "Position";
                  x: number;
                  y: number;
                  z: number;
                  alpha: number;
                  beta: number;
                  gamma: number;
                };
              };
              type: { __typename?: "SceneObjectType"; typeName: string };
            }
          | {
              __typename: "StringValue";
              stringValue: string;
              type: { __typename?: "StringType"; typeName: string };
            };
      }>;
      agents: Array<{
        __typename?: "Agent";
        id: any;
        name: string;
        representativeColor: string;
      } | null>;
    };
    agent: {
      __typename?: "Agent";
      id: any;
      name: string;
      representativeColor: string;
    };
  };
};

export type RouterTaskFieldsFragment = {
  __typename?: "RouterTask";
  name: string;
  description?: string | null;
  startTime: number;
  duration: number;
  isExecuting?: boolean | null;
  progress?: number | null;
  selectedBranchTargetNodeId?: any | null;
  selectedBranchName?: string | null;
  selectedAtUtc?: any | null;
  manuallySelectedBranch?: string | null;
  selector:
    | { __typename: "ExpressionSelector"; expression: string }
    | { __typename: "SimpleVariableSelector"; expression: string };
  branches: Array<{
    __typename?: "ConditionalBranch";
    name: string;
    condition?: string | null;
    priority: number;
    targetNodeId?: any | null;
  }>;
};

export type RouterNodeFieldsFragment = {
  __typename?: "RouterNode";
  id: any;
  parentId?: any | null;
  extent?: string | null;
  width?: number | null;
  height?: number | null;
  selectable?: boolean | null;
  selected?: boolean | null;
  draggable?: boolean | null;
  dragging?: boolean | null;
  hidden?: boolean | null;
  position: { __typename?: "NodePosition"; x: number; y: number };
  routerTask: {
    __typename?: "RouterTask";
    name: string;
    description?: string | null;
    startTime: number;
    duration: number;
    isExecuting?: boolean | null;
    progress?: number | null;
    selectedBranchTargetNodeId?: any | null;
    selectedBranchName?: string | null;
    selectedAtUtc?: any | null;
    manuallySelectedBranch?: string | null;
    selector:
      | { __typename: "ExpressionSelector"; expression: string }
      | { __typename: "SimpleVariableSelector"; expression: string };
    branches: Array<{
      __typename?: "ConditionalBranch";
      name: string;
      condition?: string | null;
      priority: number;
      targetNodeId?: any | null;
    }>;
  };
};

type NodeFields_RouterNode_Fragment = {
  __typename: "RouterNode";
  id: any;
  parentId?: any | null;
  extent?: string | null;
  width?: number | null;
  height?: number | null;
  selectable?: boolean | null;
  selected?: boolean | null;
  draggable?: boolean | null;
  dragging?: boolean | null;
  hidden?: boolean | null;
  position: { __typename?: "NodePosition"; x: number; y: number };
  routerTask: {
    __typename?: "RouterTask";
    name: string;
    description?: string | null;
    startTime: number;
    duration: number;
    isExecuting?: boolean | null;
    progress?: number | null;
    selectedBranchTargetNodeId?: any | null;
    selectedBranchName?: string | null;
    selectedAtUtc?: any | null;
    manuallySelectedBranch?: string | null;
    selector:
      | { __typename: "ExpressionSelector"; expression: string }
      | { __typename: "SimpleVariableSelector"; expression: string };
    branches: Array<{
      __typename?: "ConditionalBranch";
      name: string;
      condition?: string | null;
      priority: number;
      targetNodeId?: any | null;
    }>;
  };
};

type NodeFields_SkillExecutionNode_Fragment = {
  __typename: "SkillExecutionNode";
  id: any;
  parentId?: any | null;
  extent?: string | null;
  width?: number | null;
  height?: number | null;
  selectable?: boolean | null;
  selected?: boolean | null;
  draggable?: boolean | null;
  dragging?: boolean | null;
  hidden?: boolean | null;
  position: { __typename?: "NodePosition"; x: number; y: number };
  skillExecutionTask: {
    __typename?: "SkillExecutionTask";
    name: string;
    description?: string | null;
    startTime: number;
    duration: number;
    isExecuting?: boolean | null;
    progress?: number | null;
    skill: {
      __typename?: "Skill";
      id: any;
      name: string;
      description: string;
      properties: Array<{
        __typename?: "Property";
        name: string;
        direction: PropertyDirection;
        binding?: {
          __typename?: "VariableBinding";
          variableName: string;
          mode: BindingMode;
          transformExpression?: string | null;
        } | null;
        value:
          | {
              __typename: "BooleanValue";
              boolValue: boolean;
              type: { __typename?: "BooleanType"; typeName: string };
            }
          | {
              __typename: "NumberValue";
              numberValue: number;
              type: { __typename?: "NumberType"; typeName: string };
            }
          | {
              __typename: "PositionTagValue";
              positionTagValue: {
                __typename?: "PositionTag";
                id: any;
                tag: string;
                position: {
                  __typename?: "Position";
                  x: number;
                  y: number;
                  z: number;
                  alpha: number;
                  beta: number;
                  gamma: number;
                };
              };
              type: { __typename?: "PositionTagType"; typeName: string };
            }
          | {
              __typename: "PositionValue";
              positionValue: {
                __typename?: "Position";
                x: number;
                y: number;
                z: number;
                alpha: number;
                beta: number;
                gamma: number;
              };
              type: { __typename?: "PositionType"; typeName: string };
            }
          | {
              __typename: "SceneObjectValue";
              sceneObjectValue: {
                __typename?: "SceneObject";
                id: any;
                name: string;
                position: {
                  __typename?: "Position";
                  x: number;
                  y: number;
                  z: number;
                  alpha: number;
                  beta: number;
                  gamma: number;
                };
              };
              type: { __typename?: "SceneObjectType"; typeName: string };
            }
          | {
              __typename: "StringValue";
              stringValue: string;
              type: { __typename?: "StringType"; typeName: string };
            };
      }>;
      agents: Array<{
        __typename?: "Agent";
        id: any;
        name: string;
        representativeColor: string;
      } | null>;
    };
    agent: {
      __typename?: "Agent";
      id: any;
      name: string;
      representativeColor: string;
    };
  };
};

type NodeFields_TaskNode_Fragment = {
  __typename: "TaskNode";
  id: any;
  parentId?: any | null;
  extent?: string | null;
  width?: number | null;
  height?: number | null;
  selectable?: boolean | null;
  selected?: boolean | null;
  draggable?: boolean | null;
  dragging?: boolean | null;
  hidden?: boolean | null;
  position: { __typename?: "NodePosition"; x: number; y: number };
  task: {
    __typename?: "Task";
    name: string;
    description?: string | null;
    startTime: number;
    duration: number;
    isExecuting?: boolean | null;
    progress?: number | null;
  };
};

export type NodeFieldsFragment =
  | NodeFields_RouterNode_Fragment
  | NodeFields_SkillExecutionNode_Fragment
  | NodeFields_TaskNode_Fragment;

export type DependencyEdgeFieldsFragment = {
  __typename?: "DependencyEdge";
  id: any;
  sourceId: any;
  targetId: any;
  sourceHandle?: string | null;
  targetHandle?: string | null;
};

export type GetNodesQueryVariables = Exact<{
  where?: InputMaybe<NodeFilterInput>;
}>;

export type GetNodesQuery = {
  __typename?: "Query";
  nodes: Array<
    | {
        __typename: "RouterNode";
        id: any;
        parentId?: any | null;
        extent?: string | null;
        width?: number | null;
        height?: number | null;
        selectable?: boolean | null;
        selected?: boolean | null;
        draggable?: boolean | null;
        dragging?: boolean | null;
        hidden?: boolean | null;
        position: { __typename?: "NodePosition"; x: number; y: number };
        routerTask: {
          __typename?: "RouterTask";
          name: string;
          description?: string | null;
          startTime: number;
          duration: number;
          isExecuting?: boolean | null;
          progress?: number | null;
          selectedBranchTargetNodeId?: any | null;
          selectedBranchName?: string | null;
          selectedAtUtc?: any | null;
          manuallySelectedBranch?: string | null;
          selector:
            | { __typename: "ExpressionSelector"; expression: string }
            | { __typename: "SimpleVariableSelector"; expression: string };
          branches: Array<{
            __typename?: "ConditionalBranch";
            name: string;
            condition?: string | null;
            priority: number;
            targetNodeId?: any | null;
          }>;
        };
      }
    | {
        __typename: "SkillExecutionNode";
        id: any;
        parentId?: any | null;
        extent?: string | null;
        width?: number | null;
        height?: number | null;
        selectable?: boolean | null;
        selected?: boolean | null;
        draggable?: boolean | null;
        dragging?: boolean | null;
        hidden?: boolean | null;
        position: { __typename?: "NodePosition"; x: number; y: number };
        skillExecutionTask: {
          __typename?: "SkillExecutionTask";
          name: string;
          description?: string | null;
          startTime: number;
          duration: number;
          isExecuting?: boolean | null;
          progress?: number | null;
          skill: {
            __typename?: "Skill";
            id: any;
            name: string;
            description: string;
            properties: Array<{
              __typename?: "Property";
              name: string;
              direction: PropertyDirection;
              binding?: {
                __typename?: "VariableBinding";
                variableName: string;
                mode: BindingMode;
                transformExpression?: string | null;
              } | null;
              value:
                | {
                    __typename: "BooleanValue";
                    boolValue: boolean;
                    type: { __typename?: "BooleanType"; typeName: string };
                  }
                | {
                    __typename: "NumberValue";
                    numberValue: number;
                    type: { __typename?: "NumberType"; typeName: string };
                  }
                | {
                    __typename: "PositionTagValue";
                    positionTagValue: {
                      __typename?: "PositionTag";
                      id: any;
                      tag: string;
                      position: {
                        __typename?: "Position";
                        x: number;
                        y: number;
                        z: number;
                        alpha: number;
                        beta: number;
                        gamma: number;
                      };
                    };
                    type: { __typename?: "PositionTagType"; typeName: string };
                  }
                | {
                    __typename: "PositionValue";
                    positionValue: {
                      __typename?: "Position";
                      x: number;
                      y: number;
                      z: number;
                      alpha: number;
                      beta: number;
                      gamma: number;
                    };
                    type: { __typename?: "PositionType"; typeName: string };
                  }
                | {
                    __typename: "SceneObjectValue";
                    sceneObjectValue: {
                      __typename?: "SceneObject";
                      id: any;
                      name: string;
                      position: {
                        __typename?: "Position";
                        x: number;
                        y: number;
                        z: number;
                        alpha: number;
                        beta: number;
                        gamma: number;
                      };
                    };
                    type: { __typename?: "SceneObjectType"; typeName: string };
                  }
                | {
                    __typename: "StringValue";
                    stringValue: string;
                    type: { __typename?: "StringType"; typeName: string };
                  };
            }>;
            agents: Array<{
              __typename?: "Agent";
              id: any;
              name: string;
              representativeColor: string;
            } | null>;
          };
          agent: {
            __typename?: "Agent";
            id: any;
            name: string;
            representativeColor: string;
          };
        };
      }
    | {
        __typename: "TaskNode";
        id: any;
        parentId?: any | null;
        extent?: string | null;
        width?: number | null;
        height?: number | null;
        selectable?: boolean | null;
        selected?: boolean | null;
        draggable?: boolean | null;
        dragging?: boolean | null;
        hidden?: boolean | null;
        position: { __typename?: "NodePosition"; x: number; y: number };
        task: {
          __typename?: "Task";
          name: string;
          description?: string | null;
          startTime: number;
          duration: number;
          isExecuting?: boolean | null;
          progress?: number | null;
        };
      }
  >;
};

export type GetNodeByIdQueryVariables = Exact<{
  id: Scalars["UUID"]["input"];
}>;

export type GetNodeByIdQuery = {
  __typename?: "Query";
  nodeById?:
    | {
        __typename: "RouterNode";
        id: any;
        parentId?: any | null;
        extent?: string | null;
        width?: number | null;
        height?: number | null;
        selectable?: boolean | null;
        selected?: boolean | null;
        draggable?: boolean | null;
        dragging?: boolean | null;
        hidden?: boolean | null;
        position: { __typename?: "NodePosition"; x: number; y: number };
        routerTask: {
          __typename?: "RouterTask";
          name: string;
          description?: string | null;
          startTime: number;
          duration: number;
          isExecuting?: boolean | null;
          progress?: number | null;
          selectedBranchTargetNodeId?: any | null;
          selectedBranchName?: string | null;
          selectedAtUtc?: any | null;
          manuallySelectedBranch?: string | null;
          selector:
            | { __typename: "ExpressionSelector"; expression: string }
            | { __typename: "SimpleVariableSelector"; expression: string };
          branches: Array<{
            __typename?: "ConditionalBranch";
            name: string;
            condition?: string | null;
            priority: number;
            targetNodeId?: any | null;
          }>;
        };
      }
    | {
        __typename: "SkillExecutionNode";
        id: any;
        parentId?: any | null;
        extent?: string | null;
        width?: number | null;
        height?: number | null;
        selectable?: boolean | null;
        selected?: boolean | null;
        draggable?: boolean | null;
        dragging?: boolean | null;
        hidden?: boolean | null;
        position: { __typename?: "NodePosition"; x: number; y: number };
        skillExecutionTask: {
          __typename?: "SkillExecutionTask";
          name: string;
          description?: string | null;
          startTime: number;
          duration: number;
          isExecuting?: boolean | null;
          progress?: number | null;
          skill: {
            __typename?: "Skill";
            id: any;
            name: string;
            description: string;
            properties: Array<{
              __typename?: "Property";
              name: string;
              direction: PropertyDirection;
              binding?: {
                __typename?: "VariableBinding";
                variableName: string;
                mode: BindingMode;
                transformExpression?: string | null;
              } | null;
              value:
                | {
                    __typename: "BooleanValue";
                    boolValue: boolean;
                    type: { __typename?: "BooleanType"; typeName: string };
                  }
                | {
                    __typename: "NumberValue";
                    numberValue: number;
                    type: { __typename?: "NumberType"; typeName: string };
                  }
                | {
                    __typename: "PositionTagValue";
                    positionTagValue: {
                      __typename?: "PositionTag";
                      id: any;
                      tag: string;
                      position: {
                        __typename?: "Position";
                        x: number;
                        y: number;
                        z: number;
                        alpha: number;
                        beta: number;
                        gamma: number;
                      };
                    };
                    type: { __typename?: "PositionTagType"; typeName: string };
                  }
                | {
                    __typename: "PositionValue";
                    positionValue: {
                      __typename?: "Position";
                      x: number;
                      y: number;
                      z: number;
                      alpha: number;
                      beta: number;
                      gamma: number;
                    };
                    type: { __typename?: "PositionType"; typeName: string };
                  }
                | {
                    __typename: "SceneObjectValue";
                    sceneObjectValue: {
                      __typename?: "SceneObject";
                      id: any;
                      name: string;
                      position: {
                        __typename?: "Position";
                        x: number;
                        y: number;
                        z: number;
                        alpha: number;
                        beta: number;
                        gamma: number;
                      };
                    };
                    type: { __typename?: "SceneObjectType"; typeName: string };
                  }
                | {
                    __typename: "StringValue";
                    stringValue: string;
                    type: { __typename?: "StringType"; typeName: string };
                  };
            }>;
            agents: Array<{
              __typename?: "Agent";
              id: any;
              name: string;
              representativeColor: string;
            } | null>;
          };
          agent: {
            __typename?: "Agent";
            id: any;
            name: string;
            representativeColor: string;
          };
        };
      }
    | {
        __typename: "TaskNode";
        id: any;
        parentId?: any | null;
        extent?: string | null;
        width?: number | null;
        height?: number | null;
        selectable?: boolean | null;
        selected?: boolean | null;
        draggable?: boolean | null;
        dragging?: boolean | null;
        hidden?: boolean | null;
        position: { __typename?: "NodePosition"; x: number; y: number };
        task: {
          __typename?: "Task";
          name: string;
          description?: string | null;
          startTime: number;
          duration: number;
          isExecuting?: boolean | null;
          progress?: number | null;
        };
      }
    | null;
};

export type CreateNodeMutationVariables = Exact<{
  input: CreateNodeInput;
}>;

export type CreateNodeMutation = {
  __typename?: "Mutation";
  createNode: {
    __typename?: "CreateNodePayload";
    node?:
      | {
          __typename: "RouterNode";
          id: any;
          parentId?: any | null;
          extent?: string | null;
          width?: number | null;
          height?: number | null;
          selectable?: boolean | null;
          selected?: boolean | null;
          draggable?: boolean | null;
          dragging?: boolean | null;
          hidden?: boolean | null;
          position: { __typename?: "NodePosition"; x: number; y: number };
          routerTask: {
            __typename?: "RouterTask";
            name: string;
            description?: string | null;
            startTime: number;
            duration: number;
            isExecuting?: boolean | null;
            progress?: number | null;
            selectedBranchTargetNodeId?: any | null;
            selectedBranchName?: string | null;
            selectedAtUtc?: any | null;
            manuallySelectedBranch?: string | null;
            selector:
              | { __typename: "ExpressionSelector"; expression: string }
              | { __typename: "SimpleVariableSelector"; expression: string };
            branches: Array<{
              __typename?: "ConditionalBranch";
              name: string;
              condition?: string | null;
              priority: number;
              targetNodeId?: any | null;
            }>;
          };
        }
      | {
          __typename: "SkillExecutionNode";
          id: any;
          parentId?: any | null;
          extent?: string | null;
          width?: number | null;
          height?: number | null;
          selectable?: boolean | null;
          selected?: boolean | null;
          draggable?: boolean | null;
          dragging?: boolean | null;
          hidden?: boolean | null;
          position: { __typename?: "NodePosition"; x: number; y: number };
          skillExecutionTask: {
            __typename?: "SkillExecutionTask";
            name: string;
            description?: string | null;
            startTime: number;
            duration: number;
            isExecuting?: boolean | null;
            progress?: number | null;
            skill: {
              __typename?: "Skill";
              id: any;
              name: string;
              description: string;
              properties: Array<{
                __typename?: "Property";
                name: string;
                direction: PropertyDirection;
                binding?: {
                  __typename?: "VariableBinding";
                  variableName: string;
                  mode: BindingMode;
                  transformExpression?: string | null;
                } | null;
                value:
                  | {
                      __typename: "BooleanValue";
                      boolValue: boolean;
                      type: { __typename?: "BooleanType"; typeName: string };
                    }
                  | {
                      __typename: "NumberValue";
                      numberValue: number;
                      type: { __typename?: "NumberType"; typeName: string };
                    }
                  | {
                      __typename: "PositionTagValue";
                      positionTagValue: {
                        __typename?: "PositionTag";
                        id: any;
                        tag: string;
                        position: {
                          __typename?: "Position";
                          x: number;
                          y: number;
                          z: number;
                          alpha: number;
                          beta: number;
                          gamma: number;
                        };
                      };
                      type: {
                        __typename?: "PositionTagType";
                        typeName: string;
                      };
                    }
                  | {
                      __typename: "PositionValue";
                      positionValue: {
                        __typename?: "Position";
                        x: number;
                        y: number;
                        z: number;
                        alpha: number;
                        beta: number;
                        gamma: number;
                      };
                      type: { __typename?: "PositionType"; typeName: string };
                    }
                  | {
                      __typename: "SceneObjectValue";
                      sceneObjectValue: {
                        __typename?: "SceneObject";
                        id: any;
                        name: string;
                        position: {
                          __typename?: "Position";
                          x: number;
                          y: number;
                          z: number;
                          alpha: number;
                          beta: number;
                          gamma: number;
                        };
                      };
                      type: {
                        __typename?: "SceneObjectType";
                        typeName: string;
                      };
                    }
                  | {
                      __typename: "StringValue";
                      stringValue: string;
                      type: { __typename?: "StringType"; typeName: string };
                    };
              }>;
              agents: Array<{
                __typename?: "Agent";
                id: any;
                name: string;
                representativeColor: string;
              } | null>;
            };
            agent: {
              __typename?: "Agent";
              id: any;
              name: string;
              representativeColor: string;
            };
          };
        }
      | {
          __typename: "TaskNode";
          id: any;
          parentId?: any | null;
          extent?: string | null;
          width?: number | null;
          height?: number | null;
          selectable?: boolean | null;
          selected?: boolean | null;
          draggable?: boolean | null;
          dragging?: boolean | null;
          hidden?: boolean | null;
          position: { __typename?: "NodePosition"; x: number; y: number };
          task: {
            __typename?: "Task";
            name: string;
            description?: string | null;
            startTime: number;
            duration: number;
            isExecuting?: boolean | null;
            progress?: number | null;
          };
        }
      | null;
  };
};

export type UpdateNodeMutationVariables = Exact<{
  input: UpdateNodeInput;
}>;

export type UpdateNodeMutation = {
  __typename?: "Mutation";
  updateNode: { __typename?: "UpdateNodePayload"; boolean?: boolean | null };
};

export type DeleteNodeMutationVariables = Exact<{
  input: DeleteNodeInput;
}>;

export type DeleteNodeMutation = {
  __typename?: "Mutation";
  deleteNode: { __typename?: "DeleteNodePayload"; boolean?: boolean | null };
};

export type OnNodesChangedSubscriptionVariables = Exact<{
  [key: string]: never;
}>;

export type OnNodesChangedSubscription = {
  __typename?: "Subscription";
  nodesChanged: Array<
    | {
        __typename: "RouterNode";
        id: any;
        parentId?: any | null;
        extent?: string | null;
        width?: number | null;
        height?: number | null;
        selectable?: boolean | null;
        selected?: boolean | null;
        draggable?: boolean | null;
        dragging?: boolean | null;
        hidden?: boolean | null;
        position: { __typename?: "NodePosition"; x: number; y: number };
        routerTask: {
          __typename?: "RouterTask";
          name: string;
          description?: string | null;
          startTime: number;
          duration: number;
          isExecuting?: boolean | null;
          progress?: number | null;
          selectedBranchTargetNodeId?: any | null;
          selectedBranchName?: string | null;
          selectedAtUtc?: any | null;
          manuallySelectedBranch?: string | null;
          selector:
            | { __typename: "ExpressionSelector"; expression: string }
            | { __typename: "SimpleVariableSelector"; expression: string };
          branches: Array<{
            __typename?: "ConditionalBranch";
            name: string;
            condition?: string | null;
            priority: number;
            targetNodeId?: any | null;
          }>;
        };
      }
    | {
        __typename: "SkillExecutionNode";
        id: any;
        parentId?: any | null;
        extent?: string | null;
        width?: number | null;
        height?: number | null;
        selectable?: boolean | null;
        selected?: boolean | null;
        draggable?: boolean | null;
        dragging?: boolean | null;
        hidden?: boolean | null;
        position: { __typename?: "NodePosition"; x: number; y: number };
        skillExecutionTask: {
          __typename?: "SkillExecutionTask";
          name: string;
          description?: string | null;
          startTime: number;
          duration: number;
          isExecuting?: boolean | null;
          progress?: number | null;
          skill: {
            __typename?: "Skill";
            id: any;
            name: string;
            description: string;
            properties: Array<{
              __typename?: "Property";
              name: string;
              direction: PropertyDirection;
              binding?: {
                __typename?: "VariableBinding";
                variableName: string;
                mode: BindingMode;
                transformExpression?: string | null;
              } | null;
              value:
                | {
                    __typename: "BooleanValue";
                    boolValue: boolean;
                    type: { __typename?: "BooleanType"; typeName: string };
                  }
                | {
                    __typename: "NumberValue";
                    numberValue: number;
                    type: { __typename?: "NumberType"; typeName: string };
                  }
                | {
                    __typename: "PositionTagValue";
                    positionTagValue: {
                      __typename?: "PositionTag";
                      id: any;
                      tag: string;
                      position: {
                        __typename?: "Position";
                        x: number;
                        y: number;
                        z: number;
                        alpha: number;
                        beta: number;
                        gamma: number;
                      };
                    };
                    type: { __typename?: "PositionTagType"; typeName: string };
                  }
                | {
                    __typename: "PositionValue";
                    positionValue: {
                      __typename?: "Position";
                      x: number;
                      y: number;
                      z: number;
                      alpha: number;
                      beta: number;
                      gamma: number;
                    };
                    type: { __typename?: "PositionType"; typeName: string };
                  }
                | {
                    __typename: "SceneObjectValue";
                    sceneObjectValue: {
                      __typename?: "SceneObject";
                      id: any;
                      name: string;
                      position: {
                        __typename?: "Position";
                        x: number;
                        y: number;
                        z: number;
                        alpha: number;
                        beta: number;
                        gamma: number;
                      };
                    };
                    type: { __typename?: "SceneObjectType"; typeName: string };
                  }
                | {
                    __typename: "StringValue";
                    stringValue: string;
                    type: { __typename?: "StringType"; typeName: string };
                  };
            }>;
            agents: Array<{
              __typename?: "Agent";
              id: any;
              name: string;
              representativeColor: string;
            } | null>;
          };
          agent: {
            __typename?: "Agent";
            id: any;
            name: string;
            representativeColor: string;
          };
        };
      }
    | {
        __typename: "TaskNode";
        id: any;
        parentId?: any | null;
        extent?: string | null;
        width?: number | null;
        height?: number | null;
        selectable?: boolean | null;
        selected?: boolean | null;
        draggable?: boolean | null;
        dragging?: boolean | null;
        hidden?: boolean | null;
        position: { __typename?: "NodePosition"; x: number; y: number };
        task: {
          __typename?: "Task";
          name: string;
          description?: string | null;
          startTime: number;
          duration: number;
          isExecuting?: boolean | null;
          progress?: number | null;
        };
      }
  >;
};

export type StartLoadedProcedureMutationVariables = Exact<{
  [key: string]: never;
}>;

export type StartLoadedProcedureMutation = {
  __typename?: "Mutation";
  startLoadedProcedure: {
    __typename?: "StartLoadedProcedurePayload";
    boolean?: boolean | null;
  };
};

export type GetAllProceduresQueryVariables = Exact<{ [key: string]: never }>;

export type GetAllProceduresQuery = {
  __typename?: "Query";
  procedures: Array<{
    __typename?: "Procedure";
    id: any;
    name: string;
    description?: string | null;
    isLoaded: boolean;
    createdAtUtc: any;
    lastUpdatedAtUtc: any;
  }>;
};

export type GetLoadedProcedureQueryVariables = Exact<{ [key: string]: never }>;

export type GetLoadedProcedureQuery = {
  __typename?: "Query";
  loadedProcedure?: {
    __typename?: "Procedure";
    id: any;
    name: string;
    description?: string | null;
    isLoaded: boolean;
    lastLoadedUtc?: any | null;
    variables: Array<{
      __typename?: "VariableDefinition";
      name: string;
      defaultValue?: string | null;
      scope: VariableScope;
      source: VariableSource;
      description?: string | null;
      isReadOnly: boolean;
      type:
        | { __typename: "BooleanType"; typeName: string }
        | {
            __typename: "EnumType";
            typeName: string;
            allowedValues: Array<string>;
          }
        | {
            __typename: "ListType";
            typeName: string;
            elementType:
              | { __typename: "BooleanType"; typeName: string }
              | { __typename: "EnumType" }
              | { __typename: "ListType" }
              | { __typename: "NumberType"; typeName: string }
              | { __typename: "PositionTagType" }
              | { __typename: "PositionType" }
              | { __typename: "SceneObjectType" }
              | { __typename: "StringType"; typeName: string };
          }
        | { __typename: "NumberType"; typeName: string }
        | { __typename: "PositionTagType"; typeName: string }
        | { __typename: "PositionType"; typeName: string }
        | { __typename: "SceneObjectType"; typeName: string }
        | { __typename: "StringType"; typeName: string };
    }>;
  } | null;
};

export type LoadProcedureMutationVariables = Exact<{
  id: Scalars["UUID"]["input"];
}>;

export type LoadProcedureMutation = {
  __typename?: "Mutation";
  loadProcedure: {
    __typename?: "LoadProcedurePayload";
    procedure?: {
      __typename?: "Procedure";
      id: any;
      name: string;
      description?: string | null;
      isLoaded: boolean;
      lastLoadedUtc?: any | null;
    } | null;
  };
};

export type UnloadProcedureMutationVariables = Exact<{ [key: string]: never }>;

export type UnloadProcedureMutation = {
  __typename?: "Mutation";
  unloadProcedure: { __typename?: "UnloadProcedurePayload"; success: boolean };
};

export type CreateProcedureMutationVariables = Exact<{
  input: CreateProcedureInput;
}>;

export type CreateProcedureMutation = {
  __typename?: "Mutation";
  createProcedure: {
    __typename?: "CreateProcedurePayload";
    procedure?: {
      __typename?: "Procedure";
      id: any;
      name: string;
      description?: string | null;
      createdAtUtc: any;
    } | null;
  };
};

export type DeleteProcedureMutationVariables = Exact<{
  input: DeleteProcedureInput;
}>;

export type DeleteProcedureMutation = {
  __typename?: "Mutation";
  deleteProcedure: {
    __typename?: "DeleteProcedurePayload";
    boolean?: boolean | null;
  };
};

export type SceneObjectFieldsFragment = {
  __typename?: "SceneObject";
  id: any;
  name: string;
  position: {
    __typename?: "Position";
    x: number;
    y: number;
    z: number;
    alpha: number;
    beta: number;
    gamma: number;
  };
};

export type PositionTagFieldsFragment = {
  __typename?: "PositionTag";
  id: any;
  tag: string;
  position: {
    __typename?: "Position";
    x: number;
    y: number;
    z: number;
    alpha: number;
    beta: number;
    gamma: number;
  };
};

export type GetSceneObjectsQueryVariables = Exact<{ [key: string]: never }>;

export type GetSceneObjectsQuery = {
  __typename?: "Query";
  sceneObjects: Array<{
    __typename?: "SceneObject";
    id: any;
    name: string;
    position: {
      __typename?: "Position";
      x: number;
      y: number;
      z: number;
      alpha: number;
      beta: number;
      gamma: number;
    };
  }>;
};

export type GetPositionTagsQueryVariables = Exact<{ [key: string]: never }>;

export type GetPositionTagsQuery = {
  __typename?: "Query";
  positionTags: Array<{
    __typename?: "PositionTag";
    id: any;
    tag: string;
    position: {
      __typename?: "Position";
      x: number;
      y: number;
      z: number;
      alpha: number;
      beta: number;
      gamma: number;
    };
  }>;
};

export type GetSceneObjectByIdQueryVariables = Exact<{
  id: Scalars["UUID"]["input"];
}>;

export type GetSceneObjectByIdQuery = {
  __typename?: "Query";
  sceneObjectById?: {
    __typename?: "SceneObject";
    id: any;
    name: string;
    position: {
      __typename?: "Position";
      x: number;
      y: number;
      z: number;
      alpha: number;
      beta: number;
      gamma: number;
    };
  } | null;
};

export type GetPositionTagByIdQueryVariables = Exact<{
  id: Scalars["UUID"]["input"];
}>;

export type GetPositionTagByIdQuery = {
  __typename?: "Query";
  positionTagById?: {
    __typename?: "PositionTag";
    id: any;
    tag: string;
    position: {
      __typename?: "Position";
      x: number;
      y: number;
      z: number;
      alpha: number;
      beta: number;
      gamma: number;
    };
  } | null;
};

export type CreateSceneObjectMutationVariables = Exact<{
  input: CreateSceneObjectInput;
}>;

export type CreateSceneObjectMutation = {
  __typename?: "Mutation";
  createSceneObject: {
    __typename?: "CreateSceneObjectPayload";
    sceneObject?: {
      __typename?: "SceneObject";
      id: any;
      name: string;
      position: {
        __typename?: "Position";
        x: number;
        y: number;
        z: number;
        alpha: number;
        beta: number;
        gamma: number;
      };
    } | null;
  };
};

export type UpdateSceneObjectMutationVariables = Exact<{
  input: UpdateSceneObjectInput;
}>;

export type UpdateSceneObjectMutation = {
  __typename?: "Mutation";
  updateSceneObject: {
    __typename?: "UpdateSceneObjectPayload";
    boolean?: boolean | null;
  };
};

export type DeleteSceneObjectMutationVariables = Exact<{
  input: DeleteSceneObjectInput;
}>;

export type DeleteSceneObjectMutation = {
  __typename?: "Mutation";
  deleteSceneObject: {
    __typename?: "DeleteSceneObjectPayload";
    boolean?: boolean | null;
  };
};

export type CreatePositionTagMutationVariables = Exact<{
  input: CreatePositionTagInput;
}>;

export type CreatePositionTagMutation = {
  __typename?: "Mutation";
  createPositionTag: {
    __typename?: "CreatePositionTagPayload";
    positionTag?: {
      __typename?: "PositionTag";
      id: any;
      tag: string;
      position: {
        __typename?: "Position";
        x: number;
        y: number;
        z: number;
        alpha: number;
        beta: number;
        gamma: number;
      };
    } | null;
  };
};

export type UpdatePositionTagMutationVariables = Exact<{
  input: UpdatePositionTagInput;
}>;

export type UpdatePositionTagMutation = {
  __typename?: "Mutation";
  updatePositionTag: {
    __typename?: "UpdatePositionTagPayload";
    boolean?: boolean | null;
  };
};

export type DeletePositionTagMutationVariables = Exact<{
  input: DeletePositionTagInput;
}>;

export type DeletePositionTagMutation = {
  __typename?: "Mutation";
  deletePositionTag: {
    __typename?: "DeletePositionTagPayload";
    boolean?: boolean | null;
  };
};

export type OnProcedureValidationChangedSubscriptionVariables = Exact<{
  [key: string]: never;
}>;

export type OnProcedureValidationChangedSubscription = {
  __typename?: "Subscription";
  procedureValidationChanged: {
    __typename?: "ProcedureValidationResult";
    agentSerializationViolations: Array<{
      __typename?: "AgentSerializationViolation";
      agentId: any;
      agentName: string;
      unserializedSkills: Array<{
        __typename?: "UnserializedSkill";
        nodeId: any;
        skillName: string;
      }>;
      missingFsPairs: Array<{
        __typename?: "SkillPair";
        skillA: any;
        skillB: any;
      }>;
    }>;
  };
};

export const NodePositionFieldsFragmentDoc = {
  kind: "Document",
  definitions: [
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "NodePositionFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "NodePosition" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "x" } },
          { kind: "Field", name: { kind: "Name", value: "y" } },
        ],
      },
    },
  ],
} as unknown as DocumentNode<NodePositionFieldsFragment, unknown>;
export const TaskFieldsFragmentDoc = {
  kind: "Document",
  definitions: [
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "TaskFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Task" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          { kind: "Field", name: { kind: "Name", value: "startTime" } },
          { kind: "Field", name: { kind: "Name", value: "duration" } },
          { kind: "Field", name: { kind: "Name", value: "isExecuting" } },
          { kind: "Field", name: { kind: "Name", value: "progress" } },
        ],
      },
    },
  ],
} as unknown as DocumentNode<TaskFieldsFragment, unknown>;
export const TaskNodeFieldsFragmentDoc = {
  kind: "Document",
  definitions: [
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "TaskNodeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "TaskNode" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "position" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "NodePositionFields" },
                },
              ],
            },
          },
          { kind: "Field", name: { kind: "Name", value: "parentId" } },
          { kind: "Field", name: { kind: "Name", value: "extent" } },
          { kind: "Field", name: { kind: "Name", value: "width" } },
          { kind: "Field", name: { kind: "Name", value: "height" } },
          { kind: "Field", name: { kind: "Name", value: "selectable" } },
          { kind: "Field", name: { kind: "Name", value: "selected" } },
          { kind: "Field", name: { kind: "Name", value: "draggable" } },
          { kind: "Field", name: { kind: "Name", value: "dragging" } },
          { kind: "Field", name: { kind: "Name", value: "hidden" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "task" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "TaskFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "NodePositionFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "NodePosition" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "x" } },
          { kind: "Field", name: { kind: "Name", value: "y" } },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "TaskFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Task" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          { kind: "Field", name: { kind: "Name", value: "startTime" } },
          { kind: "Field", name: { kind: "Name", value: "duration" } },
          { kind: "Field", name: { kind: "Name", value: "isExecuting" } },
          { kind: "Field", name: { kind: "Name", value: "progress" } },
        ],
      },
    },
  ],
} as unknown as DocumentNode<TaskNodeFieldsFragment, unknown>;
export const PropertyFieldsFragmentDoc = {
  kind: "Document",
  definitions: [
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "PropertyFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Property" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "direction" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "binding" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "variableName" },
                },
                { kind: "Field", name: { kind: "Name", value: "mode" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "transformExpression" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "value" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "__typename" } },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "BooleanValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "boolValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "NumberValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "numberValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "StringValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "stringValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "x" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "y" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "z" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "alpha" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "beta" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "gamma" },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionTagValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionTagValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "tag" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "SceneObjectValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "sceneObjectValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "name" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<PropertyFieldsFragment, unknown>;
export const AgentFieldsFragmentDoc = {
  kind: "Document",
  definitions: [
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "AgentFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Agent" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "representativeColor" },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<AgentFieldsFragment, unknown>;
export const SkillFieldsFragmentDoc = {
  kind: "Document",
  definitions: [
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Skill" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "properties" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "PropertyFields" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "agents" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "AgentFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "PropertyFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Property" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "direction" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "binding" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "variableName" },
                },
                { kind: "Field", name: { kind: "Name", value: "mode" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "transformExpression" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "value" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "__typename" } },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "BooleanValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "boolValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "NumberValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "numberValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "StringValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "stringValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "x" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "y" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "z" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "alpha" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "beta" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "gamma" },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionTagValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionTagValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "tag" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "SceneObjectValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "sceneObjectValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "name" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "AgentFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Agent" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "representativeColor" },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<SkillFieldsFragment, unknown>;
export const SkillExecutionTaskFieldsFragmentDoc = {
  kind: "Document",
  definitions: [
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillExecutionTaskFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "SkillExecutionTask" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          { kind: "Field", name: { kind: "Name", value: "startTime" } },
          { kind: "Field", name: { kind: "Name", value: "duration" } },
          { kind: "Field", name: { kind: "Name", value: "isExecuting" } },
          { kind: "Field", name: { kind: "Name", value: "progress" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "skill" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "SkillFields" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "agent" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "AgentFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "PropertyFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Property" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "direction" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "binding" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "variableName" },
                },
                { kind: "Field", name: { kind: "Name", value: "mode" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "transformExpression" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "value" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "__typename" } },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "BooleanValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "boolValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "NumberValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "numberValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "StringValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "stringValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "x" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "y" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "z" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "alpha" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "beta" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "gamma" },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionTagValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionTagValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "tag" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "SceneObjectValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "sceneObjectValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "name" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "AgentFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Agent" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "representativeColor" },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Skill" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "properties" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "PropertyFields" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "agents" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "AgentFields" },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<SkillExecutionTaskFieldsFragment, unknown>;
export const SkillExecutionNodeFieldsFragmentDoc = {
  kind: "Document",
  definitions: [
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillExecutionNodeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "SkillExecutionNode" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "position" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "NodePositionFields" },
                },
              ],
            },
          },
          { kind: "Field", name: { kind: "Name", value: "parentId" } },
          { kind: "Field", name: { kind: "Name", value: "extent" } },
          { kind: "Field", name: { kind: "Name", value: "width" } },
          { kind: "Field", name: { kind: "Name", value: "height" } },
          { kind: "Field", name: { kind: "Name", value: "selectable" } },
          { kind: "Field", name: { kind: "Name", value: "selected" } },
          { kind: "Field", name: { kind: "Name", value: "draggable" } },
          { kind: "Field", name: { kind: "Name", value: "dragging" } },
          { kind: "Field", name: { kind: "Name", value: "hidden" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "skillExecutionTask" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "SkillExecutionTaskFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "PropertyFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Property" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "direction" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "binding" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "variableName" },
                },
                { kind: "Field", name: { kind: "Name", value: "mode" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "transformExpression" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "value" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "__typename" } },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "BooleanValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "boolValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "NumberValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "numberValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "StringValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "stringValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "x" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "y" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "z" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "alpha" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "beta" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "gamma" },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionTagValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionTagValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "tag" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "SceneObjectValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "sceneObjectValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "name" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "AgentFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Agent" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "representativeColor" },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Skill" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "properties" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "PropertyFields" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "agents" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "AgentFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "NodePositionFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "NodePosition" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "x" } },
          { kind: "Field", name: { kind: "Name", value: "y" } },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillExecutionTaskFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "SkillExecutionTask" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          { kind: "Field", name: { kind: "Name", value: "startTime" } },
          { kind: "Field", name: { kind: "Name", value: "duration" } },
          { kind: "Field", name: { kind: "Name", value: "isExecuting" } },
          { kind: "Field", name: { kind: "Name", value: "progress" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "skill" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "SkillFields" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "agent" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "AgentFields" },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<SkillExecutionNodeFieldsFragment, unknown>;
export const RouterTaskFieldsFragmentDoc = {
  kind: "Document",
  definitions: [
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "RouterTaskFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "RouterTask" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          { kind: "Field", name: { kind: "Name", value: "startTime" } },
          { kind: "Field", name: { kind: "Name", value: "duration" } },
          { kind: "Field", name: { kind: "Name", value: "isExecuting" } },
          { kind: "Field", name: { kind: "Name", value: "progress" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "selector" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "__typename" } },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "SimpleVariableSelector" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "expression" },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "ExpressionSelector" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "expression" },
                      },
                    ],
                  },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "branches" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "name" } },
                { kind: "Field", name: { kind: "Name", value: "condition" } },
                { kind: "Field", name: { kind: "Name", value: "priority" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "targetNodeId" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "selectedBranchTargetNodeId" },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "selectedBranchName" },
          },
          { kind: "Field", name: { kind: "Name", value: "selectedAtUtc" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "manuallySelectedBranch" },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<RouterTaskFieldsFragment, unknown>;
export const RouterNodeFieldsFragmentDoc = {
  kind: "Document",
  definitions: [
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "RouterNodeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "RouterNode" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "position" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "NodePositionFields" },
                },
              ],
            },
          },
          { kind: "Field", name: { kind: "Name", value: "parentId" } },
          { kind: "Field", name: { kind: "Name", value: "extent" } },
          { kind: "Field", name: { kind: "Name", value: "width" } },
          { kind: "Field", name: { kind: "Name", value: "height" } },
          { kind: "Field", name: { kind: "Name", value: "selectable" } },
          { kind: "Field", name: { kind: "Name", value: "selected" } },
          { kind: "Field", name: { kind: "Name", value: "draggable" } },
          { kind: "Field", name: { kind: "Name", value: "dragging" } },
          { kind: "Field", name: { kind: "Name", value: "hidden" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "routerTask" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "RouterTaskFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "NodePositionFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "NodePosition" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "x" } },
          { kind: "Field", name: { kind: "Name", value: "y" } },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "RouterTaskFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "RouterTask" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          { kind: "Field", name: { kind: "Name", value: "startTime" } },
          { kind: "Field", name: { kind: "Name", value: "duration" } },
          { kind: "Field", name: { kind: "Name", value: "isExecuting" } },
          { kind: "Field", name: { kind: "Name", value: "progress" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "selector" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "__typename" } },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "SimpleVariableSelector" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "expression" },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "ExpressionSelector" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "expression" },
                      },
                    ],
                  },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "branches" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "name" } },
                { kind: "Field", name: { kind: "Name", value: "condition" } },
                { kind: "Field", name: { kind: "Name", value: "priority" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "targetNodeId" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "selectedBranchTargetNodeId" },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "selectedBranchName" },
          },
          { kind: "Field", name: { kind: "Name", value: "selectedAtUtc" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "manuallySelectedBranch" },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<RouterNodeFieldsFragment, unknown>;
export const NodeFieldsFragmentDoc = {
  kind: "Document",
  definitions: [
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "NodeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Node" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "__typename" } },
          {
            kind: "InlineFragment",
            typeCondition: {
              kind: "NamedType",
              name: { kind: "Name", value: "TaskNode" },
            },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "TaskNodeFields" },
                },
              ],
            },
          },
          {
            kind: "InlineFragment",
            typeCondition: {
              kind: "NamedType",
              name: { kind: "Name", value: "SkillExecutionNode" },
            },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "SkillExecutionNodeFields" },
                },
              ],
            },
          },
          {
            kind: "InlineFragment",
            typeCondition: {
              kind: "NamedType",
              name: { kind: "Name", value: "RouterNode" },
            },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "RouterNodeFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "NodePositionFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "NodePosition" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "x" } },
          { kind: "Field", name: { kind: "Name", value: "y" } },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "TaskFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Task" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          { kind: "Field", name: { kind: "Name", value: "startTime" } },
          { kind: "Field", name: { kind: "Name", value: "duration" } },
          { kind: "Field", name: { kind: "Name", value: "isExecuting" } },
          { kind: "Field", name: { kind: "Name", value: "progress" } },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "PropertyFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Property" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "direction" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "binding" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "variableName" },
                },
                { kind: "Field", name: { kind: "Name", value: "mode" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "transformExpression" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "value" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "__typename" } },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "BooleanValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "boolValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "NumberValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "numberValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "StringValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "stringValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "x" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "y" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "z" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "alpha" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "beta" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "gamma" },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionTagValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionTagValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "tag" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "SceneObjectValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "sceneObjectValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "name" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "AgentFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Agent" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "representativeColor" },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Skill" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "properties" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "PropertyFields" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "agents" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "AgentFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillExecutionTaskFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "SkillExecutionTask" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          { kind: "Field", name: { kind: "Name", value: "startTime" } },
          { kind: "Field", name: { kind: "Name", value: "duration" } },
          { kind: "Field", name: { kind: "Name", value: "isExecuting" } },
          { kind: "Field", name: { kind: "Name", value: "progress" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "skill" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "SkillFields" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "agent" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "AgentFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "RouterTaskFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "RouterTask" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          { kind: "Field", name: { kind: "Name", value: "startTime" } },
          { kind: "Field", name: { kind: "Name", value: "duration" } },
          { kind: "Field", name: { kind: "Name", value: "isExecuting" } },
          { kind: "Field", name: { kind: "Name", value: "progress" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "selector" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "__typename" } },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "SimpleVariableSelector" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "expression" },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "ExpressionSelector" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "expression" },
                      },
                    ],
                  },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "branches" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "name" } },
                { kind: "Field", name: { kind: "Name", value: "condition" } },
                { kind: "Field", name: { kind: "Name", value: "priority" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "targetNodeId" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "selectedBranchTargetNodeId" },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "selectedBranchName" },
          },
          { kind: "Field", name: { kind: "Name", value: "selectedAtUtc" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "manuallySelectedBranch" },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "TaskNodeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "TaskNode" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "position" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "NodePositionFields" },
                },
              ],
            },
          },
          { kind: "Field", name: { kind: "Name", value: "parentId" } },
          { kind: "Field", name: { kind: "Name", value: "extent" } },
          { kind: "Field", name: { kind: "Name", value: "width" } },
          { kind: "Field", name: { kind: "Name", value: "height" } },
          { kind: "Field", name: { kind: "Name", value: "selectable" } },
          { kind: "Field", name: { kind: "Name", value: "selected" } },
          { kind: "Field", name: { kind: "Name", value: "draggable" } },
          { kind: "Field", name: { kind: "Name", value: "dragging" } },
          { kind: "Field", name: { kind: "Name", value: "hidden" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "task" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "TaskFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillExecutionNodeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "SkillExecutionNode" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "position" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "NodePositionFields" },
                },
              ],
            },
          },
          { kind: "Field", name: { kind: "Name", value: "parentId" } },
          { kind: "Field", name: { kind: "Name", value: "extent" } },
          { kind: "Field", name: { kind: "Name", value: "width" } },
          { kind: "Field", name: { kind: "Name", value: "height" } },
          { kind: "Field", name: { kind: "Name", value: "selectable" } },
          { kind: "Field", name: { kind: "Name", value: "selected" } },
          { kind: "Field", name: { kind: "Name", value: "draggable" } },
          { kind: "Field", name: { kind: "Name", value: "dragging" } },
          { kind: "Field", name: { kind: "Name", value: "hidden" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "skillExecutionTask" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "SkillExecutionTaskFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "RouterNodeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "RouterNode" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "position" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "NodePositionFields" },
                },
              ],
            },
          },
          { kind: "Field", name: { kind: "Name", value: "parentId" } },
          { kind: "Field", name: { kind: "Name", value: "extent" } },
          { kind: "Field", name: { kind: "Name", value: "width" } },
          { kind: "Field", name: { kind: "Name", value: "height" } },
          { kind: "Field", name: { kind: "Name", value: "selectable" } },
          { kind: "Field", name: { kind: "Name", value: "selected" } },
          { kind: "Field", name: { kind: "Name", value: "draggable" } },
          { kind: "Field", name: { kind: "Name", value: "dragging" } },
          { kind: "Field", name: { kind: "Name", value: "hidden" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "routerTask" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "RouterTaskFields" },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<NodeFieldsFragment, unknown>;
export const DependencyEdgeFieldsFragmentDoc = {
  kind: "Document",
  definitions: [
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "DependencyEdgeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "DependencyEdge" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "sourceId" } },
          { kind: "Field", name: { kind: "Name", value: "targetId" } },
          { kind: "Field", name: { kind: "Name", value: "sourceHandle" } },
          { kind: "Field", name: { kind: "Name", value: "targetHandle" } },
        ],
      },
    },
  ],
} as unknown as DocumentNode<DependencyEdgeFieldsFragment, unknown>;
export const SceneObjectFieldsFragmentDoc = {
  kind: "Document",
  definitions: [
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SceneObjectFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "SceneObject" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "position" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "x" } },
                { kind: "Field", name: { kind: "Name", value: "y" } },
                { kind: "Field", name: { kind: "Name", value: "z" } },
                { kind: "Field", name: { kind: "Name", value: "alpha" } },
                { kind: "Field", name: { kind: "Name", value: "beta" } },
                { kind: "Field", name: { kind: "Name", value: "gamma" } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<SceneObjectFieldsFragment, unknown>;
export const PositionTagFieldsFragmentDoc = {
  kind: "Document",
  definitions: [
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "PositionTagFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "PositionTag" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "tag" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "position" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "x" } },
                { kind: "Field", name: { kind: "Name", value: "y" } },
                { kind: "Field", name: { kind: "Name", value: "z" } },
                { kind: "Field", name: { kind: "Name", value: "alpha" } },
                { kind: "Field", name: { kind: "Name", value: "beta" } },
                { kind: "Field", name: { kind: "Name", value: "gamma" } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<PositionTagFieldsFragment, unknown>;
export const GetAgentsDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "query",
      name: { kind: "Name", value: "GetAgents" },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "agents" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "AgentFields" },
                },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "skills" },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "FragmentSpread",
                        name: { kind: "Name", value: "SkillFields" },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "PropertyFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Property" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "direction" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "binding" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "variableName" },
                },
                { kind: "Field", name: { kind: "Name", value: "mode" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "transformExpression" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "value" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "__typename" } },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "BooleanValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "boolValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "NumberValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "numberValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "StringValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "stringValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "x" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "y" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "z" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "alpha" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "beta" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "gamma" },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionTagValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionTagValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "tag" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "SceneObjectValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "sceneObjectValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "name" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "AgentFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Agent" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "representativeColor" },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Skill" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "properties" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "PropertyFields" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "agents" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "AgentFields" },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<GetAgentsQuery, GetAgentsQueryVariables>;
export const GetAgentByIdDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "query",
      name: { kind: "Name", value: "GetAgentById" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: { kind: "Variable", name: { kind: "Name", value: "id" } },
          type: {
            kind: "NonNullType",
            type: { kind: "NamedType", name: { kind: "Name", value: "UUID" } },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "agentById" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "id" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "id" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "AgentFields" },
                },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "skills" },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "FragmentSpread",
                        name: { kind: "Name", value: "SkillFields" },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "PropertyFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Property" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "direction" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "binding" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "variableName" },
                },
                { kind: "Field", name: { kind: "Name", value: "mode" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "transformExpression" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "value" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "__typename" } },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "BooleanValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "boolValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "NumberValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "numberValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "StringValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "stringValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "x" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "y" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "z" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "alpha" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "beta" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "gamma" },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionTagValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionTagValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "tag" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "SceneObjectValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "sceneObjectValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "name" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "AgentFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Agent" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "representativeColor" },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Skill" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "properties" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "PropertyFields" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "agents" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "AgentFields" },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<GetAgentByIdQuery, GetAgentByIdQueryVariables>;
export const GetSkillsDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "query",
      name: { kind: "Name", value: "GetSkills" },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "skills" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "SkillFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "PropertyFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Property" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "direction" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "binding" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "variableName" },
                },
                { kind: "Field", name: { kind: "Name", value: "mode" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "transformExpression" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "value" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "__typename" } },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "BooleanValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "boolValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "NumberValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "numberValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "StringValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "stringValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "x" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "y" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "z" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "alpha" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "beta" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "gamma" },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionTagValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionTagValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "tag" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "SceneObjectValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "sceneObjectValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "name" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "AgentFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Agent" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "representativeColor" },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Skill" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "properties" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "PropertyFields" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "agents" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "AgentFields" },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<GetSkillsQuery, GetSkillsQueryVariables>;
export const GetSkillByIdDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "query",
      name: { kind: "Name", value: "GetSkillById" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: {
            kind: "Variable",
            name: { kind: "Name", value: "skillId" },
          },
          type: {
            kind: "NonNullType",
            type: { kind: "NamedType", name: { kind: "Name", value: "UUID" } },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "skillById" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "id" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "skillId" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "SkillFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "PropertyFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Property" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "direction" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "binding" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "variableName" },
                },
                { kind: "Field", name: { kind: "Name", value: "mode" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "transformExpression" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "value" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "__typename" } },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "BooleanValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "boolValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "NumberValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "numberValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "StringValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "stringValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "x" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "y" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "z" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "alpha" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "beta" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "gamma" },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionTagValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionTagValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "tag" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "SceneObjectValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "sceneObjectValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "name" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "AgentFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Agent" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "representativeColor" },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Skill" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "properties" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "PropertyFields" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "agents" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "AgentFields" },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<GetSkillByIdQuery, GetSkillByIdQueryVariables>;
export const GetSkillsByAgentIdDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "query",
      name: { kind: "Name", value: "GetSkillsByAgentId" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: {
            kind: "Variable",
            name: { kind: "Name", value: "agentId" },
          },
          type: {
            kind: "NonNullType",
            type: { kind: "NamedType", name: { kind: "Name", value: "UUID" } },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "agentById" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "id" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "agentId" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "AgentFields" },
                },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "skills" },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "FragmentSpread",
                        name: { kind: "Name", value: "SkillFields" },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "PropertyFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Property" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "direction" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "binding" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "variableName" },
                },
                { kind: "Field", name: { kind: "Name", value: "mode" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "transformExpression" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "value" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "__typename" } },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "BooleanValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "boolValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "NumberValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "numberValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "StringValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "stringValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "x" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "y" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "z" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "alpha" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "beta" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "gamma" },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionTagValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionTagValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "tag" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "SceneObjectValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "sceneObjectValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "name" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "AgentFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Agent" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "representativeColor" },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Skill" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "properties" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "PropertyFields" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "agents" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "AgentFields" },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  GetSkillsByAgentIdQuery,
  GetSkillsByAgentIdQueryVariables
>;
export const GetAgentsBySkillIdDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "query",
      name: { kind: "Name", value: "GetAgentsBySkillId" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: {
            kind: "Variable",
            name: { kind: "Name", value: "skillId" },
          },
          type: {
            kind: "NonNullType",
            type: { kind: "NamedType", name: { kind: "Name", value: "UUID" } },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "skillById" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "id" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "skillId" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "SkillFields" },
                },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "agents" },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "FragmentSpread",
                        name: { kind: "Name", value: "AgentFields" },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "PropertyFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Property" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "direction" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "binding" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "variableName" },
                },
                { kind: "Field", name: { kind: "Name", value: "mode" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "transformExpression" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "value" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "__typename" } },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "BooleanValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "boolValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "NumberValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "numberValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "StringValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "stringValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "x" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "y" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "z" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "alpha" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "beta" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "gamma" },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionTagValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionTagValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "tag" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "SceneObjectValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "sceneObjectValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "name" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "AgentFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Agent" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "representativeColor" },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Skill" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "properties" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "PropertyFields" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "agents" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "AgentFields" },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  GetAgentsBySkillIdQuery,
  GetAgentsBySkillIdQueryVariables
>;
export const CreateAgentDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "mutation",
      name: { kind: "Name", value: "CreateAgent" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: {
            kind: "Variable",
            name: { kind: "Name", value: "input" },
          },
          type: {
            kind: "NonNullType",
            type: {
              kind: "NamedType",
              name: { kind: "Name", value: "CreateAgentInput" },
            },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "createAgent" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "input" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "input" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "agent" },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "FragmentSpread",
                        name: { kind: "Name", value: "AgentFields" },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "AgentFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Agent" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "representativeColor" },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<CreateAgentMutation, CreateAgentMutationVariables>;
export const UpdateAgentDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "mutation",
      name: { kind: "Name", value: "UpdateAgent" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: {
            kind: "Variable",
            name: { kind: "Name", value: "input" },
          },
          type: {
            kind: "NonNullType",
            type: {
              kind: "NamedType",
              name: { kind: "Name", value: "UpdateAgentInput" },
            },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "updateAgent" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "input" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "input" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "agent" },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "FragmentSpread",
                        name: { kind: "Name", value: "AgentFields" },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "AgentFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Agent" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "representativeColor" },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<UpdateAgentMutation, UpdateAgentMutationVariables>;
export const DeleteAgentDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "mutation",
      name: { kind: "Name", value: "DeleteAgent" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: {
            kind: "Variable",
            name: { kind: "Name", value: "input" },
          },
          type: {
            kind: "NonNullType",
            type: {
              kind: "NamedType",
              name: { kind: "Name", value: "DeleteAgentInput" },
            },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "deleteAgent" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "input" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "input" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "boolean" } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<DeleteAgentMutation, DeleteAgentMutationVariables>;
export const CreateSkillDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "mutation",
      name: { kind: "Name", value: "CreateSkill" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: {
            kind: "Variable",
            name: { kind: "Name", value: "input" },
          },
          type: {
            kind: "NonNullType",
            type: {
              kind: "NamedType",
              name: { kind: "Name", value: "CreateSkillInput" },
            },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "createSkill" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "input" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "input" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "skill" },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "FragmentSpread",
                        name: { kind: "Name", value: "SkillFields" },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "PropertyFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Property" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "direction" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "binding" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "variableName" },
                },
                { kind: "Field", name: { kind: "Name", value: "mode" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "transformExpression" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "value" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "__typename" } },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "BooleanValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "boolValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "NumberValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "numberValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "StringValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "stringValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "x" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "y" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "z" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "alpha" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "beta" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "gamma" },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionTagValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionTagValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "tag" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "SceneObjectValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "sceneObjectValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "name" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "AgentFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Agent" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "representativeColor" },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Skill" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "properties" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "PropertyFields" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "agents" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "AgentFields" },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<CreateSkillMutation, CreateSkillMutationVariables>;
export const UpdateSkillDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "mutation",
      name: { kind: "Name", value: "UpdateSkill" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: {
            kind: "Variable",
            name: { kind: "Name", value: "input" },
          },
          type: {
            kind: "NonNullType",
            type: {
              kind: "NamedType",
              name: { kind: "Name", value: "UpdateSkillInput" },
            },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "updateSkill" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "input" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "input" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "skill" },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "FragmentSpread",
                        name: { kind: "Name", value: "SkillFields" },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "PropertyFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Property" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "direction" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "binding" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "variableName" },
                },
                { kind: "Field", name: { kind: "Name", value: "mode" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "transformExpression" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "value" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "__typename" } },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "BooleanValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "boolValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "NumberValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "numberValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "StringValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "stringValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "x" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "y" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "z" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "alpha" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "beta" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "gamma" },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionTagValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionTagValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "tag" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "SceneObjectValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "sceneObjectValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "name" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "AgentFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Agent" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "representativeColor" },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Skill" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "properties" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "PropertyFields" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "agents" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "AgentFields" },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<UpdateSkillMutation, UpdateSkillMutationVariables>;
export const DeleteSkillDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "mutation",
      name: { kind: "Name", value: "DeleteSkill" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: {
            kind: "Variable",
            name: { kind: "Name", value: "input" },
          },
          type: {
            kind: "NonNullType",
            type: {
              kind: "NamedType",
              name: { kind: "Name", value: "DeleteSkillInput" },
            },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "deleteSkill" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "input" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "input" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "boolean" } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<DeleteSkillMutation, DeleteSkillMutationVariables>;
export const GetSchedulingConfigurationDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "query",
      name: { kind: "Name", value: "GetSchedulingConfiguration" },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "schedulingConfiguration" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "timeToPixelScale" },
                },
                { kind: "Field", name: { kind: "Name", value: "baseYOffset" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "siblingSpacing" },
                },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "containerTopPadding" },
                },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "containerBottomPadding" },
                },
                { kind: "Field", name: { kind: "Name", value: "baseHeight" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "routerDropdownHeight" },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  GetSchedulingConfigurationQuery,
  GetSchedulingConfigurationQueryVariables
>;
export const GetDependencyEdgesDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "query",
      name: { kind: "Name", value: "GetDependencyEdges" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: {
            kind: "Variable",
            name: { kind: "Name", value: "where" },
          },
          type: {
            kind: "NamedType",
            name: { kind: "Name", value: "DependencyEdgeFilterInput" },
          },
        },
        {
          kind: "VariableDefinition",
          variable: {
            kind: "Variable",
            name: { kind: "Name", value: "order" },
          },
          type: {
            kind: "ListType",
            type: {
              kind: "NonNullType",
              type: {
                kind: "NamedType",
                name: { kind: "Name", value: "DependencyEdgeSortInput" },
              },
            },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "dependencyEdges" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "where" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "where" },
                },
              },
              {
                kind: "Argument",
                name: { kind: "Name", value: "order" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "order" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "DependencyEdgeFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "DependencyEdgeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "DependencyEdge" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "sourceId" } },
          { kind: "Field", name: { kind: "Name", value: "targetId" } },
          { kind: "Field", name: { kind: "Name", value: "sourceHandle" } },
          { kind: "Field", name: { kind: "Name", value: "targetHandle" } },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  GetDependencyEdgesQuery,
  GetDependencyEdgesQueryVariables
>;
export const GetDependencyEdgeByIdDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "query",
      name: { kind: "Name", value: "GetDependencyEdgeById" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: { kind: "Variable", name: { kind: "Name", value: "id" } },
          type: {
            kind: "NonNullType",
            type: { kind: "NamedType", name: { kind: "Name", value: "UUID" } },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "dependencyEdgeById" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "id" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "id" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "DependencyEdgeFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "DependencyEdgeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "DependencyEdge" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "sourceId" } },
          { kind: "Field", name: { kind: "Name", value: "targetId" } },
          { kind: "Field", name: { kind: "Name", value: "sourceHandle" } },
          { kind: "Field", name: { kind: "Name", value: "targetHandle" } },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  GetDependencyEdgeByIdQuery,
  GetDependencyEdgeByIdQueryVariables
>;
export const CreateDependencyEdgeDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "mutation",
      name: { kind: "Name", value: "CreateDependencyEdge" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: {
            kind: "Variable",
            name: { kind: "Name", value: "input" },
          },
          type: {
            kind: "NonNullType",
            type: {
              kind: "NamedType",
              name: { kind: "Name", value: "CreateDependencyEdgeInput" },
            },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "createDependencyEdge" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "input" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "input" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "dependencyEdge" },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "FragmentSpread",
                        name: { kind: "Name", value: "DependencyEdgeFields" },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "DependencyEdgeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "DependencyEdge" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "sourceId" } },
          { kind: "Field", name: { kind: "Name", value: "targetId" } },
          { kind: "Field", name: { kind: "Name", value: "sourceHandle" } },
          { kind: "Field", name: { kind: "Name", value: "targetHandle" } },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  CreateDependencyEdgeMutation,
  CreateDependencyEdgeMutationVariables
>;
export const UpdateDependencyEdgeDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "mutation",
      name: { kind: "Name", value: "UpdateDependencyEdge" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: {
            kind: "Variable",
            name: { kind: "Name", value: "input" },
          },
          type: {
            kind: "NonNullType",
            type: {
              kind: "NamedType",
              name: { kind: "Name", value: "UpdateDependencyEdgeInput" },
            },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "updateDependencyEdge" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "input" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "input" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "boolean" } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  UpdateDependencyEdgeMutation,
  UpdateDependencyEdgeMutationVariables
>;
export const DeleteDependencyEdgeDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "mutation",
      name: { kind: "Name", value: "DeleteDependencyEdge" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: {
            kind: "Variable",
            name: { kind: "Name", value: "input" },
          },
          type: {
            kind: "NonNullType",
            type: {
              kind: "NamedType",
              name: { kind: "Name", value: "DeleteDependencyEdgeInput" },
            },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "deleteDependencyEdge" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "input" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "input" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "boolean" } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  DeleteDependencyEdgeMutation,
  DeleteDependencyEdgeMutationVariables
>;
export const OnDependencyEdgesChangedDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "subscription",
      name: { kind: "Name", value: "OnDependencyEdgesChanged" },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "dependencyEdgesChanged" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "DependencyEdgeFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "DependencyEdgeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "DependencyEdge" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "sourceId" } },
          { kind: "Field", name: { kind: "Name", value: "targetId" } },
          { kind: "Field", name: { kind: "Name", value: "sourceHandle" } },
          { kind: "Field", name: { kind: "Name", value: "targetHandle" } },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  OnDependencyEdgesChangedSubscription,
  OnDependencyEdgesChangedSubscriptionVariables
>;
export const GetNodesDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "query",
      name: { kind: "Name", value: "GetNodes" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: {
            kind: "Variable",
            name: { kind: "Name", value: "where" },
          },
          type: {
            kind: "NamedType",
            name: { kind: "Name", value: "NodeFilterInput" },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "nodes" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "where" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "where" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "NodeFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "NodePositionFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "NodePosition" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "x" } },
          { kind: "Field", name: { kind: "Name", value: "y" } },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "TaskFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Task" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          { kind: "Field", name: { kind: "Name", value: "startTime" } },
          { kind: "Field", name: { kind: "Name", value: "duration" } },
          { kind: "Field", name: { kind: "Name", value: "isExecuting" } },
          { kind: "Field", name: { kind: "Name", value: "progress" } },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "TaskNodeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "TaskNode" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "position" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "NodePositionFields" },
                },
              ],
            },
          },
          { kind: "Field", name: { kind: "Name", value: "parentId" } },
          { kind: "Field", name: { kind: "Name", value: "extent" } },
          { kind: "Field", name: { kind: "Name", value: "width" } },
          { kind: "Field", name: { kind: "Name", value: "height" } },
          { kind: "Field", name: { kind: "Name", value: "selectable" } },
          { kind: "Field", name: { kind: "Name", value: "selected" } },
          { kind: "Field", name: { kind: "Name", value: "draggable" } },
          { kind: "Field", name: { kind: "Name", value: "dragging" } },
          { kind: "Field", name: { kind: "Name", value: "hidden" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "task" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "TaskFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "PropertyFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Property" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "direction" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "binding" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "variableName" },
                },
                { kind: "Field", name: { kind: "Name", value: "mode" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "transformExpression" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "value" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "__typename" } },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "BooleanValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "boolValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "NumberValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "numberValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "StringValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "stringValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "x" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "y" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "z" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "alpha" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "beta" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "gamma" },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionTagValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionTagValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "tag" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "SceneObjectValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "sceneObjectValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "name" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "AgentFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Agent" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "representativeColor" },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Skill" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "properties" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "PropertyFields" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "agents" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "AgentFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillExecutionTaskFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "SkillExecutionTask" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          { kind: "Field", name: { kind: "Name", value: "startTime" } },
          { kind: "Field", name: { kind: "Name", value: "duration" } },
          { kind: "Field", name: { kind: "Name", value: "isExecuting" } },
          { kind: "Field", name: { kind: "Name", value: "progress" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "skill" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "SkillFields" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "agent" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "AgentFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillExecutionNodeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "SkillExecutionNode" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "position" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "NodePositionFields" },
                },
              ],
            },
          },
          { kind: "Field", name: { kind: "Name", value: "parentId" } },
          { kind: "Field", name: { kind: "Name", value: "extent" } },
          { kind: "Field", name: { kind: "Name", value: "width" } },
          { kind: "Field", name: { kind: "Name", value: "height" } },
          { kind: "Field", name: { kind: "Name", value: "selectable" } },
          { kind: "Field", name: { kind: "Name", value: "selected" } },
          { kind: "Field", name: { kind: "Name", value: "draggable" } },
          { kind: "Field", name: { kind: "Name", value: "dragging" } },
          { kind: "Field", name: { kind: "Name", value: "hidden" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "skillExecutionTask" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "SkillExecutionTaskFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "RouterTaskFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "RouterTask" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          { kind: "Field", name: { kind: "Name", value: "startTime" } },
          { kind: "Field", name: { kind: "Name", value: "duration" } },
          { kind: "Field", name: { kind: "Name", value: "isExecuting" } },
          { kind: "Field", name: { kind: "Name", value: "progress" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "selector" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "__typename" } },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "SimpleVariableSelector" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "expression" },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "ExpressionSelector" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "expression" },
                      },
                    ],
                  },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "branches" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "name" } },
                { kind: "Field", name: { kind: "Name", value: "condition" } },
                { kind: "Field", name: { kind: "Name", value: "priority" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "targetNodeId" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "selectedBranchTargetNodeId" },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "selectedBranchName" },
          },
          { kind: "Field", name: { kind: "Name", value: "selectedAtUtc" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "manuallySelectedBranch" },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "RouterNodeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "RouterNode" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "position" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "NodePositionFields" },
                },
              ],
            },
          },
          { kind: "Field", name: { kind: "Name", value: "parentId" } },
          { kind: "Field", name: { kind: "Name", value: "extent" } },
          { kind: "Field", name: { kind: "Name", value: "width" } },
          { kind: "Field", name: { kind: "Name", value: "height" } },
          { kind: "Field", name: { kind: "Name", value: "selectable" } },
          { kind: "Field", name: { kind: "Name", value: "selected" } },
          { kind: "Field", name: { kind: "Name", value: "draggable" } },
          { kind: "Field", name: { kind: "Name", value: "dragging" } },
          { kind: "Field", name: { kind: "Name", value: "hidden" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "routerTask" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "RouterTaskFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "NodeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Node" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "__typename" } },
          {
            kind: "InlineFragment",
            typeCondition: {
              kind: "NamedType",
              name: { kind: "Name", value: "TaskNode" },
            },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "TaskNodeFields" },
                },
              ],
            },
          },
          {
            kind: "InlineFragment",
            typeCondition: {
              kind: "NamedType",
              name: { kind: "Name", value: "SkillExecutionNode" },
            },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "SkillExecutionNodeFields" },
                },
              ],
            },
          },
          {
            kind: "InlineFragment",
            typeCondition: {
              kind: "NamedType",
              name: { kind: "Name", value: "RouterNode" },
            },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "RouterNodeFields" },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<GetNodesQuery, GetNodesQueryVariables>;
export const GetNodeByIdDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "query",
      name: { kind: "Name", value: "GetNodeById" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: { kind: "Variable", name: { kind: "Name", value: "id" } },
          type: {
            kind: "NonNullType",
            type: { kind: "NamedType", name: { kind: "Name", value: "UUID" } },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "nodeById" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "id" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "id" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "NodeFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "NodePositionFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "NodePosition" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "x" } },
          { kind: "Field", name: { kind: "Name", value: "y" } },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "TaskFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Task" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          { kind: "Field", name: { kind: "Name", value: "startTime" } },
          { kind: "Field", name: { kind: "Name", value: "duration" } },
          { kind: "Field", name: { kind: "Name", value: "isExecuting" } },
          { kind: "Field", name: { kind: "Name", value: "progress" } },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "TaskNodeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "TaskNode" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "position" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "NodePositionFields" },
                },
              ],
            },
          },
          { kind: "Field", name: { kind: "Name", value: "parentId" } },
          { kind: "Field", name: { kind: "Name", value: "extent" } },
          { kind: "Field", name: { kind: "Name", value: "width" } },
          { kind: "Field", name: { kind: "Name", value: "height" } },
          { kind: "Field", name: { kind: "Name", value: "selectable" } },
          { kind: "Field", name: { kind: "Name", value: "selected" } },
          { kind: "Field", name: { kind: "Name", value: "draggable" } },
          { kind: "Field", name: { kind: "Name", value: "dragging" } },
          { kind: "Field", name: { kind: "Name", value: "hidden" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "task" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "TaskFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "PropertyFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Property" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "direction" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "binding" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "variableName" },
                },
                { kind: "Field", name: { kind: "Name", value: "mode" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "transformExpression" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "value" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "__typename" } },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "BooleanValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "boolValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "NumberValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "numberValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "StringValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "stringValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "x" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "y" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "z" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "alpha" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "beta" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "gamma" },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionTagValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionTagValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "tag" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "SceneObjectValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "sceneObjectValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "name" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "AgentFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Agent" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "representativeColor" },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Skill" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "properties" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "PropertyFields" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "agents" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "AgentFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillExecutionTaskFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "SkillExecutionTask" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          { kind: "Field", name: { kind: "Name", value: "startTime" } },
          { kind: "Field", name: { kind: "Name", value: "duration" } },
          { kind: "Field", name: { kind: "Name", value: "isExecuting" } },
          { kind: "Field", name: { kind: "Name", value: "progress" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "skill" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "SkillFields" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "agent" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "AgentFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillExecutionNodeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "SkillExecutionNode" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "position" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "NodePositionFields" },
                },
              ],
            },
          },
          { kind: "Field", name: { kind: "Name", value: "parentId" } },
          { kind: "Field", name: { kind: "Name", value: "extent" } },
          { kind: "Field", name: { kind: "Name", value: "width" } },
          { kind: "Field", name: { kind: "Name", value: "height" } },
          { kind: "Field", name: { kind: "Name", value: "selectable" } },
          { kind: "Field", name: { kind: "Name", value: "selected" } },
          { kind: "Field", name: { kind: "Name", value: "draggable" } },
          { kind: "Field", name: { kind: "Name", value: "dragging" } },
          { kind: "Field", name: { kind: "Name", value: "hidden" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "skillExecutionTask" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "SkillExecutionTaskFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "RouterTaskFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "RouterTask" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          { kind: "Field", name: { kind: "Name", value: "startTime" } },
          { kind: "Field", name: { kind: "Name", value: "duration" } },
          { kind: "Field", name: { kind: "Name", value: "isExecuting" } },
          { kind: "Field", name: { kind: "Name", value: "progress" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "selector" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "__typename" } },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "SimpleVariableSelector" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "expression" },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "ExpressionSelector" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "expression" },
                      },
                    ],
                  },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "branches" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "name" } },
                { kind: "Field", name: { kind: "Name", value: "condition" } },
                { kind: "Field", name: { kind: "Name", value: "priority" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "targetNodeId" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "selectedBranchTargetNodeId" },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "selectedBranchName" },
          },
          { kind: "Field", name: { kind: "Name", value: "selectedAtUtc" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "manuallySelectedBranch" },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "RouterNodeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "RouterNode" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "position" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "NodePositionFields" },
                },
              ],
            },
          },
          { kind: "Field", name: { kind: "Name", value: "parentId" } },
          { kind: "Field", name: { kind: "Name", value: "extent" } },
          { kind: "Field", name: { kind: "Name", value: "width" } },
          { kind: "Field", name: { kind: "Name", value: "height" } },
          { kind: "Field", name: { kind: "Name", value: "selectable" } },
          { kind: "Field", name: { kind: "Name", value: "selected" } },
          { kind: "Field", name: { kind: "Name", value: "draggable" } },
          { kind: "Field", name: { kind: "Name", value: "dragging" } },
          { kind: "Field", name: { kind: "Name", value: "hidden" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "routerTask" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "RouterTaskFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "NodeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Node" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "__typename" } },
          {
            kind: "InlineFragment",
            typeCondition: {
              kind: "NamedType",
              name: { kind: "Name", value: "TaskNode" },
            },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "TaskNodeFields" },
                },
              ],
            },
          },
          {
            kind: "InlineFragment",
            typeCondition: {
              kind: "NamedType",
              name: { kind: "Name", value: "SkillExecutionNode" },
            },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "SkillExecutionNodeFields" },
                },
              ],
            },
          },
          {
            kind: "InlineFragment",
            typeCondition: {
              kind: "NamedType",
              name: { kind: "Name", value: "RouterNode" },
            },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "RouterNodeFields" },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<GetNodeByIdQuery, GetNodeByIdQueryVariables>;
export const CreateNodeDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "mutation",
      name: { kind: "Name", value: "CreateNode" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: {
            kind: "Variable",
            name: { kind: "Name", value: "input" },
          },
          type: {
            kind: "NonNullType",
            type: {
              kind: "NamedType",
              name: { kind: "Name", value: "CreateNodeInput" },
            },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "createNode" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "input" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "input" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "node" },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "FragmentSpread",
                        name: { kind: "Name", value: "NodeFields" },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "NodePositionFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "NodePosition" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "x" } },
          { kind: "Field", name: { kind: "Name", value: "y" } },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "TaskFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Task" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          { kind: "Field", name: { kind: "Name", value: "startTime" } },
          { kind: "Field", name: { kind: "Name", value: "duration" } },
          { kind: "Field", name: { kind: "Name", value: "isExecuting" } },
          { kind: "Field", name: { kind: "Name", value: "progress" } },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "TaskNodeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "TaskNode" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "position" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "NodePositionFields" },
                },
              ],
            },
          },
          { kind: "Field", name: { kind: "Name", value: "parentId" } },
          { kind: "Field", name: { kind: "Name", value: "extent" } },
          { kind: "Field", name: { kind: "Name", value: "width" } },
          { kind: "Field", name: { kind: "Name", value: "height" } },
          { kind: "Field", name: { kind: "Name", value: "selectable" } },
          { kind: "Field", name: { kind: "Name", value: "selected" } },
          { kind: "Field", name: { kind: "Name", value: "draggable" } },
          { kind: "Field", name: { kind: "Name", value: "dragging" } },
          { kind: "Field", name: { kind: "Name", value: "hidden" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "task" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "TaskFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "PropertyFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Property" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "direction" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "binding" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "variableName" },
                },
                { kind: "Field", name: { kind: "Name", value: "mode" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "transformExpression" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "value" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "__typename" } },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "BooleanValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "boolValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "NumberValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "numberValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "StringValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "stringValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "x" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "y" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "z" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "alpha" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "beta" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "gamma" },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionTagValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionTagValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "tag" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "SceneObjectValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "sceneObjectValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "name" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "AgentFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Agent" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "representativeColor" },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Skill" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "properties" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "PropertyFields" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "agents" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "AgentFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillExecutionTaskFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "SkillExecutionTask" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          { kind: "Field", name: { kind: "Name", value: "startTime" } },
          { kind: "Field", name: { kind: "Name", value: "duration" } },
          { kind: "Field", name: { kind: "Name", value: "isExecuting" } },
          { kind: "Field", name: { kind: "Name", value: "progress" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "skill" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "SkillFields" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "agent" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "AgentFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillExecutionNodeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "SkillExecutionNode" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "position" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "NodePositionFields" },
                },
              ],
            },
          },
          { kind: "Field", name: { kind: "Name", value: "parentId" } },
          { kind: "Field", name: { kind: "Name", value: "extent" } },
          { kind: "Field", name: { kind: "Name", value: "width" } },
          { kind: "Field", name: { kind: "Name", value: "height" } },
          { kind: "Field", name: { kind: "Name", value: "selectable" } },
          { kind: "Field", name: { kind: "Name", value: "selected" } },
          { kind: "Field", name: { kind: "Name", value: "draggable" } },
          { kind: "Field", name: { kind: "Name", value: "dragging" } },
          { kind: "Field", name: { kind: "Name", value: "hidden" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "skillExecutionTask" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "SkillExecutionTaskFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "RouterTaskFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "RouterTask" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          { kind: "Field", name: { kind: "Name", value: "startTime" } },
          { kind: "Field", name: { kind: "Name", value: "duration" } },
          { kind: "Field", name: { kind: "Name", value: "isExecuting" } },
          { kind: "Field", name: { kind: "Name", value: "progress" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "selector" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "__typename" } },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "SimpleVariableSelector" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "expression" },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "ExpressionSelector" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "expression" },
                      },
                    ],
                  },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "branches" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "name" } },
                { kind: "Field", name: { kind: "Name", value: "condition" } },
                { kind: "Field", name: { kind: "Name", value: "priority" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "targetNodeId" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "selectedBranchTargetNodeId" },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "selectedBranchName" },
          },
          { kind: "Field", name: { kind: "Name", value: "selectedAtUtc" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "manuallySelectedBranch" },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "RouterNodeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "RouterNode" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "position" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "NodePositionFields" },
                },
              ],
            },
          },
          { kind: "Field", name: { kind: "Name", value: "parentId" } },
          { kind: "Field", name: { kind: "Name", value: "extent" } },
          { kind: "Field", name: { kind: "Name", value: "width" } },
          { kind: "Field", name: { kind: "Name", value: "height" } },
          { kind: "Field", name: { kind: "Name", value: "selectable" } },
          { kind: "Field", name: { kind: "Name", value: "selected" } },
          { kind: "Field", name: { kind: "Name", value: "draggable" } },
          { kind: "Field", name: { kind: "Name", value: "dragging" } },
          { kind: "Field", name: { kind: "Name", value: "hidden" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "routerTask" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "RouterTaskFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "NodeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Node" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "__typename" } },
          {
            kind: "InlineFragment",
            typeCondition: {
              kind: "NamedType",
              name: { kind: "Name", value: "TaskNode" },
            },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "TaskNodeFields" },
                },
              ],
            },
          },
          {
            kind: "InlineFragment",
            typeCondition: {
              kind: "NamedType",
              name: { kind: "Name", value: "SkillExecutionNode" },
            },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "SkillExecutionNodeFields" },
                },
              ],
            },
          },
          {
            kind: "InlineFragment",
            typeCondition: {
              kind: "NamedType",
              name: { kind: "Name", value: "RouterNode" },
            },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "RouterNodeFields" },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<CreateNodeMutation, CreateNodeMutationVariables>;
export const UpdateNodeDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "mutation",
      name: { kind: "Name", value: "UpdateNode" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: {
            kind: "Variable",
            name: { kind: "Name", value: "input" },
          },
          type: {
            kind: "NonNullType",
            type: {
              kind: "NamedType",
              name: { kind: "Name", value: "UpdateNodeInput" },
            },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "updateNode" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "input" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "input" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "boolean" } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<UpdateNodeMutation, UpdateNodeMutationVariables>;
export const DeleteNodeDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "mutation",
      name: { kind: "Name", value: "DeleteNode" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: {
            kind: "Variable",
            name: { kind: "Name", value: "input" },
          },
          type: {
            kind: "NonNullType",
            type: {
              kind: "NamedType",
              name: { kind: "Name", value: "DeleteNodeInput" },
            },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "deleteNode" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "input" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "input" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "boolean" } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<DeleteNodeMutation, DeleteNodeMutationVariables>;
export const OnNodesChangedDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "subscription",
      name: { kind: "Name", value: "OnNodesChanged" },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "nodesChanged" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "NodeFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "NodePositionFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "NodePosition" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "x" } },
          { kind: "Field", name: { kind: "Name", value: "y" } },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "TaskFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Task" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          { kind: "Field", name: { kind: "Name", value: "startTime" } },
          { kind: "Field", name: { kind: "Name", value: "duration" } },
          { kind: "Field", name: { kind: "Name", value: "isExecuting" } },
          { kind: "Field", name: { kind: "Name", value: "progress" } },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "TaskNodeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "TaskNode" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "position" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "NodePositionFields" },
                },
              ],
            },
          },
          { kind: "Field", name: { kind: "Name", value: "parentId" } },
          { kind: "Field", name: { kind: "Name", value: "extent" } },
          { kind: "Field", name: { kind: "Name", value: "width" } },
          { kind: "Field", name: { kind: "Name", value: "height" } },
          { kind: "Field", name: { kind: "Name", value: "selectable" } },
          { kind: "Field", name: { kind: "Name", value: "selected" } },
          { kind: "Field", name: { kind: "Name", value: "draggable" } },
          { kind: "Field", name: { kind: "Name", value: "dragging" } },
          { kind: "Field", name: { kind: "Name", value: "hidden" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "task" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "TaskFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "PropertyFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Property" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "direction" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "binding" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "variableName" },
                },
                { kind: "Field", name: { kind: "Name", value: "mode" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "transformExpression" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "value" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "__typename" } },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "BooleanValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "boolValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "NumberValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "numberValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "StringValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "stringValue" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "x" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "y" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "z" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "alpha" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "beta" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "gamma" },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "PositionTagValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "positionTagValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "tag" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "SceneObjectValue" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "sceneObjectValue" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "id" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "name" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "position" },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "x" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "y" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "z" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "alpha" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "beta" },
                                  },
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "gamma" },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "typeName" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "AgentFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Agent" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "representativeColor" },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Skill" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "properties" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "PropertyFields" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "agents" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "AgentFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillExecutionTaskFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "SkillExecutionTask" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          { kind: "Field", name: { kind: "Name", value: "startTime" } },
          { kind: "Field", name: { kind: "Name", value: "duration" } },
          { kind: "Field", name: { kind: "Name", value: "isExecuting" } },
          { kind: "Field", name: { kind: "Name", value: "progress" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "skill" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "SkillFields" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "agent" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "AgentFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SkillExecutionNodeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "SkillExecutionNode" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "position" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "NodePositionFields" },
                },
              ],
            },
          },
          { kind: "Field", name: { kind: "Name", value: "parentId" } },
          { kind: "Field", name: { kind: "Name", value: "extent" } },
          { kind: "Field", name: { kind: "Name", value: "width" } },
          { kind: "Field", name: { kind: "Name", value: "height" } },
          { kind: "Field", name: { kind: "Name", value: "selectable" } },
          { kind: "Field", name: { kind: "Name", value: "selected" } },
          { kind: "Field", name: { kind: "Name", value: "draggable" } },
          { kind: "Field", name: { kind: "Name", value: "dragging" } },
          { kind: "Field", name: { kind: "Name", value: "hidden" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "skillExecutionTask" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "SkillExecutionTaskFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "RouterTaskFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "RouterTask" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "name" } },
          { kind: "Field", name: { kind: "Name", value: "description" } },
          { kind: "Field", name: { kind: "Name", value: "startTime" } },
          { kind: "Field", name: { kind: "Name", value: "duration" } },
          { kind: "Field", name: { kind: "Name", value: "isExecuting" } },
          { kind: "Field", name: { kind: "Name", value: "progress" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "selector" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "__typename" } },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "SimpleVariableSelector" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "expression" },
                      },
                    ],
                  },
                },
                {
                  kind: "InlineFragment",
                  typeCondition: {
                    kind: "NamedType",
                    name: { kind: "Name", value: "ExpressionSelector" },
                  },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "expression" },
                      },
                    ],
                  },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "branches" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "name" } },
                { kind: "Field", name: { kind: "Name", value: "condition" } },
                { kind: "Field", name: { kind: "Name", value: "priority" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "targetNodeId" },
                },
              ],
            },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "selectedBranchTargetNodeId" },
          },
          {
            kind: "Field",
            name: { kind: "Name", value: "selectedBranchName" },
          },
          { kind: "Field", name: { kind: "Name", value: "selectedAtUtc" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "manuallySelectedBranch" },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "RouterNodeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "RouterNode" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "position" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "NodePositionFields" },
                },
              ],
            },
          },
          { kind: "Field", name: { kind: "Name", value: "parentId" } },
          { kind: "Field", name: { kind: "Name", value: "extent" } },
          { kind: "Field", name: { kind: "Name", value: "width" } },
          { kind: "Field", name: { kind: "Name", value: "height" } },
          { kind: "Field", name: { kind: "Name", value: "selectable" } },
          { kind: "Field", name: { kind: "Name", value: "selected" } },
          { kind: "Field", name: { kind: "Name", value: "draggable" } },
          { kind: "Field", name: { kind: "Name", value: "dragging" } },
          { kind: "Field", name: { kind: "Name", value: "hidden" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "routerTask" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "RouterTaskFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "NodeFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "Node" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "__typename" } },
          {
            kind: "InlineFragment",
            typeCondition: {
              kind: "NamedType",
              name: { kind: "Name", value: "TaskNode" },
            },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "TaskNodeFields" },
                },
              ],
            },
          },
          {
            kind: "InlineFragment",
            typeCondition: {
              kind: "NamedType",
              name: { kind: "Name", value: "SkillExecutionNode" },
            },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "SkillExecutionNodeFields" },
                },
              ],
            },
          },
          {
            kind: "InlineFragment",
            typeCondition: {
              kind: "NamedType",
              name: { kind: "Name", value: "RouterNode" },
            },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "RouterNodeFields" },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  OnNodesChangedSubscription,
  OnNodesChangedSubscriptionVariables
>;
export const StartLoadedProcedureDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "mutation",
      name: { kind: "Name", value: "StartLoadedProcedure" },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "startLoadedProcedure" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "boolean" } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  StartLoadedProcedureMutation,
  StartLoadedProcedureMutationVariables
>;
export const GetAllProceduresDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "query",
      name: { kind: "Name", value: "GetAllProcedures" },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "procedures" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "id" } },
                { kind: "Field", name: { kind: "Name", value: "name" } },
                { kind: "Field", name: { kind: "Name", value: "description" } },
                { kind: "Field", name: { kind: "Name", value: "isLoaded" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "createdAtUtc" },
                },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "lastUpdatedAtUtc" },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  GetAllProceduresQuery,
  GetAllProceduresQueryVariables
>;
export const GetLoadedProcedureDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "query",
      name: { kind: "Name", value: "GetLoadedProcedure" },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "loadedProcedure" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "id" } },
                { kind: "Field", name: { kind: "Name", value: "name" } },
                { kind: "Field", name: { kind: "Name", value: "description" } },
                { kind: "Field", name: { kind: "Name", value: "isLoaded" } },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "lastLoadedUtc" },
                },
                {
                  kind: "Field",
                  name: { kind: "Name", value: "variables" },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      { kind: "Field", name: { kind: "Name", value: "name" } },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "type" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "__typename" },
                            },
                            {
                              kind: "InlineFragment",
                              typeCondition: {
                                kind: "NamedType",
                                name: { kind: "Name", value: "BooleanType" },
                              },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "typeName" },
                                  },
                                ],
                              },
                            },
                            {
                              kind: "InlineFragment",
                              typeCondition: {
                                kind: "NamedType",
                                name: { kind: "Name", value: "NumberType" },
                              },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "typeName" },
                                  },
                                ],
                              },
                            },
                            {
                              kind: "InlineFragment",
                              typeCondition: {
                                kind: "NamedType",
                                name: { kind: "Name", value: "StringType" },
                              },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "typeName" },
                                  },
                                ],
                              },
                            },
                            {
                              kind: "InlineFragment",
                              typeCondition: {
                                kind: "NamedType",
                                name: { kind: "Name", value: "PositionType" },
                              },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "typeName" },
                                  },
                                ],
                              },
                            },
                            {
                              kind: "InlineFragment",
                              typeCondition: {
                                kind: "NamedType",
                                name: {
                                  kind: "Name",
                                  value: "PositionTagType",
                                },
                              },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "typeName" },
                                  },
                                ],
                              },
                            },
                            {
                              kind: "InlineFragment",
                              typeCondition: {
                                kind: "NamedType",
                                name: {
                                  kind: "Name",
                                  value: "SceneObjectType",
                                },
                              },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "typeName" },
                                  },
                                ],
                              },
                            },
                            {
                              kind: "InlineFragment",
                              typeCondition: {
                                kind: "NamedType",
                                name: { kind: "Name", value: "EnumType" },
                              },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "typeName" },
                                  },
                                  {
                                    kind: "Field",
                                    name: {
                                      kind: "Name",
                                      value: "allowedValues",
                                    },
                                  },
                                ],
                              },
                            },
                            {
                              kind: "InlineFragment",
                              typeCondition: {
                                kind: "NamedType",
                                name: { kind: "Name", value: "ListType" },
                              },
                              selectionSet: {
                                kind: "SelectionSet",
                                selections: [
                                  {
                                    kind: "Field",
                                    name: { kind: "Name", value: "typeName" },
                                  },
                                  {
                                    kind: "Field",
                                    name: {
                                      kind: "Name",
                                      value: "elementType",
                                    },
                                    selectionSet: {
                                      kind: "SelectionSet",
                                      selections: [
                                        {
                                          kind: "Field",
                                          name: {
                                            kind: "Name",
                                            value: "__typename",
                                          },
                                        },
                                        {
                                          kind: "InlineFragment",
                                          typeCondition: {
                                            kind: "NamedType",
                                            name: {
                                              kind: "Name",
                                              value: "BooleanType",
                                            },
                                          },
                                          selectionSet: {
                                            kind: "SelectionSet",
                                            selections: [
                                              {
                                                kind: "Field",
                                                name: {
                                                  kind: "Name",
                                                  value: "typeName",
                                                },
                                              },
                                            ],
                                          },
                                        },
                                        {
                                          kind: "InlineFragment",
                                          typeCondition: {
                                            kind: "NamedType",
                                            name: {
                                              kind: "Name",
                                              value: "NumberType",
                                            },
                                          },
                                          selectionSet: {
                                            kind: "SelectionSet",
                                            selections: [
                                              {
                                                kind: "Field",
                                                name: {
                                                  kind: "Name",
                                                  value: "typeName",
                                                },
                                              },
                                            ],
                                          },
                                        },
                                        {
                                          kind: "InlineFragment",
                                          typeCondition: {
                                            kind: "NamedType",
                                            name: {
                                              kind: "Name",
                                              value: "StringType",
                                            },
                                          },
                                          selectionSet: {
                                            kind: "SelectionSet",
                                            selections: [
                                              {
                                                kind: "Field",
                                                name: {
                                                  kind: "Name",
                                                  value: "typeName",
                                                },
                                              },
                                            ],
                                          },
                                        },
                                      ],
                                    },
                                  },
                                ],
                              },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "defaultValue" },
                      },
                      { kind: "Field", name: { kind: "Name", value: "scope" } },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "source" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "description" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "isReadOnly" },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  GetLoadedProcedureQuery,
  GetLoadedProcedureQueryVariables
>;
export const LoadProcedureDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "mutation",
      name: { kind: "Name", value: "LoadProcedure" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: { kind: "Variable", name: { kind: "Name", value: "id" } },
          type: {
            kind: "NonNullType",
            type: { kind: "NamedType", name: { kind: "Name", value: "UUID" } },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "loadProcedure" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "id" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "id" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "procedure" },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      { kind: "Field", name: { kind: "Name", value: "id" } },
                      { kind: "Field", name: { kind: "Name", value: "name" } },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "description" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "isLoaded" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "lastLoadedUtc" },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  LoadProcedureMutation,
  LoadProcedureMutationVariables
>;
export const UnloadProcedureDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "mutation",
      name: { kind: "Name", value: "UnloadProcedure" },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "unloadProcedure" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "success" } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  UnloadProcedureMutation,
  UnloadProcedureMutationVariables
>;
export const CreateProcedureDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "mutation",
      name: { kind: "Name", value: "CreateProcedure" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: {
            kind: "Variable",
            name: { kind: "Name", value: "input" },
          },
          type: {
            kind: "NonNullType",
            type: {
              kind: "NamedType",
              name: { kind: "Name", value: "CreateProcedureInput" },
            },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "createProcedure" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "input" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "input" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "procedure" },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      { kind: "Field", name: { kind: "Name", value: "id" } },
                      { kind: "Field", name: { kind: "Name", value: "name" } },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "description" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "createdAtUtc" },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  CreateProcedureMutation,
  CreateProcedureMutationVariables
>;
export const DeleteProcedureDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "mutation",
      name: { kind: "Name", value: "DeleteProcedure" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: {
            kind: "Variable",
            name: { kind: "Name", value: "input" },
          },
          type: {
            kind: "NonNullType",
            type: {
              kind: "NamedType",
              name: { kind: "Name", value: "DeleteProcedureInput" },
            },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "deleteProcedure" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "input" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "input" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "boolean" } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  DeleteProcedureMutation,
  DeleteProcedureMutationVariables
>;
export const GetSceneObjectsDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "query",
      name: { kind: "Name", value: "GetSceneObjects" },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "sceneObjects" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "SceneObjectFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SceneObjectFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "SceneObject" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "position" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "x" } },
                { kind: "Field", name: { kind: "Name", value: "y" } },
                { kind: "Field", name: { kind: "Name", value: "z" } },
                { kind: "Field", name: { kind: "Name", value: "alpha" } },
                { kind: "Field", name: { kind: "Name", value: "beta" } },
                { kind: "Field", name: { kind: "Name", value: "gamma" } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  GetSceneObjectsQuery,
  GetSceneObjectsQueryVariables
>;
export const GetPositionTagsDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "query",
      name: { kind: "Name", value: "GetPositionTags" },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "positionTags" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "PositionTagFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "PositionTagFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "PositionTag" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "tag" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "position" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "x" } },
                { kind: "Field", name: { kind: "Name", value: "y" } },
                { kind: "Field", name: { kind: "Name", value: "z" } },
                { kind: "Field", name: { kind: "Name", value: "alpha" } },
                { kind: "Field", name: { kind: "Name", value: "beta" } },
                { kind: "Field", name: { kind: "Name", value: "gamma" } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  GetPositionTagsQuery,
  GetPositionTagsQueryVariables
>;
export const GetSceneObjectByIdDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "query",
      name: { kind: "Name", value: "GetSceneObjectById" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: { kind: "Variable", name: { kind: "Name", value: "id" } },
          type: {
            kind: "NonNullType",
            type: { kind: "NamedType", name: { kind: "Name", value: "UUID" } },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "sceneObjectById" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "id" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "id" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "SceneObjectFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SceneObjectFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "SceneObject" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "position" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "x" } },
                { kind: "Field", name: { kind: "Name", value: "y" } },
                { kind: "Field", name: { kind: "Name", value: "z" } },
                { kind: "Field", name: { kind: "Name", value: "alpha" } },
                { kind: "Field", name: { kind: "Name", value: "beta" } },
                { kind: "Field", name: { kind: "Name", value: "gamma" } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  GetSceneObjectByIdQuery,
  GetSceneObjectByIdQueryVariables
>;
export const GetPositionTagByIdDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "query",
      name: { kind: "Name", value: "GetPositionTagById" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: { kind: "Variable", name: { kind: "Name", value: "id" } },
          type: {
            kind: "NonNullType",
            type: { kind: "NamedType", name: { kind: "Name", value: "UUID" } },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "positionTagById" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "id" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "id" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "FragmentSpread",
                  name: { kind: "Name", value: "PositionTagFields" },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "PositionTagFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "PositionTag" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "tag" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "position" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "x" } },
                { kind: "Field", name: { kind: "Name", value: "y" } },
                { kind: "Field", name: { kind: "Name", value: "z" } },
                { kind: "Field", name: { kind: "Name", value: "alpha" } },
                { kind: "Field", name: { kind: "Name", value: "beta" } },
                { kind: "Field", name: { kind: "Name", value: "gamma" } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  GetPositionTagByIdQuery,
  GetPositionTagByIdQueryVariables
>;
export const CreateSceneObjectDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "mutation",
      name: { kind: "Name", value: "CreateSceneObject" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: {
            kind: "Variable",
            name: { kind: "Name", value: "input" },
          },
          type: {
            kind: "NonNullType",
            type: {
              kind: "NamedType",
              name: { kind: "Name", value: "CreateSceneObjectInput" },
            },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "createSceneObject" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "input" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "input" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "sceneObject" },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "FragmentSpread",
                        name: { kind: "Name", value: "SceneObjectFields" },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "SceneObjectFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "SceneObject" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "name" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "position" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "x" } },
                { kind: "Field", name: { kind: "Name", value: "y" } },
                { kind: "Field", name: { kind: "Name", value: "z" } },
                { kind: "Field", name: { kind: "Name", value: "alpha" } },
                { kind: "Field", name: { kind: "Name", value: "beta" } },
                { kind: "Field", name: { kind: "Name", value: "gamma" } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  CreateSceneObjectMutation,
  CreateSceneObjectMutationVariables
>;
export const UpdateSceneObjectDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "mutation",
      name: { kind: "Name", value: "UpdateSceneObject" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: {
            kind: "Variable",
            name: { kind: "Name", value: "input" },
          },
          type: {
            kind: "NonNullType",
            type: {
              kind: "NamedType",
              name: { kind: "Name", value: "UpdateSceneObjectInput" },
            },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "updateSceneObject" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "input" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "input" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "boolean" } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  UpdateSceneObjectMutation,
  UpdateSceneObjectMutationVariables
>;
export const DeleteSceneObjectDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "mutation",
      name: { kind: "Name", value: "DeleteSceneObject" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: {
            kind: "Variable",
            name: { kind: "Name", value: "input" },
          },
          type: {
            kind: "NonNullType",
            type: {
              kind: "NamedType",
              name: { kind: "Name", value: "DeleteSceneObjectInput" },
            },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "deleteSceneObject" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "input" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "input" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "boolean" } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  DeleteSceneObjectMutation,
  DeleteSceneObjectMutationVariables
>;
export const CreatePositionTagDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "mutation",
      name: { kind: "Name", value: "CreatePositionTag" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: {
            kind: "Variable",
            name: { kind: "Name", value: "input" },
          },
          type: {
            kind: "NonNullType",
            type: {
              kind: "NamedType",
              name: { kind: "Name", value: "CreatePositionTagInput" },
            },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "createPositionTag" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "input" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "input" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "positionTag" },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "FragmentSpread",
                        name: { kind: "Name", value: "PositionTagFields" },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
    {
      kind: "FragmentDefinition",
      name: { kind: "Name", value: "PositionTagFields" },
      typeCondition: {
        kind: "NamedType",
        name: { kind: "Name", value: "PositionTag" },
      },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          { kind: "Field", name: { kind: "Name", value: "id" } },
          { kind: "Field", name: { kind: "Name", value: "tag" } },
          {
            kind: "Field",
            name: { kind: "Name", value: "position" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "x" } },
                { kind: "Field", name: { kind: "Name", value: "y" } },
                { kind: "Field", name: { kind: "Name", value: "z" } },
                { kind: "Field", name: { kind: "Name", value: "alpha" } },
                { kind: "Field", name: { kind: "Name", value: "beta" } },
                { kind: "Field", name: { kind: "Name", value: "gamma" } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  CreatePositionTagMutation,
  CreatePositionTagMutationVariables
>;
export const UpdatePositionTagDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "mutation",
      name: { kind: "Name", value: "UpdatePositionTag" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: {
            kind: "Variable",
            name: { kind: "Name", value: "input" },
          },
          type: {
            kind: "NonNullType",
            type: {
              kind: "NamedType",
              name: { kind: "Name", value: "UpdatePositionTagInput" },
            },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "updatePositionTag" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "input" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "input" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "boolean" } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  UpdatePositionTagMutation,
  UpdatePositionTagMutationVariables
>;
export const DeletePositionTagDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "mutation",
      name: { kind: "Name", value: "DeletePositionTag" },
      variableDefinitions: [
        {
          kind: "VariableDefinition",
          variable: {
            kind: "Variable",
            name: { kind: "Name", value: "input" },
          },
          type: {
            kind: "NonNullType",
            type: {
              kind: "NamedType",
              name: { kind: "Name", value: "DeletePositionTagInput" },
            },
          },
        },
      ],
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "deletePositionTag" },
            arguments: [
              {
                kind: "Argument",
                name: { kind: "Name", value: "input" },
                value: {
                  kind: "Variable",
                  name: { kind: "Name", value: "input" },
                },
              },
            ],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                { kind: "Field", name: { kind: "Name", value: "boolean" } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  DeletePositionTagMutation,
  DeletePositionTagMutationVariables
>;
export const OnProcedureValidationChangedDocument = {
  kind: "Document",
  definitions: [
    {
      kind: "OperationDefinition",
      operation: "subscription",
      name: { kind: "Name", value: "OnProcedureValidationChanged" },
      selectionSet: {
        kind: "SelectionSet",
        selections: [
          {
            kind: "Field",
            name: { kind: "Name", value: "procedureValidationChanged" },
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  name: { kind: "Name", value: "agentSerializationViolations" },
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "agentId" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "agentName" },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "unserializedSkills" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "nodeId" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "skillName" },
                            },
                          ],
                        },
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "missingFsPairs" },
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "skillA" },
                            },
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "skillB" },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  OnProcedureValidationChangedSubscription,
  OnProcedureValidationChangedSubscriptionVariables
>;
