import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { UserProfile } from '../../../../core/auth/auth.models';
import { AuthStore } from '../../../../core/auth/auth.store';
import { ProjectMembersApi } from '../../project-members.api';
import { ProjectsApi } from '../../projects.api';
import { ProjectMember } from '../../projects.models';
import { TasksApi } from '../../tasks.api';
import { ProjectDetailsPage } from './project-details-page';

describe('ProjectDetailsPage permission-sensitive actions', () => {
  const project = {
    id: 'p1',
    name: 'Alpha',
    description: '',
    status: 'Active',
    ownerId: 'owner',
    ownerName: 'Olga Owner',
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: null,
  };
  const member = (userId: string): ProjectMember => ({
    memberId: `m-${userId}`,
    userId,
    email: `${userId}@example.com`,
    firstName: userId,
    lastName: 'Member',
    role: 'Developer',
    projectRole: 'Developer',
    joinedAt: '2026-01-02T00:00:00Z',
  });

  function actions(user: Pick<UserProfile, 'id' | 'role'>, members: ProjectMember[]) {
    TestBed.configureTestingModule({
      imports: [ProjectDetailsPage],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: new Map([['projectId', 'p1']]) } },
        },
        { provide: AuthStore, useValue: { profile: signal(user) } },
        {
          provide: ProjectsApi,
          useValue: {
            getProject: () => of(project),
            getStatistics: () => of({ tasksByStatus: {} }),
          },
        },
        { provide: ProjectMembersApi, useValue: { listMembers: () => of({ items: members }) } },
        {
          provide: TasksApi,
          useValue: { listTasks: () => of({ items: [], totalCount: 0, page: 1, pageSize: 20 }) },
        },
      ],
    });
    const fixture = TestBed.createComponent(ProjectDetailsPage);
    fixture.detectChanges();
    return [...(fixture.nativeElement as HTMLElement).querySelectorAll('button')].map(
      (b) => b.getAttribute('aria-label') ?? b.textContent?.trim(),
    );
  }

  it('owner manages project, team and tasks, but cannot be removed', () => {
    const labels = actions({ id: 'owner', role: 'Developer' }, [member('owner'), member('dev')]);
    expect(labels).toEqual(expect.arrayContaining(['Edit', 'Delete', 'Add member', 'New task']));
    expect(labels).toContain('Remove dev Member');
    expect(labels).not.toContain('Remove owner Member');
  });

  it('a non-owner Manager manages members but not the project', () => {
    const labels = actions({ id: 'mgr', role: 'Manager' }, [member('mgr')]);
    expect(labels).toContain('Add member');
    expect(labels).not.toContain('Edit');
    expect(labels).not.toContain('Delete');
  });

  it('an unrelated Admin manages the project but cannot change tasks', () => {
    const labels = actions({ id: 'admin', role: 'Admin' }, [member('dev')]);
    expect(labels).toEqual(expect.arrayContaining(['Edit', 'Delete', 'Add member']));
    expect(labels).not.toContain('New task');
  });

  it('a plain member can change tasks but sees no management actions', () => {
    const labels = actions({ id: 'dev', role: 'Developer' }, [member('dev')]);
    expect(labels).toContain('New task');
    expect(labels).not.toContain('Edit');
    expect(labels).not.toContain('Add member');
    expect(labels.some((l) => l?.startsWith('Remove'))).toBe(false);
  });
});
