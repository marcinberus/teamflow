import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/config/api.config';
import {
  CreateProjectRequest,
  CreateProjectResponse,
  ListProjectsParams,
  ProjectDetails,
  ProjectListResponse,
  ProjectStatistics,
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

  getStatistics(projectId: string): Observable<ProjectStatistics> {
    return this.http.get<ProjectStatistics>(`${API_BASE_URL}/projects/${projectId}/statistics`);
  }
}
