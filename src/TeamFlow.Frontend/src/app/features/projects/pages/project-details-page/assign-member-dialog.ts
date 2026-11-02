import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import {
  catchError,
  debounceTime,
  distinctUntilChanged,
  filter,
  finalize,
  of,
  startWith,
  switchMap,
  tap,
} from 'rxjs';
import { USER_ROLES } from '../../../../core/auth/auth.models';
import { isApiValidationProblemDetails } from '../../../../core/http/api-problem.models';
import { ProjectMembersApi } from '../../project-members.api';
import { UserSummary } from '../../projects.models';
import { UsersApi } from '../../users.api';

export interface AssignMemberDialogData {
  projectId: string;
  /** Users who already belong to the project (owner included); hidden from the results. */
  excludedUserIds: readonly string[];
}

/** Picks a user through server-side search and assigns them. Closes with `true` on success. */
@Component({
  selector: 'app-assign-member-dialog',
  imports: [
    MatAutocompleteModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  templateUrl: './assign-member-dialog.html',
  styleUrl: '../../project-form-dialog.scss',
})
export class AssignMemberDialog {
  protected readonly dialogRef = inject(MatDialogRef<AssignMemberDialog>);
  private readonly data = inject<AssignMemberDialogData>(MAT_DIALOG_DATA);
  private readonly usersApi = inject(UsersApi);
  private readonly membersApi = inject(ProjectMembersApi);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly roles = USER_ROLES;
  protected readonly form = inject(FormBuilder).nonNullable.group({
    // a string while typing, a UserSummary once an option is picked
    user: [
      '' as string | UserSummary,
      [
        (c: { value: string | UserSummary }) =>
          typeof c.value === 'string' ? { picked: true } : null,
      ],
    ],
    projectRole: ['Developer' as (typeof USER_ROLES)[number], Validators.required],
  });
  protected readonly options = signal<UserSummary[]>([]);
  protected readonly isSearching = signal(false);
  protected readonly searchFailed = signal(false);
  protected readonly isSubmitting = signal(false);
  protected readonly formError = signal<string | null>(null);

  constructor() {
    this.form.controls.user.valueChanges
      .pipe(
        startWith(''),
        filter((value): value is string => typeof value === 'string'),
        debounceTime(300),
        distinctUntilChanged(),
        tap(() => {
          this.isSearching.set(true);
          this.searchFailed.set(false);
        }),
        // switchMap drops stale responses while the user keeps typing
        switchMap((search) =>
          this.usersApi.searchUsers(search.trim()).pipe(
            catchError(() => {
              this.searchFailed.set(true);
              return of(null);
            }),
          ),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((response) => {
        this.isSearching.set(false);
        if (!response) return;
        const excluded = new Set(this.data.excludedUserIds);
        this.options.set(response.items.filter((user) => !excluded.has(user.id)));
      });
  }

  protected display(user: string | UserSummary | null): string {
    return typeof user === 'object' && user ? `${user.firstName} ${user.lastName}` : '';
  }

  protected submit(): void {
    const { user, projectRole } = this.form.getRawValue();
    if (this.form.invalid || typeof user === 'string' || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.formError.set(null);
    this.isSubmitting.set(true);
    this.membersApi
      .assignMember(this.data.projectId, { userId: user.id, projectRole })
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => this.dialogRef.close(true),
        error: (error: unknown) => this.formError.set(this.message(error)),
      });
  }

  private message(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 403) return 'You do not have permission to add members.';
      if (error.status === 404) return 'The project or the user no longer exists.';
      if (error.status === 409) return 'This user is already a member of the project.';
      if (error.status === 422 && isApiValidationProblemDetails(error.error)) {
        return (
          Object.values(error.error.errors).flat().join(' ') ||
          error.error.detail ||
          'The member could not be added.'
        );
      }
    }
    return 'Unable to add the member. Please try again.';
  }
}
