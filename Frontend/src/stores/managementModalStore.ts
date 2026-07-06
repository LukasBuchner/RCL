import { create } from "zustand";

/** Tracks whether a management modal (create/edit) is currently visible. */
interface ManagementModalState {
  /** `true` when a management modal should be displayed. */
  isOpen: boolean;
  /** Show the management modal. */
  open: () => void;
  /** Hide the management modal. */
  close: () => void;
}

export const useManagementModalStore = create<ManagementModalState>((set) => ({
  isOpen: false,
  open: () => set({ isOpen: true }),
  close: () => set({ isOpen: false }),
}));
