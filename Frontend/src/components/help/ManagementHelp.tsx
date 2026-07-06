import { Card } from "react-bootstrap";
import { MotionCard } from "../motion/MotionCard";
import { useTranslation } from "react-i18next";

export default function ManagementHelp() {
  const { t } = useTranslation();

  return (
    <MotionCard
      interaction="subtle"
      className="h-100"
      style={{ borderRadius: "12px" }}
    >
      <Card className="h-100 border-0" style={{ borderRadius: "12px" }}>
        <Card.Body className="p-4">
          <h3 className="mb-3">{t("help.managementHelp.title")}</h3>
          <p className="text-muted">{t("help.managementHelp.description")}</p>

          <div className="mt-4">
            <h5>{t("help.managementHelp.agentManagement")}</h5>
            <ul className="text-muted">
              <li>
                {t("help.managementHelp.agentManagementList.createAgents")}
              </li>
              <li>
                {t("help.managementHelp.agentManagementList.assignColors")}
              </li>
              <li>
                {t("help.managementHelp.agentManagementList.associateSkills")}
              </li>
              <li>
                {t("help.managementHelp.agentManagementList.editProperties")}
              </li>
            </ul>
          </div>

          <div className="mt-4">
            <h5>{t("help.managementHelp.skillManagement")}</h5>
            <ul className="text-muted">
              <li>
                {t("help.managementHelp.skillManagementList.defineSkills")}
              </li>
              <li>
                {t(
                  "help.managementHelp.skillManagementList.configureParameters",
                )}
              </li>
              <li>
                {t("help.managementHelp.skillManagementList.setupDependencies")}
              </li>
              <li>
                {t("help.managementHelp.skillManagementList.testValidate")}
              </li>
            </ul>
          </div>

          <div className="mt-4">
            <h5>{t("help.managementHelp.positionTags")}</h5>
            <ul className="text-muted">
              <li>
                {t("help.managementHelp.positionTagsList.createPositions")}
              </li>
              <li>
                {t("help.managementHelp.positionTagsList.defineCoordinates")}
              </li>
              <li>
                {t("help.managementHelp.positionTagsList.referencePositions")}
              </li>
              <li>
                {t("help.managementHelp.positionTagsList.organizeLayout")}
              </li>
            </ul>
          </div>

          <div className="mt-4">
            <h5>{t("help.managementHelp.sceneObjects")}</h5>
            <ul className="text-muted">
              <li>{t("help.managementHelp.sceneObjectsList.defineObjects")}</li>
              <li>{t("help.managementHelp.sceneObjectsList.setProperties")}</li>
              <li>
                {t(
                  "help.managementHelp.sceneObjectsList.configureRelationships",
                )}
              </li>
              <li>
                {t("help.managementHelp.sceneObjectsList.manageLifecycle")}
              </li>
            </ul>
          </div>
        </Card.Body>
      </Card>
    </MotionCard>
  );
}
