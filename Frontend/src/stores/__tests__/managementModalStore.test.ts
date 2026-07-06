import { act } from "@testing-library/react";
import { useManagementModalStore } from "../managementModalStore";

describe("managementModalStore", () => {
  beforeEach(() => {
    // Reset store between tests
    act(() => {
      useManagementModalStore.getState().close();
    });
  });

  it("starts closed", () => {
    expect(useManagementModalStore.getState().isOpen).toBe(false);
  });

  it("opens when open() is called", () => {
    act(() => {
      useManagementModalStore.getState().open();
    });

    expect(useManagementModalStore.getState().isOpen).toBe(true);
  });

  it("closes when close() is called", () => {
    act(() => {
      useManagementModalStore.getState().open();
    });
    expect(useManagementModalStore.getState().isOpen).toBe(true);

    act(() => {
      useManagementModalStore.getState().close();
    });
    expect(useManagementModalStore.getState().isOpen).toBe(false);
  });

  it("close is idempotent", () => {
    act(() => {
      useManagementModalStore.getState().close();
      useManagementModalStore.getState().close();
    });

    expect(useManagementModalStore.getState().isOpen).toBe(false);
  });

  it("open is idempotent", () => {
    act(() => {
      useManagementModalStore.getState().open();
      useManagementModalStore.getState().open();
    });

    expect(useManagementModalStore.getState().isOpen).toBe(true);
  });
});
