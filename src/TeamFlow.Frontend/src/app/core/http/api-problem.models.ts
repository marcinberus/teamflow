export interface ApiProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
}

export interface ApiValidationProblemDetails extends ApiProblemDetails {
  errors: Record<string, string[]>;
}
