import { TestBed } from '@angular/core/testing';
import { PageEvent } from '@angular/material/paginator';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ProjectsApi } from '../../projects.api';
import { ProjectsListPage } from './projects-list-page';

describe('ProjectsListPage query', () => {
  const listProjects = vi.fn();

  type Internals = {
    searchControl: { setValue(v: string): void };
    setStatus(s: 'Active' | null): void;
    changePage(e: Partial<PageEvent>): void;
  };

  function create() {
    const fixture = TestBed.createComponent(ProjectsListPage);
    fixture.detectChanges();
    return fixture.componentInstance as unknown as Internals;
  }

  beforeEach(() => {
    vi.useFakeTimers();
    listProjects.mockReset();
    listProjects.mockImplementation(({ page, pageSize }) =>
      of({ items: [], totalCount: 100, page, pageSize }),
    );
    TestBed.configureTestingModule({
      imports: [ProjectsListPage],
      providers: [provideRouter([]), { provide: ProjectsApi, useValue: { listProjects } }],
    });
  });

  afterEach(() => vi.useRealTimers());

  it('resets to page 1 when the status filter changes', () => {
    const page = create();
    page.changePage({ pageIndex: 2, pageSize: 20 });
    expect(listProjects).toHaveBeenLastCalledWith(expect.objectContaining({ page: 3 }));

    page.setStatus('Active');
    expect(listProjects).toHaveBeenLastCalledWith(
      expect.objectContaining({ page: 1, status: 'Active' }),
    );
  });

  it('debounces search into one request, resets the page and ignores unchanged text', () => {
    const page = create();
    page.changePage({ pageIndex: 2, pageSize: 20 });
    listProjects.mockClear();

    for (const text of ['a', 'ab', 'abc']) page.searchControl.setValue(text);
    vi.advanceTimersByTime(299);
    expect(listProjects).not.toHaveBeenCalled();

    vi.advanceTimersByTime(1);
    expect(listProjects).toHaveBeenCalledTimes(1);
    expect(listProjects).toHaveBeenCalledWith(expect.objectContaining({ page: 1, search: 'abc' }));

    page.searchControl.setValue('abc ');
    vi.advanceTimersByTime(300);
    expect(listProjects).toHaveBeenCalledTimes(1);
  });
});
