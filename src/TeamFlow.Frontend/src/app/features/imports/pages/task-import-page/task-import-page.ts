import { Component } from '@angular/core';

@Component({
  selector: 'app-task-import-page',
  template: `
    <header class="page-header">
      <h1>Import tasks</h1>
      <p>Task imports stay scoped to the project identified by this route.</p>
    </header>
    <section class="page-panel" aria-labelledby="task-import-placeholder-title">
      <h2 id="task-import-placeholder-title">Task files</h2>
      <p>This destination will be linked from project details when imports are built.</p>
    </section>
  `,
})
export class TaskImportPage {}
