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
