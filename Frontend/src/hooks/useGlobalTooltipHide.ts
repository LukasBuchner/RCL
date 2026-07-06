import { useEffect } from "react";

/**
 * Type definition for Bootstrap tooltip instance
 */
interface BootstrapTooltipInstance {
  hide: () => void;
  show: () => void;
  dispose: () => void;
  toggle: () => void;
}

/**
 * Type definition for Bootstrap global object
 */
interface BootstrapGlobal {
  Tooltip?: {
    getInstance: (element: Element) => BootstrapTooltipInstance | null;
  };
}

/**
 * Hook that manages global tooltip hiding behavior.
 * Automatically hides all Bootstrap tooltips when clicking anywhere on the page.
 *
 * This follows the Single Responsibility Principle by handling only tooltip hiding logic.
 */
export function useGlobalTooltipHide() {
  useEffect(() => {
    const hideAllTooltips = () => {
      const bootstrap = (window as Window & { bootstrap?: BootstrapGlobal })
        .bootstrap;
      if (!bootstrap?.Tooltip) return;

      const { Tooltip } = bootstrap;

      // Find all tooltip triggers and hide their tooltips
      document
        .querySelectorAll('[data-bs-toggle="tooltip"]')
        .forEach((element) => {
          const tooltipInstance = Tooltip.getInstance(element);
          if (tooltipInstance) {
            tooltipInstance.hide();
          }
        });
    };

    // Add click listener to hide tooltips
    document.addEventListener("click", hideAllTooltips);

    // Cleanup listener on unmount
    return () => {
      document.removeEventListener("click", hideAllTooltips);
    };
  }, []);
}
