import React, { useState } from "react";
import { Alert, Card, Form } from "react-bootstrap";
import { useTranslation } from "react-i18next";
import { MotionCard } from "../motion/MotionCard";
import { MotionButton } from "../motion/MotionButton";
import { useSettingsStore } from "../../stores/settingsStore";
import { useLanguage } from "../../hooks/useLanguage";

export default function GeneralSettings() {
  const { t } = useTranslation();
  const { changeLanguage, availableLanguages } = useLanguage();
  const {
    settings,
    updateGeneralSettings,
    exportSettings,
    importSettings,
    resetSettings,
  } = useSettingsStore();
  const [localSettings, setLocalSettings] = useState(settings.general);
  const [hasChanges, setHasChanges] = useState(false);
  const [saveResult, setSaveResult] = useState<{
    success: boolean;
    message: string;
  } | null>(null);

  const handleInputChange = (
    field: keyof typeof localSettings,
    value: string | boolean | number,
  ) => {
    setLocalSettings((prev) => ({ ...prev, [field]: value }));
    setHasChanges(true);
    setSaveResult(null);
  };

  const handleLanguageChange = (language: string) => {
    setLocalSettings((prev) => ({
      ...prev,
      language: language as typeof prev.language,
    }));
    setHasChanges(true);
    setSaveResult(null);
  };

  const handleSave = () => {
    updateGeneralSettings(localSettings);
    // Update i18n language immediately
    if (localSettings.language !== settings.general.language) {
      changeLanguage(localSettings.language);
    }
    setHasChanges(false);
    setSaveResult({
      success: true,
      message: t("settings.settingsSavedSuccess"),
    });
  };

  const handleReset = () => {
    setLocalSettings(settings.general);
    setHasChanges(false);
    setSaveResult(null);
  };

  const handleExportSettings = () => {
    const settingsJson = exportSettings();
    const blob = new Blob([settingsJson], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = "app-settings.json";
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
    setSaveResult({
      success: true,
      message: t("settings.settingsExportedSuccess"),
    });
  };

  const handleImportSettings = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      const reader = new FileReader();
      reader.onload = (e) => {
        const content = e.target?.result as string;
        if (importSettings(content)) {
          setLocalSettings(settings.general);
          setHasChanges(false);
          setSaveResult({
            success: true,
            message: t("settings.settingsImportedSuccess"),
          });
        } else {
          setSaveResult({
            success: false,
            message: t("settings.settingsImportError"),
          });
        }
      };
      reader.readAsText(file);
    }
    // Reset the input
    event.target.value = "";
  };

  const handleResetAllSettings = () => {
    if (window.confirm(t("settings.resetAllConfirm"))) {
      resetSettings();
      setLocalSettings(settings.general);
      setHasChanges(false);
      setSaveResult({
        success: true,
        message: t("settings.settingsResetSuccess"),
      });
    }
  };

  return (
    <MotionCard
      interaction="subtle"
      className="h-100"
      style={{ borderRadius: "12px" }}
    >
      <Card className="h-100 border-0" style={{ borderRadius: "12px" }}>
        <Card.Body className="p-4">
          <h3 className="mb-3">{t("settings.generalSettings")}</h3>
          <p className="text-muted">{t("settings.generalDescription")}</p>

          <Form className="mt-4">
            <Form.Group className="mb-4">
              <Form.Check
                type="switch"
                id="enable-notifications"
                label={t("settings.enableNotifications")}
                checked={localSettings.enableNotifications}
                onChange={(e) =>
                  handleInputChange("enableNotifications", e.target.checked)
                }
              />
              <Form.Text className="text-muted">
                {t("settings.notificationsDescription")}
              </Form.Text>
            </Form.Group>

            <Form.Group className="mb-4">
              <Form.Label>{t("settings.language")}</Form.Label>
              <Form.Select
                value={localSettings.language}
                onChange={(e) => handleLanguageChange(e.target.value)}
              >
                {availableLanguages.map((lang) => (
                  <option key={lang.code} value={lang.code}>
                    {t(`languages.${lang.code}`)}
                  </option>
                ))}
              </Form.Select>
              <Form.Text className="text-muted">
                {t("settings.interfaceLanguage")}
              </Form.Text>
            </Form.Group>

            {saveResult && (
              <Alert
                variant={saveResult.success ? "success" : "danger"}
                className="mb-3"
              >
                <i
                  className={`bi ${saveResult.success ? "bi-check-circle" : "bi-exclamation-triangle"} me-2`}
                ></i>
                {saveResult.message}
              </Alert>
            )}

            <div className="d-flex gap-2 mb-4 flex-wrap">
              <MotionButton
                variant="primary"
                size="sm"
                onClick={handleSave}
                disabled={!hasChanges}
              >
                <i className="bi bi-check-lg me-1"></i>
                {t("settings.saveChanges")}
              </MotionButton>
              {hasChanges && (
                <MotionButton variant="warning" size="sm" onClick={handleReset}>
                  <i className="bi bi-arrow-counterclockwise me-1"></i>
                  {t("settings.reset")}
                </MotionButton>
              )}
            </div>

            <hr className="my-4" />

            <div className="mt-4">
              <h5>{t("settings.settingsManagement")}</h5>
              <p className="text-muted mb-3">
                {t("settings.settingsManagementDescription")}
              </p>

              <div className="d-flex gap-2 flex-wrap">
                <MotionButton
                  variant="outline-primary"
                  size="sm"
                  onClick={handleExportSettings}
                >
                  <i className="bi bi-download me-1"></i>
                  {t("settings.exportSettings")}
                </MotionButton>

                <MotionButton
                  variant="outline-primary"
                  size="sm"
                  onClick={() =>
                    document.getElementById("import-settings")?.click()
                  }
                >
                  <i className="bi bi-upload me-1"></i>
                  {t("settings.importSettings")}
                </MotionButton>

                <MotionButton
                  variant="outline-danger"
                  size="sm"
                  onClick={handleResetAllSettings}
                >
                  <i className="bi bi-arrow-counterclockwise me-1"></i>
                  {t("settings.resetAll")}
                </MotionButton>
              </div>

              <input
                id="import-settings"
                type="file"
                accept=".json"
                style={{ display: "none" }}
                onChange={handleImportSettings}
              />
            </div>
          </Form>
        </Card.Body>
      </Card>
    </MotionCard>
  );
}
