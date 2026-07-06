import { create } from "zustand";

/** Tracks whether any node in the loaded procedure is currently executing. */
interface ExecutionState {
  /** `true` when at least one node has `isExecuting === true`. */
  isProcedureRunning: boolean;
  /** Called by Flow whenever the node list changes to recompute the flag. */
  setRunning: (running: boolean) => void;
}

export const useExecutionStore = create<ExecutionState>((set) => ({
  isProcedureRunning: false,
  setRunning: (running) => set({ isProcedureRunning: running }),
}));
