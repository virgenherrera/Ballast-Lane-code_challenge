import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';

type HealthStatus = 'loading' | 'ok' | 'degraded' | 'error';

interface HealthResponse {
  status: string;
  liveSince: string;
  db: string;
}

@Component({
  selector: 'app-health',
  templateUrl: './health.component.html',
  styleUrl: './health.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HealthComponent implements OnInit {
  protected readonly status = signal<HealthStatus>('loading');
  protected readonly message = signal('Checking API health...');
  protected readonly liveSince = signal('');

  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);

  ngOnInit(): void {
    this.http
      .get<HealthResponse>('/health')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.liveSince.set(response.liveSince);

          if (response.status === 'ok' && response.db === 'ok') {
            this.status.set('ok');
            this.message.set('API is healthy');
          } else {
            this.status.set('degraded');
            this.message.set('API is up, DB is down');
          }
        },
        error: () => {
          this.status.set('error');
          this.message.set('API is unreachable');
        },
      });
  }
}
