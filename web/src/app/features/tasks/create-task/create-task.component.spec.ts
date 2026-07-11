import { provideHttpClient } from '@angular/common/http';
import { HttpErrorResponse } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { TaskService } from '../data-access/task.service';
import type { TaskResponse } from '../models/task.model';
import { CreateTaskComponent } from './create-task.component';

describe('CreateTaskComponent', () => {
  let fixture: ComponentFixture<CreateTaskComponent>;
  let component: CreateTaskComponent;
  let mockTaskService: { createTask: ReturnType<typeof vi.fn> };

  const taskResponse: TaskResponse = {
    id: '01961234-89ab-7cde-f012-3456789abcde',
    title: 'Buy groceries',
    description: null,
    status: 'Pending',
    dueDate: null,
    ownerId: '01961234-5678-7abc-def0-123456789abc',
    createdAt: '2026-07-11T12:00:00Z',
    updatedAt: '2026-07-11T12:00:00Z',
  };

  beforeEach(async () => {
    mockTaskService = { createTask: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [CreateTaskComponent, ReactiveFormsModule],
      providers: [
        { provide: TaskService, useValue: mockTaskService },
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CreateTaskComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('CreateTaskComponent_SubmitWithEmptyTitle_ShowsValidationError', () => {
    component.form.controls.title.setValue('');

    component.onSubmit();
    fixture.detectChanges();

    expect(component.form.controls.title.hasError('required')).toBe(true);
    expect(component.form.controls.title.touched).toBe(true);
    expect(mockTaskService.createTask).not.toHaveBeenCalled();
  });

  it('CreateTaskComponent_SubmitWithWhitespaceOrNbspTitle_ShowsValidationError', () => {
    component.form.controls.title.setValue('      ');

    component.onSubmit();
    fixture.detectChanges();

    expect(component.form.controls.title.hasError('notBlank')).toBe(true);
    expect(mockTaskService.createTask).not.toHaveBeenCalled();
  });

  it('CreateTaskComponent_SubmitWithPastDueDate_ShowsValidationError', () => {
    component.form.controls.title.setValue('Buy groceries');
    component.form.controls.dueDate.setValue('2020-01-01T00:00');

    component.onSubmit();
    fixture.detectChanges();

    expect(component.form.controls.dueDate.hasError('futureDate')).toBe(true);
    expect(mockTaskService.createTask).not.toHaveBeenCalled();
  });

  it('CreateTaskComponent_SubmitDisabledWhileInvalid', () => {
    component.form.controls.title.setValue('');
    fixture.detectChanges();

    const submitButton = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;

    expect(submitButton.disabled).toBe(true);
  });

  it('CreateTaskComponent_SubmitSuccess_ResetsFormAndShowsLoadingState', () => {
    mockTaskService.createTask.mockReturnValue(of(taskResponse));

    component.form.controls.title.setValue('Buy groceries');

    component.onSubmit();

    expect(component.isLoading()).toBe(false);
    expect(component.form.controls.title.value).toBe('');
    expect(component.fieldErrors()).toEqual({});
    expect(component.submitSucceeded()).toBe(true);
  });

  it('CreateTaskComponent_ApiReturns400_MapsDetailsToFieldLevelErrors', () => {
    const errorResponse = new HttpErrorResponse({
      status: 400,
      error: {
        status: 400,
        error: 'VALIDATION_ERROR',
        message: 'Validation failed',
        details: [{ field: 'title', issue: 'title required' }],
      },
    });

    mockTaskService.createTask.mockReturnValue(throwError(() => errorResponse));

    component.form.controls.title.setValue('Buy groceries');

    component.onSubmit();

    expect(component.fieldErrors()).toEqual({ title: 'title required' });
    expect(component.isLoading()).toBe(false);
  });
});
