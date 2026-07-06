import { useCallback, useMemo, useRef, useState } from "react";
import { FlowCommand, FlowCommandContext } from "./types";

/** Exposed state and actions of {@link useFlowUndoManager}. */
export interface FlowUndoManager {
  /** `true` when there is at least one command available to undo. */
  readonly canUndo: boolean;
  /** `true` when there is at least one command available to redo. */
  readonly canRedo: boolean;
  /** Label describing what the next `undo` call would do, or `null`. */
  readonly nextUndoDescription: string | null;
  /** Label describing what the next `redo` call would do, or `null`. */
  readonly nextRedoDescription: string | null;
  /**
   * Applies `command` to the context and pushes it onto the past stack. Clears
   * the redo stack (branching model — matches the official React Flow example,
   * Google Slides, and the browser URL bar).
   */
  dispatch(command: FlowCommand): void;
  /** Inverts and re-applies the most recent past command. No-op on empty stack. */
  undo(): void;
  /** Re-applies the most recently undone command. No-op on empty future stack. */
  redo(): void;
  /** Discards both stacks without touching local state. */
  clear(): void;
}

/** Default combined stack size. Matches the order of magnitude used by most editors. */
const DEFAULT_CAPACITY = 200;

/**
 * Branching undo/redo manager bound to a single `ctxRef` supplier. The caller
 * owns the React Flow state setters and passes them in via `ctxRef` so they
 * stay current across re-renders without forcing the hook to re-subscribe.
 *
 * Why a ref for context: the React Flow component re-creates `setNodes` /
 * `setEdges` closures on every render. If the manager captured them at hook
 * call time, every dispatch after the first render would operate on stale
 * setters. The ref indirection keeps the manager stateless w.r.t. context.
 *
 * @param ctxRef Mutable ref whose `current` carries the latest command context.
 * @param capacity Maximum combined size of past + future. Oldest past entries
 * are dropped when exceeded. Negative disables the cap.
 */
export function useFlowUndoManager(
  ctxRef: React.MutableRefObject<FlowCommandContext>,
  capacity: number = DEFAULT_CAPACITY,
): FlowUndoManager {
  const pastRef = useRef<FlowCommand[]>([]);
  const futureRef = useRef<FlowCommand[]>([]);

  const [canUndo, setCanUndo] = useState(false);
  const [canRedo, setCanRedo] = useState(false);
  const [nextUndoDescription, setNextUndoDescription] = useState<string | null>(
    null,
  );
  const [nextRedoDescription, setNextRedoDescription] = useState<string | null>(
    null,
  );

  const refresh = useCallback(() => {
    setCanUndo(pastRef.current.length > 0);
    setCanRedo(futureRef.current.length > 0);
    setNextUndoDescription(
      pastRef.current[pastRef.current.length - 1]?.description ?? null,
    );
    setNextRedoDescription(
      futureRef.current[futureRef.current.length - 1]?.description ?? null,
    );
  }, []);

  const enforceCapacity = useCallback(() => {
    if (capacity < 0) return;
    while (
      pastRef.current.length + futureRef.current.length > capacity &&
      pastRef.current.length > 0
    ) {
      pastRef.current = pastRef.current.slice(1);
    }
  }, [capacity]);

  const dispatch = useCallback(
    (command: FlowCommand) => {
      command.apply(ctxRef.current);
      pastRef.current = [...pastRef.current, command];
      futureRef.current = [];
      enforceCapacity();
      refresh();
    },
    [ctxRef, enforceCapacity, refresh],
  );

  const undo = useCallback(() => {
    const cmd = pastRef.current[pastRef.current.length - 1];
    if (!cmd) return;
    cmd.inverse().apply(ctxRef.current);
    pastRef.current = pastRef.current.slice(0, -1);
    futureRef.current = [...futureRef.current, cmd];
    refresh();
  }, [ctxRef, refresh]);

  const redo = useCallback(() => {
    const cmd = futureRef.current[futureRef.current.length - 1];
    if (!cmd) return;
    cmd.apply(ctxRef.current);
    futureRef.current = futureRef.current.slice(0, -1);
    pastRef.current = [...pastRef.current, cmd];
    refresh();
  }, [ctxRef, refresh]);

  const clear = useCallback(() => {
    pastRef.current = [];
    futureRef.current = [];
    refresh();
  }, [refresh]);

  return useMemo<FlowUndoManager>(
    () => ({
      canUndo,
      canRedo,
      nextUndoDescription,
      nextRedoDescription,
      dispatch,
      undo,
      redo,
      clear,
    }),
    [
      canUndo,
      canRedo,
      nextUndoDescription,
      nextRedoDescription,
      dispatch,
      undo,
      redo,
      clear,
    ],
  );
}
