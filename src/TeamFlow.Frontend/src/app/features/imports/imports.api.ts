import { HttpClient, HttpEvent, HttpEventType } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { filter, map, Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/config/api.config';

export interface ImportProjectsResponse {
  projectIds: string[];
}

export interface ImportTasksResponse {
  taskItemsIds: string[];
}

/** 100% upload is not "done": `processing` lasts until the server answers. */
export type ImportProgress =
  { kind: 'uploading'; percent: number } | { kind: 'processing' } | { kind: 'done'; count: number };

export function toImportProgress<T>(
  event: HttpEvent<T>,
  count: (body: T) => number,
): ImportProgress | null {
  switch (event.type) {
    case HttpEventType.Sent:
      return { kind: 'uploading', percent: 0 };
    case HttpEventType.UploadProgress: {
      if (!event.total) return null;
      return event.loaded >= event.total
        ? { kind: 'processing' }
        : { kind: 'uploading', percent: Math.floor((event.loaded / event.total) * 100) };
    }
    case HttpEventType.Response:
      return { kind: 'done', count: event.body ? count(event.body) : 0 };
    default:
      return null;
  }
}

@Injectable({ providedIn: 'root' })
export class ImportsApi {
  private readonly http = inject(HttpClient);

  importProjects(file: File): Observable<ImportProgress> {
    return this.upload<ImportProjectsResponse>(
      `${API_BASE_URL}/projects/import`,
      file,
      (body) => body.projectIds.length,
    );
  }

  importTasks(projectId: string, file: File): Observable<ImportProgress> {
    return this.upload<ImportTasksResponse>(
      `${API_BASE_URL}/projects/${projectId}/tasks/import`,
      file,
      (body) => body.taskItemsIds.length,
    );
  }

  private upload<T>(
    url: string,
    file: File,
    count: (body: T) => number,
  ): Observable<ImportProgress> {
    const body = new FormData();
    body.append('file', file);

    return this.http.post<T>(url, body, { observe: 'events', reportProgress: true }).pipe(
      map((event) => toImportProgress(event, count)),
      filter((progress) => progress !== null),
    );
  }
}
