import { TestBed } from '@angular/core/testing';
import { FormGroup } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { of } from 'rxjs';
import { TaskItem } from '../../projects.models';
import { TasksApi } from '../../tasks.api';
import { TaskFormDialog, TaskFormDialogData } from './task-form-dialog';

describe('TaskFormDialog', () => {
  const api = { createTask: vi.fn(), updateTask: vi.fn() };
  const dialogRef = { close: vi.fn() };

  function create(task?: TaskItem) {
    const data: TaskFormDialogData = { projectId: 'p1', assignees: [], task };
    TestBed.configureTestingModule({
      imports: [TaskFormDialog],
      providers: [
        { provide: TasksApi, useValue: api },
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: data },
      ],
    });
    return TestBed.createComponent(TaskFormDialog).componentInstance as unknown as {
      form: FormGroup;
      submit(): void;
    };
  }

  beforeEach(() => vi.clearAllMocks());

  it('requires a title and rejects a past due date', () => {
    const { form } = create();
    expect(form.controls['title'].hasError('required')).toBe(true);
    form.controls['dueDate'].setValue('2000-01-01');
    expect(form.controls['dueDate'].hasError('past')).toBe(true);
  });

  it('creates with a null assignee ("Me") and the due date at end of the local day', () => {
    api.createTask.mockReturnValue(of({ taskId: 't1' }));
    const c = create();
    c.form.patchValue({ title: ' New ', dueDate: '2999-05-06' });
    c.submit();
    expect(api.createTask).toHaveBeenCalledWith('p1', {
      title: 'New',
      description: '',
      assignedUserId: null,
      dueDate: new Date('2999-05-06T23:59:59').toISOString(),
    });
    expect(dialogRef.close).toHaveBeenCalledWith(true);
  });

  it('updates, sending an untouched past due date back unchanged and null as unassigned', () => {
    api.updateTask.mockReturnValue(of(undefined));
    const dueDate = '2020-01-02T10:00:00Z';
    const c = create({
      id: 't1',
      title: 'Old',
      description: '',
      status: 'Todo',
      assignedUserId: null,
      assigneeName: null,
      dueDate,
      createdAt: '2019-01-01T00:00:00Z',
    });
    expect(c.form.valid).toBe(true);
    c.submit();
    expect(api.createTask).not.toHaveBeenCalled();
    expect(api.updateTask).toHaveBeenCalledWith('p1', 't1', {
      title: 'Old',
      description: '',
      assignedUserId: null,
      dueDate,
    });
  });
});
