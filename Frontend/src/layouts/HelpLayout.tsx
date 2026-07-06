import { useTranslation } from "react-i18next";
import SidebarLayout, { SidebarLink } from "./SidebarLayout";

export default function HelpLayout() {
  const { t } = useTranslation();

  const helpLinks: SidebarLink[] = [
    {
      path: "/help/getting-started",
      label: t("help.gettingStarted"),
      icon: "bi bi-play-circle",
    },
    {
      path: "/help/flow-editor",
      label: t("help.flowEditor"),
      icon: "bi bi-diagram-3",
    },
    {
      path: "/help/management",
      label: t("help.management"),
      icon: "bi bi-gear",
    },
    {
      path: "/help/troubleshooting",
      label: t("help.troubleshooting"),
      icon: "bi bi-tools",
    },
  ];

  return (
    <SidebarLayout
      title={t("help.title")}
      subtitle={t("help.subtitle")}
      headerIcon="bi bi-question-circle"
      sidebarLinks={helpLinks}
    />
  );
}
