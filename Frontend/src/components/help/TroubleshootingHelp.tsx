import { Card } from "react-bootstrap";
import { MotionCard } from "../motion/MotionCard";
import { useTranslation } from "react-i18next";

export default function TroubleshootingHelp() {
  const { t } = useTranslation();

  return (
    <MotionCard
      interaction="subtle"
      className="h-100"
      style={{ borderRadius: "12px" }}
    >
      <Card className="h-100 border-0" style={{ borderRadius: "12px" }}>
        <Card.Body className="p-4">
          <h3 className="mb-3">{t("help.troubleshootingHelp.title")}</h3>
          <p className="text-muted">
            {t("help.troubleshootingHelp.description")}
          </p>

          <div className="mt-4">
            <h5>{t("help.troubleshootingHelp.connectionIssues")}</h5>
            <div className="card border-warning mb-3">
              <div className="card-body py-2">
                <strong>Problem:</strong>{" "}
                {t("help.troubleshootingHelp.connectionProblem")}
              </div>
            </div>
            <ul className="text-muted">
              <li>
                {t("help.troubleshootingHelp.connectionSolutions.checkServer")}
              </li>
              <li>
                {t(
                  "help.troubleshootingHelp.connectionSolutions.verifyNetwork",
                )}
              </li>
              <li>
                {t(
                  "help.troubleshootingHelp.connectionSolutions.restartServer",
                )}
              </li>
            </ul>
          </div>

          <div className="mt-4">
            <h5>{t("help.troubleshootingHelp.performanceIssues")}</h5>
            <div className="card border-warning mb-3">
              <div className="card-body py-2">
                <strong>Problem:</strong>{" "}
                {t("help.troubleshootingHelp.performanceProblem")}
              </div>
            </div>
            <ul className="text-muted">
              <li>
                {t(
                  "help.troubleshootingHelp.performanceSolutions.checkConsole",
                )}
              </li>
              <li>
                {t(
                  "help.troubleshootingHelp.performanceSolutions.reduceAnimation",
                )}
              </li>
              <li>
                {t("help.troubleshootingHelp.performanceSolutions.clearCache")}
              </li>
              <li>
                {t("help.troubleshootingHelp.performanceSolutions.closeTabs")}
              </li>
            </ul>
          </div>

          <div className="mt-4">
            <h5>{t("help.troubleshootingHelp.dataIssues")}</h5>
            <div className="card border-warning mb-3">
              <div className="card-body py-2">
                <strong>Problem:</strong>{" "}
                {t("help.troubleshootingHelp.dataProblem")}
              </div>
            </div>
            <ul className="text-muted">
              <li>{t("help.troubleshootingHelp.dataSolutions.refreshPage")}</li>
              <li>{t("help.troubleshootingHelp.dataSolutions.checkLogs")}</li>
              <li>
                {t("help.troubleshootingHelp.dataSolutions.verifyDatabase")}
              </li>
              <li>
                {t("help.troubleshootingHelp.dataSolutions.clearAppCache")}
              </li>
            </ul>
          </div>

          <div className="mt-4">
            <h5>{t("help.troubleshootingHelp.gettingHelp")}</h5>
            <p className="text-muted">
              {t("help.troubleshootingHelp.gettingHelpDescription")}
            </p>
          </div>
        </Card.Body>
      </Card>
    </MotionCard>
  );
}
