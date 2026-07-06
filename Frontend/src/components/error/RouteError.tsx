import { useTranslation } from "react-i18next";
import ErrorState from "./ErrorState";

interface RouteErrorProps {
  type: "root" | "management";
}

export default function RouteError({ type }: RouteErrorProps) {
  const { t } = useTranslation();

  const getErrorMessage = () => {
    switch (type) {
      case "root":
        return t("common.rootRouteError");
      case "management":
        return t("common.managementRouteError");
      default:
        return t("common.unexpectedError");
    }
  };

  return (
    <ErrorState
      title={t("common.oopsError")}
      message={getErrorMessage()}
      severity="error"
      fullScreen={true}
    />
  );
}
