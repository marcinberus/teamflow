import { Component } from '@angular/core';

@Component({
  selector: 'app-profile-page',
  template: `
    <header class="page-header">
      <h1>Profile</h1>
      <p>Your account details and profile editing will live here.</p>
    </header>
    <section class="page-panel" aria-labelledby="profile-placeholder-title">
      <h2 id="profile-placeholder-title">Account</h2>
      <p>The profile route is ready for authenticated user data.</p>
    </section>
  `,
})
export class ProfilePage {}
