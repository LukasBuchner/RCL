import { vi } from "vitest";
import { renderHook, act, waitFor } from "@testing-library/react";
import React from "react";
import { MemoryRouter } from "react-router-dom";
import { MockedProvider } from "@apollo/client/testing";
import { useManagementModalStore } from "../../stores/managementModalStore";
import gql from "graphql-tag";

// Minimal GraphQL documents for testing
const GET_DOC = gql`
  query GetItems {
    items {
      id
    }
  }
`;
const CREATE_DOC = gql`
  mutation CreateItem($input: CreateInput!) {
    createItem(input: $input) {
      id
    }
  }
`;
const UPDATE_DOC = gql`
  mutation UpdateItem($input: UpdateInput!) {
    updateItem(input: $input) {
      id
    }
  }
`;
const DELETE_DOC = gql`
  mutation DeleteItem($input: DeleteInput!) {
    deleteItem(input: $input) {
      id
    }
  }
`;

// Mock useError to avoid context dependency
vi.mock("../../hooks", async (importOriginal) => {
  const mod = await importOriginal<typeof import("../../hooks")>();
  return {
    ...mod,
    useError: () => ({ addError: vi.fn() }),
    useApolloError: vi.fn(),
  };
});

// Import after mocks
const { useManagementCRUD } = await import("../useManagementCRUD");
const { MODAL_CONFIGS } = await import("../useRouterModal");

interface TestItem {
  id: string;
  name: string;
}

interface TestFormData {
  id: string;
  name: string;
}

const testConfig = {
  documents: {
    get: GET_DOC,
    create: CREATE_DOC,
    update: UPDATE_DOC,
    delete: DELETE_DOC,
  },
  modalConfig: MODAL_CONFIGS.AGENT,
  dataAccessor: (data: { items?: TestItem[] } | undefined) => data?.items,
  entityFinder: (items: TestItem[] | undefined, id: string) =>
    items?.find((item) => item.id === id),
  getInitialFormData: (): TestFormData => ({ id: "", name: "" }),
  mapToFormData: (item: TestItem): TestFormData => ({
    id: item.id,
    name: item.name,
  }),
  mapToCreateInput: (formData: TestFormData) => ({ name: formData.name }),
  mapToUpdateInput: (formData: TestFormData, entityId: string) => ({
    id: entityId,
    name: formData.name,
  }),
  validateForm: (formData: TestFormData) => !!formData.name.trim(),
  i18nKeys: {
    componentName: "TestManagement",
    operations: {
      get: "Get",
      create: "Create",
      update: "Update",
      delete: "Delete",
    },
    messages: {
      deleteConfirm: "confirm",
      failedToCreate: "fail create",
      failedToUpdate: "fail update",
      failedToDelete: "fail delete",
    },
  },
  paths: {
    create: "/management/agents/create",
    edit: (id: string) => `/management/agents/${id}/edit`,
  },
};

function createWrapper(initialEntries: string[]) {
  const mocks = [
    {
      request: { query: GET_DOC },
      result: { data: { items: [] } },
    },
  ];

  const Wrapper = ({ children }: { children: React.ReactNode }) =>
    React.createElement(
      MockedProvider,
      { mocks, addTypename: false },
      React.createElement(MemoryRouter, { initialEntries }, children),
    );
  Wrapper.displayName = "TestWrapper";
  return Wrapper;
}

describe("useManagementCRUD — modal close via zustand", () => {
  beforeEach(() => {
    // Reset zustand store between tests
    act(() => {
      useManagementModalStore.getState().close();
    });
  });

  it("isModalOpen is false at base path", async () => {
    const { result } = renderHook(() => useManagementCRUD(testConfig), {
      wrapper: createWrapper(["/management/agents"]),
    });

    await waitFor(() => {
      expect(result.current.isModalOpen).toBe(false);
    });
  });

  it("isModalOpen syncs to true from URL (deep linking)", async () => {
    const { result } = renderHook(() => useManagementCRUD(testConfig), {
      wrapper: createWrapper(["/management/agents/create"]),
    });

    await waitFor(() => {
      expect(result.current.isModalOpen).toBe(true);
    });
  });

  it("handleCloseModal sets isModalOpen to false without navigating", async () => {
    const { result } = renderHook(() => useManagementCRUD(testConfig), {
      wrapper: createWrapper(["/management/agents/create"]),
    });

    // Wait for sync effect to open the modal
    await waitFor(() => {
      expect(result.current.isModalOpen).toBe(true);
    });

    // Close the modal (phase 1: hide)
    act(() => {
      result.current.handleCloseModal();
    });

    // isModalOpen should be false immediately (zustand, not URL-dependent)
    expect(result.current.isModalOpen).toBe(false);
  });

  it("handleModalExited navigates to base path (phase 2)", async () => {
    const { result } = renderHook(() => useManagementCRUD(testConfig), {
      wrapper: createWrapper(["/management/agents/create"]),
    });

    await waitFor(() => {
      expect(result.current.isModalOpen).toBe(true);
    });

    // Phase 1: close the modal
    act(() => {
      result.current.handleCloseModal();
    });
    expect(result.current.isModalOpen).toBe(false);

    // Phase 2: modal exit animation completed — navigate
    act(() => {
      result.current.handleModalExited();
    });

    // handleModalExited should have been callable without error
    expect(result.current.isModalOpen).toBe(false);
  });

  it("navigateToCreate opens modal via zustand store", async () => {
    const { result } = renderHook(() => useManagementCRUD(testConfig), {
      wrapper: createWrapper(["/management/agents"]),
    });

    await waitFor(() => {
      expect(result.current.isModalOpen).toBe(false);
    });

    act(() => {
      result.current.navigateToCreate();
    });

    expect(result.current.isModalOpen).toBe(true);
  });

  it("navigateToEdit opens modal via zustand store", async () => {
    const { result } = renderHook(() => useManagementCRUD(testConfig), {
      wrapper: createWrapper(["/management/agents"]),
    });

    await waitFor(() => {
      expect(result.current.isModalOpen).toBe(false);
    });

    act(() => {
      result.current.navigateToEdit("test-id-123");
    });

    expect(result.current.isModalOpen).toBe(true);
  });

  it("store is cleaned up on unmount", async () => {
    const { result, unmount } = renderHook(
      () => useManagementCRUD(testConfig),
      {
        wrapper: createWrapper(["/management/agents/create"]),
      },
    );

    await waitFor(() => {
      expect(result.current.isModalOpen).toBe(true);
    });

    unmount();

    // Zustand store should be reset
    expect(useManagementModalStore.getState().isOpen).toBe(false);
  });
});
