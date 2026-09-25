import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { RouterLink } from '@angular/router';
import { EMPTY, Subject, merge } from 'rxjs';
import {
  catchError,
  debounceTime,
  distinctUntilChanged,
  finalize,
  map,
  startWith,
  switchMap,
  tap,
} from 'rxjs/operators';
import { ProjectsApi } from '../../projects.api';
import { PROJECT_STATUSES, ProjectStatus, ProjectSummary } from '../../projects.models';
import { CreateProjectDialog } from './create-project-dialog';

const SEARCH_DEBOUNCE_MS = 300;

@Component({
  selector: 'app-projects-list-page',
  imports: [
    DatePipe,
    MatButtonModule,
    MatButtonToggleModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSnackBarModule,
    MatTableModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: './projects-list-page.html',
  styleUrl: './projects-list-page.scss',
})
export class ProjectsListPage {
  private readonly api = inject(ProjectsApi);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reloadRequests = new Subject<void>();

  protected readonly statuses = PROJECT_STATUSES;
  protected readonly searchControl = new FormControl('', { nonNullable: true });
  protected readonly projects = signal<ProjectSummary[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(20);
  protected readonly status = signal<ProjectStatus | null>(null);
  protected readonly search = signal('');
  protected readonly isLoading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly hasResults = computed(() => this.projects().length > 0);
  protected readonly hasFilters = computed(() => this.status() !== null || this.search() !== '');
  protected readonly displayedColumns = [
    'name',
    'status',
    'owner',
    'tasks',
    'members',
    'created',
    'actions',
  ];

  constructor() {
    merge(
      this.reloadRequests,
      this.searchControl.valueChanges.pipe(
        debounceTime(SEARCH_DEBOUNCE_MS),
        map((search) => search.trim()),
        distinctUntilChanged(),
        tap((search) => {
          this.search.set(search);
          this.page.set(1);
        }),
      ),
    )
      .pipe(
        startWith(undefined),
        switchMap(() => this.loadProjects()),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe();
  }

  protected setStatus(status: ProjectStatus | null): void {
    if (this.status() === status) return;
    this.status.set(status);
    this.page.set(1);
    this.reload();
  }

  protected changePage({ pageIndex, pageSize }: PageEvent): void {
    this.page.set(pageIndex + 1);
    this.pageSize.set(pageSize);
    this.reload();
  }

  protected clearFilters(): void {
    this.status.set(null);
    this.searchControl.setValue('', { emitEvent: false });
    this.search.set('');
    this.page.set(1);
    this.reload();
  }

  protected retry(): void {
    this.reload();
  }

  protected openCreateDialog(): void {
    this.dialog
      .open(CreateProjectDialog, { autoFocus: 'first-tabbable', width: '34rem' })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((projectId: string | undefined) => {
        if (!projectId) return;
        this.snackBar.open('Project created.', 'Dismiss', { duration: 4000 });
        this.reload();
      });
  }

  protected statusLabel(status: ProjectStatus): string {
    return status === 'OnHold' ? 'On hold' : status;
  }

  private reload(): void {
    this.reloadRequests.next();
  }

  private loadProjects() {
    this.isLoading.set(true);
    this.error.set(null);

    return this.api
      .listProjects({
        page: this.page(),
        pageSize: this.pageSize(),
        status: this.status(),
        search: this.search(),
      })
      .pipe(
        tap(({ items, totalCount, page, pageSize }) => {
          this.projects.set(items);
          this.totalCount.set(totalCount);
          this.page.set(page);
          this.pageSize.set(pageSize);
        }),
        catchError((error: unknown) => {
          this.projects.set([]);
          this.totalCount.set(0);
          this.error.set(this.errorMessage(error));
          return EMPTY;
        }),
        finalize(() => this.isLoading.set(false)),
      );
  }

  private errorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse && error.status === 403) {
      return 'You do not have permission to view projects.';
    }

    return 'Projects could not be loaded. Please try again.';
  }
}
