import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { of, Subject } from 'rxjs';
import { vi } from 'vitest';

import { TaskService } from '../data-access/task.service';
import type { TaskListResponse } from '../models/task.model';
import { TaskListComponent } from './task-list.component';

describe('TaskListComponent', () => {
  let fixture: ComponentFixture<TaskListComponent>;
  let mockTaskService: { getTasks: ReturnType<typeof vi.fn> };
  let mockRouter: { navigate: ReturnType<typeof vi.fn> };
  let queryParamMapSubject: Subject<ReturnType<typeof convertToParamMap>>;

  const listResponse: TaskListResponse = {
    items: [
      { id: '01961234-89ab-7cde-f012-3456789abcde', title: 'Buy groceries', status: 'Pending', dueDate: null },
      { id: '01961234-89ab-7cde-f012-3456789abcdf', title: 'Write report', status: 'In Progress', dueDate: '2026-07-20T00:00:00Z' },
    ],
    paging: { page: 1, perPage: 20, total: 2, prev: null, next: null },
  };

  const emptyResponse: TaskListResponse = {
    items: [],
    paging: { page: 1, perPage: 20, total: 0, prev: null, next: null },
  };

  function setup(): void {
    mockTaskService = { getTasks: vi.fn().mockReturnValue(of(listResponse)) };
    mockRouter = { navigate: vi.fn() };
    queryParamMapSubject = new Subject();

    TestBed.configureTestingModule({
      imports: [TaskListComponent],
      providers: [
        { provide: TaskService, useValue: mockTaskService },
        { provide: Router, useValue: mockRouter },
        {
          provide: ActivatedRoute,
          useValue: { queryParamMap: queryParamMapSubject.asObservable(), snapshot: {} },
        },
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    fixture = TestBed.createComponent(TaskListComponent);
    fixture.detectChanges();
  }

  it('TaskListComponent_LoadsTasks_RendersListItems', () => {
    setup();
    queryParamMapSubject.next(convertToParamMap({}));
    fixture.detectChanges();

    const items = fixture.nativeElement.querySelectorAll('[data-testid="task-list-item"]');

    expect(items.length).toBe(2);
    expect(mockTaskService.getTasks).toHaveBeenCalledWith({ page: 1 });
  });

  it('TaskListComponent_NoTasksNoFilter_ShowsNoTasksYetMessage', () => {
    setup();
    mockTaskService.getTasks.mockReturnValue(of(emptyResponse));
    queryParamMapSubject.next(convertToParamMap({}));
    fixture.detectChanges();

    const emptyState = fixture.nativeElement.querySelector('[data-testid="empty-state"]');

    expect(emptyState.textContent).toContain('No tasks yet.');
  });

  it('TaskListComponent_NoTasksWithFilter_ShowsNoMatchMessage', () => {
    setup();
    mockTaskService.getTasks.mockReturnValue(of(emptyResponse));
    queryParamMapSubject.next(convertToParamMap({ status: 'Completed' }));
    fixture.detectChanges();

    const emptyState = fixture.nativeElement.querySelector('[data-testid="empty-state"]');

    expect(emptyState.textContent).toContain('No tasks match the selected filter.');
  });

  it('TaskListComponent_PaginationButtons_DisabledAtBoundaries', () => {
    setup();
    queryParamMapSubject.next(convertToParamMap({}));
    fixture.detectChanges();

    const prevBtn = fixture.nativeElement.querySelector('[data-testid="page-prev"]') as HTMLButtonElement;
    const nextBtn = fixture.nativeElement.querySelector('[data-testid="page-next"]') as HTMLButtonElement;

    expect(prevBtn.disabled).toBe(true);
    expect(nextBtn.disabled).toBe(true);
  });

  it('TaskListComponent_PaginationButtons_EnabledWhenPrevNextAvailable', () => {
    setup();
    mockTaskService.getTasks.mockReturnValue(
      of({
        items: listResponse.items,
        paging: { page: 2, perPage: 20, total: 40, prev: '/api/tasks?page=1', next: '/api/tasks?page=3' },
      }),
    );
    queryParamMapSubject.next(convertToParamMap({ page: '2' }));
    fixture.detectChanges();

    const prevBtn = fixture.nativeElement.querySelector('[data-testid="page-prev"]') as HTMLButtonElement;
    const nextBtn = fixture.nativeElement.querySelector('[data-testid="page-next"]') as HTMLButtonElement;

    expect(prevBtn.disabled).toBe(false);
    expect(nextBtn.disabled).toBe(false);
  });

  it('TaskListComponent_StatusFilterChange_NavigatesWithStatusAndResetsPage', () => {
    setup();
    queryParamMapSubject.next(convertToParamMap({ page: '3' }));
    fixture.detectChanges();

    component().onStatusChange('Completed');

    expect(mockRouter.navigate).toHaveBeenCalledWith([], expect.objectContaining({
      queryParams: { status: 'Completed', page: null },
      queryParamsHandling: 'merge',
    }));
  });

  it('TaskListComponent_StatusFilterChangeToAll_NavigatesWithNullStatus', () => {
    setup();
    queryParamMapSubject.next(convertToParamMap({ status: 'Pending' }));
    fixture.detectChanges();

    component().onStatusChange(null);

    expect(mockRouter.navigate).toHaveBeenCalledWith([], expect.objectContaining({
      queryParams: { status: null, page: null },
      queryParamsHandling: 'merge',
    }));
  });

  it('TaskListComponent_TaskRow_LinksToDetailRoute', () => {
    setup();
    queryParamMapSubject.next(convertToParamMap({}));
    fixture.detectChanges();

    const link = fixture.nativeElement.querySelector('[data-testid="task-list-item"] a') as HTMLAnchorElement;

    expect(link).toBeTruthy();
    expect(link.textContent).toContain(listResponse.items[0].title);
  });

  it('TaskListComponent_DeepLinkWithStatusAndPage_RestoresStateFromUrl', () => {
    setup();
    queryParamMapSubject.next(convertToParamMap({ status: 'In Progress', page: '2' }));
    fixture.detectChanges();

    expect(mockTaskService.getTasks).toHaveBeenCalledWith({ status: 'In Progress', page: 2 });
  });

  function component(): TaskListComponent {
    return fixture.componentInstance;
  }
});
