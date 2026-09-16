export interface ApiProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
}

export interface ApiValidationProblemDetails extends ApiProblemDetails {
  errors: Record<string, string[]>;
}

export function isApiValidationProblemDetails(
  value: unknown,
): value is ApiValidationProblemDetails {
  if (!isRecord(value) || !isRecord(value['errors'])) {
    return false;
  }

  return Object.values(value['errors']).every(
    (messages) =>
      Array.isArray(messages) && messages.every((message) => typeof message === 'string'),
  );
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}
