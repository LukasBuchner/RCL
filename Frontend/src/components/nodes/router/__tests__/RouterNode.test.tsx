import { render, screen } from "../../../../test/test-utils";
import RouterNode from "../RouterNode";
import { Node, NodeProps, ReactFlowProvider } from "@xyflow/react";
import { RouterBasicData } from "../../../../types/nodeTypes";

type RouterNodeType = Node<RouterBasicData, "routerNode">;

const createMockProps = (
  data: Partial<RouterBasicData>,
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
  height: 80,
  width: 200,
  parentId: undefined,
  sourcePosition: undefined,
  targetPosition: undefined,
  dragHandle: undefined,
});

const renderWithReactFlow = (ui: React.ReactElement) => {
  return render(<ReactFlowProvider>{ui}</ReactFlowProvider>);
};

describe("RouterNode", () => {
  it("renders router name", () => {
    const props = createMockProps({});

    renderWithReactFlow(<RouterNode {...props} />);

    expect(screen.getAllByText("Status Router")[0]).toBeInTheDocument();
  });

  it("shows router icon", () => {
    const props = createMockProps({});

    renderWithReactFlow(<RouterNode {...props} />);

    const icon = screen.getByRole("img", { hidden: true });
    expect(icon).toHaveClass("bi-signpost-split");
  });

  it("shows branch count in title attribute", () => {
    const props = createMockProps({});

    renderWithReactFlow(<RouterNode {...props} />);

    // Branch count is shown as a title attribute, not as visible text
    const element = screen.getByTitle(/2 branches configured/i);
    expect(element).toBeInTheDocument();
  });

  it("displays branch selection dropdown when branches exist", () => {
    const props = createMockProps({});

    renderWithReactFlow(<RouterNode {...props} />);

    const dropdown = screen.getByLabelText("Select branch");
    expect(dropdown).toBeInTheDocument();
    expect(dropdown).toHaveValue("");
  });

  it("shows all branches in the dropdown", () => {
    const props = createMockProps({});

    renderWithReactFlow(<RouterNode {...props} />);

    expect(screen.getByText('Success (== "success")')).toBeInTheDocument();
    expect(screen.getByText("Default (default)")).toBeInTheDocument();
  });

  it("sets dropdown value when branch is selected", () => {
    const props = createMockProps({
      selectedBranch: "Success",
    });

    renderWithReactFlow(<RouterNode {...props} />);

    const dropdown = screen.getByLabelText("Select branch");
    expect(dropdown).toHaveValue("Success");
  });

  it("does not show selected branch badge when none is selected", () => {
    const props = createMockProps({
      selectedBranch: undefined,
    });

    renderWithReactFlow(<RouterNode {...props} />);

    expect(screen.queryByText(/current:/i)).not.toBeInTheDocument();
  });

  it("does not show dropdown when branches array is empty", () => {
    const props = createMockProps({
      branches: [],
    });

    renderWithReactFlow(<RouterNode {...props} />);

    expect(screen.queryByLabelText("Select branch")).not.toBeInTheDocument();
  });

  it("formats branch labels correctly for default branch", () => {
    const props = createMockProps({
      branches: [
        {
          name: "Default",
          condition: "",
          priority: 999,
          targetNodeId: "node-2",
        },
      ],
    });

    renderWithReactFlow(<RouterNode {...props} />);

    expect(screen.getByText("Default (default)")).toBeInTheDocument();
  });

  it("formats branch labels correctly with condition", () => {
    const props = createMockProps({
      branches: [
        {
          name: "Error",
          condition: '== "error"',
          priority: 1,
          targetNodeId: "node-3",
        },
      ],
    });

    renderWithReactFlow(<RouterNode {...props} />);

    expect(screen.getByText('Error (== "error")')).toBeInTheDocument();
  });

  it("formats branch labels correctly without condition", () => {
    const props = createMockProps({
      branches: [
        {
          name: "Branch1",
          priority: 0,
          targetNodeId: "node-1",
        },
      ],
    });

    renderWithReactFlow(<RouterNode {...props} />);

    expect(screen.getByText("Branch1")).toBeInTheDocument();
  });
});
