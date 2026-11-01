import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/config/api.config';
import { AssignProjectMemberRequest, ProjectMemberListResponse } from './projects.models';

@Injectable({ providedIn: 'root' })
export class ProjectMembersApi {
  private readonly http = inject(HttpClient);

  listMembers(projectId: string): Observable<ProjectMemberListResponse> {
    return this.http.get<ProjectMemberListResponse>(
      `${API_BASE_URL}/projects/${projectId}/members`,
    );
  }

  assignMember(projectId: string, request: AssignProjectMemberRequest): Observable<void> {
    return this.http.post<void>(`${API_BASE_URL}/projects/${projectId}/members`, request);
  }

  removeMember(projectId: string, userId: string): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/projects/${projectId}/members/${userId}`);
  }
}
