import { UserRole } from '../../core/auth/auth.models';

export const PROJECT_STATUSES = ['Active', 'OnHold', 'Completed'] as const;

export type ProjectStatus = (typeof PROJECT_STATUSES)[number];

export interface ListProjectsParams {
  page?: number;
  pageSize?: number;
  status?: ProjectStatus | null;
  search?: string | null;
}

export interface ProjectSummary {
  id: string;
  name: string;
  description: string;
  status: ProjectStatus;
  ownerName: string;
  taskCount: number;
  memberCount: number;
  createdAt: string;
}

export interface ProjectListResponse {
  items: ProjectSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CreateProjectRequest {
  name: string;
  description: string;
}

export interface CreateProjectResponse {
  projectId: string;
}

export type UpdateProjectRequest = CreateProjectRequest;

export interface ChangeProjectStatusRequest {
  status: ProjectStatus;
}

export interface ProjectDetails {
  id: string;
  name: string;
  description: string;
  status: ProjectStatus;
  ownerId: string;
  ownerName: string;
  createdAt: string;
  updatedAt: string | null;
}

export const TASK_STATUSES = ['Todo', 'InProgress', 'Verification', 'Done', 'Cancelled'] as const;

export type TaskStatus = (typeof TASK_STATUSES)[number];

export interface ProjectStatistics {
  projectId: string;
  totalTasks: number;
  tasksByStatus: Record<TaskStatus, number>;
  totalMembers: number;
  // backend returns two-decimal text, e.g. "66.67"
  completionPercentage: string;
}

export interface ProjectMember {
  memberId: string;
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  // global user role
  role: UserRole;
  // role held in this project
  projectRole: UserRole;
  joinedAt: string;
}

export interface ProjectMemberListResponse {
  items: ProjectMember[];
}

export interface AssignProjectMemberRequest {
  userId: string;
  projectRole: UserRole;
}

export interface UserSummary {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
}

export interface UserListResponse {
  items: UserSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ListTasksParams {
  status?: TaskStatus | null;
  assignedUserId?: string | null;
  page?: number;
  pageSize?: number;
}

export interface TaskItem {
  id: string;
  title: string;
  description: string;
  status: TaskStatus;
  assignedUserId: string | null;
  assigneeName: string | null;
  dueDate: string | null;
  createdAt: string;
}

export interface TaskListResponse {
  items: TaskItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}
