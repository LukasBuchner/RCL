import { create } from "zustand";
import { persist } from "zustand/middleware";
import { AppSettings, defaultSettings } from "../types/settings";
import { createLogger } from "../utils/logger";

const log = createLogger("Settings");

interface SettingsState {
  settings: AppSettings;
  updateSettings: (updates: Partial<AppSettings>) => void;
  updateGraphQLSettings: (updates: Partial<AppSettings["graphql"]>) => void;
  updateAppearanceSettings: (
    updates: Partial<AppSettings["appearance"]>,
  ) => void;
  updateGeneralSettings: (updates: Partial<AppSettings["general"]>) => void;
  resetSettings: () => void;
  exportSettings: () => string;
  importSettings: (settingsJson: string) => boolean;
}

export const useSettingsStore = create<SettingsState>()(
  persist(
    (set, get) => ({
      settings: defaultSettings,

      updateSettings: (updates) => {
        set((state) => ({
          settings: { ...state.settings, ...updates },
        }));
      },

      updateGraphQLSettings: (updates) => {
        set((state) => ({
          settings: {
            ...state.settings,
            graphql: { ...state.settings.graphql, ...updates },
          },
        }));
      },

      updateAppearanceSettings: (updates) => {
        set((state) => ({
          settings: {
            ...state.settings,
            appearance: { ...state.settings.appearance, ...updates },
          },
        }));
      },

      updateGeneralSettings: (updates) => {
        set((state) => ({
          settings: {
            ...state.settings,
            general: { ...state.settings.general, ...updates },
          },
        }));
      },

      resetSettings: () => {
        set({ settings: defaultSettings });
      },

      exportSettings: () => {
        return JSON.stringify(get().settings, null, 2);
      },

      importSettings: (settingsJson) => {
        try {
          const parsed = JSON.parse(settingsJson);
          set({ settings: { ...defaultSettings, ...parsed } });
          return true;
        } catch (error) {
          log.error("Failed to import settings:", error);
          return false;
        }
      },
    }),
    {
      name: "app-settings",
      version: 1,
    },
  ),
);
