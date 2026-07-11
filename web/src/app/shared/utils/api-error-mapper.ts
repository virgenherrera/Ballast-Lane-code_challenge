export interface ApiErrorDetail {
  field: string;
  issue: string;
}

export interface ApiError {
  status: number;
  error: string;
  message: string;
  details: ApiErrorDetail[];
}

export type FieldErrors = Record<string, string>;

export function mapApiErrorToFieldErrors(error: ApiError): FieldErrors {
  return error.details.reduce<FieldErrors>((fieldErrors, detail) => {
    fieldErrors[detail.field] = detail.issue;
    return fieldErrors;
  }, {});
}
