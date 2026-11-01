import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, WritableSignal, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable, finalize } from 'rxjs';
import { AuthStore } from '../../../../core/auth/auth.store';
import { ProjectFormDialog } from '../../project-form-dialog';
import { ProjectMembersApi } from '../../project-members.api';
import { canManageMembers, canManageProject } from '../../project.policies';
import { ProjectsApi } from '../../projects.api';
import {
  PROJECT_STATUSES,
  ProjectDetails,
  ProjectMember,
  ProjectStatistics,
  ProjectStatus,
  TASK_STATUSES,
  TaskListResponse,
} from '../../projects.models';
import { TasksApi } from '../../tasks.api';
import { AssignMemberDialog, AssignMemberDialogData } from './assign-member-dialog';
import { DeleteProjectDialog } from './delete-project-dialog';
import { RemoveMemberDialog, RemoveMemberDialogData } from './remove-member-dialog';

type Section<T> =
  { state: 'loading' } | { state: 'error'; notFound: boolean } | { state: 'loaded'; data: T };

const loading = { state: 'loading' } as const;

@Component({
  selector: 'app-project-details-page',
  imports: [
    DatePipe,
    MatButtonModule,
    MatDialogModule,
    MatMenuModule,
    MatProgressBarModule,
    MatSnackBarModule,
    MatTableModule,
    RouterLink,
  ],
  templateUrl: './project-details-page.html',
  styleUrl: './project-details-page.scss',
})
export class ProjectDetailsPage {
  private readonly projectId = inject(ActivatedRoute).snapshot.paramMap.get('projectId')!;
  private readonly projectsApi = inject(ProjectsApi);
  private readonly membersApi = inject(ProjectMembersApi);
  private readonly tasksApi = inject(TasksApi);
  private readonly auth = inject(AuthStore);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly project = signal<Section<ProjectDetails>>(loading);
  protected readonly statistics = signal<Section<ProjectStatistics>>(loading);
  protected readonly members = signal<Section<ProjectMember[]>>(loading);
  protected readonly tasks = signal<Section<TaskListResponse>>(loading);

  protected readonly canManage = computed(() => {
    const section = this.project();
    return section.state === 'loaded' && canManageProject(section.data, this.auth.profile());
  });
  protected readonly canManageTeam = computed(() => {
    const section = this.project();
    return section.state === 'loaded' && canManageMembers(section.data, this.auth.profile());
  });
  protected readonly isChangingStatus = signal(false);
  protected readonly projectStatuses = PROJECT_STATUSES;
  protected readonly taskStatuses = TASK_STATUSES;
  protected readonly memberColumns = computed(() => {
    const columns = ['name', 'email', 'role', 'projectRole', 'joined'];
    return this.canManageTeam() ? [...columns, 'actions'] : columns;
  });
  protected readonly taskColumns = ['title', 'status', 'assignee', 'dueDate', 'created'];

  // tasks show the first page only; filters and pagination arrive with the tasks PR.
  private readonly taskPage = { page: 1, pageSize: 20 };

  constructor() {
    this.loadProject();
    this.loadStatistics();
    this.loadMembers();
    this.loadTasks();
  }

  protected loadProject(silent = false): void {
    this.load(this.project, this.projectsApi.getProject(this.projectId), (p) => p, silent);
  }

  protected loadStatistics(silent = false): void {
    this.load(
      this.statistics,
      this.projectsApi.getStatistics(this.projectId),
      (stats) => stats,
      silent,
    );
  }

  protected loadMembers(silent = false): void {
    this.load(
      this.members,
      this.membersApi.listMembers(this.projectId),
      ({ items }) => items,
      silent,
    );
  }

  protected addMember(project: ProjectDetails): void {
    const section = this.members();
    const data: AssignMemberDialogData = {
      projectId: project.id,
      excludedUserIds: [
        project.ownerId,
        ...(section.state === 'loaded' ? section.data.map((m) => m.userId) : []),
      ],
    };

    this.dialog
      .open(AssignMemberDialog, { autoFocus: 'first-tabbable', width: '34rem', data })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((added: boolean | undefined) => {
        if (!added) return;
        this.snackBar.open('Member added.', 'Dismiss', { duration: 4000 });
        this.refreshMembership();
      });
  }

  protected removeMember(member: ProjectMember): void {
    const data: RemoveMemberDialogData = { projectId: this.projectId, member };

    this.dialog
      .open(RemoveMemberDialog, { data })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((removed: boolean | undefined) => {
        if (!removed) return;
        this.snackBar.open(`${member.firstName} ${member.lastName} removed.`, 'Dismiss', {
          duration: 4000,
        });
        this.refreshMembership();
      });
  }

  /** Members and statistics only; the rest of the page is untouched. */
  private refreshMembership(): void {
    this.loadMembers(true);
    this.loadStatistics(true);
  }

  protected loadTasks(): void {
    this.load(this.tasks, this.tasksApi.listTasks(this.projectId, this.taskPage), (tasks) => tasks);
  }

  /** "OnHold" -> "On hold", "InProgress" -> "In progress". */
  protected label(status: string): string {
    return status.replace(
      /([a-z])([A-Z])/g,
      (_, a: string, b: string) => `${a} ${b.toLowerCase()}`,
    );
  }

  protected edit(project: ProjectDetails): void {
    this.dialog
      .open(ProjectFormDialog, { autoFocus: 'first-tabbable', width: '34rem', data: project })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((projectId: string | undefined) => {
        if (!projectId) return;
        this.snackBar.open('Project updated.', 'Dismiss', { duration: 4000 });
        this.loadProject(true);
      });
  }

  protected changeStatus(project: ProjectDetails, status: ProjectStatus): void {
    if (status === project.status || this.isChangingStatus()) return;

    this.isChangingStatus.set(true);
    this.projectsApi
      .changeStatus(project.id, status)
      .pipe(
        finalize(() => this.isChangingStatus.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.snackBar.open('Status changed.', 'Dismiss', { duration: 4000 });
          this.loadProject(true);
        },
        error: (error: unknown) => {
          const status = error instanceof HttpErrorResponse ? error.status : 0;
          this.snackBar.open(
            status === 403
              ? 'You do not have permission to change the status.'
              : status === 404
                ? 'This project no longer exists.'
                : 'The status could not be changed. Please try again.',
            'Dismiss',
            { duration: 6000 },
          );
        },
      });
  }

  protected confirmDelete(project: ProjectDetails): void {
    this.dialog
      .open(DeleteProjectDialog, { data: project })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((deleted: boolean | undefined) => {
        if (!deleted) return;
        this.snackBar.open(`Project "${project.name}" deleted.`, 'Dismiss', { duration: 4000 });
        void this.router.navigateByUrl('/projects');
      });
  }

  /** `silent` refreshes in place: no loading state, and a failed refresh keeps the old data. */
  private load<T, R>(
    target: WritableSignal<Section<T>>,
    request: Observable<R>,
    map: (response: R) => T,
    silent = false,
  ): void {
    if (!silent) target.set(loading);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response) => target.set({ state: 'loaded', data: map(response) }),
      error: (error: unknown) => {
        if (silent) return;
        target.set({
          state: 'error',
          notFound: error instanceof HttpErrorResponse && error.status === 404,
        });
      },
    });
  }
}
