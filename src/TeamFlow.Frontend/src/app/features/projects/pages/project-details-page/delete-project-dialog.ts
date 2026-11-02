import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';
import { ProjectsApi } from '../../projects.api';
import { ProjectDetails } from '../../projects.models';

/** Confirms and performs the delete. Closes with `true` once the project is gone. */
@Component({
  selector: 'app-delete-project-dialog',
  imports: [MatButtonModule, MatDialogModule, MatProgressSpinnerModule],
  templateUrl: './delete-project-dialog.html',
})
export class DeleteProjectDialog {
  protected readonly dialogRef = inject(MatDialogRef<DeleteProjectDialog>);
  protected readonly project = inject<ProjectDetails>(MAT_DIALOG_DATA);
  private readonly api = inject(ProjectsApi);

  protected readonly isDeleting = signal(false);
  protected readonly error = signal<string | null>(null);

  protected confirm(): void {
    if (this.isDeleting()) return;

    this.error.set(null);
    this.isDeleting.set(true);
    this.api
      .deleteProject(this.project.id)
      .pipe(finalize(() => this.isDeleting.set(false)))
      .subscribe({
        next: () => this.dialogRef.close(true),
        error: (error: unknown) => this.error.set(this.message(error)),
      });
  }

  private message(error: unknown): string {
    const status = error instanceof HttpErrorResponse ? error.status : 0;
    if (status === 403) return 'You do not have permission to delete this project.';
    if (status === 404) return 'This project no longer exists.';
    return 'Unable to delete the project. Please try again.';
  }
}
