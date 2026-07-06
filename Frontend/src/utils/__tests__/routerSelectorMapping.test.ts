import {
  extractVariablesFromCondition,
  RouterSelectorMapper,
  type UIBranch,
} from "../routerSelectorMapping";
import type {
  ConditionalBranch,
  ExpressionSelector,
  SimpleVariableSelector,
} from "../../__generated__/graphql";

describe("RouterSelectorMapper", () => {
  describe("toBackend - Simple Case", () => {
    // TEST 1: Single variable across all branches
    it("should infer SimpleVariableSelector when all branches use same variable", () => {
      // ARRANGE
      const uiBranches: UIBranch[] = [
        {
          name: "Success",
          condition: 'quality_result == "OK"',
          priority: 1,
          targetNodeId: "node-abc",
        },
        {
          name: "Failure",
          condition: 'quality_result == "NotOK"',
          priority: 2,
          targetNodeId: "node-def",
        },
      ];

      // ACT
      const result = RouterSelectorMapper.toBackend(uiBranches);

      // ASSERT
      expect(result.selector.__typename).toBe("SimpleVariableSelector");
      expect((result.selector as SimpleVariableSelector).expression).toBe(
        "quality_result",
      );
      expect(result.branches).toHaveLength(2);
      expect(result.branches[0].condition).toBe('quality_result == "OK"');
    });

    // TEST 2: Default branch handling
    it("should handle default branch (null condition)", () => {
      const uiBranches: UIBranch[] = [
        {
          name: "Success",
          condition: 'status == "OK"',
          priority: 1,
          targetNodeId: "node-1",
        },
        {
          name: "Default",
          condition: null,
          priority: 999,
          targetNodeId: "node-default",
        },
      ];

      const result = RouterSelectorMapper.toBackend(uiBranches);

      expect(result.selector.__typename).toBe("SimpleVariableSelector");
      expect((result.selector as SimpleVariableSelector).expression).toBe(
        "status",
      );
      expect(result.branches[1].condition).toBeNull();
    });

    // TEST 3: Numeric comparisons
    it("should handle numeric variable comparisons", () => {
      const uiBranches: UIBranch[] = [
        {
          name: "Hot",
          condition: "temperature > 50",
          priority: 1,
          targetNodeId: "n1",
        },
        {
          name: "Cold",
          condition: "temperature <= 50",
          priority: 2,
          targetNodeId: "n2",
        },
      ];

      const result = RouterSelectorMapper.toBackend(uiBranches);

      expect((result.selector as SimpleVariableSelector).expression).toBe(
        "temperature",
      );
    });

    // TEST 4: String operators (contains, startsWith)
    it("should handle string operators", () => {
      const uiBranches: UIBranch[] = [
        {
          name: "Contains",
          condition: 'message contains "error"',
          priority: 1,
          targetNodeId: "n1",
        },
      ];

      const result = RouterSelectorMapper.toBackend(uiBranches);

      expect((result.selector as SimpleVariableSelector).expression).toBe(
        "message",
      );
    });
  });

  describe("toBackend - Complex Case", () => {
    // TEST 5: Multiple variables → ExpressionSelector
    it("should infer ExpressionSelector when branches use different variables", () => {
      const uiBranches: UIBranch[] = [
        {
          name: "High Temp",
          condition: "temperature > 100",
          priority: 1,
          targetNodeId: "node-1",
        },
        {
          name: "Low Pressure",
          condition: "pressure < 50",
          priority: 2,
          targetNodeId: "node-2",
        },
      ];

      const result = RouterSelectorMapper.toBackend(uiBranches);

      expect(result.selector.__typename).toBe("ExpressionSelector");
      expect(result.branches[0].condition).toBe("temperature > 100");
      expect(result.branches[1].condition).toBe("pressure < 50");
    });

    // TEST 6: Complex boolean logic
    it("should handle complex AND/OR conditions", () => {
      const uiBranches: UIBranch[] = [
        {
          name: "Critical",
          condition: "temperature > 100 && pressure < 50",
          priority: 1,
          targetNodeId: "node-1",
        },
      ];

      const result = RouterSelectorMapper.toBackend(uiBranches);

      expect(result.selector.__typename).toBe("ExpressionSelector");
    });
  });

  describe("fromBackend - Load and Display", () => {
    // TEST 7: Convert SimpleVariableSelector to UI
    it("should convert SimpleVariableSelector back to UI format", () => {
      const selector: SimpleVariableSelector = {
        __typename: "SimpleVariableSelector",
        expression: "quality_result",
      };

      const branches: ConditionalBranch[] = [
        {
          __typename: "ConditionalBranch",
          name: "Success",
          condition: 'quality_result == "OK"',
          priority: 1,
          targetNodeId: "node-abc",
        },
      ];

      const uiBranches = RouterSelectorMapper.fromBackend(selector, branches);

      expect(uiBranches).toHaveLength(1);
      expect(uiBranches[0].condition).toBe('quality_result == "OK"');
      expect(uiBranches[0].name).toBe("Success");
    });

    // TEST 8: Convert ExpressionSelector to UI
    it("should convert ExpressionSelector back to UI format", () => {
      const selector: ExpressionSelector = {
        __typename: "ExpressionSelector",
        expression: "true",
      };

      const branches: ConditionalBranch[] = [
        {
          __typename: "ConditionalBranch",
          name: "Complex",
          condition: "temp > 50 && pressure < 100",
          priority: 1,
          targetNodeId: "node-1",
        },
      ];

      const uiBranches = RouterSelectorMapper.fromBackend(selector, branches);

      expect(uiBranches[0].condition).toBe("temp > 50 && pressure < 100");
    });
  });

  describe("Variable Extraction Utility", () => {
    // TEST 9: Extract single variable
    it("should extract single variable from condition", () => {
      const condition = 'quality_result == "OK"';
      const variables = extractVariablesFromCondition(condition);

      expect(variables).toEqual(["quality_result"]);
    });

    // TEST 10: Extract multiple variables
    it("should extract multiple variables from complex condition", () => {
      const condition = "temperature > 50 && pressure < 100";
      const variables = extractVariablesFromCondition(condition);

      expect(variables).toEqual(["temperature", "pressure"]);
    });

    // TEST 11: Handle quotes and strings
    it("should not treat string literals as variables", () => {
      const condition = 'status == "OK" && message contains "success"';
      const variables = extractVariablesFromCondition(condition);

      expect(variables).toEqual(["status", "message"]);
      expect(variables).not.toContain("OK");
      expect(variables).not.toContain("success");
    });

    // TEST 12: Handle operators correctly
    it("should extract variables with various operators", () => {
      const conditions = [
        "count >= 10",
        'name != "test"',
        "value <= 100",
        'text contains "hello"',
      ];

      conditions.forEach((condition) => {
        const vars = extractVariablesFromCondition(condition);
        expect(vars.length).toBeGreaterThan(0);
      });
    });
  });

  describe("Edge Cases", () => {
    // TEST 13: Empty branches array
    it("should throw error for empty branches", () => {
      expect(() => {
        RouterSelectorMapper.toBackend([]);
      }).toThrow("Router must have at least one branch");
    });

    // TEST 14: All branches are default (no conditions)
    it("should throw error when all branches are default", () => {
      const uiBranches: UIBranch[] = [
        { name: "Default", condition: null, priority: 1, targetNodeId: "n1" },
      ];

      expect(() => {
        RouterSelectorMapper.toBackend(uiBranches);
      }).toThrow("At least one branch must have a condition");
    });

    // TEST 15: Invalid condition syntax
    it("should handle invalid condition syntax gracefully", () => {
      const uiBranches: UIBranch[] = [
        {
          name: "Bad",
          condition: "invalid syntax ===",
          priority: 1,
          targetNodeId: "n1",
        },
      ];

      const result = RouterSelectorMapper.toBackend(uiBranches);

      expect(result.selector.__typename).toBe("ExpressionSelector");
    });
  });
});
