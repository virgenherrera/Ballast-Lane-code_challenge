import { HttpClient, HttpParameterCodec, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type {
  CreateTaskRequest,
  TaskListResponse,
  TaskResponse,
} from '../models/task.model';

// ASP.NET Core's query-string binder does not reliably treat `+` as an
// encoded space the way form-urlencoded bodies do. Using encodeURIComponent
// (which produces %20) avoids any ambiguity for values like "In Progress".
class PercentEncodingCodec implements HttpParameterCodec {
  encodeKey(key: string): string {
    return encodeURIComponent(key);
  }

  encodeValue(value: string): string {
    return encodeURIComponent(value);
  }

  decodeKey(key: string): string {
    return decodeURIComponent(key);
  }

  decodeValue(value: string): string {
    return decodeURIComponent(value);
  }
}

const PARAMETER_CODEC = new PercentEncodingCodec();

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);

  createTask(request: CreateTaskRequest): Observable<TaskResponse> {
    return this.http.post<TaskResponse>(`${environment.apiBaseUrl}/api/tasks`, request);
  }

  getTasks(params?: {
    status?: string;
    page?: number;
    perPage?: number;
  }): Observable<TaskListResponse> {
    let httpParams = new HttpParams({ encoder: PARAMETER_CODEC });

    if (params?.status) {
      httpParams = httpParams.set('status', params.status);
    }

    if (params?.page !== undefined) {
      httpParams = httpParams.set('page', params.page);
    }

    if (params?.perPage !== undefined) {
      httpParams = httpParams.set('perPage', params.perPage);
    }

    return this.http.get<TaskListResponse>(`${environment.apiBaseUrl}/api/tasks`, {
      params: httpParams,
    });
  }

  getTaskById(id: string): Observable<TaskResponse> {
    return this.http.get<TaskResponse>(`${environment.apiBaseUrl}/api/tasks/${id}`);
  }

  updateTask(
    id: string,
    payload: { title?: string; description?: string; status?: string; dueDate?: string },
  ): Observable<TaskResponse> {
    return this.http.patch<TaskResponse>(`${environment.apiBaseUrl}/api/tasks/${id}`, payload);
  }

  deleteTask(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiBaseUrl}/api/tasks/${id}`);
  }
}
