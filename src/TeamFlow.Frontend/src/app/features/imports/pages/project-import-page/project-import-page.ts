import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { FileImport } from '../../file-import';
import { ImportsApi } from '../../imports.api';

@Component({
  selector: 'app-project-import-page',
  imports: [FileImport, MatButtonModule, RouterLink],
  template: `
    <header class="page-header">
      <h1>Import projects</h1>
      <p>Upload a CSV or JSON file to create several projects at once.</p>
    </header>
    <section class="page-panel" aria-labelledby="project-import-title">
      <h2 id="project-import-title">Project file</h2>
      <app-file-import
        noun="projects"
        forbiddenMessage="You do not have permission to import projects."
        [upload]="upload"
      >
        <a success mat-button routerLink="/projects">Back to projects</a>
        <h3>Accepted formats</h3>
        <p><strong>CSV</strong> — no header row. Each row has exactly two quoted fields:</p>
        <pre>"Name","Description"</pre>
        <p>Fields must be quoted and cannot contain quote characters.</p>
        <p><strong>JSON</strong> — a top-level array of objects:</p>
        <pre>[{{ '{' }} "name": "Website Redesign", "description": "..." {{ '}' }}]</pre>
      </app-file-import>
    </section>
  `,
})
export class ProjectImportPage {
  private readonly api = inject(ImportsApi);
  protected readonly upload = (file: File) => this.api.importProjects(file);
}
