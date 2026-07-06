import { render, screen, waitFor } from "../../../test/test-utils";
import userEvent from "@testing-library/user-event";
import VariableManager from "../VariableManager";
import {
  GetProcedureVariablesDocument,
  RemoveProcedureVariableDocument,
} from "../../../graphql/variables";

const mockProcedureId = "12345678-1234-1234-1234-123456789012";

// GraphQL response must use `procedureById` key and union type format for `type`
const mockProcedureData = {
  procedureById: {
    id: mockProcedureId,
    name: "Test Procedure",
    variables: [
      {
        name: "counter",
        type: { __typename: "NumberType", typeName: "Number" },
        defaultValue: "0",
        scope: "PROCEDURE",
        source: "USER_DEFINED",
        description: "A counter variable",
        isReadOnly: false,
      },
      {
        name: "status",
        type: { __typename: "StringType", typeName: "String" },
        defaultValue: null,
        scope: "PROCEDURE",
        source: "USER_DEFINED",
        description: null,
        isReadOnly: false,
      },
    ],
  },
};

describe("VariableManager", () => {
  it("renders with procedure ID prop", () => {
    const mocks = [
      {
        request: {
          query: GetProcedureVariablesDocument,
          variables: { procedureId: mockProcedureId },
        },
        result: {
          data: mockProcedureData,
        },
      },
    ];

    render(<VariableManager procedureId={mockProcedureId} />, { mocks });

    // Heading text is "Variables" (exact match to avoid matching "Add Variable" button)
    expect(screen.getByText("Variables")).toBeInTheDocument();
  });

  it("shows skeleton loading state while fetching variables", () => {
    const mocks = [
      {
        request: {
          query: GetProcedureVariablesDocument,
          variables: { procedureId: mockProcedureId },
        },
        result: {
          data: mockProcedureData,
        },
        delay: 100,
      },
    ];

    const { container } = render(
      <VariableManager procedureId={mockProcedureId} />,
      { mocks },
    );

    // ManagementContainer shows skeleton shimmer cards during loading
    expect(container.querySelector(".shimmer")).toBeInTheDocument();
  });

  it("displays error state if query fails", async () => {
    const mocks = [
      {
        request: {
          query: GetProcedureVariablesDocument,
          variables: { procedureId: mockProcedureId },
        },
        error: new Error("Failed to fetch"),
      },
    ];

    render(<VariableManager procedureId={mockProcedureId} />, { mocks });

    await waitFor(() => {
      expect(screen.getByText(/failed to load/i)).toBeInTheDocument();
    });
  });

  it("shows VariableList with fetched variables", async () => {
    const mocks = [
      {
        request: {
          query: GetProcedureVariablesDocument,
          variables: { procedureId: mockProcedureId },
        },
        result: {
          data: mockProcedureData,
        },
      },
    ];

    render(<VariableManager procedureId={mockProcedureId} />, { mocks });

    await waitFor(() => {
      expect(screen.getByText("counter")).toBeInTheDocument();
      expect(screen.getByText("status")).toBeInTheDocument();
    });
  });

  it("opens VariableEditor when Add Variable is clicked", async () => {
    const user = userEvent.setup();
    const mocks = [
      {
        request: {
          query: GetProcedureVariablesDocument,
          variables: { procedureId: mockProcedureId },
        },
        result: {
          data: mockProcedureData,
        },
      },
    ];

    render(<VariableManager procedureId={mockProcedureId} />, { mocks });

    await waitFor(() => {
      expect(screen.getByText("counter")).toBeInTheDocument();
    });

    const addButton = screen.getByRole("button", { name: /add variable/i });
    await user.click(addButton);

    await waitFor(() => {
      // VariableEditor modal is open when the name input is visible
      expect(screen.getByLabelText(/^name/i)).toBeInTheDocument();
    });
  });

  it("opens VariableEditor in edit mode when edit button clicked", async () => {
    const user = userEvent.setup();
    const mocks = [
      {
        request: {
          query: GetProcedureVariablesDocument,
          variables: { procedureId: mockProcedureId },
        },
        result: {
          data: mockProcedureData,
        },
      },
    ];

    render(<VariableManager procedureId={mockProcedureId} />, { mocks });

    await waitFor(() => {
      expect(screen.getByText("counter")).toBeInTheDocument();
    });

    const editButton = screen.getByRole("button", { name: /edit counter/i });
    await user.click(editButton);

    await waitFor(() => {
      expect(screen.getByText(/edit variable/i)).toBeInTheDocument();
      const nameInput = screen.getByLabelText(/^name/i) as HTMLInputElement;
      expect(nameInput.value).toBe("counter");
      expect(nameInput).toBeDisabled();
    });
  });

  it("calls removeProcedureVariable mutation when deleting", async () => {
    const user = userEvent.setup();

    const mocks = [
      {
        request: {
          query: GetProcedureVariablesDocument,
          variables: { procedureId: mockProcedureId },
        },
        result: {
          data: mockProcedureData,
        },
      },
      {
        request: {
          query: RemoveProcedureVariableDocument,
          variables: {
            procedureId: mockProcedureId,
            variableName: "status",
          },
        },
        result: {
          data: {
            removeProcedureVariable: {
              id: mockProcedureId,
              name: "Test Procedure",
              variables: [mockProcedureData.procedureById.variables[0]],
            },
          },
        },
      },
      // Refetch after mutation
      {
        request: {
          query: GetProcedureVariablesDocument,
          variables: { procedureId: mockProcedureId },
        },
        result: {
          data: {
            procedureById: {
              ...mockProcedureData.procedureById,
              variables: [mockProcedureData.procedureById.variables[0]],
            },
          },
        },
      },
    ];

    render(<VariableManager procedureId={mockProcedureId} />, { mocks });

    await waitFor(() => {
      expect(screen.getByText("status")).toBeInTheDocument();
    });

    const deleteButton = screen.getByRole("button", {
      name: /delete status/i,
    });
    await user.click(deleteButton);

    // Click confirm delete in the modal
    const deleteButtons = screen.getAllByRole("button", { name: /delete/i });
    await user.click(deleteButtons[deleteButtons.length - 1]);

    await waitFor(() => {
      expect(screen.queryByText("status")).not.toBeInTheDocument();
    });
  });
});
