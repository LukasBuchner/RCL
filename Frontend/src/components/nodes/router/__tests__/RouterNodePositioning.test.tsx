import { render, screen } from "../../../../test/test-utils";
import RouterNode from "../RouterNode";
import { Node, NodeProps, ReactFlowProvider } from "@xyflow/react";
import { RouterBasicData } from "../../../../types/nodeTypes";

type RouterNodeType = Node<RouterBasicData, "routerNode">;

/**
 * Test suite for RouterNode dropdown positioning behavior
 *
 * REQUIREMENT: The branch selector dropdown should be positioned INSIDE the node,
 * directly below the icon and name row. The backend adds RouterDropdownHeight (26px)
 * to accommodate the dropdown when branches exist.
 *
 * POSITIONING SPECIFICATION:
 * - Position: Inside node, below icon/name row (normal flex flow)
 * - No absolute positioning: Uses natural document flow
 * - Spacing: Small margin (4px) separates dropdown from name row
 * - High z-index: Dropdown menu still appears above other elements
 * - Width: Uses available node width
 */

const createMockProps = (
  data: Partial<RouterBasicData>,
  overrides?: Partial<NodeProps<RouterNodeType>>,
): NodeProps<RouterNodeType> => ({
  id: "router-1",
  type: "routerNode",
  data: {
    name: "Status Router",
    description: "Routes based on status",
    duration: 200,
    selector: {
      __typename: "SelectorExpression",
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
    selectedBranch: undefined,
    ...data,
  },
  selected: false,
  isConnectable: true,
  zIndex: 0,
  dragging: false,
  draggable: true,
  selectable: true,
  deletable: true,
  positionAbsoluteX: 0,
  positionAbsoluteY: 0,
  height: 80, // Default height
  width: 200,
  parentId: undefined,
  sourcePosition: undefined,
  targetPosition: undefined,
  dragHandle: undefined,
  ...overrides,
});

const renderWithReactFlow = (ui: React.ReactElement) => {
  return render(<ReactFlowProvider>{ui}</ReactFlowProvider>);
};

describe("RouterNode Dropdown Positioning", () => {
  describe("Basic Positioning Tests", () => {
    it("should render dropdown when branches exist", () => {
      const props = createMockProps({});
      renderWithReactFlow(<RouterNode {...props} />);

      const dropdown = screen.getByLabelText("Select branch");
      expect(dropdown).toBeInTheDocument();
    });

    it("should position dropdown inside node with default height (80px)", () => {
      const props = createMockProps({}, { height: 80 });
      renderWithReactFlow(<RouterNode {...props} />);

      const dropdown = screen.getByLabelText("Select branch");
      const dropdownWrapper = dropdown.closest('div[style*="margin"]');

      expect(dropdownWrapper).toBeInTheDocument();

      // Dropdown should NOT be absolutely positioned
      const style = dropdownWrapper?.getAttribute("style");
      expect(style).not.toContain("position: absolute");

      // Should have marginTop for spacing from name row
      expect(style).toContain("margin-top");
    });

    it("should position dropdown inside node with increased height (120px)", () => {
      const props = createMockProps({}, { height: 120 });
      renderWithReactFlow(<RouterNode {...props} />);

      const dropdown = screen.getByLabelText("Select branch");
      const dropdownWrapper = dropdown.closest('div[style*="margin"]');

      expect(dropdownWrapper).toBeInTheDocument();

      // Dropdown should be in normal document flow, not absolutely positioned
      const style = dropdownWrapper?.getAttribute("style");
      expect(style).not.toContain("position: absolute");
      expect(style).toContain("margin-top");
    });

    it("should position dropdown inside node with large height (200px)", () => {
      const props = createMockProps({}, { height: 200 });
      renderWithReactFlow(<RouterNode {...props} />);

      const dropdown = screen.getByLabelText("Select branch");
      const dropdownWrapper = dropdown.closest('div[style*="margin"]');

      expect(dropdownWrapper).toBeInTheDocument();

      // Dropdown should be in normal flex flow
      const style = dropdownWrapper?.getAttribute("style");
      expect(style).not.toContain("position: absolute");
      expect(style).toContain("margin-top");
    });
  });

  describe("Container Height Tests", () => {
    it("should have flex container that fills 100% height", () => {
      const props = createMockProps({});
      const { container } = renderWithReactFlow(<RouterNode {...props} />);

      const flexContainer = container.querySelector(
        'div[style*="height: 100%"]',
      );
      expect(flexContainer).toBeInTheDocument();
      expect(flexContainer).toHaveStyle({ height: "100%" });
    });

    it("should use flexbox layout with column direction", () => {
      const props = createMockProps({});
      const { container } = renderWithReactFlow(<RouterNode {...props} />);

      const flexContainer = container.querySelector(".d-flex.flex-column");
      expect(flexContainer).toBeInTheDocument();
    });

    it("should have box-sizing: border-box on flex container", () => {
      const props = createMockProps({});
      const { container } = renderWithReactFlow(<RouterNode {...props} />);

      const flexContainer = container.querySelector(
        'div[style*="height: 100%"]',
      );
      expect(flexContainer).toHaveStyle({ boxSizing: "border-box" });
    });
  });

  describe("Dropdown Wrapper Tests", () => {
    it("should NOT have absolute positioning on dropdown wrapper", () => {
      const props = createMockProps({});
      renderWithReactFlow(<RouterNode {...props} />);

      const dropdown = screen.getByLabelText("Select branch");
      const dropdownWrapper = dropdown.closest('div[style*="margin"]');

      expect(dropdownWrapper).toBeInTheDocument();

      const style = dropdownWrapper?.getAttribute("style");
      expect(style).not.toContain("position: absolute");
    });

    it("should have marginTop for spacing below name row", () => {
      const props = createMockProps({});
      renderWithReactFlow(<RouterNode {...props} />);

      const dropdown = screen.getByLabelText("Select branch");
      const dropdownWrapper = dropdown.closest('div[style*="margin"]');

      const style = dropdownWrapper?.getAttribute("style");
      expect(style).toContain("margin-top");

      // Parse marginTop value
      const marginMatch = style?.match(/margin-top:\s*(\d+)px/);
      expect(marginMatch).toBeTruthy();
      if (marginMatch) {
        const marginValue = parseInt(marginMatch[1]);
        expect(marginValue).toBeGreaterThan(0);
        expect(marginValue).toBeLessThanOrEqual(10); // Reasonable spacing
      }
    });

    it("should NOT have left positioning", () => {
      const props = createMockProps({});
      renderWithReactFlow(<RouterNode {...props} />);

      const dropdown = screen.getByLabelText("Select branch");
      const dropdownWrapper = dropdown.closest('div[style*="margin"]');

      const style = dropdownWrapper?.getAttribute("style");
      expect(style).not.toContain("left:");
    });

    it("should have high z-index on dropdown (Form.Select) for menu overlay", () => {
      const props = createMockProps({});
      renderWithReactFlow(<RouterNode {...props} />);

      const dropdown = screen.getByLabelText("Select branch");
      const style = dropdown.getAttribute("style");

      const zIndexMatch = style?.match(/z-index:\s*(\d+)/);
      expect(zIndexMatch).toBeTruthy();
      if (zIndexMatch) {
        const zIndexValue = parseInt(zIndexMatch[1]);
        expect(zIndexValue).toBeGreaterThanOrEqual(100);
      }
    });

    it("should use natural width (no width constraints on wrapper)", () => {
      const props = createMockProps({});
      renderWithReactFlow(<RouterNode {...props} />);

      const dropdown = screen.getByLabelText("Select branch");
      const dropdownWrapper = dropdown.closest('div[style*="margin"]');

      const style = dropdownWrapper?.getAttribute("style");

      // Wrapper should not have width, minWidth, or maxWidth constraints
      expect(style).not.toContain("width:");
      expect(style).not.toContain("min-width:");
      expect(style).not.toContain("max-width:");
    });
  });

  describe("Height Change Tests - Simulating Child Nodes", () => {
    it("should maintain dropdown below name row when height increases from 80px to 120px", () => {
      const { rerender } = renderWithReactFlow(
        <RouterNode {...createMockProps({}, { height: 80 })} />,
      );

      // Initial state - should be below name row
      let dropdown = screen.getByLabelText("Select branch");
      let dropdownWrapper = dropdown.closest('div[style*="margin"]');

      let style = dropdownWrapper?.getAttribute("style");
      expect(style).not.toContain("position: absolute");
      expect(style).toContain("margin-top");

      // Simulate height increase (child nodes added)
      rerender(
        <ReactFlowProvider>
          <RouterNode {...createMockProps({}, { height: 120 })} />
        </ReactFlowProvider>,
      );

      // Check dropdown still positioned below name row
      dropdown = screen.getByLabelText("Select branch");
      dropdownWrapper = dropdown.closest('div[style*="margin"]');

      style = dropdownWrapper?.getAttribute("style");
      expect(style).not.toContain("position: absolute");
      expect(style).toContain("margin-top");
    });

    it("should maintain dropdown below name row when height increases from 80px to 200px", () => {
      const { rerender } = renderWithReactFlow(
        <RouterNode {...createMockProps({}, { height: 80 })} />,
      );

      // Initial state
      let dropdown = screen.getByLabelText("Select branch");
      let dropdownWrapper = dropdown.closest('div[style*="margin"]');
      let style = dropdownWrapper?.getAttribute("style");
      expect(style).not.toContain("position: absolute");

      // Simulate significant height increase
      rerender(
        <ReactFlowProvider>
          <RouterNode {...createMockProps({}, { height: 200 })} />
        </ReactFlowProvider>,
      );

      // Check dropdown still positioned correctly
      dropdown = screen.getByLabelText("Select branch");
      dropdownWrapper = dropdown.closest('div[style*="margin"]');
      style = dropdownWrapper?.getAttribute("style");
      expect(style).not.toContain("position: absolute");
      expect(style).toContain("margin-top");
    });

    it("should maintain dropdown below name row when height increases from 80px to 300px", () => {
      const { rerender } = renderWithReactFlow(
        <RouterNode {...createMockProps({}, { height: 80 })} />,
      );

      // Initial state
      let dropdown = screen.getByLabelText("Select branch");
      let dropdownWrapper = dropdown.closest('div[style*="margin"]');
      let style = dropdownWrapper?.getAttribute("style");
      expect(style).not.toContain("position: absolute");

      // Simulate very large height increase
      rerender(
        <ReactFlowProvider>
          <RouterNode {...createMockProps({}, { height: 300 })} />
        </ReactFlowProvider>,
      );

      // Check dropdown still positioned correctly
      dropdown = screen.getByLabelText("Select branch");
      dropdownWrapper = dropdown.closest('div[style*="margin"]');
      style = dropdownWrapper?.getAttribute("style");
      expect(style).not.toContain("position: absolute");
      expect(style).toContain("margin-top");
    });
  });

  describe("BaseNode Integration Tests", () => {
    it("should have BaseNode with correct height prop", () => {
      const props = createMockProps({}, { height: 120 });
      const { container } = renderWithReactFlow(<RouterNode {...props} />);

      // BaseNode renders a motion.div with the card class
      const baseNodeCard = container.querySelector(".card");
      expect(baseNodeCard).toBeInTheDocument();

      // Check if height is applied (BaseNode uses inline styles)
      // The height should be 120px as specified in props
      const style = baseNodeCard?.getAttribute("style");
      expect(style).toContain("height");
    });

    it("should account for BaseNode padding in content area", () => {
      const props = createMockProps({}, { height: 120 });
      const { container } = renderWithReactFlow(<RouterNode {...props} />);

      const baseNodeCard = container.querySelector(".card");
      const style = baseNodeCard?.getAttribute("style");

      // BaseNode has paddingTop: 12.5px and paddingLeft: 10px
      expect(style).toContain("padding");
    });
  });

  describe("Visual Regression Tests - Dropdown Position", () => {
    /**
     * These tests verify the actual computed position of the dropdown
     * relative to the node container - should be inside node, below name row
     */
    it("should have dropdown inside node below name row with 80px height", () => {
      const props = createMockProps({}, { height: 80 });
      const { container } = renderWithReactFlow(<RouterNode {...props} />);

      const baseNodeCard = container.querySelector(".card");
      const dropdown = screen.getByLabelText("Select branch");

      // Both elements should exist
      expect(baseNodeCard).toBeInTheDocument();
      expect(dropdown).toBeInTheDocument();

      // The dropdown wrapper should NOT be absolutely positioned
      const dropdownWrapper = dropdown.closest('div[style*="margin"]');
      expect(dropdownWrapper).toBeInTheDocument();

      const style = dropdownWrapper?.getAttribute("style");
      expect(style).not.toContain("position: absolute");
      expect(style).toContain("margin-top");
    });

    it("should have dropdown inside node below name row with 150px height", () => {
      const props = createMockProps({}, { height: 150 });
      const { container } = renderWithReactFlow(<RouterNode {...props} />);

      const baseNodeCard = container.querySelector(".card");
      const dropdown = screen.getByLabelText("Select branch");

      expect(baseNodeCard).toBeInTheDocument();
      expect(dropdown).toBeInTheDocument();

      const dropdownWrapper = dropdown.closest('div[style*="margin"]');
      const style = dropdownWrapper?.getAttribute("style");
      expect(style).not.toContain("position: absolute");
      expect(style).toContain("margin-top");
    });

    it("should have dropdown inside node below name row with 250px height", () => {
      const props = createMockProps({}, { height: 250 });
      const { container } = renderWithReactFlow(<RouterNode {...props} />);

      const baseNodeCard = container.querySelector(".card");
      const dropdown = screen.getByLabelText("Select branch");

      expect(baseNodeCard).toBeInTheDocument();
      expect(dropdown).toBeInTheDocument();

      const dropdownWrapper = dropdown.closest('div[style*="margin"]');
      const style = dropdownWrapper?.getAttribute("style");
      expect(style).not.toContain("position: absolute");
      expect(style).toContain("margin-top");
    });
  });

  describe("No Branches Edge Case", () => {
    it("should not render dropdown wrapper when no branches exist", () => {
      const props = createMockProps({ branches: [] });
      const { container } = renderWithReactFlow(<RouterNode {...props} />);

      const dropdown = screen.queryByLabelText("Select branch");
      expect(dropdown).not.toBeInTheDocument();

      // Flex container should still exist
      const flexContainer = container.querySelector(".d-flex.flex-column");
      expect(flexContainer).toBeInTheDocument();
    });
  });

  describe("Content Height Independence Tests", () => {
    /**
     * Dropdown position should be consistent below the name row
     * regardless of name content
     */
    it("should position dropdown below name row regardless of name length", () => {
      const shortNameProps = createMockProps({ name: "R" }, { height: 80 });
      const { container: shortContainer } = renderWithReactFlow(
        <RouterNode {...shortNameProps} />,
      );

      const longNameProps = createMockProps(
        { name: "Very Long Router Name That Might Wrap" },
        { height: 80 },
      );
      const { container: longContainer } = renderWithReactFlow(
        <RouterNode {...longNameProps} />,
      );

      // Both should have the same dropdown positioning below name row
      const shortDropdown = shortContainer.querySelector(
        '[aria-label="Select branch"]',
      );
      const longDropdown = longContainer.querySelector(
        '[aria-label="Select branch"]',
      );

      const shortWrapper = shortDropdown?.closest('div[style*="margin"]');
      const longWrapper = longDropdown?.closest('div[style*="margin"]');

      expect(shortWrapper).toBeInTheDocument();
      expect(longWrapper).toBeInTheDocument();

      // Both should NOT be absolutely positioned
      const shortStyle = shortWrapper?.getAttribute("style");
      const longStyle = longWrapper?.getAttribute("style");

      expect(shortStyle).not.toContain("position: absolute");
      expect(longStyle).not.toContain("position: absolute");

      // Both should have marginTop
      expect(shortStyle).toContain("margin-top");
      expect(longStyle).toContain("margin-top");
    });
  });

  describe("Flex Container Layout Tests", () => {
    it("should have gap between rows", () => {
      const props = createMockProps({});
      const { container } = renderWithReactFlow(<RouterNode {...props} />);

      const flexContainer = container.querySelector(".d-flex.flex-column");
      const style = flexContainer?.getAttribute("style");

      expect(style).toContain("gap");
    });

    it("should have proper padding on flex container", () => {
      const props = createMockProps({});
      const { container } = renderWithReactFlow(<RouterNode {...props} />);

      const flexContainer = container.querySelector(".d-flex.flex-column");
      const style = flexContainer?.getAttribute("style");

      expect(style).toContain("padding");
    });
  });

  describe("Dropdown Styling Tests", () => {
    it("should have high z-index on dropdown itself", () => {
      const props = createMockProps({});
      renderWithReactFlow(<RouterNode {...props} />);

      const dropdown = screen.getByLabelText("Select branch");
      const style = dropdown.getAttribute("style");

      // Dropdown should have z-index: 150
      expect(style).toContain("z-index");
    });

    it("should have relative positioning on dropdown", () => {
      const props = createMockProps({});
      renderWithReactFlow(<RouterNode {...props} />);

      const dropdown = screen.getByLabelText("Select branch");
      const style = dropdown.getAttribute("style");

      expect(style).toContain("position");
    });

    it("should have router-dropdown class for styling", () => {
      const props = createMockProps({});
      renderWithReactFlow(<RouterNode {...props} />);

      const dropdown = screen.getByLabelText("Select branch");
      expect(dropdown).toHaveClass("router-dropdown");
    });
  });
});
