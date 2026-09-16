import { AbstractControl, FormGroup } from '@angular/forms';
import { ApiValidationProblemDetails } from './api-problem.models';

export function applyServerValidationErrors(
  form: FormGroup,
  problem: ApiValidationProblemDetails,
  fieldMap: Readonly<Record<string, string>> = {},
): string[] {
  clearServerErrors(form);

  const mappedErrors = new Map<AbstractControl, string[]>();
  const unmappedErrors: string[] = [];

  for (const [propertyName, messages] of Object.entries(problem.errors)) {
    const control = findControl(form, propertyName, fieldMap);

    if (!control) {
      unmappedErrors.push(...messages);
      continue;
    }

    mappedErrors.set(control, [...(mappedErrors.get(control) ?? []), ...messages]);
  }

  for (const [control, messages] of mappedErrors) {
    control.setErrors({ ...control.errors, server: messages });
    control.markAsTouched();
  }

  return unmappedErrors;
}

function findControl(
  form: FormGroup,
  propertyName: string,
  fieldMap: Readonly<Record<string, string>>,
): AbstractControl | null {
  const mappedEntry = Object.entries(fieldMap).find(
    ([backendName]) => backendName.toLowerCase() === propertyName.toLowerCase(),
  );
  const requestedName = mappedEntry?.[1] ?? propertyName;
  const controlName = Object.keys(form.controls).find(
    (name) => name.toLowerCase() === requestedName.toLowerCase(),
  );

  return controlName ? form.controls[controlName] : form.get(requestedName);
}

function clearServerErrors(form: FormGroup): void {
  for (const control of Object.values(form.controls)) {
    if (!control.errors?.['server']) {
      continue;
    }

    const errors = { ...control.errors };
    delete errors['server'];
    control.setErrors(Object.keys(errors).length ? errors : null);
  }
}
