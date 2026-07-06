import { Card } from "react-bootstrap";
import { MotionCard } from "../motion";
import { useTranslation } from "react-i18next";

export default function FlowEditorHelp() {
  const { t } = useTranslation();

  return (
    <MotionCard
      interaction="subtle"
      className="h-100"
      style={{ borderRadius: "12px" }}
    >
      <Card className="h-100 border-0" style={{ borderRadius: "12px" }}>
        <Card.Body className="p-4">
          <h3 className="mb-3">{t("help.flowEditorHelp.title")}</h3>
          <p className="text-muted">{t("help.flowEditorHelp.description")}</p>

          <div className="mt-4">
            <h5>{t("help.flowEditorHelp.creatingTasks")}</h5>
            <ul className="text-muted">
              <li>{t("help.flowEditorHelp.creatingTasksList.addTask")}</li>
              <li>
                {t("help.flowEditorHelp.creatingTasksList.configureTask")}
              </li>
              <li>
                {t("help.flowEditorHelp.creatingTasksList.addDependencies")}
              </li>
            </ul>
          </div>

          <div className="mt-4">
            <h5>{t("help.flowEditorHelp.addingSkills")}</h5>
            <ul className="text-muted">
              <li>{t("help.flowEditorHelp.addingSkillsList.addSkill")}</li>
              <li>
                {t("help.flowEditorHelp.addingSkillsList.skillsRepresent")}
              </li>
              <li>{t("help.flowEditorHelp.addingSkillsList.connectSkills")}</li>
            </ul>
          </div>

          <div className="mt-4">
            <h5>{t("help.flowEditorHelp.timelineView")}</h5>
            <p className="text-muted">
              {t("help.flowEditorHelp.timelineViewDescription")}
            </p>
          </div>

          <div className="mt-4">
            <h5>{t("help.flowEditorHelp.keyboardShortcuts")}</h5>
            <ul className="text-muted">
              <li>
                <kbd>Ctrl+C</kbd> / <kbd>Cmd+C</kbd> -{" "}
                {t("help.flowEditorHelp.keyboardShortcutsList.copy")}
              </li>
              <li>
                <kbd>Ctrl+X</kbd> / <kbd>Cmd+X</kbd> -{" "}
                {t("help.flowEditorHelp.keyboardShortcutsList.cut")}
              </li>
              <li>
                <kbd>Ctrl+V</kbd> / <kbd>Cmd+V</kbd> -{" "}
                {t("help.flowEditorHelp.keyboardShortcutsList.paste")}
              </li>
              <li>
                <kbd>Delete</kbd> -{" "}
                {t("help.flowEditorHelp.keyboardShortcutsList.delete")}
              </li>
            </ul>
          </div>
        </Card.Body>
      </Card>
    </MotionCard>
  );
}
