import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/config/api.config';
import {
  ChangeProjectStatusRequest,
  CreateProjectRequest,
  CreateProjectResponse,
  ListProjectsParams,
  ProjectDetails,
  ProjectListResponse,
  ProjectStatistics,
  ProjectStatus,
  UpdateProjectRequest,
} from './projects.models';

@Injectable({ providedIn: 'root' })
export class ProjectsApi {
  private readonly http = inject(HttpClient);

  listProjects({
    page,
    pageSize,
    status,
    search,
  }: ListProjectsParams): Observable<ProjectListResponse> {
    let params = new HttpParams();

    if (page) params = params.set('page', page);
    if (pageSize) params = params.set('pageSize', pageSize);
    if (status) params = params.set('status', status);
    if (search) params = params.set('search', search);

    return this.http.get<ProjectListResponse>(`${API_BASE_URL}/projects`, { params });
  }

  createProject(request: CreateProjectRequest): Observable<CreateProjectResponse> {
    return this.http.post<CreateProjectResponse>(`${API_BASE_URL}/projects`, request);
  }

  getProject(projectId: string): Observable<ProjectDetails> {
    return this.http.get<ProjectDetails>(`${API_BASE_URL}/projects/${projectId}`);
  }

  updateProject(projectId: string, request: UpdateProjectRequest): Observable<void> {
    return this.http.put<void>(`${API_BASE_URL}/projects/${projectId}`, request);
  }

  changeStatus(projectId: string, status: ProjectStatus): Observable<void> {
    const request: ChangeProjectStatusRequest = { status };
    return this.http.patch<void>(`${API_BASE_URL}/projects/${projectId}/status`, request);
  }

  deleteProject(projectId: string): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/projects/${projectId}`);
  }

  getStatistics(projectId: string): Observable<ProjectStatistics> {
    return this.http.get<ProjectStatistics>(`${API_BASE_URL}/projects/${projectId}/statistics`);
  }
}
