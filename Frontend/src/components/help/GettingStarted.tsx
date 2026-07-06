import { Card } from "react-bootstrap";
import { MotionCard } from "../motion/MotionCard";
import { useTranslation } from "react-i18next";

export default function GettingStarted() {
  const { t } = useTranslation();

  return (
    <MotionCard
      interaction="subtle"
      className="h-100"
      style={{ borderRadius: "12px" }}
    >
      <Card className="h-100 border-0" style={{ borderRadius: "12px" }}>
        <Card.Body className="p-4">
          <h3 className="mb-3">{t("help.gettingStartedHelp.title")}</h3>
          <p className="text-muted">
            {t("help.gettingStartedHelp.description")}
          </p>

          <div className="mt-4">
            <h5>{t("help.gettingStartedHelp.keyFeatures")}</h5>
            <ul className="text-muted">
              <li>
                {t("help.gettingStartedHelp.keyFeaturesList.visualFlowEditor")}
              </li>
              <li>
                {t("help.gettingStartedHelp.keyFeaturesList.realTimeUpdates")}
              </li>
              <li>
                {t(
                  "help.gettingStartedHelp.keyFeaturesList.agentSkillManagement",
                )}
              </li>
              <li>
                {t(
                  "help.gettingStartedHelp.keyFeaturesList.timelineVisualization",
                )}
              </li>
            </ul>
          </div>

          <div className="mt-4">
            <h5>{t("help.gettingStartedHelp.navigation")}</h5>
            <ul className="text-muted">
              <li>
                <strong>
                  {t("help.gettingStartedHelp.navigationList.flowEditor")}:
                </strong>{" "}
                {t(
                  "help.gettingStartedHelp.navigationList.flowEditorDescription",
                )}
              </li>
              <li>
                <strong>
                  {t("help.gettingStartedHelp.navigationList.management")}:
                </strong>{" "}
                {t(
                  "help.gettingStartedHelp.navigationList.managementDescription",
                )}
              </li>
              <li>
                <strong>
                  {t("help.gettingStartedHelp.navigationList.settings")}:
                </strong>{" "}
                {t(
                  "help.gettingStartedHelp.navigationList.settingsDescription",
                )}
              </li>
            </ul>
          </div>

          <div className="mt-4">
            <h5>{t("help.gettingStartedHelp.quickStart")}</h5>
            <ol className="text-muted">
              <li>{t("help.gettingStartedHelp.quickStartSteps.step1")}</li>
              <li>{t("help.gettingStartedHelp.quickStartSteps.step2")}</li>
              <li>{t("help.gettingStartedHelp.quickStartSteps.step3")}</li>
              <li>{t("help.gettingStartedHelp.quickStartSteps.step4")}</li>
            </ol>
          </div>
        </Card.Body>
      </Card>
    </MotionCard>
  );
}
