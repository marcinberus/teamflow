import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';
import { ProjectMembersApi } from '../../project-members.api';
import { ProjectMember } from '../../projects.models';

export interface RemoveMemberDialogData {
  projectId: string;
  member: ProjectMember;
}

/** Confirms and performs the removal. Closes with `true` once the member is gone. */
@Component({
  selector: 'app-remove-member-dialog',
  imports: [MatButtonModule, MatDialogModule, MatProgressSpinnerModule],
  templateUrl: './remove-member-dialog.html',
})
export class RemoveMemberDialog {
  protected readonly dialogRef = inject(MatDialogRef<RemoveMemberDialog>);
  private readonly data = inject<RemoveMemberDialogData>(MAT_DIALOG_DATA);
  private readonly api = inject(ProjectMembersApi);

  protected readonly member = this.data.member;
  protected readonly isRemoving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected confirm(): void {
    if (this.isRemoving()) return;

    this.error.set(null);
    this.isRemoving.set(true);
    this.api
      .removeMember(this.data.projectId, this.member.userId)
      .pipe(finalize(() => this.isRemoving.set(false)))
      .subscribe({
        next: () => this.dialogRef.close(true),
        error: (error: unknown) => {
          const status = error instanceof HttpErrorResponse ? error.status : 0;
          if (status === 404) return this.dialogRef.close(true); // already gone
          this.error.set(this.message(status));
        },
      });
  }

  private message(status: number): string {
    if (status === 403) return 'You do not have permission to remove members.';
    if (status === 409) return 'The project owner cannot be removed.';
    return 'Unable to remove the member. Please try again.';
  }
}
