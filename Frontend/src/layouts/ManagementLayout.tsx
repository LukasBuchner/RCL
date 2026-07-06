import { useTranslation } from "react-i18next";
import SidebarLayout, { SidebarLink } from "./SidebarLayout";

export default function ManagementLayout() {
  const { t } = useTranslation();

  const managementLinks: SidebarLink[] = [
    {
      path: "/management/agents",
      label: t("management.agents"),
      icon: "bi bi-people-fill",
    },
    {
      path: "/management/skills",
      label: t("management.skills"),
      icon: "bi bi-tools",
    },
    {
      path: "/management/position-tags",
      label: t("management.positionTags"),
      icon: "bi bi-geo-alt-fill",
    },
    {
      path: "/management/scene-objects",
      label: t("management.sceneObjects"),
      icon: "bi bi-box-seam",
    },
    {
      path: "/management/variables",
      label: t("management.variables", "Variables"),
      icon: "bi bi-code-square",
    },
  ];

  return (
    <SidebarLayout
      title={t("management.title")}
      subtitle={t("management.subtitle")}
      headerIcon="bi bi-gear-wide-connected"
      sidebarLinks={managementLinks}
    />
  );
}
