import { createContext, ReactNode, useContext } from "react";
import { FlowUndoManager } from "./useFlowUndoManager";
import { FlowPersister, NoOpFlowPersister } from "./types";

/**
 * Contents carried by {@link FlowUndoContext}: the active {@link FlowUndoManager}
 * and the {@link FlowPersister} commands should use to propagate side-effects.
 *
 * The default value pairs a throw-on-dispatch manager with {@link NoOpFlowPersister}
 * so tests that read the context without a provider fail loudly rather than
 * silently swallowing dispatches.
 */
export interface FlowUndoContextValue {
  manager: FlowUndoManager;
  persister: FlowPersister;
}

const defaultManager: FlowUndoManager = {
  canUndo: false,
  canRedo: false,
  nextUndoDescription: null,
  nextRedoDescription: null,
  dispatch: () => {
    throw new Error(
      "FlowUndoContext not provided — wrap the component tree in <FlowUndoProvider>.",
    );
  },
  undo: () => {},
  redo: () => {},
  clear: () => {},
};

const FlowUndoContext = createContext<FlowUndoContextValue>({
  manager: defaultManager,
  persister: NoOpFlowPersister,
});

/**
 * Provider that exposes the undo manager and persister to descendant components
 * — notably the config modals which are rendered inside the Flow tree but do
 * not receive props. Consumers read the value via {@link useFlowUndo}.
 */
export function FlowUndoProvider({
  manager,
  persister,
  children,
}: FlowUndoContextValue & { children: ReactNode }): JSX.Element {
  return (
    <FlowUndoContext.Provider value={{ manager, persister }}>
      {children}
    </FlowUndoContext.Provider>
  );
}

/**
 * Reads the active {@link FlowUndoContextValue}. Throws if called outside a
 * {@link FlowUndoProvider} via the context's default `dispatch` — this is an
 * explicit contract, not an accident, because a silently discarded dispatch
 * is worse than a fail-fast error.
 */
export function useFlowUndo(): FlowUndoContextValue {
  return useContext(FlowUndoContext);
}
