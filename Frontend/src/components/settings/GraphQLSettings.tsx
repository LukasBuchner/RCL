import { useState } from "react";
import { Card, Form, Alert } from "react-bootstrap";
import { useTranslation } from "react-i18next";
import { MotionCard } from "../motion/MotionCard";
import { MotionButton } from "../motion/MotionButton";
import { useSettingsStore } from "../../stores/settingsStore";
import { useApollo } from "../../providers/ApolloProvider";

export default function GraphQLSettings() {
  const { t } = useTranslation();
  const { settings, updateGraphQLSettings } = useSettingsStore();
  const { reconnect, isConnected } = useApollo();
  const [localSettings, setLocalSettings] = useState(settings.graphql);
  const [hasChanges, setHasChanges] = useState(false);
  const [testResult, setTestResult] = useState<{
    success: boolean;
    message: string;
  } | null>(null);
  const [isTestingConnection, setIsTestingConnection] = useState(false);

  const handleInputChange = (
    field: keyof typeof localSettings,
    value: string | boolean | number,
  ) => {
    setLocalSettings((prev) => ({ ...prev, [field]: value }));
    setHasChanges(true);
    setTestResult(null);
  };

  const handleSave = () => {
    updateGraphQLSettings(localSettings);
    setHasChanges(false);
    setTestResult({
      success: true,
      message: t("settings.settingsSavedSuccess"),
    });
  };

  const handleTestConnection = async () => {
    setIsTestingConnection(true);
    setTestResult(null);

    try {
      // For now, just test if the endpoints are valid URLs
      new URL(localSettings.httpEndpoint);
      new URL(localSettings.wsEndpoint);

      // If we saved the settings, test the actual connection
      if (!hasChanges) {
        reconnect();
        // Give it a moment to connect
        setTimeout(() => {
          setTestResult({
            success: isConnected,
            message: isConnected
              ? t("settings.connectionSuccessful")
              : t("settings.connectionFailed"),
          });
        }, 1000);
      } else {
        setTestResult({
          success: true,
          message: t("settings.endpointsValid"),
        });
      }
    } catch {
      setTestResult({
        success: false,
        message: t("settings.endpointsInvalid"),
      });
    } finally {
      setIsTestingConnection(false);
    }
  };

  const handleReset = () => {
    setLocalSettings(settings.graphql);
    setHasChanges(false);
    setTestResult(null);
  };

  return (
    <MotionCard
      interaction="subtle"
      className="h-100"
      style={{ borderRadius: "12px" }}
    >
      <Card className="h-100 border-0" style={{ borderRadius: "12px" }}>
        <Card.Body className="p-4">
          <h3 className="mb-3">{t("settings.graphqlSettings")}</h3>
          <p className="text-muted">{t("settings.graphqlDescription")}</p>

          <Form className="mt-4">
            <Form.Group className="mb-3">
              <Form.Label>{t("settings.graphqlEndpoint")}</Form.Label>
              <Form.Control
                type="url"
                placeholder="http://localhost:5095/graphql"
                value={localSettings.httpEndpoint}
                onChange={(e) =>
                  handleInputChange("httpEndpoint", e.target.value)
                }
              />
              <Form.Text className="text-muted">
                {t("settings.graphqlEndpointDescription")}
              </Form.Text>
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label>{t("settings.websocketEndpoint")}</Form.Label>
              <Form.Control
                type="url"
                placeholder="ws://localhost:5095/graphql"
                value={localSettings.wsEndpoint}
                onChange={(e) =>
                  handleInputChange("wsEndpoint", e.target.value)
                }
              />
              <Form.Text className="text-muted">
                {t("settings.websocketEndpointDescription")}
              </Form.Text>
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Check
                type="switch"
                id="enable-subscriptions"
                label={t("settings.enableSubscriptions")}
                checked={localSettings.enableSubscriptions}
                onChange={(e) =>
                  handleInputChange("enableSubscriptions", e.target.checked)
                }
              />
              <Form.Text className="text-muted">
                {t("settings.subscriptionsDescription")}
              </Form.Text>
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label>{t("settings.connectionTimeout")}</Form.Label>
              <Form.Control
                type="number"
                min="5000"
                max="120000"
                step="1000"
                value={localSettings.timeout}
                onChange={(e) =>
                  handleInputChange(
                    "timeout",
                    parseInt(e.target.value) || 30000,
                  )
                }
              />
              <Form.Text className="text-muted">
                {t("settings.connectionTimeoutDescription")}
              </Form.Text>
            </Form.Group>

            {testResult && (
              <Alert
                variant={testResult.success ? "success" : "danger"}
                className="mb-3"
              >
                <i
                  className={`bi ${testResult.success ? "bi-check-circle" : "bi-exclamation-triangle"} me-2`}
                ></i>
                {testResult.message}
              </Alert>
            )}

            <div className="d-flex gap-2 flex-wrap">
              <MotionButton
                variant="primary"
                size="sm"
                onClick={handleSave}
                disabled={!hasChanges}
              >
                <i className="bi bi-check-lg me-1"></i>
                {t("settings.saveChanges")}
              </MotionButton>
              <MotionButton
                variant="outline-secondary"
                size="sm"
                onClick={handleTestConnection}
                disabled={isTestingConnection}
              >
                <i
                  className={`bi ${isTestingConnection ? "bi-hourglass-split" : "bi-arrow-clockwise"} me-1`}
                ></i>
                {t("settings.testConnection")}
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
