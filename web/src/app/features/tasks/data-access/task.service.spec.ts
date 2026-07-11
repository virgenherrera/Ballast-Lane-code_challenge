import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../../environments/environment';
import type { CreateTaskRequest, TaskListResponse, TaskResponse } from '../models/task.model';
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

  it('TaskService_getTasks_NoParams_RequestsListEndpointWithoutQueryString', () => {
    const listResponse: TaskListResponse = {
      items: [],
      paging: { page: 1, perPage: 20, total: 0, prev: null, next: null },
    };

    service.getTasks().subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/tasks`);

    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys().length).toBe(0);
    req.flush(listResponse);
  });

  it('TaskService_getTasks_WithPageAndPerPage_SendsCorrectQueryParams', () => {
    const listResponse: TaskListResponse = {
      items: [],
      paging: { page: 2, perPage: 5, total: 8, prev: null, next: null },
    };

    service.getTasks({ page: 2, perPage: 5 }).subscribe();

    const req = httpMock.expectOne(
      (request) =>
        request.url === `${environment.apiBaseUrl}/api/tasks` &&
        request.params.get('page') === '2' &&
        request.params.get('perPage') === '5',
    );

    expect(req.request.method).toBe('GET');
    req.flush(listResponse);
  });

  it('TaskService_getTasks_WithStatusFilter_EncodesSpaceAsPercent20', () => {
    const listResponse: TaskListResponse = {
      items: [],
      paging: { page: 1, perPage: 20, total: 0, prev: null, next: null },
    };

    service.getTasks({ status: 'In Progress' }).subscribe();

    const req = httpMock.expectOne(
      (request) =>
        request.url === `${environment.apiBaseUrl}/api/tasks` &&
        request.params.get('status') === 'In Progress',
    );

    expect(req.request.urlWithParams).toContain('status=In%20Progress');

    req.flush(listResponse);
  });

  it('TaskService_getTasks_OmitsStatusParam_WhenNotProvided', () => {
    const listResponse: TaskListResponse = {
      items: [],
      paging: { page: 1, perPage: 20, total: 0, prev: null, next: null },
    };

    service.getTasks({ page: 1 }).subscribe();

    const req = httpMock.expectOne(
      (request) =>
        request.url === `${environment.apiBaseUrl}/api/tasks` && !request.params.has('status'),
    );

    req.flush(listResponse);
  });

  it('TaskService_getTasks_ReturnsItemsAndPaging', () => {
    const listResponse: TaskListResponse = {
      items: [{ id: '01961234-89ab-7cde-f012-3456789abcde', title: 'Buy groceries', status: 'Pending', dueDate: null }],
      paging: { page: 1, perPage: 20, total: 1, prev: null, next: null },
    };

    let actual: TaskListResponse | undefined;

    service.getTasks().subscribe((result) => {
      actual = result;
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/tasks`);
    req.flush(listResponse);

    expect(actual).toEqual(listResponse);
  });

  it('TaskService_getTaskById_RequestsCorrectEndpoint', () => {
    const taskId = '01961234-89ab-7cde-f012-3456789abcde';
    let actual: TaskResponse | undefined;

    service.getTaskById(taskId).subscribe((result) => {
      actual = result;
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/tasks/${taskId}`);

    expect(req.request.method).toBe('GET');

    req.flush(response);

    expect(actual).toEqual(response);
  });
});
