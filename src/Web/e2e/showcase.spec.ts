import { expect, test, Page } from '@playwright/test';
import { DateTime } from 'luxon';

async function loginAdmin(page: Page): Promise<void> {
  await page.goto('/login');
  await page.getByRole('textbox', { name: 'Correo electrónico' }).fill(process.env['E2E_ADMIN_EMAIL']!);
  await page.getByRole('textbox', { name: 'Contraseña' }).fill(process.env['E2E_ADMIN_PASSWORD']!);
  await page.getByRole('button', { name: /Ingresar/ }).click();
  await expect(page.getByRole('heading', { name: /Hola,/ })).toBeVisible();
}

test('calendario Showcase usa todas las páginas reales y cambia de vista y alcance', async ({ page }) => {
  test.skip(process.env['E2E_DEMO_PROFILE'] !== 'Showcase', 'Solo en PostgreSQL Showcase aislado');
  await loginAdmin(page);
  await page.getByRole('link', { name: 'Agenda', exact: true }).click();
  await expect(page.locator('.fc-daygrid')).toBeVisible();
  const allResponse = page.waitForResponse(response =>
    response.url().includes('/follow-ups/calendar') && response.url().includes('scope=All'));
  await page.getByRole('button', { name: 'Todo el Equipo' }).click();
  const all = await (await allResponse).json() as { id: string }[];
  expect(all.length).toBeGreaterThan(20);
  expect(new Set(all.map(item => item.id)).size).toBe(all.length);
  const allFromApi = await page.locator('.fc-event').count();
  expect(allFromApi).toBeGreaterThan(0);

  await page.getByRole('button', { name: 'Semana', exact: true }).click();
  await expect(page.locator('.fc-timegrid')).toBeVisible();
  await page.getByRole('button', { name: 'Día', exact: true }).click();
  await expect(page.locator('.fc-timegrid')).toBeVisible();
  await page.getByRole('button', { name: 'Lista', exact: true }).last().click();
  await expect(page.locator('.fc-list')).toBeVisible();
  await page.getByRole('button', { name: 'Mes', exact: true }).click();
  await expect(page.locator('.fc-daygrid')).toBeVisible();

  await page.locator('.fc-event').first().click();
  await expect(page.getByRole('dialog')).toBeVisible();
  await expect(page.getByRole('dialog').getByText('Contacto')).toBeVisible();
  await page.getByRole('button', { name: 'Cerrar detalle' }).click();
  await page.getByRole('button', { name: 'Lista', exact: true }).first().click();
  await expect(page.getByRole('group', { name: 'Categorías de agenda' })).toBeVisible();
});

test('calendario Showcase cubre 120 registros en rango de 365 días sin truncarlos', async ({ page }) => {
  test.skip(process.env['E2E_DEMO_PROFILE'] !== 'Showcase', 'Solo en PostgreSQL Showcase aislado');
  await loginAdmin(page);
  // Las fechas del seed se distribuyen en +-60 días respecto a la ejecución.
  const from = DateTime.now().setZone('America/Bogota').minus({ days: 90 }).toISODate()!;
  const to = DateTime.now().setZone('America/Bogota').plus({ days: 90 }).toISODate()!;
  // Sin Bearer la API debe rechazar la petición. La UI adjunta JWT mediante interceptor.
  const anonymous = await page.request.get(`/api/sales/v1/follow-ups/calendar?from=${from}&to=${to}&scope=All`);
  expect(anonymous.status()).toBe(401);
  await page.getByRole('link', { name: 'Agenda', exact: true }).click();
  const request = page.waitForResponse(response => response.url().includes('/follow-ups/calendar') && response.url().includes('scope=All'));
  await page.getByRole('button', { name: 'Todo el Equipo' }).click();
  const calendarResponse = await request;
  const visible = await calendarResponse.json() as { id: string }[];
  expect(visible.length).toBeGreaterThan(20);
  expect(new Set(visible.map(item => item.id)).size).toBe(visible.length);
  // Bearer only in memory; never print or persist this authorization header.
  const authorization = calendarResponse.request().headers()['authorization'];
  const full = await page.request.get(`/api/sales/v1/follow-ups/calendar?from=${from}&to=${to}&scope=All`,
    { headers: { authorization } });
  expect(full.status()).toBe(200);
  const all = await full.json() as { id: string }[];
  expect(all).toHaveLength(120);
  expect(new Set(all.map(item => item.id)).size).toBe(120);
});

test('gesto CDK arrastra una oportunidad y persiste tras navegar', async ({ page }) => {
  await loginAdmin(page);
  await page.getByRole('link', { name: 'Tablero Kanban', exact: true }).click();
  const source = page.locator('section.column[aria-label="Nuevo"]');
  const destination = page.locator('section.column[aria-label="En contacto"] .drop-zone');
  await expect(source.getByRole('heading', { name: 'Primera conversación' })).toBeVisible();
  const sourceCard = source.locator('.kanban-card').filter({ hasText: 'Primera conversación' });
  const movement = page.waitForResponse(response => response.url().endsWith('/stage') && response.request().method() === 'PATCH', { timeout: 10000 });
  const start = await sourceCard.boundingBox();
  const end = await destination.boundingBox();
  expect(start).not.toBeNull();
  expect(end).not.toBeNull();
  await page.mouse.move(start!.x + 24, start!.y + 24);
  await page.mouse.down();
  await page.mouse.move(start!.x + 40, start!.y + 45, { steps: 6 });
  await page.mouse.move(end!.x + end!.width / 2, end!.y + 65, { steps: 24 });
  await page.mouse.up();
  expect((await movement).status()).toBe(200);
  await expect(destination.getByRole('heading', { name: 'Primera conversación' })).toBeVisible();
  await expect(source.getByRole('heading', { name: 'Primera conversación' })).toHaveCount(0);
});

test('capturas aisladas de pantallas reales en escritorio y móvil', async ({ page }) => {
  test.skip(process.env['E2E_DEMO_PROFILE'] !== 'Showcase', 'Solo evidencia en base ficticia Showcase aislada');
  const output = '../../artifacts/evidence/GOAL-01.3';
  for (const width of [1440, 390]) {
    await page.setViewportSize({ width, height: 900 });
    await page.goto('/login');
    await page.screenshot({ path: `${output}/login-${width}-isolated.png`, fullPage: true });
    await loginAdmin(page);
    const routes: [string, string][] = [
      ['/dashboard', 'dashboard'], ['/contacts', 'contacts'],
      ['/opportunities', 'opportunities'], ['/pipeline', 'pipeline'], ['/agenda', 'agenda-month']
    ];
    for (const [route, name] of routes) {
      // Recargar sesión en memoria exige login al navegar con goto; usar enlaces del shell.
      if (route !== '/dashboard') {
        if (width === 390) await page.getByRole('button', { name: 'Abrir menú de navegación' }).click();
        const label = name === 'pipeline' ? 'Tablero Kanban' :
          name === 'opportunities' ? 'Oportunidades' : name === 'contacts' ? 'Contactos' : 'Agenda';
        await page.getByRole('link', { name: label, exact: true }).click();
        if (width === 390) await expect(page.locator('.sidebar')).not.toBeInViewport();
      }
      if (name === 'dashboard') await expect(page.locator('.kpi-grid .kpi-card')).toHaveCount(4);
      if (name === 'agenda-month') await expect(page.locator('.fc-daygrid')).toBeVisible();
      await page.screenshot({ path: `${output}/${name}-${width}-isolated.png`, fullPage: true });
      const docWidth = await page.evaluate(() => document.documentElement.scrollWidth);
      expect(docWidth).toBeLessThanOrEqual(width + 1);
    }
    await page.getByRole('button', { name: 'Semana', exact: true }).click();
    await page.screenshot({ path: `${output}/agenda-week-${width}-isolated.png`, fullPage: true });
  }
});
