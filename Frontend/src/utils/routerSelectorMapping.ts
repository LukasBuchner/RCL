import type {
  ConditionalBranch,
  ExpressionSelector,
  SelectorExpression,
  SimpleVariableSelector,
} from "../__generated__/graphql";

export interface UIBranch {
  name: string;
  condition: string | null;
  priority: number;
  targetNodeId: string;
}

export interface BackendRouterData {
  selector: SelectorExpression;
  branches: ConditionalBranch[];
}

/**
 * Extracts variable names from a condition expression.
 *
 * Parses a condition string and extracts all variable names while excluding:
 * - String literals in quotes
 * - Numeric literals
 * - Operators and keywords
 *
 * @param condition - The condition expression to parse
 * @returns Array of unique variable names found in the condition
 *
 * @example
 * extractVariablesFromCondition('quality_result == "OK"')
 * // returns ['quality_result']
 *
 * extractVariablesFromCondition('temperature > 50 && pressure < 100')
 * // returns ['temperature', 'pressure']
 */
export function extractVariablesFromCondition(
  condition: string | null,
): string[] {
  if (!condition) {
    return [];
  }

  const variables = new Set<string>();

  // Remove string literals (both single and double quotes) to avoid matching their contents
  const cleaned = condition.replace(/"[^"]*"/g, '""').replace(/'[^']*'/g, "''");

  // Regular expression to match variable names
  // Matches identifiers that are:
  // - Word characters (letters, digits, underscores)
  // - Not preceded by a quote
  // - Not numeric literals
  const variablePattern = /\b([a-zA-Z_][a-zA-Z0-9_]*)\b/g;

  let match;
  while ((match = variablePattern.exec(cleaned)) !== null) {
    const token = match[1];

    // Exclude keywords and operators
    const excludedTokens = new Set([
      "true",
      "false",
      "null",
      "undefined",
      "contains",
      "startsWith",
      "endsWith",
      "and",
      "or",
      "not",
    ]);

    if (!excludedTokens.has(token.toLowerCase())) {
      variables.add(token);
    }
  }

  return Array.from(variables);
}

/**
 * Maps between UI-friendly branch format and backend SelectorExpression format.
 *
 * The mapper automatically infers the appropriate selector type based on branch conditions:
 * - SimpleVariableSelector: When all branches use the same single variable
 * - ExpressionSelector: When branches use multiple variables or complex logic
 */
export class RouterSelectorMapper {
  /**
   * Converts UI branch data to backend format with auto-inferred selector.
   *
   * Algorithm:
   * 1. Validates that at least one branch has a condition
   * 2. Extracts variables from all non-default branches
   * 3. If all branches use the same single variable → SimpleVariableSelector
   * 4. Otherwise → ExpressionSelector (for complex/multi-variable conditions)
   *
   * @param uiBranches - Array of branches from UI
   * @returns Backend format with selector and branches
   * @throws Error if branches array is empty or all branches are default
   *
   * @example
   * // Simple case - single variable
   * toBackend([
   *   { name: 'Success', condition: 'quality_result == "OK"', priority: 1, targetNodeId: 'n1' },
   *   { name: 'Failure', condition: 'quality_result == "NotOK"', priority: 2, targetNodeId: 'n2' }
   * ])
   * // returns:
   * // {
   * //   selector: { __typename: 'SimpleVariableSelector', expression: 'quality_result' },
   * //   branches: [...]
   * // }
   *
   * @example
   * // Complex case - multiple variables
   * toBackend([
   *   { name: 'Hot', condition: 'temperature > 100', priority: 1, targetNodeId: 'n1' },
   *   { name: 'Cold', condition: 'pressure < 50', priority: 2, targetNodeId: 'n2' }
   * ])
   * // returns:
   * // {
   * //   selector: { __typename: 'ExpressionSelector', expression: 'true' },
   * //   branches: [...]
   * // }
   */
  static toBackend(uiBranches: UIBranch[]): BackendRouterData {
    // Validation
    if (uiBranches.length === 0) {
      throw new Error("Router must have at least one branch");
    }

    const nonDefaultBranches = uiBranches.filter((b) => b.condition !== null);
    if (nonDefaultBranches.length === 0) {
      throw new Error("At least one branch must have a condition");
    }

    // Extract all variables from all conditions
    const allVariables = new Set<string>();
    const variablesByBranch: string[][] = [];

    for (const branch of nonDefaultBranches) {
      const vars = extractVariablesFromCondition(branch.condition);
      variablesByBranch.push(vars);
      vars.forEach((v) => allVariables.add(v));
    }

    // Determine selector type based on variable analysis
    let selector: SelectorExpression;

    // Check if all branches use the same single variable
    const singleVariable =
      allVariables.size === 1 &&
      variablesByBranch.every((vars) => vars.length === 1);

    if (singleVariable) {
      // SimpleVariableSelector - all branches reference the same variable
      const variableName = Array.from(allVariables)[0];
      selector = {
        __typename: "SimpleVariableSelector",
        expression: variableName,
      } as SimpleVariableSelector;
    } else {
      // ExpressionSelector - multiple variables or complex logic
      selector = {
        __typename: "ExpressionSelector",
        expression: "true",
      } as ExpressionSelector;
    }

    // Convert UI branches to backend format
    const branches: ConditionalBranch[] = uiBranches.map((branch) => ({
      __typename: "ConditionalBranch",
      name: branch.name,
      condition: branch.condition,
      priority: branch.priority,
      targetNodeId: branch.targetNodeId,
    }));

    return {
      selector,
      branches,
    };
  }

  /**
   * Converts backend format with selector to UI-friendly branch format.
   *
   * The selector is hidden from the user in the UI - they only see full conditions per branch.
   * This method simply extracts the branch data, making the selector transparent to users.
   *
   * @param selector - The backend selector expression (SimpleVariableSelector or ExpressionSelector)
   * @param branches - Array of backend branches
   * @returns Array of UI branches with full conditions visible
   *
   * @example
   * fromBackend(
   *   { __typename: 'SimpleVariableSelector', expression: 'quality_result' },
   *   [{ name: 'Success', condition: 'quality_result == "OK"', priority: 1, targetNodeId: 'n1' }]
   * )
   * // returns:
   * // [{ name: 'Success', condition: 'quality_result == "OK"', priority: 1, targetNodeId: 'n1' }]
   */
  static fromBackend(
    _selector: SelectorExpression,
    branches: ConditionalBranch[],
  ): UIBranch[] {
    // User doesn't need to see the selector - just show full conditions
    return branches.map((branch) => ({
      name: branch.name,
      condition: branch.condition ?? null,
      priority: branch.priority,
      targetNodeId: branch.targetNodeId,
    }));
  }
}
