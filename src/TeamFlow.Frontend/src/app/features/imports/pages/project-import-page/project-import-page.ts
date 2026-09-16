import { Component } from '@angular/core';

@Component({
  selector: 'app-project-import-page',
  template: `
    <header class="page-header">
      <h1>Import projects</h1>
      <p>CSV and JSON project uploads will be implemented in the import phase.</p>
    </header>
    <section class="page-panel" aria-labelledby="project-import-placeholder-title">
      <h2 id="project-import-placeholder-title">Project files</h2>
      <p>The global import destination is now part of the protected navigation.</p>
    </section>
  `,
})
export class ProjectImportPage {}
