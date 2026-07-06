import { Container, Card } from "react-bootstrap";
import { MotionContainer } from "../motion/MotionContainer";
import { MotionCard } from "../motion/MotionCard";
import { useTranslation } from "react-i18next";

export default function HelpPage() {
  const { t } = useTranslation();

  return (
    <Container fluid className="py-3">
      <MotionContainer>
        <h1 className="mb-4">{t("help.title")}</h1>

        <MotionCard interaction="subtle">
          <Card>
            <Card.Body>
              <h2>{t("help.gettingStartedHelp.title")}</h2>
              <p>{t("help.gettingStartedHelp.description")}</p>

              <h3 className="mt-4">
                {t("help.gettingStartedHelp.keyFeatures")}
              </h3>
              <ul>
                <li>
                  {t(
                    "help.gettingStartedHelp.keyFeaturesList.visualFlowEditor",
                  )}
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

              <h3 className="mt-4">
                {t("help.gettingStartedHelp.navigation")}
              </h3>
              <ul>
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
            </Card.Body>
          </Card>
        </MotionCard>
      </MotionContainer>
    </Container>
  );
}
