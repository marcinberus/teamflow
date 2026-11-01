import { assigneeOptions } from './project.policies';
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
