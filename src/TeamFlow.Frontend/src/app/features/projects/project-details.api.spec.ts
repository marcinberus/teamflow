import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ProjectMembersApi } from './project-members.api';
import { ProjectsApi } from './projects.api';
import { TasksApi } from './tasks.api';
import { UsersApi } from './users.api';

describe('project details data access', () => {
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('calls the four composed endpoints with GET', () => {
    TestBed.inject(ProjectsApi).getProject('p1').subscribe();
    TestBed.inject(ProjectsApi).getStatistics('p1').subscribe();
    TestBed.inject(ProjectMembersApi).listMembers('p1').subscribe();
    TestBed.inject(TasksApi).listTasks('p1', { page: 1, pageSize: 20 }).subscribe();

    for (const url of [
      '/api/v1/projects/p1',
      '/api/v1/projects/p1/statistics',
      '/api/v1/projects/p1/members',
      '/api/v1/projects/p1/tasks?page=1&pageSize=20',
    ]) {
      expect(httpTesting.expectOne(url).request.method).toBe('GET');
    }
  });

  it('creates, updates and changes task status with the documented requests', () => {
    const tasks = TestBed.inject(TasksApi);
    const body = { title: 'T', description: '', assignedUserId: null, dueDate: null };
    tasks.createTask('p1', body).subscribe();
    tasks.updateTask('p1', 't1', body).subscribe();
    tasks.changeStatus('p1', 't1', 'Done').subscribe();

    const create = httpTesting.expectOne('/api/v1/projects/p1/tasks');
    expect(create.request.method).toBe('POST');
    expect(create.request.body).toEqual(body);
    const update = httpTesting.expectOne('/api/v1/projects/p1/tasks/t1');
    expect(update.request.method).toBe('PUT');
    expect(update.request.body).toEqual(body);
    const status = httpTesting.expectOne('/api/v1/projects/p1/tasks/t1/status');
    expect(status.request.method).toBe('PATCH');
    expect(status.request.body).toEqual({ status: 'Done' });
  });

  it('assigns, removes and searches users with the documented requests', () => {
    const members = TestBed.inject(ProjectMembersApi);
    members.assignMember('p1', { userId: 'u1', projectRole: 'Developer' }).subscribe();
    members.removeMember('p1', 'u1').subscribe();
    TestBed.inject(UsersApi).searchUsers('ann').subscribe();

    const assign = httpTesting.expectOne('/api/v1/projects/p1/members');
    expect(assign.request.method).toBe('POST');
    expect(assign.request.body).toEqual({ userId: 'u1', projectRole: 'Developer' });
    expect(httpTesting.expectOne('/api/v1/projects/p1/members/u1').request.method).toBe('DELETE');
    expect(
      httpTesting.expectOne('/api/v1/users?page=1&pageSize=20&search=ann').request.method,
    ).toBe('GET');
  });
});
