import { mapNodeFieldsFragmentToAppNode } from "../mapNodeFieldsFragmentsToAppNodes";
import { RouterBasicData, RouterNode, TaskBasicData } from "../../../nodeTypes";
import { NodeFieldsFragment } from "../../../../__generated__/graphql";
import { MIN_CONTAINER_WIDTH } from "../../../../components/nodes/BaseNode";

describe("mapNodeFieldsFragmentToAppNode", () => {
  describe("RouterNode", () => {
    it("should map RouterNode fragment to AppNode", () => {
      const fragment: NodeFieldsFragment = {
        __typename: "RouterNode",
        id: "router-1",
        position: { x: 100, y: 200 },
        parentId: "parent-1",
        extent: "parent",
        width: 250,
        height: 120,
        selectable: true,
        selected: false,
        draggable: true,
        dragging: false,
        hidden: false,
        routerTask: {
          name: "Status Router",
          description: "Routes based on status",
          startTime: 1000,
          duration: 200,
          isExecuting: false,
          progress: 0,
          selector: {
            __typename: "SimpleVariableSelector",
            expression: "status",
          },
          branches: [
            {
              name: "Success",
              condition: '== "success"',
              priority: 0,
              targetNodeId: "node-1",
            },
            {
              name: "Default",
              condition: "",
              priority: 999,
              targetNodeId: "node-2",
            },
          ],
        },
      };

      const result = mapNodeFieldsFragmentToAppNode(fragment);

      expect(result).toEqual({
        id: "router-1",
        type: "routerNode",
        position: { x: 100, y: 200 },
        parentId: "parent-1",
        extent: "parent",
        width: 250,
        height: 120,
        selectable: true,
        selected: undefined,
        draggable: true,
        dragging: undefined,
        hidden: false,
        data: {
          name: "Status Router",
          description: "Routes based on status",
          startTime: 1000,
          duration: 200,
          width: 250,
          isExecuting: false,
          progress: 0,
          selector: {
            __typename: "SimpleVariableSelector",
            expression: "status",
          },
          branches: [
            {
              name: "Success",
              condition: '== "success"',
              priority: 0,
              targetNodeId: "node-1",
            },
            {
              name: "Default",
              condition: "",
              priority: 999,
              targetNodeId: "node-2",
            },
          ],
        },
      } as RouterNode);
    });

    it("should floor a zero backend width to the attachable container minimum", () => {
      const fragment: NodeFieldsFragment = {
        __typename: "RouterNode",
        id: "router-empty",
        position: { x: 0, y: 0 },
        parentId: null,
        extent: null,
        width: 0,
        height: 0,
        selectable: null,
        selected: null,
        draggable: null,
        dragging: null,
        hidden: null,
        routerTask: {
          name: "Empty Router",
          description: null,
          startTime: 0,
          duration: 0,
          isExecuting: null,
          progress: null,
          selector: {
            __typename: "SimpleVariableSelector",
            expression: "var",
          },
          branches: [],
        },
      };

      const result = mapNodeFieldsFragmentToAppNode(fragment);

      expect(result.width).toBe(MIN_CONTAINER_WIDTH);
      expect((result.data as RouterBasicData).width).toBe(MIN_CONTAINER_WIDTH);
    });

    it("should handle optional fields", () => {
      const fragment: NodeFieldsFragment = {
        __typename: "RouterNode",
        id: "router-2",
        position: { x: 0, y: 0 },
        parentId: null,
        extent: null,
        width: null,
        height: null,
        selectable: null,
        selected: null,
        draggable: null,
        dragging: null,
        hidden: null,
        routerTask: {
          name: "Simple Router",
          description: null,
          startTime: 0,
          duration: 100,
          isExecuting: null,
          progress: null,
          selector: {
            __typename: "SimpleVariableSelector",
            expression: "var",
          },
          branches: [],
        },
      };

      const result = mapNodeFieldsFragmentToAppNode(fragment);

      expect(result.type).toBe("routerNode");
      const routerData = result.data as RouterBasicData;
      expect(routerData.name).toBe("Simple Router");
      expect(routerData.description).toBe("");
      expect(routerData.branches).toEqual([]);
    });
  });

  describe("TaskNode", () => {
    it("should floor a zero backend width to the attachable container minimum", () => {
      const fragment: NodeFieldsFragment = {
        __typename: "TaskNode",
        id: "task-empty",
        position: { x: 0, y: 0 },
        parentId: null,
        extent: null,
        width: 0,
        height: 0,
        selectable: null,
        selected: null,
        draggable: null,
        dragging: null,
        hidden: null,
        task: {
          name: "Empty Task",
          description: null,
          startTime: 0,
          duration: 0,
          isExecuting: false,
          progress: 0,
        },
      } as unknown as NodeFieldsFragment;

      const result = mapNodeFieldsFragmentToAppNode(fragment);

      expect(result.width).toBe(MIN_CONTAINER_WIDTH);
      expect((result.data as TaskBasicData).width).toBe(MIN_CONTAINER_WIDTH);
    });
  });

  describe("Error handling", () => {
    it("should throw error for unknown node type", () => {
      const fragment = {
        __typename: "UnknownNode",
        id: "unknown-1",
      } as unknown as NodeFieldsFragment;

      expect(() => mapNodeFieldsFragmentToAppNode(fragment)).toThrow(
        "Unknown node type",
      );
    });
  });
});
