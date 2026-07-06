import { RefObject, useEffect } from "react";
import { createLogger } from "../utils/logger";

const log = createLogger("Tooltips");

// Local type definitions for Bootstrap tooltips to avoid global conflicts
interface BootstrapTooltipInstance {
  dispose: () => void;
  hide: () => void;
  show: () => void;
  toggle: () => void;
}

interface BootstrapTooltipConstructor {
  new (
    element: Element,
    options?: Record<string, unknown>,
  ): BootstrapTooltipInstance;
  getInstance(element: Element): BootstrapTooltipInstance | null;
}

interface BootstrapNamespace {
  Tooltip?: BootstrapTooltipConstructor;
}

/**
 * Custom hook to initialize and manage Bootstrap tooltips within a referenced element.
 *
 * @param ref A React RefObject pointing to the container element where tooltips should be initialized.
 * @param options Optional Bootstrap Tooltip options.
 */
function useBootstrapTooltips(
  ref: RefObject<HTMLElement | null>,
  options?: Record<string, unknown>,
) {
  useEffect(() => {
    // Ensure the ref points to an element
    if (!ref.current) {
      return;
    }

    // Check if Bootstrap's Tooltip constructor is available
    const bootstrap = (window as Window & { bootstrap?: BootstrapNamespace })
      .bootstrap;
    const BSNativeTooltip = bootstrap?.Tooltip;
    if (typeof BSNativeTooltip === "undefined") {
      log.warn(
        "Bootstrap JavaScript components (Tooltip) are not loaded. Tooltips will not be initialized.",
      );
      return;
    }

    const containerElement = ref.current;
    // Select all elements with the tooltip trigger attribute within the container
    const tooltipTriggerList = containerElement.querySelectorAll<HTMLElement>(
      '[data-bs-toggle="tooltip"]',
    );

    // Initialize tooltips for each trigger element
    const tooltipInstances = [...tooltipTriggerList].map((tooltipTriggerEl) => {
      // Check if a tooltip instance already exists for this element
      let tooltipInstance = BSNativeTooltip.getInstance(tooltipTriggerEl);
      if (!tooltipInstance) {
        // Create a new instance if one doesn't exist
        tooltipInstance = new BSNativeTooltip(tooltipTriggerEl, options);
      }
      return tooltipInstance;
    });

    // Cleanup function: Dispose all initialized tooltips when the component unmounts
    // or when the ref.current changes (which triggers the effect cleanup before re-running)
    return () => {
      tooltipInstances.forEach((tooltip) => {
        // Ensure the instance exists and has a dispose method before calling
        if (tooltip && typeof tooltip.dispose === "function") {
          try {
            tooltip.dispose();
          } catch (error) {
            // Handle potential errors during disposal if needed
            log.error("Error disposing tooltip:", error);
          }
        }
      });
    };
    // Dependency array: Re-run the effect if the referenced element changes,
    // or if the options object changes identity.
  }, [ref, options]); // Include options in dependency array if they might change
}

export default useBootstrapTooltips;
