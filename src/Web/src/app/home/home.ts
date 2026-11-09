import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { UserSummary } from '../auth/auth.models';
import { OpportunitiesApi } from '../opportunities/opportunities.api';
import { Stage } from '../opportunities/opportunity.models';
import { DashboardApi } from '../dashboard/dashboard.api';
import { Dashboard } from '../dashboard/dashboard.models';

@Component({
  selector: 'app-home',
  imports: [MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    ReactiveFormsModule, RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.scss'
})
export class Home {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly api = inject(DashboardApi);
  private readonly opportunities = inject(OpportunitiesApi);
  private readonly destroyRef = inject(DestroyRef);
  private requestId = 0;
  readonly user = this.auth.user;
  readonly users = signal<UserSummary[]>([]);
  readonly usersError = signal<string | null>(null);
  readonly owner = new FormControl('', { nonNullable: true });
  readonly createdFrom = new FormControl('', { nonNullable: true });
  readonly createdTo = new FormControl('', { nonNullable: true });
  readonly data = signal<Dashboard | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly validationError = signal<string | null>(null);

  constructor() {
    this.load();
    this.opportunities.users().pipe(takeUntilDestroyed()).subscribe({
      next: result => this.users.set(result.items),
      error: () => this.usersError.set('No se cargaron los nombres de responsables.')
    });
  }

  apply(): void {
    if (this.createdFrom.value && this.createdTo.value && this.createdFrom.value > this.createdTo.value) {
      this.validationError.set('La fecha inicial no puede ser posterior a la fecha final.');
      return;
    }
    this.validationError.set(null);
    this.load();
  }

  clear(): void {
    this.owner.setValue('');
    this.createdFrom.setValue('');
    this.createdTo.setValue('');
    this.validationError.set(null);
    this.load();
  }

  load(): void {
    const id = ++this.requestId;
    this.loading.set(true);
    this.error.set(null);
    this.data.set(null);
    this.api.get(this.owner.value, this.createdFrom.value, this.createdTo.value)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          if (id !== this.requestId) return;
          this.data.set(result);
          this.loading.set(false);
        },
        error: () => {
          if (id !== this.requestId) return;
          this.loading.set(false);
          this.error.set('No se pudieron cargar los indicadores.');
        }
      });
  }

  stageLabel(stage: Stage): string {
    return ({ New: 'Nuevo', Contacted: 'En contacto', Proposal: 'Propuesta',
      Won: 'Ganada', Lost: 'Perdida' })[stage];
  }
  barWidth(count: number): number {
    const max = Math.max(1, ...(this.data()?.byStage.map(item => item.count) ?? []));
    return count / max * 100;
  }
  money(value: number): string {
    return new Intl.NumberFormat('es-CO', { style: 'currency', currency: 'COP' }).format(value);
  }

  logout(): void {
    this.auth.clear();
    void this.router.navigateByUrl('/login');
  }
}
