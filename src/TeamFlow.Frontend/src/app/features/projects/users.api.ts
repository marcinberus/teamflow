import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/config/api.config';
import { UserListResponse } from './projects.models';

@Injectable({ providedIn: 'root' })
export class UsersApi {
  private readonly http = inject(HttpClient);

  searchUsers(search: string, pageSize = 20): Observable<UserListResponse> {
    let params = new HttpParams().set('page', 1).set('pageSize', pageSize);
    if (search) params = params.set('search', search);

    return this.http.get<UserListResponse>(`${API_BASE_URL}/users`, { params });
  }
}
