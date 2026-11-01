import { UserProfile } from '../../core/auth/auth.models';
import { ProjectDetails, ProjectMember } from './projects.models';

/** Edit, change status, delete: project owner or global Admin. */
export function canManageProject(
  project: Pick<ProjectDetails, 'ownerId'>,
  user: Pick<UserProfile, 'id' | 'role'> | null,
): boolean {
  return !!user && (user.id === project.ownerId || user.role === 'Admin');
}

/** Assign/remove members: project owner or global Manager/Admin (never `projectRole`). */
export function canManageMembers(
  project: Pick<ProjectDetails, 'ownerId'>,
  user: Pick<UserProfile, 'id' | 'role'> | null,
): boolean {
  return (
    !!user && (user.id === project.ownerId || user.role === 'Admin' || user.role === 'Manager')
  );
}

export interface AssigneeOption {
  userId: string;
  name: string;
}

/** Project owner ∪ members, deduped by userId. The owner may have no membership row. */
export function assigneeOptions(
  project: Pick<ProjectDetails, 'ownerId' | 'ownerName'>,
  members: readonly ProjectMember[],
): AssigneeOption[] {
  const options = new Map<string, AssigneeOption>([
    [project.ownerId, { userId: project.ownerId, name: project.ownerName }],
  ]);

  for (const { userId, firstName, lastName } of members) {
    if (!options.has(userId)) options.set(userId, { userId, name: `${firstName} ${lastName}` });
  }

  return [...options.values()];
}
