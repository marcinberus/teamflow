import { HttpErrorResponse } from '@angular/common/http';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { FileImport } from './file-import';
import { ImportProgress } from './imports.api';

let stream: Subject<ImportProgress>;
let uploads: File[];

@Component({
  imports: [FileImport],
  template: `<app-file-import [upload]="upload" noun="projects" />`,
})
class Host {
  upload = (file: File) => {
    uploads.push(file);
    return stream;
  };
}

describe('FileImport', () => {
  function create() {
    const fixture = TestBed.createComponent(Host);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    const drop = (file: File) => {
      const event = new Event('drop') as DragEvent;
      Object.defineProperty(event, 'dataTransfer', { value: { files: [file] } });
      el.querySelector('.dropzone')!.dispatchEvent(event);
      fixture.detectChanges();
    };
    const upload = () => {
      [...el.querySelectorAll('button')].find((b) => b.textContent?.includes('Upload'))!.click();
      fixture.detectChanges();
    };
    return { fixture, el, drop, upload };
  }

  beforeEach(() => {
    stream = new Subject();
    uploads = [];
    TestBed.configureTestingModule({ imports: [Host] });
  });

  it('rejects files that are not .csv or .json', () => {
    const { el, drop } = create();
    drop(new File(['x'], 'notes.txt'));
    expect(el.textContent).toContain('Only .csv and .json files can be imported.');
    expect(el.querySelector('.actions')).toBeNull();
  });

  it('walks uploading -> processing -> success; 100% upload is not done', () => {
    const { fixture, el, drop, upload } = create();
    drop(new File(['a,b'], 'p.csv'));
    upload();
    expect(uploads).toHaveLength(1);

    stream.next({ kind: 'uploading', percent: 40 });
    fixture.detectChanges();
    expect(el.textContent).toContain('Uploading… 40%');

    stream.next({ kind: 'processing' });
    fixture.detectChanges();
    expect(el.textContent).toContain('Processing on the server');
    expect(el.textContent).not.toContain('Imported');

    stream.next({ kind: 'done', count: 3 });
    fixture.detectChanges();
    expect(el.textContent).toContain('Imported 3 projects');
  });

  it('shows a 422 detail as-is, a generic message on 500, and allows re-upload', () => {
    const { fixture, el, drop, upload } = create();
    drop(new File(['x'], 'p.json'));
    upload();
    stream.error(
      new HttpErrorResponse({ status: 422, error: { detail: 'Row 2: name is required.' } }),
    );
    fixture.detectChanges();
    expect(el.querySelector('[role=alert]')?.textContent).toContain('Row 2: name is required.');

    stream = new Subject();
    upload();
    stream.error(new HttpErrorResponse({ status: 500 }));
    fixture.detectChanges();
    expect(uploads).toHaveLength(2);
    expect(el.querySelector('[role=alert]')?.textContent).toContain(
      'The import could not be completed.',
    );
  });
});
