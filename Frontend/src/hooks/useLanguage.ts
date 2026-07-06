import { useEffect } from "react";
import { useTranslation } from "react-i18next";
import { useSettingsStore } from "../stores/settingsStore";
import type { GeneralSettings } from "../types/settings";

const supportedLanguages: GeneralSettings["language"][] = [
  "en",
  "de",
  "pl",
  "es",
];

function isSupportedLanguage(
  lang: string,
): lang is GeneralSettings["language"] {
  return (supportedLanguages as string[]).includes(lang);
}

export function useLanguage() {
  const { i18n } = useTranslation();
  const { settings, updateGeneralSettings } = useSettingsStore();

  // Update i18n when settings change
  useEffect(() => {
    if (settings.general.language !== i18n.language) {
      i18n.changeLanguage(settings.general.language);
    }
  }, [settings.general.language, i18n]);

  // Update settings when i18n language changes (e.g., from language detector)
  useEffect(() => {
    if (i18n.language !== settings.general.language) {
      const languageCode = isSupportedLanguage(i18n.language)
        ? i18n.language
        : "en";
      updateGeneralSettings({ language: languageCode });
    }
  }, [i18n.language, settings.general.language, updateGeneralSettings]);

  const changeLanguage = (language: string) => {
    const languageCode = isSupportedLanguage(language) ? language : "en";
    updateGeneralSettings({ language: languageCode });
  };

  return {
    currentLanguage: settings.general.language,
    changeLanguage,
    availableLanguages: supportedLanguages.map((code) => ({
      code,
      name: code,
    })),
  };
}
