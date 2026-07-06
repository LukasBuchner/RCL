import { useEffect, useState } from "react";
import { useSettingsStore } from "../stores/settingsStore";

/**
 * Hook to manage theme application including system preference detection
 * Follows Single Responsibility Principle - only handles theme logic
 */
export const useTheme = () => {
  const { settings } = useSettingsStore();
  const { theme } = settings.appearance;

  // Track system preference
  const [systemPrefersDark, setSystemPrefersDark] = useState(() => {
    return window.matchMedia("(prefers-color-scheme: dark)").matches;
  });

  // Listen for system theme changes
  useEffect(() => {
    const mediaQuery = window.matchMedia("(prefers-color-scheme: dark)");

    const handleChange = (e: MediaQueryListEvent) => {
      setSystemPrefersDark(e.matches);
    };

    // Add event listener
    mediaQuery.addEventListener("change", handleChange);

    return () => {
      mediaQuery.removeEventListener("change", handleChange);
    };
  }, []);

  // Determine effective theme
  const effectiveTheme =
    theme === "auto" ? (systemPrefersDark ? "dark" : "light") : theme;

  // Apply theme to document
  useEffect(() => {
    // Apply Bootstrap theme
    document.documentElement.setAttribute("data-bs-theme", effectiveTheme);

    // Apply custom theme attribute for our CSS
    document.documentElement.setAttribute("data-theme", effectiveTheme);

    // Update meta theme-color for mobile browsers
    const metaThemeColor = document.querySelector('meta[name="theme-color"]');
    if (metaThemeColor) {
      const bgColor = getComputedStyle(document.documentElement)
        .getPropertyValue("--app-bg")
        .trim();
      metaThemeColor.setAttribute("content", bgColor);
    } else {
      const meta = document.createElement("meta");
      meta.name = "theme-color";
      const bgColor = getComputedStyle(document.documentElement)
        .getPropertyValue("--app-bg")
        .trim();
      meta.content = bgColor;
      document.head.appendChild(meta);
    }
  }, [effectiveTheme]);

  return {
    theme: effectiveTheme,
    systemPrefersDark,
    isAuto: theme === "auto",
  };
};
