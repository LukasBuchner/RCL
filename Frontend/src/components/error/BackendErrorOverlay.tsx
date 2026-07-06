import { useTranslation } from "react-i18next";
import ErrorState from "./ErrorState";

interface BackendErrorOverlayProps {
  onRetry?: () => void | Promise<void>;
}

/**
 * A prominent full-screen overlay that displays when the backend server is unreachable.
 * Features a clear error message, helpful instructions, and a retry button.
 * Designed to be impossible to miss and informative about the issue.
 * Reuses the ErrorState component with overlay mode for consistency.
 */
export default function BackendErrorOverlay({
  onRetry,
}: BackendErrorOverlayProps) {
  const { t } = useTranslation();

  return (
    <ErrorState
      title={t("errors.backendUnreachable")}
      severity="critical"
      onRetry={onRetry}
      overlayMode={true}
      customIcon="bi-plugin"
      imageMaxWidth="300px"
    >
      <p className="mb-3">{t("errors.backendUnreachableDescription")}</p>
      <ul className="mb-3">
        <li>{t("errors.startBackendServer")}</li>
        <li>{t("errors.checkGraphQLEndpoint")}</li>
        <li>{t("errors.verifyNetworkConnectivity")}</li>
      </ul>
    </ErrorState>
  );
}
