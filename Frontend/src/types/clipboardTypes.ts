// Define the types for our clipboard state
import { NodeFieldsFragment } from "../__generated__/graphql";

export type ClipboardData = {
  node: NodeFieldsFragment;
  isCut: boolean;
};

// Define the clipboard store state
export type ClipboardState = {
  // Clipboard data
  clipboardData: ClipboardData | null;

  // Functions to manipulate the clipboard
  copyNode: (node: NodeFieldsFragment) => void;
  cutNode: (node: NodeFieldsFragment) => void;
  clearClipboard: () => void;

  // Function to check if there's data in the clipboard
  hasClipboardData: () => boolean;
};
