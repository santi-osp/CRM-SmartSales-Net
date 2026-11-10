import { Component, computed, inject, signal, ViewChild, ElementRef } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../auth/auth.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.scss'
})
export class AppShell {
  @ViewChild('mobileToggleBtn') mobileToggleBtn?: ElementRef<HTMLButtonElement>;
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly user = this.auth.user;
  readonly roleLabel = computed(() =>
    this.user()?.role === 'Administrator' ? 'Administrador' : 'Comercial'
  );
  readonly initials = computed(() => {
    const name = this.user()?.displayName ?? 'SS';
    const parts = name.trim().split(/\s+/);
    if (parts.length >= 2) return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
    return name.substring(0, 2).toUpperCase();
  });

  readonly isSidebarOpen = signal(false);
  readonly currentTitle = signal('Dashboard');

  constructor() {
    this.updateTitle(this.router.url);
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      takeUntilDestroyed()
    ).subscribe(e => {
      this.updateTitle(e.urlAfterRedirects);
      this.isSidebarOpen.set(false);
    });
  }

  toggleSidebar(): void {
    this.isSidebarOpen.update(v => !v);
  }

  closeSidebar(): void {
    this.isSidebarOpen.set(false);
    this.mobileToggleBtn?.nativeElement.focus();
  }

  onEscape(event: Event): void {
    if (this.isSidebarOpen()) {
      event.preventDefault();
      this.closeSidebar();
    }
  }

  private updateTitle(url: string): void {
    if (url.includes('/contacts')) this.currentTitle.set('Contactos');
    else if (url.includes('/opportunities')) this.currentTitle.set('Oportunidades');
    else if (url.includes('/pipeline')) this.currentTitle.set('Tablero Kanban');
    else if (url.includes('/agenda')) this.currentTitle.set('Agenda de Seguimientos');
    else this.currentTitle.set('Dashboard');
  }

  logout(): void {
    this.auth.clear();
    void this.router.navigateByUrl('/login');
  }
}
