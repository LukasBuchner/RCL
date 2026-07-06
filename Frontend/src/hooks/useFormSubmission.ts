import { useCallback, useState } from "react";
import { useError } from "../contexts/ErrorContext";
import { createLogger } from "../utils/logger";

const log = createLogger("FormSubmission");

interface UseFormSubmissionOptions<T> {
  /**
   * Validation function that returns true if form is valid, or an error message
   */
  validate: (formData: T) => true | string;

  /**
   * Function to prepare the create mutation input
   */
  prepareCreateInput?: (formData: T) => unknown;

  /**
   * Function to prepare the update mutation input
   */
  prepareUpdateInput?: (formData: T, editingItem: unknown) => unknown;

  /**
   * The create mutation function from Apollo
   */
  createMutation?: (options: {
    variables: { input: unknown };
  }) => Promise<unknown>;

  /**
   * The update mutation function from Apollo
   */
  updateMutation?: (options: {
    variables: { input: unknown };
  }) => Promise<unknown>;

  /**
   * Callback after successful submission
   */
  onSuccess?: () => void;

  /**
   * Entity name for error messages (e.g., "agent", "skill")
   */
  entityName: string;

  /**
   * Whether to enable console logging
   */
  debug?: boolean;
}

/**
 * Custom hook for handling form submissions with consistent error handling,
 * validation, and loading states across all forms in the application.
 */
export function useFormSubmission<T>({
  validate,
  prepareCreateInput,
  prepareUpdateInput,
  createMutation,
  updateMutation,
  onSuccess,
  entityName,
  debug = false,
}: UseFormSubmissionOptions<T>) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const { addError } = useError();

  const handleSubmit = useCallback(
    async (
      e: React.FormEvent,
      formData: T,
      editingItem: unknown = null,
    ): Promise<boolean> => {
      e.preventDefault();

      // Validate form data
      const validationResult = validate(formData);
      if (validationResult !== true) {
        addError({
          message: validationResult,
          severity: "warning",
        });
        return false;
      }

      setIsSubmitting(true);

      try {
        if (editingItem) {
          // Update mode
          if (!updateMutation || !prepareUpdateInput) {
            throw new Error(
              `Update functionality not configured for ${entityName}`,
            );
          }

          const updateInput = prepareUpdateInput(formData, editingItem);

          if (debug) {
            log.debug(
              `Updating ${entityName} with input:`,
              JSON.stringify(updateInput, null, 2),
            );
          }

          await updateMutation({ variables: { input: updateInput } });
        } else {
          // Create mode
          if (!createMutation || !prepareCreateInput) {
            throw new Error(
              `Create functionality not configured for ${entityName}`,
            );
          }

          const createInput = prepareCreateInput(formData);

          if (debug) {
            log.debug(
              `Creating ${entityName} with input:`,
              JSON.stringify(createInput, null, 2),
            );
          }

          await createMutation({ variables: { input: createInput } });
        }

        // Call success callback
        onSuccess?.();

        setIsSubmitting(false);
        return true;
      } catch (error) {
        log.error(`${entityName} save error:`, error);

        addError({
          message: `Failed to ${editingItem ? "update" : "create"} ${entityName}`,
          severity: "error",
          retry: async () => {
            await handleSubmit(e, formData, editingItem);
          },
        });

        setIsSubmitting(false);
        return false;
      }
    },
    [
      validate,
      prepareCreateInput,
      prepareUpdateInput,
      createMutation,
      updateMutation,
      onSuccess,
      entityName,
      debug,
      addError,
    ],
  );

  return {
    handleSubmit,
    isSubmitting,
  };
}
