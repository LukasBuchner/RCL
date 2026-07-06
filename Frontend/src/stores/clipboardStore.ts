import { create } from "zustand";
import { ClipboardState } from "../types/clipboardTypes";
import { NodeFieldsFragment } from "../__generated__/graphql";

// Create the clipboard store
export const useClipboardStore = create<ClipboardState>((set, get) => ({
  // Clipboard data
  clipboardData: null,

  // Functions to manipulate the clipboard
  copyNode: (node: NodeFieldsFragment) =>
    set({
      clipboardData: {
        node,
        isCut: false,
      },
    }),

  cutNode: (node: NodeFieldsFragment) =>
    set({
      clipboardData: {
        node,
        isCut: true,
      },
    }),

  clearClipboard: () => set({ clipboardData: null }),

  // Function to check if there's data in the clipboard
  hasClipboardData: () => get().clipboardData !== null,
}));
