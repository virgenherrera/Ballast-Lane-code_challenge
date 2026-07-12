import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';

import { AuthService } from '../auth/auth.service';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [RouterOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-gray-50">
      <header class="bg-white border-b border-gray-200 px-4 sm:px-6 md:px-8 py-3 flex items-center justify-between">
        <span class="text-lg md:text-xl font-semibold text-gray-900">TaskFlow</span>

        <div class="relative">
          <button
            type="button"
            class="flex items-center gap-2"
            (click)="toggleMenu()"
          >
            <span
              class="flex h-8 w-8 items-center justify-center rounded-full bg-blue-600 text-white text-sm font-medium"
            >
              {{ initials() }}
            </span>
            <span class="hidden sm:inline text-sm font-medium text-gray-700">{{ userName() }}</span>
          </button>

          @if (menuOpen()) {
            <div class="absolute right-0 mt-2 w-40 rounded-lg border border-gray-200 bg-white shadow-lg py-1">
              <button
                type="button"
                class="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
                (click)="onLogout()"
              >
                Logout
              </button>
            </div>
          }
        </div>
      </header>

      <main class="md:px-4 lg:px-8">
        <router-outlet />
      </main>
    </div>
  `,
})
export class AppLayoutComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly menuOpen = signal(false);

  readonly userName = computed(() => this.authService.currentUser()?.name ?? '');

  readonly initials = computed(() => {
    const name = this.userName();

    if (!name) {
      return '';
    }

    return name
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0]?.toUpperCase())
      .join('');
  });

  toggleMenu(): void {
    this.menuOpen.update((open) => !open);
  }

  onLogout(): void {
    this.authService.logout();
    this.router.navigateByUrl('/login');
  }
}
