import { TestBed } from '@angular/core/testing';
import { FormGroup } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { of } from 'rxjs';
import { ProjectFormDialog } from './project-form-dialog';
import { ProjectsApi } from './projects.api';
import { ProjectDetails } from './projects.models';

describe('ProjectFormDialog', () => {
  const api = { createProject: vi.fn(), updateProject: vi.fn() };
  const dialogRef = { close: vi.fn() };

  function create(project: ProjectDetails | null = null) {
    TestBed.configureTestingModule({
      imports: [ProjectFormDialog],
      providers: [
        { provide: ProjectsApi, useValue: api },
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: project },
      ],
    });
    return TestBed.createComponent(ProjectFormDialog).componentInstance as unknown as {
      form: FormGroup;
      submit(): void;
    };
  }

  beforeEach(() => vi.clearAllMocks());

  it('validates name (required, max 200) and description (max 2000)', () => {
    const { form } = create();
    expect(form.controls['name'].hasError('required')).toBe(true);
    form.controls['name'].setValue('x'.repeat(201));
    expect(form.controls['name'].hasError('maxlength')).toBe(true);
    form.controls['name'].setValue('x'.repeat(200));
    form.controls['description'].setValue('d'.repeat(2001));
    expect(form.invalid).toBe(true);
    form.controls['description'].setValue('d'.repeat(2000));
    expect(form.valid).toBe(true);
  });

  it('does not call the API while invalid', () => {
    create().submit();
    expect(api.createProject).not.toHaveBeenCalled();
  });

  it('creates and closes with the new id', () => {
    api.createProject.mockReturnValue(of({ projectId: 'p9' }));
    const c = create();
    c.form.setValue({ name: 'Alpha', description: '' });
    c.submit();
    expect(api.createProject).toHaveBeenCalledWith({ name: 'Alpha', description: '' });
    expect(dialogRef.close).toHaveBeenCalledWith('p9');
  });

  it('edits the given project instead of creating', () => {
    api.updateProject.mockReturnValue(of(undefined));
    const c = create({ id: 'p1', name: 'Old', description: 'd' } as ProjectDetails);
    expect(c.form.getRawValue()).toEqual({ name: 'Old', description: 'd' });
    c.form.controls['name'].setValue('New');
    c.submit();
    expect(api.updateProject).toHaveBeenCalledWith('p1', { name: 'New', description: 'd' });
    expect(api.createProject).not.toHaveBeenCalled();
    expect(dialogRef.close).toHaveBeenCalledWith('p1');
  });
});
