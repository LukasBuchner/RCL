import {
  PropertyFieldsFragment,
  PropertyInput,
  PropertyTypeInput,
} from "../../../__generated__/graphql";
import { mapPropertyFieldsFragmentValueToPropertyTypeInput } from "./mapPropertyFieldsFragmentValueToPropertyTypeInput";

export const mapPropertyFieldsFragmentToPropertyInput = (
  propertyFieldsFragment: PropertyFieldsFragment,
): PropertyInput => {
  const propertyInput: PropertyInput = {
    name: propertyFieldsFragment.name,
    direction: propertyFieldsFragment.direction,
    value: mapPropertyFieldsFragmentValueToPropertyTypeInput(
      propertyFieldsFragment.value,
    ) as PropertyTypeInput,
  };

  // Only include binding if it exists and has a variableName
  if (propertyFieldsFragment.binding?.variableName) {
    propertyInput.binding = {
      variableName: propertyFieldsFragment.binding.variableName,
      mode: propertyFieldsFragment.binding.mode,
      transformExpression:
        propertyFieldsFragment.binding.transformExpression || undefined,
    };
  }

  return propertyInput;
};
