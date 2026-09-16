import { Component } from '@angular/core';

@Component({
  selector: 'app-projects-list-page',
  template: `
    <header class="page-header">
      <h1>Projects</h1>
      <p>Search, filters, pagination, and project creation arrive in Phase 5.</p>
    </header>
    <section class="page-panel" aria-labelledby="projects-placeholder-title">
      <h2 id="projects-placeholder-title">Project workspace</h2>
      <p>The route and responsive application shell are ready for project data.</p>
    </section>
  `,
})
export class ProjectsListPage {}
