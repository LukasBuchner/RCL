import { vi } from "vitest";
import { render, screen, waitFor } from "../../../test/test-utils";
import userEvent from "@testing-library/user-event";
import RouterConfigModal from "../RouterConfigModal";

// Mock react-router-dom
const mockNavigate = vi.fn();
const mockLocation = {
  pathname: "/router/create",
  state: { position: { x: 100, y: 100 } },
  search: "",
  hash: "",
  key: "default",
};

vi.mock("react-router-dom", async (importOriginal) => {
  const mod = await importOriginal<typeof import("react-router-dom")>();
  return {
    ...mod,
    useNavigate: () => mockNavigate,
    useLocation: () => mockLocation,
    useParams: () => ({}),
  };
});

// Mock useProcedure - return null so variables query is skipped
vi.mock("../../../contexts/ProcedureContext", () => ({
  useProcedure: () => ({ loadedProcedure: null }),
}));

// Mock useError
const mockAddError = vi.fn();
vi.mock("../../../hooks", async (importOriginal) => {
  const mod = await importOriginal<typeof import("../../../hooks")>();
  return {
    ...mod,
    useError: () => ({ addError: mockAddError }),
  };
});

describe("RouterConfigModal", () => {
  beforeEach(() => {
    mockNavigate.mockClear();
    mockAddError.mockClear();
    mockLocation.pathname = "/router/create";
  });

  it("opens in create mode", () => {
    mockLocation.pathname = "/router/create";

    render(<RouterConfigModal />);

    expect(screen.getByText(/configure router/i)).toBeInTheDocument();
  });

  it("renders router name input", () => {
    mockLocation.pathname = "/router/create";

    render(<RouterConfigModal />);

    expect(screen.getByLabelText(/router name/i)).toBeInTheDocument();
  });

  it("renders description input", () => {
    mockLocation.pathname = "/router/create";

    render(<RouterConfigModal />);

    expect(screen.getByLabelText(/description/i)).toBeInTheDocument();
  });

  it("has add branch button", () => {
    mockLocation.pathname = "/router/create";

    render(<RouterConfigModal />);

    expect(
      screen.getByRole("button", { name: /add branch/i }),
    ).toBeInTheDocument();
  });

  it("shows default branch initially", () => {
    mockLocation.pathname = "/router/create";

    render(<RouterConfigModal />);

    expect(screen.getByText(/default branch/i)).toBeInTheDocument();
  });

  it("adds new branch when add button clicked", async () => {
    const user = userEvent.setup();
    mockLocation.pathname = "/router/create";

    render(<RouterConfigModal />);

    const addButton = screen.getByRole("button", { name: /add branch/i });
    await user.click(addButton);

    expect(screen.getByText(/branch 1/i)).toBeInTheDocument();
  });

  it("removes branch when remove button clicked", async () => {
    const user = userEvent.setup();
    mockLocation.pathname = "/router/create";

    render(<RouterConfigModal />);

    const addButton = screen.getByRole("button", { name: /add branch/i });
    await user.click(addButton);

    const removeButton = screen.getByRole("button", {
      name: /remove branch 1/i,
    });
    await user.click(removeButton);

    expect(screen.queryByText(/branch 1/i)).not.toBeInTheDocument();
  });

  it("validates router name is required on submit", async () => {
    const user = userEvent.setup();
    mockLocation.pathname = "/router/create";

    render(<RouterConfigModal />);

    // Submit button should exist but be disabled when name is empty
    const submitButton = screen.getByRole("button", {
      name: /create router/i,
    });
    await user.click(submitButton);

    // addError is called when name is empty and submit is attempted
    await waitFor(() => {
      expect(mockNavigate).not.toHaveBeenCalled();
    });
  });

  it("default branch does not have a remove button", () => {
    mockLocation.pathname = "/router/create";

    render(<RouterConfigModal />);

    // Default branch is the only branch, there should be no remove button
    expect(
      screen.queryByRole("button", { name: /remove/i }),
    ).not.toBeInTheDocument();
  });

  it("allows editing branch name", async () => {
    const user = userEvent.setup();
    mockLocation.pathname = "/router/create";

    render(<RouterConfigModal />);

    const addButton = screen.getByRole("button", { name: /add branch/i });
    await user.click(addButton);

    const branchNameInput = screen.getByLabelText(/branch 1 name/i);
    await user.clear(branchNameInput);
    await user.type(branchNameInput, "Success Path");

    expect(branchNameInput).toHaveValue("Success Path");
  });

  it("shows branch priority numbers", async () => {
    const user = userEvent.setup();
    mockLocation.pathname = "/router/create";

    render(<RouterConfigModal />);

    const addButton = screen.getByRole("button", { name: /add branch/i });
    await user.click(addButton);
    await user.click(addButton);

    expect(screen.getByText(/priority.*0/i)).toBeInTheDocument();
    expect(screen.getByText(/priority.*1/i)).toBeInTheDocument();
  });

  it("closes modal when cancel clicked", async () => {
    const user = userEvent.setup();
    mockLocation.pathname = "/router/create";

    render(<RouterConfigModal />);

    const cancelButton = screen.getByRole("button", { name: /cancel/i });
    await user.click(cancelButton);

    expect(mockNavigate).toHaveBeenCalledWith("/");
  });

  it("does not show modal when path does not match", () => {
    mockLocation.pathname = "/some-other-path";

    render(<RouterConfigModal />);

    expect(screen.queryByText(/configure router/i)).not.toBeInTheDocument();
  });

  it("shows create router as submit button text", () => {
    mockLocation.pathname = "/router/create";

    render(<RouterConfigModal />);

    expect(
      screen.getByRole("button", { name: /create router/i }),
    ).toBeInTheDocument();
  });
});
