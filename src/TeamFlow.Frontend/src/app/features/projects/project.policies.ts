import { ProjectDetails, ProjectMember } from './projects.models';

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
