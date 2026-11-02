import { HttpErrorResponse } from '@angular/common/http';
import { Component, input, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { finalize, Observable } from 'rxjs';
import { ImportProgress } from './imports.api';

const ACCEPT = '.csv,.json,application/json,text/csv';
const ALLOWED_EXTENSIONS = ['csv', 'json'];

/** Drop zone + upload for one import. Pages supply the request and the format help (projected). */
@Component({
  selector: 'app-file-import',
  imports: [MatButtonModule, MatIconModule, MatProgressBarModule],
  styleUrl: './file-import.scss',
  templateUrl: './file-import.html',
})
export class FileImport {
  readonly upload = input.required<(file: File) => Observable<ImportProgress>>();
  /** Plural noun for the success message, e.g. "projects". */
  readonly noun = input.required<string>();
  /** Shown on 403/404, which differ per import. */
  readonly forbiddenMessage = input('You do not have permission to import here.');

  protected readonly accept = ACCEPT;
  protected readonly file = signal<File | null>(null);
  protected readonly fileError = signal<string | null>(null);
  protected readonly isDragging = signal(false);
  protected readonly state = signal<
    { kind: 'idle' } | { kind: 'error' } | { kind: 'success' } | ImportProgress
  >({ kind: 'idle' });
  protected readonly percent = signal(0);
  protected readonly successMessage = signal('');
  protected readonly errorMessage = signal('');

  protected isBusy(): boolean {
    const kind = this.state().kind;
    return kind === 'uploading' || kind === 'processing';
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    if (!this.isBusy()) this.isDragging.set(true);
  }

  protected onDragLeave(event: DragEvent): void {
    // Leaving into a child element is not leaving the zone.
    if (!(event.currentTarget as HTMLElement).contains(event.relatedTarget as Node | null)) {
      this.isDragging.set(false);
    }
  }

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(false);
    const dropped = event.dataTransfer?.files[0];
    if (dropped && !this.isBusy()) this.select(dropped);
  }

  protected onBrowse(input: HTMLInputElement): void {
    const picked = input.files?.[0];
    input.value = ''; // allow re-selecting the same file after a correction
    if (picked) this.select(picked);
  }

  protected clear(): void {
    this.file.set(null);
    this.fileError.set(null);
    this.state.set({ kind: 'idle' });
  }

  protected start(): void {
    const file = this.file();
    if (!file || this.isBusy()) return;

    this.state.set({ kind: 'uploading', percent: 0 });
    this.percent.set(0);
    this.upload()(file)
      .pipe(
        finalize(() => {
          if (this.isBusy()) this.state.set({ kind: 'idle' });
        }),
      )
      .subscribe({
        next: (progress) => {
          if (progress.kind === 'uploading') this.percent.set(progress.percent);
          if (progress.kind === 'done') {
            this.successMessage.set(`Imported ${progress.count} ${this.noun()}`);
            this.state.set({ kind: 'success' });
          } else {
            this.state.set(progress);
          }
        },
        error: (error: unknown) => {
          this.errorMessage.set(this.message(error));
          this.state.set({ kind: 'error' });
        },
      });
  }

  protected typeLabel(file: File): string {
    return file.type || this.extension(file).toUpperCase();
  }

  protected sizeLabel(file: File): string {
    return file.size < 1024 ? `${file.size} B` : `${(file.size / 1024).toFixed(1)} KB`;
  }

  private select(file: File): void {
    if (!ALLOWED_EXTENSIONS.includes(this.extension(file))) {
      this.fileError.set('Only .csv and .json files can be imported.');
      return;
    }

    this.fileError.set(null);
    this.file.set(file);
    this.state.set({ kind: 'idle' });
  }

  private extension(file: File): string {
    return file.name.split('.').pop()?.toLowerCase() ?? '';
  }

  private message(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 422 && typeof error.error?.detail === 'string')
        return error.error.detail;
      if (error.status === 403) return this.forbiddenMessage();
      if (error.status === 404) return 'The target of this import no longer exists.';
    }
    return 'The import could not be completed.';
  }
}
