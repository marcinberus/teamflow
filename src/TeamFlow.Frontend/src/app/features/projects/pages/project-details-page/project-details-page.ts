import { Component } from '@angular/core';

@Component({
  selector: 'app-project-details-page',
  template: `
    <header class="page-header">
      <h1>Project details</h1>
      <p>Summary, statistics, members, and tasks will load here independently.</p>
    </header>
    <section class="page-panel" aria-labelledby="details-placeholder-title">
      <h2 id="details-placeholder-title">Project overview</h2>
      <p>The parameterized project route is ready for the Phase 6 feature.</p>
    </section>
  `,
})
export class ProjectDetailsPage {}
