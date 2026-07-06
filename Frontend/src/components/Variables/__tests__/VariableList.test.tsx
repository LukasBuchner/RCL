import { render, screen, waitFor } from "../../../test/test-utils";
import userEvent from "@testing-library/user-event";
import VariableList from "../VariableList";
import { VariableDefinition } from "../types";

describe("VariableList", () => {
  const mockOnEdit = vi.fn();
  const mockOnDelete = vi.fn();
  const mockOnAdd = vi.fn();

  beforeEach(() => {
    mockOnEdit.mockClear();
    mockOnDelete.mockClear();
    mockOnAdd.mockClear();
  });

  it("renders empty state with no variables", () => {
    const { container } = render(
      <VariableList
        variables={[]}
        onEdit={mockOnEdit}
        onDelete={mockOnDelete}
        onAdd={mockOnAdd}
      />,
    );

    // Empty grid is rendered with no variable cards
    expect(container.querySelector(".variables-grid")).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: /edit/i }),
    ).not.toBeInTheDocument();
  });

  it("renders list of variables with correct data", () => {
    const variables: VariableDefinition[] = [
      {
        name: "counter",
        type: "Number",
        defaultValue: "0",
        scope: "PROCEDURE",
        source: "USER_DEFINED",
        description: "A counter variable",
        isReadOnly: false,
      },
      {
        name: "status",
        type: "String",
        scope: "PROCEDURE",
        source: "USER_DEFINED",
        isReadOnly: false,
      },
    ];

    render(
      <VariableList
        variables={variables}
        onEdit={mockOnEdit}
        onDelete={mockOnDelete}
        onAdd={mockOnAdd}
      />,
    );

    expect(screen.getByText("counter")).toBeInTheDocument();
    expect(screen.getByText("status")).toBeInTheDocument();
    expect(screen.getByText("Number")).toBeInTheDocument();
    expect(screen.getByText("String")).toBeInTheDocument();
  });

  it("shows variable details in card layout", () => {
    const variables: VariableDefinition[] = [
      {
        name: "testVar",
        type: "Boolean",
        defaultValue: "true",
        scope: "PROCEDURE",
        source: "USER_DEFINED",
        description: "Test description",
        isReadOnly: false,
      },
    ];

    render(
      <VariableList
        variables={variables}
        onEdit={mockOnEdit}
        onDelete={mockOnDelete}
        onAdd={mockOnAdd}
      />,
    );

    expect(screen.getByText("testVar")).toBeInTheDocument();
    expect(screen.getByText("Boolean")).toBeInTheDocument();
    expect(screen.getByText("PROCEDURE")).toBeInTheDocument();
    expect(screen.getByText("USER_DEFINED")).toBeInTheDocument();
    expect(screen.getByText("true")).toBeInTheDocument();
    expect(screen.getByText("Test description")).toBeInTheDocument();
  });

  it("calls onEdit when edit button clicked", async () => {
    const user = userEvent.setup();
    const variables: VariableDefinition[] = [
      {
        name: "testVar",
        type: "String",
        scope: "PROCEDURE",
        source: "USER_DEFINED",
        isReadOnly: false,
      },
    ];

    render(
      <VariableList
        variables={variables}
        onEdit={mockOnEdit}
        onDelete={mockOnDelete}
        onAdd={mockOnAdd}
      />,
    );

    const editButton = screen.getByRole("button", { name: /edit testvar/i });
    await user.click(editButton);

    expect(mockOnEdit).toHaveBeenCalledWith(variables[0]);
  });

  it("shows confirmation dialog when delete clicked", async () => {
    const user = userEvent.setup();
    const variables: VariableDefinition[] = [
      {
        name: "testVar",
        type: "String",
        scope: "PROCEDURE",
        source: "USER_DEFINED",
        isReadOnly: false,
      },
    ];

    render(
      <VariableList
        variables={variables}
        onEdit={mockOnEdit}
        onDelete={mockOnDelete}
        onAdd={mockOnAdd}
      />,
    );

    const deleteButton = screen.getByRole("button", {
      name: /delete testvar/i,
    });
    await user.click(deleteButton);

    // Both modal title and body contain "are you sure" text
    const matches = screen.getAllByText(/are you sure/i);
    expect(matches.length).toBeGreaterThanOrEqual(1);
  });

  it("calls onDelete after confirmation", async () => {
    const user = userEvent.setup();
    const variables: VariableDefinition[] = [
      {
        name: "testVar",
        type: "String",
        scope: "PROCEDURE",
        source: "USER_DEFINED",
        isReadOnly: false,
      },
    ];

    render(
      <VariableList
        variables={variables}
        onEdit={mockOnEdit}
        onDelete={mockOnDelete}
        onAdd={mockOnAdd}
      />,
    );

    const deleteButton = screen.getByRole("button", {
      name: /delete testvar/i,
    });
    await user.click(deleteButton);

    // Click the Delete confirm button in the modal footer
    const confirmButton = screen.getAllByRole("button", {
      name: /delete/i,
    });
    // The last delete button is the confirm in the modal
    await user.click(confirmButton[confirmButton.length - 1]);

    await waitFor(() => {
      expect(mockOnDelete).toHaveBeenCalledWith("testVar");
    });
  });

  it("does not call onDelete when cancel clicked in confirmation", async () => {
    const user = userEvent.setup();
    const variables: VariableDefinition[] = [
      {
        name: "testVar",
        type: "String",
        scope: "PROCEDURE",
        source: "USER_DEFINED",
        isReadOnly: false,
      },
    ];

    render(
      <VariableList
        variables={variables}
        onEdit={mockOnEdit}
        onDelete={mockOnDelete}
        onAdd={mockOnAdd}
      />,
    );

    const deleteButton = screen.getByRole("button", {
      name: /delete testvar/i,
    });
    await user.click(deleteButton);

    const cancelButton = screen.getByRole("button", { name: /cancel/i });
    await user.click(cancelButton);

    expect(mockOnDelete).not.toHaveBeenCalled();
  });

  it("does not show default value when not set", () => {
    const variables: VariableDefinition[] = [
      {
        name: "noDefault",
        type: "String",
        scope: "PROCEDURE",
        source: "USER_DEFINED",
        isReadOnly: false,
      },
    ];

    render(
      <VariableList
        variables={variables}
        onEdit={mockOnEdit}
        onDelete={mockOnDelete}
        onAdd={mockOnAdd}
      />,
    );

    // Default value section should not be rendered when no default value
    expect(screen.queryByText(/default value/i)).not.toBeInTheDocument();
  });

  it("disables delete button for read-only variables", () => {
    const variables: VariableDefinition[] = [
      {
        name: "readOnlyVar",
        type: "String",
        scope: "PROCEDURE",
        source: "SYSTEM",
        isReadOnly: true,
      },
    ];

    render(
      <VariableList
        variables={variables}
        onEdit={mockOnEdit}
        onDelete={mockOnDelete}
        onAdd={mockOnAdd}
      />,
    );

    const deleteButton = screen.getByRole("button", {
      name: /delete readonlyvar/i,
    });
    expect(deleteButton).toBeDisabled();
  });

  it("renders multiple variables correctly", () => {
    const variables: VariableDefinition[] = [
      {
        name: "var1",
        type: "String",
        scope: "PROCEDURE",
        source: "USER_DEFINED",
        isReadOnly: false,
      },
      {
        name: "var2",
        type: "Number",
        scope: "TASK",
        source: "SKILL_OUTPUT",
        isReadOnly: false,
      },
      {
        name: "var3",
        type: "Boolean",
        scope: "GLOBAL",
        source: "AGENT_STATE",
        isReadOnly: true,
      },
    ];

    render(
      <VariableList
        variables={variables}
        onEdit={mockOnEdit}
        onDelete={mockOnDelete}
        onAdd={mockOnAdd}
      />,
    );

    expect(screen.getByText("var1")).toBeInTheDocument();
    expect(screen.getByText("var2")).toBeInTheDocument();
    expect(screen.getByText("var3")).toBeInTheDocument();

    expect(screen.getByText("TASK")).toBeInTheDocument();
    expect(screen.getByText("GLOBAL")).toBeInTheDocument();

    expect(screen.getByText("SKILL_OUTPUT")).toBeInTheDocument();
    expect(screen.getByText("AGENT_STATE")).toBeInTheDocument();
  });
});
