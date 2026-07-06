import React from "react";
import { Card, Container } from "react-bootstrap";
import { useTranslation } from "react-i18next";
import VariableManager from "../Variables/VariableManager";
import { useProcedure } from "../../contexts/ProcedureContext";
import { LoadingState } from "../loading";
import { ProcedureSelector } from "./ProcedureSelector";

const VariableManagement: React.FC = () => {
  const { loadedProcedure, isLoading } = useProcedure();
  const { t } = useTranslation();

  if (isLoading) {
    return <LoadingState text="Loading procedure..." />;
  }

  if (!loadedProcedure) {
    return (
      <Container
        className="d-flex flex-column align-items-center justify-content-center"
        style={{ minHeight: "70vh" }}
      >
        <Card
          style={{ maxWidth: "600px", width: "100%" }}
          className="shadow-sm"
        >
          <Card.Body className="p-5">
            <div className="text-center mb-4">
              <div className="mb-3">
                <i
                  className="bi bi-file-earmark-text"
                  style={{ fontSize: "3rem", color: "#6c757d" }}
                ></i>
              </div>
              <h4 className="mb-2">{t("variables.noProcedureLoaded")}</h4>
              <p className="text-muted mb-4">
                {t("variables.selectProcedureToManage")}
              </p>
            </div>
            <hr className="my-4" />
            <div>
              <h5 className="mb-3 text-center">
                {t("variables.selectProcedure")}
              </h5>
              <ProcedureSelector />
            </div>
          </Card.Body>
        </Card>
      </Container>
    );
  }

  return <VariableManager procedureId={loadedProcedure.id} />;
};

export default VariableManagement;
