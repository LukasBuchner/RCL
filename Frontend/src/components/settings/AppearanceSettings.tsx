import { useCallback, useEffect, useState } from "react";
import { Alert, Card, Form } from "react-bootstrap";
import { useTranslation } from "react-i18next";
import { MotionCard } from "../motion/MotionCard";
import { MotionButton } from "../motion/MotionButton";
import { useSettingsStore } from "../../stores/settingsStore";
import { useTheme } from "../../hooks/useTheme";

export default function AppearanceSettings() {
  const { t } = useTranslation();
  const { settings, updateAppearanceSettings } = useSettingsStore();
  const { systemPrefersDark, isAuto } = useTheme();
  const [localSettings, setLocalSettings] = useState(settings.appearance);
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

  const applyAppearanceSettings = useCallback(
    (settings: typeof localSettings) => {
      // Apply reduce motion and animation speed settings
      if (settings.reduceMotion) {
        document.documentElement.style.setProperty("--motion-duration", "0.1s");
        document.documentElement.style.setProperty("--motion-distance", "2px");
      } else {
        const speedMultiplier = settings.animationSpeed;
        const baseDuration = 0.3;
        const baseDistance = 8;

        document.documentElement.style.setProperty(
          "--motion-duration",
          `${baseDuration / speedMultiplier}s`,
        );
        document.documentElement.style.setProperty(
          "--motion-distance",
          `${baseDistance * speedMultiplier}px`,
        );
      }

      // Theme is now handled by useTheme hook
    },
    [],
  );

  const handleSave = () => {
    updateAppearanceSettings(localSettings);
    setHasChanges(false);
    setSaveResult({
      success: true,
      message: t("settings.settingsSavedSuccess"),
    });

    // Apply changes immediately
    applyAppearanceSettings(localSettings);
  };

  const handleReset = () => {
    setLocalSettings(settings.appearance);
    setHasChanges(false);
    setSaveResult(null);
  };

  // Apply settings on mount and when settings change
  useEffect(() => {
    applyAppearanceSettings(settings.appearance);
  }, [settings.appearance, applyAppearanceSettings]);

  const getAnimationSpeedLabel = (speed: number) => {
    if (speed <= 0.5) return t("settings.slow");
    if (speed === 1) return t("settings.normal");
    return t("settings.fast");
  };

  return (
    <MotionCard
      interaction="subtle"
      className="h-100"
      style={{ borderRadius: "12px" }}
    >
      <Card className="h-100 border-0" style={{ borderRadius: "12px" }}>
        <Card.Body className="p-4">
          <h3 className="mb-3">{t("settings.appearanceSettings")}</h3>
          <p className="text-muted">{t("settings.appearanceDescription")}</p>

          <Form className="mt-4">
            <Form.Group className="mb-4">
              <Form.Label>{t("settings.theme")}</Form.Label>
              <div>
                <Form.Check
                  type="radio"
                  id="theme-light"
                  label={
                    <span>
                      <i className="bi bi-sun me-2"></i>
                      {t("settings.light")}
                    </span>
                  }
                  name="theme"
                  checked={localSettings.theme === "light"}
                  onChange={() => handleInputChange("theme", "light")}
                />
                <Form.Check
                  type="radio"
                  id="theme-dark"
                  label={
                    <span>
                      <i className="bi bi-moon me-2"></i>
                      {t("settings.dark")}
                    </span>
                  }
                  name="theme"
                  checked={localSettings.theme === "dark"}
                  onChange={() => handleInputChange("theme", "dark")}
                />
                <Form.Check
                  type="radio"
                  id="theme-auto"
                  label={
                    <span>
                      <i className="bi bi-circle-half me-2"></i>
                      {t("settings.auto")}
                      {isAuto && (
                        <small className="text-muted ms-2">
                          (
                          {systemPrefersDark
                            ? t("settings.dark")
                            : t("settings.light")}
                          )
                        </small>
                      )}
                    </span>
                  }
                  name="theme"
                  checked={localSettings.theme === "auto"}
                  onChange={() => handleInputChange("theme", "auto")}
                />
              </div>
              <Form.Text className="text-muted">
                {t("settings.themeDescription")}
              </Form.Text>
            </Form.Group>

            <Form.Group className="mb-4">
              <Form.Label>
                {t("settings.animationSpeed")}:{" "}
                {getAnimationSpeedLabel(localSettings.animationSpeed)}
              </Form.Label>
              <Form.Range
                min="0.5"
                max="2"
                step="0.5"
                value={localSettings.animationSpeed}
                onChange={(e) =>
                  handleInputChange(
                    "animationSpeed",
                    parseFloat(e.target.value),
                  )
                }
                disabled={localSettings.reduceMotion}
              />
              <div className="d-flex justify-content-between text-muted small">
                <span>{t("settings.slow")}</span>
                <span>{t("settings.normal")}</span>
                <span>{t("settings.fast")}</span>
              </div>
            </Form.Group>

            <Form.Group className="mb-4">
              <Form.Check
                type="switch"
                id="reduce-motion"
                label={t("settings.reduceMotion")}
                checked={localSettings.reduceMotion}
                onChange={(e) =>
                  handleInputChange("reduceMotion", e.target.checked)
                }
              />
              <Form.Text className="text-muted">
                {t("settings.reduceMotionDescription")}
              </Form.Text>
            </Form.Group>

            <Form.Group className="mb-4">
              <Form.Check
                type="switch"
                id="show-edge-labels"
                label={t("settings.showEdgeLabels")}
                checked={localSettings.showEdgeLabels}
                onChange={(e) =>
                  handleInputChange("showEdgeLabels", e.target.checked)
                }
              />
              <Form.Text className="text-muted">
                {t("settings.showEdgeLabelsDescription")}
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

            <div className="d-flex gap-2">
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
          </Form>
        </Card.Body>
      </Card>
    </MotionCard>
  );
}
