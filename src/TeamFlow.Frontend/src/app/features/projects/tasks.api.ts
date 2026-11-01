import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/config/api.config';
import { ListTasksParams, TaskListResponse } from './projects.models';

@Injectable({ providedIn: 'root' })
export class TasksApi {
  private readonly http = inject(HttpClient);

  listTasks(
    projectId: string,
    { status, assignedUserId, page, pageSize }: ListTasksParams = {},
  ): Observable<TaskListResponse> {
    let params = new HttpParams();

    if (page) params = params.set('page', page);
    if (pageSize) params = params.set('pageSize', pageSize);
    if (status) params = params.set('status', status);
    if (assignedUserId) params = params.set('assignedUserId', assignedUserId);

    return this.http.get<TaskListResponse>(`${API_BASE_URL}/projects/${projectId}/tasks`, {
      params,
    });
  }
}
