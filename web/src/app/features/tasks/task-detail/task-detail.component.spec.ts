import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { convertToParamMap, ActivatedRoute } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { vi } from 'vitest';

import { TaskService } from '../data-access/task.service';
import type { TaskResponse } from '../models/task.model';
import { TaskDetailComponent } from './task-detail.component';

describe('TaskDetailComponent', () => {
  let fixture: ComponentFixture<TaskDetailComponent>;
  let mockTaskService: { getTaskById: ReturnType<typeof vi.fn> };
  let paramMapSubject: Subject<ReturnType<typeof convertToParamMap>>;

  const taskId = '01961234-89ab-7cde-f012-3456789abcde';

  const fullTask: TaskResponse = {
    id: taskId,
    title: 'Buy groceries',
    description: 'Milk, eggs, bread',
    status: 'Pending',
    dueDate: '2026-07-18T12:00:00Z',
    ownerId: '01961234-5678-7abc-def0-123456789abc',
    createdAt: '2026-07-11T12:00:00Z',
    updatedAt: '2026-07-11T12:00:00Z',
  };

  function setup(): void {
    mockTaskService = { getTaskById: vi.fn().mockReturnValue(of(fullTask)) };
    paramMapSubject = new Subject();

    TestBed.configureTestingModule({
      imports: [TaskDetailComponent],
      providers: [
        { provide: TaskService, useValue: mockTaskService },
        {
          provide: ActivatedRoute,
          useValue: { paramMap: paramMapSubject.asObservable() },
        },
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    fixture = TestBed.createComponent(TaskDetailComponent);
    fixture.detectChanges();
  }

  it('TaskDetailComponent_LoadsTask_RendersAllFields', () => {
    setup();
    mockTaskService.getTaskById.mockReturnValue(of(fullTask));
    paramMapSubject.next(convertToParamMap({ id: taskId }));
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';

    expect(text).toContain('Buy groceries');
    expect(text).toContain('Pending');
    expect(text).toContain('Milk, eggs, bread');
    expect(text).toContain('2026-07-18T12:00:00Z');
    expect(text).toContain(fullTask.ownerId);
    expect(text).toContain(fullTask.createdAt);
    expect(text).toContain(fullTask.updatedAt);
    expect(text).toContain(fullTask.id);
  });

  it('TaskDetailComponent_NullDescriptionAndDueDate_RendersFallbackText', () => {
    setup();

    const taskWithNulls: TaskResponse = { ...fullTask, description: null, dueDate: null };
    mockTaskService.getTaskById.mockReturnValue(of(taskWithNulls));
    paramMapSubject.next(convertToParamMap({ id: taskId }));
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';

    expect(text).toContain('No description');
    expect(text).toContain('No due date');
  });

  it('TaskDetailComponent_ApiReturns404_ShowsNotFoundMessage', () => {
    setup();

    const notFoundError = new HttpErrorResponse({ status: 404 });
    mockTaskService.getTaskById.mockReturnValue(throwError(() => notFoundError));
    paramMapSubject.next(convertToParamMap({ id: taskId }));
    fixture.detectChanges();

    const notFoundEl = fixture.nativeElement.querySelector('[data-testid="task-not-found"]');

    expect(notFoundEl).toBeTruthy();
    expect(notFoundEl.textContent).toContain('Task not found.');
  });

  it('TaskDetailComponent_HasTaskDetailTestId', () => {
    setup();
    mockTaskService.getTaskById.mockReturnValue(of(fullTask));
    paramMapSubject.next(convertToParamMap({ id: taskId }));
    fixture.detectChanges();

    const container = fixture.nativeElement.querySelector('[data-testid="task-detail"]');

    expect(container).toBeTruthy();
  });
});
