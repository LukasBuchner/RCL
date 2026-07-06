import {
  NodeInput,
  NodePositionInput,
  PropertyFieldsFragment,
  PropertyInput,
  RouterNodeInput,
  RouterTaskInput,
  SkillExecutionNodeInput,
  SkillExecutionTaskInput,
  SkillInput,
  TaskInput,
  TaskNodeInput,
} from "../../../__generated__/graphql";
import {
  AppNode,
  RouterBasicData,
  RouterNode,
  SkillExecutionBasicData,
  SkillExecutionNode,
  TaskBasicData,
  TaskNode,
} from "../../nodeTypes";
import { mapPropertyFieldsFragmentToPropertyInput } from "../property/mapPropertyFieldsFragmentToPropertyInput";

export function mapAppNodeToNodeInput(node: AppNode): NodeInput {
  const position: NodePositionInput = {
    x: node.position.x,
    y: node.position.y,
  };

  switch (node.type) {
    case "taskNode": {
      const taskNode = node as TaskNode;
      const data = taskNode.data as TaskBasicData;

      const extent =
        typeof taskNode.extent === "string" && taskNode.extent === "parent"
          ? "parent"
          : undefined;

      const taskInput: TaskInput = {
        name: data.name,
        startTime: data.startTime,
        duration: data.duration,
        description: data.description,
      };

      const taskNodeInput: TaskNodeInput = {
        id: taskNode.id,
        parentId: taskNode.parentId,
        position,
        draggable: taskNode.draggable,
        dragging: undefined,
        extent: extent,
        height: taskNode.height,
        hidden: taskNode.hidden,
        selectable: taskNode.selectable,
        selected: undefined,
        width: taskNode.width,
        taskInput: taskInput,
      };

      return {
        taskNode: taskNodeInput,
      };
    }

    case "skillExecutionNode": {
      const skillNode = node as SkillExecutionNode;
      const data = skillNode.data as SkillExecutionBasicData;

      const extent =
        typeof skillNode.extent === "string" && skillNode.extent === "parent"
          ? "parent"
          : undefined;

      const skillExecutionTaskInput: SkillExecutionTaskInput = {
        name: data.name,
        description: data.description,
        startTime: data.startTime,
        duration: data.duration,
        skill: {
          id: data.skill.id,
          description: data.skill.description,
          name: data.skill.name,
          properties: data.skill.properties.map(
            (property: PropertyFieldsFragment): PropertyInput =>
              mapPropertyFieldsFragmentToPropertyInput(property),
          ),
        } as SkillInput,
        agentId: data.agent.id,
      };

      const skillExecutionNodeInput: SkillExecutionNodeInput = {
        id: skillNode.id,
        parentId: skillNode.parentId,
        position,
        draggable: skillNode.draggable,
        dragging: undefined,
        extent: extent,
        height: skillNode.height,
        hidden: skillNode.hidden,
        selectable: skillNode.selectable,
        selected: undefined,
        width: skillNode.width,
        skillExecutionTask: skillExecutionTaskInput,
      };

      return {
        skillExecutionNode: skillExecutionNodeInput,
      };
    }

    case "routerNode": {
      const routerNode = node as RouterNode;
      const data = routerNode.data as RouterBasicData;

      const extent =
        typeof routerNode.extent === "string" && routerNode.extent === "parent"
          ? "parent"
          : undefined;

      const routerTaskInput: RouterTaskInput = {
        name: data.name,
        description: data.description,
        startTime: data.startTime ?? 0,
        duration: data.duration,
        selector: {
          // Use OneOf pattern - exactly one selector type must be set
          // For now, default to SimpleVariableSelector
          // TODO: Determine selector type based on data.selector.type when available
          simpleVariableSelector: {
            expression: data.selector.expression,
          },
        },
        branches: data.branches.map((branch) => ({
          name: branch.name,
          condition: branch.condition || null,
          priority: branch.priority,
          targetNodeId: branch.targetNodeId || null,
        })),
      };

      const routerNodeInput: RouterNodeInput = {
        id: routerNode.id,
        parentId: routerNode.parentId,
        position,
        draggable: routerNode.draggable,
        dragging: undefined,
        extent: extent,
        height: routerNode.height,
        hidden: routerNode.hidden,
        selectable: routerNode.selectable,
        selected: undefined,
        width: routerNode.width,
        routerTaskInput: routerTaskInput,
      };

      return {
        routerNode: routerNodeInput,
      };
    }

    default:
      throw new Error(`Unsupported node type: ${node.type}`);
  }
}
