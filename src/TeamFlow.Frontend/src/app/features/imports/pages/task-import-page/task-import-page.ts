import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { FileImport } from '../../file-import';
import { ImportsApi } from '../../imports.api';
import { ProjectsApi } from '../../../projects/projects.api';

@Component({
  selector: 'app-task-import-page',
  imports: [FileImport, MatButtonModule, RouterLink],
  templateUrl: './task-import-page.html',
})
export class TaskImportPage {
  protected readonly projectId = inject(ActivatedRoute).snapshot.paramMap.get('projectId')!;

  private readonly imports = inject(ImportsApi);
  private readonly projects = inject(ProjectsApi);

  protected readonly projectName = signal<string | null>(null);
  protected readonly upload = (file: File) => this.imports.importTasks(this.projectId, file);

  constructor() {
    this.projects.getProject(this.projectId).subscribe({
      next: (project) => this.projectName.set(project.name),
      error: () => undefined, // context only; the import itself reports 404/403
    });
  }
}
