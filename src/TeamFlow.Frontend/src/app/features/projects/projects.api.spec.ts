import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ProjectsApi } from './projects.api';

describe('ProjectsApi', () => {
  let api: ProjectsApi;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ProjectsApi, provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(ProjectsApi);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('uses server-side paging and filter contracts', () => {
    api.listProjects({ page: 2, pageSize: 20, status: 'OnHold', search: 'portal' }).subscribe();

    const listRequest = httpTesting.expectOne(
      '/api/v1/projects?page=2&pageSize=20&status=OnHold&search=portal',
    );
    expect(listRequest.request.method).toBe('GET');
    listRequest.flush({ items: [], totalCount: 0, page: 2, pageSize: 20 });

    const project = { name: 'Customer portal', description: 'A self-service workspace.' };
    api.createProject(project).subscribe();
    const createRequest = httpTesting.expectOne('/api/v1/projects');
    expect(createRequest.request.method).toBe('POST');
    expect(createRequest.request.body).toEqual(project);
    createRequest.flush({ projectId: 'project-1' });
  });
});
