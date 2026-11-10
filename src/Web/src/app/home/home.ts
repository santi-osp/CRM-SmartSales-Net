import {
  Component,
  DestroyRef,
  ElementRef,
  OnDestroy,
  ViewChild,
  computed,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  Chart,
  DoughnutController,
  BarController,
  ArcElement,
  BarElement,
  CategoryScale,
  LinearScale,
  Tooltip,
  Legend
} from 'chart.js';

import { AuthService } from '../auth/auth.service';
import { UserSummary } from '../auth/auth.models';
import { OpportunitiesApi } from '../opportunities/opportunities.api';
import { Stage } from '../opportunities/opportunity.models';
import { DashboardApi } from '../dashboard/dashboard.api';
import { Dashboard } from '../dashboard/dashboard.models';

Chart.register(
  DoughnutController,
  BarController,
  ArcElement,
  BarElement,
  CategoryScale,
  LinearScale,
  Tooltip,
  Legend
);

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    ReactiveFormsModule,
    RouterLink
  ],
  templateUrl: './home.html',
  styleUrl: './home.scss'
})
export class Home implements OnDestroy {
  @ViewChild('doughnutCanvas') doughnutCanvas?: ElementRef<HTMLCanvasElement>;
  @ViewChild('barCanvas') barCanvas?: ElementRef<HTMLCanvasElement>;

  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute, { optional: true });
  private readonly api = inject(DashboardApi);
  private readonly opportunities = inject(OpportunitiesApi);
  private readonly destroyRef = inject(DestroyRef);
  private requestId = 0;

  private doughnutChart: Chart | null = null;
  private barChart: Chart | null = null;
  private chartUpdateTimer: ReturnType<typeof setTimeout> | null = null;

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

  readonly winRatio = computed(() => {
    const d = this.data();
    if (!d) return null;
    const closed = d.wonCount + d.lostCount;
    if (closed === 0) return null;
    return Math.round((d.wonCount / closed) * 100);
  });

  constructor() {
    const params = this.route?.snapshot.queryParams;
    if (params) {
      if (params['owner']) this.owner.setValue(params['owner']);
      if (params['from']) this.createdFrom.setValue(params['from']);
      if (params['to']) this.createdTo.setValue(params['to']);
    }
    this.load();
    this.opportunities.users().pipe(takeUntilDestroyed()).subscribe({
      next: result => this.users.set(result.items),
      error: () => this.usersError.set('No se cargaron los nombres de responsables.')
    });
  }

  ngOnDestroy(): void {
    if (this.chartUpdateTimer) clearTimeout(this.chartUpdateTimer);
    this.destroyCharts();
  }

  apply(): void {
    if (this.createdFrom.value && this.createdTo.value && this.createdFrom.value > this.createdTo.value) {
      this.validationError.set('La fecha inicial no puede ser posterior a la fecha final.');
      return;
    }
    this.validationError.set(null);
    this.updateQueryParams();
    this.load();
  }

  clear(): void {
    this.owner.setValue('');
    this.createdFrom.setValue('');
    this.createdTo.setValue('');
    this.validationError.set(null);
    this.updateQueryParams();
    this.load();
  }

  private updateQueryParams(): void {
    if (!this.router) return;
    this.router.navigate([], {
      queryParams: {
        owner: this.owner.value || null,
        from: this.createdFrom.value || null,
        to: this.createdTo.value || null
      },
      queryParamsHandling: 'merge'
    }).catch(() => {});
  }

  load(): void {
    const id = ++this.requestId;
    if (this.chartUpdateTimer) clearTimeout(this.chartUpdateTimer);
    this.loading.set(true);
    this.error.set(null);
    this.data.set(null);
    this.destroyCharts();

    this.api.get(this.owner.value, this.createdFrom.value, this.createdTo.value)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          if (id !== this.requestId) return;
          this.data.set(result);
          this.loading.set(false);
          this.chartUpdateTimer = setTimeout(() => {
            if (!this.destroyRef.destroyed && this.data() === result) this.renderCharts();
          }, 0);
        },
        error: () => {
          if (id !== this.requestId) return;
          this.loading.set(false);
          this.error.set('No se pudieron cargar los indicadores.');
        }
      });
  }

  private destroyCharts(): void {
    if (this.doughnutChart) {
      this.doughnutChart.destroy();
      this.doughnutChart = null;
    }
    if (this.barChart) {
      this.barChart.destroy();
      this.barChart = null;
    }
  }

  private renderCharts(): void {
    this.destroyCharts();
    const d = this.data();
    if (!d) return;

    const stages: Stage[] = ['New', 'Contacted', 'Proposal', 'Won', 'Lost'];
    const labels = stages.map(s => this.stageLabel(s));
    const counts = stages.map(s => d.byStage.find(item => item.stage === s)?.count ?? 0);
    const colors = ['#315BFF', '#D38A12', '#7957D9', '#0B9E87', '#D44F62'];

    if (this.doughnutCanvas?.nativeElement) {
      this.doughnutChart = new Chart(this.doughnutCanvas.nativeElement, {
        type: 'doughnut',
        data: {
          labels,
          datasets: [{
            data: counts,
            backgroundColor: colors,
            borderWidth: 2,
            borderColor: '#ffffff',
            hoverOffset: 6
          }]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          animation: false,
          plugins: {
            legend: {
              position: 'bottom',
              labels: {
                boxWidth: 12,
                font: { family: 'inherit', weight: 600 },
                color: '#1F2A44'
              }
            }
          },
          cutout: '68%'
        }
      });
    }

    if (this.barCanvas?.nativeElement) {
      this.barChart = new Chart(this.barCanvas.nativeElement, {
        type: 'bar',
        data: {
          labels,
          datasets: [{
            label: 'Oportunidades',
            data: counts,
            backgroundColor: colors,
            borderRadius: 6
          }]
        },
        options: {
          indexAxis: 'y',
          responsive: true,
          maintainAspectRatio: false,
          animation: false,
          plugins: {
            legend: { display: false }
          },
          scales: {
            x: {
              beginAtZero: true,
              ticks: { precision: 0, color: '#5A6B87' },
              grid: { color: '#EEF3FA' }
            },
            y: {
              ticks: { font: { weight: 600 }, color: '#1F2A44' },
              grid: { display: false }
            }
          }
        }
      });
    }
  }

  stageLabel(stage: Stage): string {
    return ({
      New: 'Nuevo',
      Contacted: 'En contacto',
      Proposal: 'Propuesta',
      Won: 'Ganada',
      Lost: 'Perdida'
    })[stage];
  }

  barWidth(count: number): number {
    const max = Math.max(1, ...(this.data()?.byStage.map(item => item.count) ?? []));
    return (count / max) * 100;
  }

  money(value: number): string {
    return new Intl.NumberFormat('es-CO', { style: 'currency', currency: 'COP' }).format(value);
  }
}
