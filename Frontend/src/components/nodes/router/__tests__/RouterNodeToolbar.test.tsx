import { vi } from "vitest";
import { render, screen } from "../../../../test/test-utils";
import userEvent from "@testing-library/user-event";
import RouterNodeToolbar from "../RouterNodeToolbar";

const mockNavigate = vi.fn();

vi.mock("react-router-dom", async (importOriginal) => {
  const mod = await importOriginal<typeof import("react-router-dom")>();
  return {
    ...mod,
    useNavigate: () => mockNavigate,
  };
});

describe("RouterNodeToolbar", () => {
  beforeEach(() => {
    mockNavigate.mockClear();
  });

  it("renders edit button", () => {
    render(<RouterNodeToolbar nodeId="router-1" />);

    expect(screen.getByRole("button", { name: /edit/i })).toBeInTheDocument();
  });

  it("renders delete button", () => {
    render(<RouterNodeToolbar nodeId="router-1" />);

    expect(screen.getByRole("button", { name: /delete/i })).toBeInTheDocument();
  });

  it("navigates to edit modal when edit clicked", async () => {
    const user = userEvent.setup();

    render(<RouterNodeToolbar nodeId="router-1" />);

    const editButton = screen.getByRole("button", { name: /edit/i });
    await user.click(editButton);

    expect(mockNavigate).toHaveBeenCalledWith("/router/router-1/edit");
  });

  it("shows copy button", () => {
    render(<RouterNodeToolbar nodeId="router-1" />);

    expect(screen.getByRole("button", { name: /copy/i })).toBeInTheDocument();
  });

  it("shows cut button", () => {
    render(<RouterNodeToolbar nodeId="router-1" />);

    expect(screen.getByRole("button", { name: /cut/i })).toBeInTheDocument();
  });
});
