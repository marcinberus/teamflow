import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, WritableSignal, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { ProjectMembersApi } from '../../project-members.api';
import { ProjectsApi } from '../../projects.api';
import {
  ProjectDetails,
  ProjectMember,
  ProjectStatistics,
  TASK_STATUSES,
  TaskListResponse,
} from '../../projects.models';
import { TasksApi } from '../../tasks.api';

type Section<T> =
  { state: 'loading' } | { state: 'error'; notFound: boolean } | { state: 'loaded'; data: T };

const loading = { state: 'loading' } as const;

@Component({
  selector: 'app-project-details-page',
  imports: [DatePipe, MatButtonModule, MatProgressBarModule, MatTableModule, RouterLink],
  templateUrl: './project-details-page.html',
  styleUrl: './project-details-page.scss',
})
export class ProjectDetailsPage {
  private readonly projectId = inject(ActivatedRoute).snapshot.paramMap.get('projectId')!;
  private readonly projectsApi = inject(ProjectsApi);
  private readonly membersApi = inject(ProjectMembersApi);
  private readonly tasksApi = inject(TasksApi);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly project = signal<Section<ProjectDetails>>(loading);
  protected readonly statistics = signal<Section<ProjectStatistics>>(loading);
  protected readonly members = signal<Section<ProjectMember[]>>(loading);
  protected readonly tasks = signal<Section<TaskListResponse>>(loading);

  protected readonly taskStatuses = TASK_STATUSES;
  protected readonly memberColumns = ['name', 'email', 'role', 'projectRole', 'joined'];
  protected readonly taskColumns = ['title', 'status', 'assignee', 'dueDate', 'created'];

  // tasks show the first page only; filters and pagination arrive with the tasks PR.
  private readonly taskPage = { page: 1, pageSize: 20 };

  constructor() {
    this.loadProject();
    this.loadStatistics();
    this.loadMembers();
    this.loadTasks();
  }

  protected loadProject(): void {
    this.load(this.project, this.projectsApi.getProject(this.projectId), (project) => project);
  }

  protected loadStatistics(): void {
    this.load(this.statistics, this.projectsApi.getStatistics(this.projectId), (stats) => stats);
  }

  protected loadMembers(): void {
    this.load(this.members, this.membersApi.listMembers(this.projectId), ({ items }) => items);
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

  private load<T, R>(
    target: WritableSignal<Section<T>>,
    request: Observable<R>,
    map: (response: R) => T,
  ): void {
    target.set(loading);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response) => target.set({ state: 'loaded', data: map(response) }),
      error: (error: unknown) =>
        target.set({
          state: 'error',
          notFound: error instanceof HttpErrorResponse && error.status === 404,
        }),
    });
  }
}
