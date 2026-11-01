import { assigneeOptions, canManageMembers, canManageProject } from './project.policies';
import { ProjectMember } from './projects.models';

const member = (userId: string, firstName: string): ProjectMember => ({
  memberId: `m-${userId}`,
  userId,
  email: `${firstName}@example.com`,
  firstName,
  lastName: 'Doe',
  role: 'Developer',
  projectRole: 'Developer',
  joinedAt: '2026-01-01T00:00:00Z',
});

describe('canManageProject', () => {
  const project = { ownerId: 'o' };

  it('allows the owner and any Admin, but not a non-owner Manager or anonymous', () => {
    expect(canManageProject(project, { id: 'o', role: 'Developer' })).toBe(true);
    expect(canManageProject(project, { id: 'x', role: 'Admin' })).toBe(true);
    expect(canManageProject(project, { id: 'x', role: 'Manager' })).toBe(false);
    expect(canManageProject(project, null)).toBe(false);
  });
});

describe('canManageMembers', () => {
  const project = { ownerId: 'o' };

  it('allows the owner, any Manager and any Admin, but not other Developers or anonymous', () => {
    expect(canManageMembers(project, { id: 'o', role: 'Developer' })).toBe(true);
    expect(canManageMembers(project, { id: 'x', role: 'Manager' })).toBe(true);
    expect(canManageMembers(project, { id: 'x', role: 'Admin' })).toBe(true);
    expect(canManageMembers(project, { id: 'x', role: 'Developer' })).toBe(false);
    expect(canManageMembers(project, null)).toBe(false);
  });
});

describe('assigneeOptions', () => {
  const project = { ownerId: 'o', ownerName: 'Olga Owner' };

  it('adds the owner when they have no membership row', () => {
    expect(assigneeOptions(project, [member('a', 'Ann')])).toEqual([
      { userId: 'o', name: 'Olga Owner' },
      { userId: 'a', name: 'Ann Doe' },
    ]);
  });

  it('dedupes the owner when they are also a member', () => {
    expect(assigneeOptions(project, [member('o', 'Olga')])).toEqual([
      { userId: 'o', name: 'Olga Owner' },
    ]);
  });
});
