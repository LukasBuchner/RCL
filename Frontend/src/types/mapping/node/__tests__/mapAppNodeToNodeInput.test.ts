import { mapAppNodeToNodeInput } from "../mapAppNodeToNodeInput";
import { RouterNode } from "../../../nodeTypes";

describe("mapAppNodeToNodeInput", () => {
  describe("RouterNode", () => {
    it("should map RouterNode to NodeInput", () => {
      const routerNode: RouterNode = {
        id: "router-1",
        type: "routerNode",
        position: { x: 100, y: 200 },
        parentId: "parent-1",
        extent: "parent",
        width: 250,
        height: 120,
        selectable: true,
        draggable: true,
        hidden: false,
        data: {
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

      const result = mapAppNodeToNodeInput(routerNode);

      expect(result).toEqual({
        routerNode: {
          id: "router-1",
          parentId: "parent-1",
          position: {
            x: 100,
            y: 200,
          },
          draggable: true,
          dragging: undefined,
          extent: "parent",
          height: 120,
          hidden: false,
          selectable: true,
          selected: undefined,
          width: 250,
          routerTaskInput: {
            name: "Status Router",
            description: "Routes based on status",
            startTime: 1000,
            duration: 200,
            selector: {
              simpleVariableSelector: {
                expression: "status",
              },
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
                condition: null,
                priority: 999,
                targetNodeId: "node-2",
              },
            ],
          },
        },
      });
    });

    it("should handle RouterNode without parent", () => {
      const routerNode: RouterNode = {
        id: "router-2",
        type: "routerNode",
        position: { x: 0, y: 0 },
        data: {
          name: "Simple Router",
          description: "",
          startTime: 0,
          duration: 100,
          selector: {
            __typename: "SimpleVariableSelector",
            expression: "var",
          },
          branches: [],
        },
      };

      const result = mapAppNodeToNodeInput(routerNode);

      expect(result.routerNode).toBeDefined();
      expect(result.routerNode?.parentId).toBeUndefined();
      expect(result.routerNode?.extent).toBeUndefined();
    });

    it("should map empty branch targetNodeId to null (new router with unlinked branches)", () => {
      const routerNode: RouterNode = {
        id: "router-3",
        type: "routerNode",
        position: { x: 0, y: 0 },
        data: {
          name: "New Router",
          description: "",
          startTime: 0,
          duration: 100,
          selector: {
            __typename: "SimpleVariableSelector",
            expression: "status",
          },
          branches: [
            {
              name: "Success",
              condition: '== "success"',
              priority: 0,
              targetNodeId: "",
            },
            {
              name: "Default",
              condition: "",
              priority: 999,
              targetNodeId: "",
            },
          ],
        },
      };

      const result = mapAppNodeToNodeInput(routerNode);

      const branches = result.routerNode?.routerTaskInput.branches;
      expect(branches?.[0].targetNodeId).toBeNull();
      expect(branches?.[1].targetNodeId).toBeNull();
    });
  });
});
