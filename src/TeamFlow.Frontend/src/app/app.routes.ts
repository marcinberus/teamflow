import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'login',
    title: 'Sign in | TeamFlow',
    loadComponent: () =>
      import('./features/auth/pages/login-page/login-page').then((page) => page.LoginPage),
  },
  {
    path: 'register',
    title: 'Register | TeamFlow',
    loadComponent: () =>
      import('./features/auth/pages/register-page/register-page').then((page) => page.RegisterPage),
  },
  {
    path: '',
    loadComponent: () => import('./layout/app-shell/app-shell').then((layout) => layout.AppShell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'projects' },
      {
        path: 'projects',
        title: 'Projects | TeamFlow',
        loadComponent: () =>
          import('./features/projects/pages/projects-list-page/projects-list-page').then(
            (page) => page.ProjectsListPage,
          ),
      },
      {
        path: 'projects/:projectId/import-tasks',
        title: 'Import tasks | TeamFlow',
        loadComponent: () =>
          import('./features/imports/pages/task-import-page/task-import-page').then(
            (page) => page.TaskImportPage,
          ),
      },
      {
        path: 'projects/:projectId',
        title: 'Project details | TeamFlow',
        loadComponent: () =>
          import('./features/projects/pages/project-details-page/project-details-page').then(
            (page) => page.ProjectDetailsPage,
          ),
      },
      {
        path: 'imports/projects',
        title: 'Import projects | TeamFlow',
        loadComponent: () =>
          import('./features/imports/pages/project-import-page/project-import-page').then(
            (page) => page.ProjectImportPage,
          ),
      },
      {
        path: 'profile',
        title: 'Profile | TeamFlow',
        loadComponent: () =>
          import('./features/profile/pages/profile-page/profile-page').then(
            (page) => page.ProfilePage,
          ),
      },
    ],
  },
  { path: '**', redirectTo: 'projects' },
];
