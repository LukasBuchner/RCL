import i18n from "i18next";
import { initReactI18next } from "react-i18next";
import LanguageDetector from "i18next-browser-languagedetector";

// Import translation files
import enTranslations from "./locales/en.json";
import deTranslations from "./locales/de.json";
import plTranslations from "./locales/pl.json";
import esTranslations from "./locales/es.json";

const resources = {
  en: {
    translation: enTranslations,
  },
  de: {
    translation: deTranslations,
  },
  pl: {
    translation: plTranslations,
  },
  es: {
    translation: esTranslations,
  },
};

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources,
    fallbackLng: "en",
    debug: process.env.NODE_ENV === "development",

    interpolation: {
      escapeValue: false, // React already escapes by default
    },

    detection: {
      order: ["localStorage", "navigator", "htmlTag"],
      caches: ["localStorage"],
    },
  });

export default i18n;
