import { renderHook, act } from "@testing-library/react";
import React from "react";
import { MemoryRouter } from "react-router-dom";
import { useRouterModal, MODAL_CONFIGS } from "../useRouterModal";

function wrapper(initialEntries: string[]) {
  const Wrapper = ({ children }: { children: React.ReactNode }) =>
    React.createElement(MemoryRouter, { initialEntries }, children);
  Wrapper.displayName = "TestWrapper";
  return Wrapper;
}

describe("useRouterModal — AGENT config", () => {
  const config = MODAL_CONFIGS.AGENT;

  it("isModalOpen is false at /management/agents", () => {
    const { result } = renderHook(() => useRouterModal(config), {
      wrapper: wrapper(["/management/agents"]),
    });

    expect(result.current.isModalOpen).toBe(false);
  });

  it("isModalOpen is true at /management/agents/create", () => {
    const { result } = renderHook(() => useRouterModal(config), {
      wrapper: wrapper(["/management/agents/create"]),
    });

    expect(result.current.isModalOpen).toBe(true);
  });

  it("isModalOpen is true at /management/agents/:id/edit", () => {
    const { result } = renderHook(() => useRouterModal(config), {
      wrapper: wrapper(["/management/agents/abc-123/edit"]),
    });

    expect(result.current.isModalOpen).toBe(true);
  });

  it("isEditing is false at create path", () => {
    const { result } = renderHook(() => useRouterModal(config), {
      wrapper: wrapper(["/management/agents/create"]),
    });

    expect(result.current.isEditing).toBe(false);
  });

  it("isEditing is true at edit path", () => {
    const { result } = renderHook(() => useRouterModal(config), {
      wrapper: wrapper(["/management/agents/abc-123/edit"]),
    });

    expect(result.current.isEditing).toBe(true);
  });

  it("closeModal navigates to base path", () => {
    const { result } = renderHook(() => useRouterModal(config), {
      wrapper: wrapper(["/management/agents/create"]),
    });

    act(() => {
      result.current.closeModal();
    });

    // After navigation, isModalOpen should become false
    expect(result.current.isModalOpen).toBe(false);
  });
});

describe("useRouterModal — POSITION_TAG config", () => {
  const config = MODAL_CONFIGS.POSITION_TAG;

  it("isModalOpen is false at /management/position-tags", () => {
    const { result } = renderHook(() => useRouterModal(config), {
      wrapper: wrapper(["/management/position-tags"]),
    });

    expect(result.current.isModalOpen).toBe(false);
  });

  it("isModalOpen is true at /management/position-tags/create", () => {
    const { result } = renderHook(() => useRouterModal(config), {
      wrapper: wrapper(["/management/position-tags/create"]),
    });

    expect(result.current.isModalOpen).toBe(true);
  });

  it("closeModal navigates to base path", () => {
    const { result } = renderHook(() => useRouterModal(config), {
      wrapper: wrapper(["/management/position-tags/create"]),
    });

    act(() => {
      result.current.closeModal();
    });

    expect(result.current.isModalOpen).toBe(false);
  });
});

describe("useRouterModal — SKILL_CONFIG (root-based, working reference)", () => {
  const config = MODAL_CONFIGS.SKILL_CONFIG;

  it("isModalOpen is false at /", () => {
    const { result } = renderHook(() => useRouterModal(config), {
      wrapper: wrapper(["/"]),
    });

    expect(result.current.isModalOpen).toBe(false);
  });

  it("isModalOpen is true at /skill/create", () => {
    const { result } = renderHook(() => useRouterModal(config), {
      wrapper: wrapper(["/skill/create"]),
    });

    expect(result.current.isModalOpen).toBe(true);
  });

  it("closeModal navigates to /", () => {
    const { result } = renderHook(() => useRouterModal(config), {
      wrapper: wrapper(["/skill/create"]),
    });

    act(() => {
      result.current.closeModal();
    });

    expect(result.current.isModalOpen).toBe(false);
  });
});
