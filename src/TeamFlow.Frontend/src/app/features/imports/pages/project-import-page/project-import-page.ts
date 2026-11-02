import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { FileImport } from '../../file-import';
import { ImportsApi } from '../../imports.api';

@Component({
  selector: 'app-project-import-page',
  imports: [FileImport, MatButtonModule, RouterLink],
  templateUrl: './project-import-page.html',
})
export class ProjectImportPage {
  private readonly api = inject(ImportsApi);
  protected readonly upload = (file: File) => this.api.importProjects(file);
}
