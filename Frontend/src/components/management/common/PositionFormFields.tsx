import React from "react";
import { useTranslation } from "react-i18next";
import { FormColumn, FormField, FormRow, FormSection } from "./FormSection";

interface Position {
  x: number;
  y: number;
  z: number;
  alpha: number;
  beta: number;
  gamma: number;
}

interface PositionFormFieldsProps {
  position: Position | undefined;
  onPositionChange: (field: keyof Position, value: number) => void;
  coordinateLabels?: {
    x?: string;
    y?: string;
    z?: string;
  };
  angleLabels?: {
    alpha?: string;
    beta?: string;
    gamma?: string;
  };
}

export const PositionFormFields: React.FC<PositionFormFieldsProps> = ({
  position,
  onPositionChange,
  coordinateLabels = {},
  angleLabels = {},
}) => {
  const { t } = useTranslation();

  const defaultCoordinateLabels = {
    x: coordinateLabels.x || "X",
    y: coordinateLabels.y || "Y",
    z: coordinateLabels.z || "Z",
  };

  const defaultAngleLabels = {
    alpha: angleLabels.alpha || "Alpha (α)",
    beta: angleLabels.beta || "Beta (β)",
    gamma: angleLabels.gamma || "Gamma (γ)",
  };

  return (
    <>
      <FormSection>
        <h6>{t("common.position")}</h6>
        <FormRow>
          <FormColumn md={4}>
            <FormField
              label={defaultCoordinateLabels.x}
              type="number"
              value={position?.x?.toString() || "0"}
              onChange={(value) =>
                onPositionChange("x", parseFloat(value) || 0)
              }
              required
            />
          </FormColumn>
          <FormColumn md={4}>
            <FormField
              label={defaultCoordinateLabels.y}
              type="number"
              value={position?.y?.toString() || "0"}
              onChange={(value) =>
                onPositionChange("y", parseFloat(value) || 0)
              }
              required
            />
          </FormColumn>
          <FormColumn md={4}>
            <FormField
              label={defaultCoordinateLabels.z}
              type="number"
              value={position?.z?.toString() || "0"}
              onChange={(value) =>
                onPositionChange("z", parseFloat(value) || 0)
              }
              required
            />
          </FormColumn>
        </FormRow>
      </FormSection>

      <FormSection>
        <h6>{t("common.rotation")}</h6>
        <FormRow>
          <FormColumn md={4}>
            <FormField
              label={defaultAngleLabels.alpha}
              type="number"
              value={position?.alpha?.toString() || "0"}
              onChange={(value) =>
                onPositionChange("alpha", parseFloat(value) || 0)
              }
              required
            />
          </FormColumn>
          <FormColumn md={4}>
            <FormField
              label={defaultAngleLabels.beta}
              type="number"
              value={position?.beta?.toString() || "0"}
              onChange={(value) =>
                onPositionChange("beta", parseFloat(value) || 0)
              }
              required
            />
          </FormColumn>
          <FormColumn md={4}>
            <FormField
              label={defaultAngleLabels.gamma}
              type="number"
              value={position?.gamma?.toString() || "0"}
              onChange={(value) =>
                onPositionChange("gamma", parseFloat(value) || 0)
              }
              required
            />
          </FormColumn>
        </FormRow>
      </FormSection>
    </>
  );
};
