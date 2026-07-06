import { create } from "zustand";

/** Validation state derived from the live procedure validation subscription. */
interface ValidationState {
  /** IDs of skill execution nodes that have an agent serialization warning. */
  warningNodeIds: Set<string>;

  /** Replaces the current warning node ID set with a new one. */
  setWarningNodeIds: (ids: Set<string>) => void;
}

export const useValidationStore = create<ValidationState>((set) => ({
  warningNodeIds: new Set(),

  setWarningNodeIds: (ids) => set({ warningNodeIds: ids }),
}));
