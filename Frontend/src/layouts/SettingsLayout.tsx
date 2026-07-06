import { useTranslation } from "react-i18next";
import SidebarLayout, { SidebarLink } from "./SidebarLayout";

export default function SettingsLayout() {
  const { t } = useTranslation();

  const settingsLinks: SidebarLink[] = [
    {
      path: "/settings/general",
      label: t("settings.general"),
      icon: "bi bi-gear",
    },
    {
      path: "/settings/graphql",
      label: t("settings.graphql"),
      icon: "bi bi-diagram-3",
    },
    {
      path: "/settings/appearance",
      label: t("settings.appearance"),
      icon: "bi bi-palette",
    },
  ];

  return (
    <SidebarLayout
      title={t("settings.title")}
      subtitle={t("settings.subtitle")}
      headerIcon="bi bi-sliders"
      sidebarLinks={settingsLinks}
    />
  );
}
