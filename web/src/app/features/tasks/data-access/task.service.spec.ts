import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../../environments/environment';
import type { CreateTaskRequest, TaskResponse } from '../models/task.model';
import { TaskService } from './task.service';

describe('TaskService', () => {
  let service: TaskService;
  let httpMock: HttpTestingController;

  const request: CreateTaskRequest = {
    title: 'Buy groceries',
    description: 'Milk, eggs, bread',
    dueDate: '2026-07-18T12:00:00Z',
  };

  const response: TaskResponse = {
    id: '01961234-89ab-7cde-f012-3456789abcde',
    title: 'Buy groceries',
    description: 'Milk, eggs, bread',
    status: 'Pending',
    dueDate: '2026-07-18T12:00:00Z',
    ownerId: '01961234-5678-7abc-def0-123456789abc',
    createdAt: '2026-07-11T12:00:00Z',
    updatedAt: '2026-07-11T12:00:00Z',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(TaskService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('TaskService_createTask_PostsToCorrectEndpoint', () => {
    service.createTask(request).subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/tasks`);

    expect(req.request.method).toBe('POST');
    req.flush(response);
  });

  it('TaskService_createTask_SendsRequestBodyCorrectly', () => {
    let actual: TaskResponse | undefined;

    service.createTask(request).subscribe((result) => {
      actual = result;
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/tasks`);

    expect(req.request.body).toEqual(request);

    req.flush(response);

    expect(actual).toEqual(response);
  });

  it('TaskService_deleteTask_SendsDeleteToCorrectEndpoint', () => {
    const taskId = '01961234-89ab-7cde-f012-3456789abcde';
    service.deleteTask(taskId).subscribe();

    const req = httpMock.expectOne(
      `${environment.apiBaseUrl}/api/tasks/${taskId}`
    );
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });
  });

  it('TaskService_deleteTask_CompletesWithoutErrorOn204', () => {
    const taskId = '01961234-89ab-7cde-f012-3456789abcde';
    let completed = false;
    let errored = false;

    service.deleteTask(taskId).subscribe({
      complete: () => { completed = true; },
      error: () => { errored = true; },
    });

    const req = httpMock.expectOne(
      `${environment.apiBaseUrl}/api/tasks/${taskId}`
    );
    req.flush(null, { status: 204, statusText: 'No Content' });

    expect(completed).toBe(true);
    expect(errored).toBe(false);
  });
});
