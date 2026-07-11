import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type { CreateTaskRequest, TaskResponse } from '../models/task.model';

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);

  createTask(request: CreateTaskRequest): Observable<TaskResponse> {
    return this.http.post<TaskResponse>(`${environment.apiBaseUrl}/api/tasks`, request);
  }

  // TEMPORARY: no filtering/sorting/pagination. Superseded by US-005.
  getTasks(): Observable<TaskResponse[]> {
    return this.http.get<TaskResponse[]>(`${environment.apiBaseUrl}/api/tasks`);
  }

  updateTask(
    id: string,
    payload: { title?: string; description?: string; status?: string; dueDate?: string },
  ): Observable<TaskResponse> {
    return this.http.patch<TaskResponse>(`${environment.apiBaseUrl}/api/tasks/${id}`, payload);
  }
}
