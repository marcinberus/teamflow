import { HttpEventType } from '@angular/common/http';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ImportProgress, ImportsApi } from './imports.api';

describe('ImportsApi', () => {
  let api: ImportsApi;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(ImportsApi);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('posts multipart field "file" and reports upload, processing and done', () => {
    const file = new File(['"a","b"'], 'projects.csv', { type: 'text/csv' });
    const seen: ImportProgress[] = [];
    api.importProjects(file).subscribe((p) => seen.push(p));

    const req = httpTesting.expectOne('/api/v1/projects/import');
    expect(req.request.method).toBe('POST');
    expect((req.request.body as FormData).get('file')).toBe(file);

    req.event({ type: HttpEventType.UploadProgress, loaded: 50, total: 100 });
    req.event({ type: HttpEventType.UploadProgress, loaded: 100, total: 100 });
    req.flush({ projectIds: ['1', '2'] });

    expect(seen).toEqual([
      { kind: 'uploading', percent: 0 },
      { kind: 'uploading', percent: 50 },
      { kind: 'processing' },
      { kind: 'done', count: 2 },
    ]);
  });

  it('targets the project task import endpoint and counts taskItemsIds', () => {
    let last: ImportProgress | undefined;
    api.importTasks('p1', new File(['[]'], 't.json')).subscribe((p) => (last = p));

    const req = httpTesting.expectOne('/api/v1/projects/p1/tasks/import');
    req.flush({ taskItemsIds: ['a'] });

    expect(last).toEqual({ kind: 'done', count: 1 });
  });
});
