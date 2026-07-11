import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TASK_STATUSES } from '../models/task-status.enum';
import { TaskStatusFilterComponent } from './task-status-filter.component';

describe('TaskStatusFilterComponent', () => {
  let fixture: ComponentFixture<TaskStatusFilterComponent>;
  let component: TaskStatusFilterComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TaskStatusFilterComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(TaskStatusFilterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('TaskStatusFilterComponent_Renders_AllOptionsIncludingAll', () => {
    const select = fixture.nativeElement.querySelector('select[data-testid="status-filter"]') as HTMLSelectElement;
    const optionValues = Array.from(select.options).map((option) => option.value);

    expect(optionValues).toEqual(['All', ...TASK_STATUSES]);
  });

  it('TaskStatusFilterComponent_ChangeToStatus_EmitsStatusChangeWithValue', () => {
    let emitted: string | null | undefined;

    component.statusChange.subscribe((value) => {
      emitted = value;
    });

    const select = fixture.nativeElement.querySelector('select[data-testid="status-filter"]') as HTMLSelectElement;
    select.value = 'In Progress';
    select.dispatchEvent(new Event('change'));

    expect(emitted).toBe('In Progress');
  });

  it('TaskStatusFilterComponent_ChangeToAll_EmitsNull', () => {
    let emitted: string | null | undefined = 'unset';

    component.statusChange.subscribe((value) => {
      emitted = value;
    });

    const select = fixture.nativeElement.querySelector('select[data-testid="status-filter"]') as HTMLSelectElement;
    select.value = 'All';
    select.dispatchEvent(new Event('change'));

    expect(emitted).toBeNull();
  });

  it('TaskStatusFilterComponent_CurrentStatusInput_ReflectsSelectedValue', () => {
    fixture.componentRef.setInput('currentStatus', 'Completed');
    fixture.detectChanges();

    const select = fixture.nativeElement.querySelector('select[data-testid="status-filter"]') as HTMLSelectElement;

    expect(select.value).toBe('Completed');
  });
});
