import { expect, test, Page } from '@playwright/test';
import { DateTime } from 'luxon';

async function login(page: Page, role: 'ADMIN' | 'COMMERCIAL' = 'ADMIN'): Promise<void> {
  await page.goto('/login');
  await page.getByRole('textbox', { name: 'Correo electrónico' })
    .fill(process.env[`E2E_${role}_EMAIL`]!);
  await page.getByLabel('Contraseña').fill(process.env[`E2E_${role}_PASSWORD`]!);
  await page.getByRole('button', { name: 'Ingresar' }).click();
  await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();
}

test('recorre el CRM y persiste movimiento y cierre entre las dos APIs', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await login(page);
  await expect(page.getByText('Monto estimado abierto')).toBeVisible();
  await page.getByRole('link', { name: 'Contactos' }).click();
  await page.locator('.contact-list button').filter({ hasText: 'Café Aurora' }).click();
  await expect(page.getByRole('heading', { name: 'Café Aurora' })).toBeVisible();
  await expect(page.getByText('Conversamos sobre el alcance inicial.')).toBeVisible();

  await page.getByRole('link', { name: 'Inicio' }).click();
  await page.getByRole('link', { name: 'Oportunidades' }).click();
  await page.locator('.opportunity-list button').filter({ hasText: 'Primera conversación' }).click();
  await expect(page.getByRole('heading', { name: 'Primera conversación' })).toBeVisible();
  await expect(page.getByText('Conversamos sobre el alcance inicial.')).toBeVisible();

  await page.getByRole('link', { name: 'Tablero' }).click();
  const newColumn = page.locator('section.column[aria-label="Nuevo"]');
  const contactedColumn = page.locator('section.column[aria-label="En contacto"]');
  await expect(newColumn.getByRole('heading', { name: 'Primera conversación' })).toBeVisible();
  await newColumn.getByRole('button', { name: 'Acciones de Primera conversación' }).click();
  await page.getByRole('menuitem', { name: 'Mover a En contacto' }).click();
  await expect(contactedColumn.getByRole('heading', { name: 'Primera conversación' })).toBeVisible();

  await page.reload();
  await login(page);
  await page.getByRole('link', { name: 'Tablero' }).click();
  await expect(contactedColumn.getByRole('heading', { name: 'Primera conversación' })).toBeVisible();
  await newColumn.getByRole('button', { name: 'Acciones de Nueva sede' }).click();
  await page.getByRole('menuitem', { name: 'Cerrar oportunidad' }).click();
  await expect(page.getByRole('dialog')).toBeVisible();
  const closeResponse = page.waitForResponse(response =>
    response.url().endsWith('/close') && response.request().method() === 'POST');
  await page.getByRole('dialog').getByRole('button', { name: 'Confirmar cierre' }).click();
  expect((await closeResponse).status()).toBe(200);
  await expect(page.getByRole('dialog')).toHaveCount(0);
  await expect(newColumn.getByRole('heading', { name: 'Nueva sede' })).toHaveCount(0);
  await page.getByRole('link', { name: 'Inicio' }).click();
  await expect(page.getByText('Ganadas').locator('..').locator('strong')).toHaveText('2');

  await page.getByRole('link', { name: 'Agenda' }).click();
  await page.getByRole('button', { name: 'Todo el equipo' }).click();
  await page.getByRole('button', { name: 'Vencidos' }).click();
  await expect(page.getByText('Retomar conversación')).toBeVisible();
  await page.getByRole('button', { name: 'Próximos' }).click();
  await expect(page.getByText('Revisar propuesta')).toBeVisible();
});

test('crea datos en las dos APIs y aplica el permiso del rol Comercial', async ({ page }) => {
  await login(page);
  await page.getByRole('link', { name: 'Contactos' }).click();
  await page.getByRole('button', { name: 'Nuevo contacto' }).click();
  await page.getByRole('textbox', { name: 'Nombre', exact: true }).fill('E2E Cliente');
  await page.getByRole('textbox', { name: 'Correo electrónico' }).fill('e2e-cliente@demo.example');
  await page.getByRole('button', { name: 'Guardar' }).click();
  await expect(page.getByRole('heading', { name: 'E2E Cliente' })).toBeVisible();
  await page.getByRole('textbox', { name: 'Resumen' }).fill('Llamada de presentación E2E');
  await page.getByRole('button', { name: 'Registrar actividad' }).click();
  await expect(page.getByText('Llamada de presentación E2E')).toBeVisible();

  await page.getByRole('link', { name: 'Inicio' }).click();
  await page.getByRole('link', { name: 'Oportunidades' }).click();
  await page.getByRole('button', { name: 'Nueva oportunidad' }).click();
  const searchResponse = page.waitForResponse(response =>
    response.url().includes('/api/contacts/v1/contacts?q=E2E') && response.status() === 200);
  await page.getByRole('textbox', { name: 'Buscar contacto' }).fill('E2E Cliente');
  const searchResult = await (await searchResponse).json();
  expect(searchResult.items.map((item: { name: string }) => item.name)).toContain('E2E Cliente');
  await page.getByRole('combobox', { name: 'Contacto', exact: true }).click();
  await page.getByRole('option', { name: /E2E Cliente/ }).click();
  await page.getByRole('textbox', { name: 'Título' }).fill('Venta E2E');
  await page.getByRole('spinbutton', { name: 'Monto estimado (COP)' }).fill('120000');
  await page.getByLabel('Fecha esperada de cierre').fill(
    DateTime.now().setZone('America/Bogota').plus({ days: 7 }).toISODate()!);
  await page.getByRole('button', { name: 'Guardar' }).click();
  await expect(page.getByRole('heading', { name: 'Venta E2E' })).toBeVisible();
  await page.getByRole('combobox', { name: 'Nuevo responsable' }).click();
  await page.getByRole('option', { name: 'Comercial Demo' }).click();
  const ownerResponse = page.waitForResponse(response =>
    response.url().endsWith('/owner') && response.request().method() === 'PATCH');
  await page.getByRole('button', { name: 'Reasignar' }).click();
  expect((await ownerResponse).status()).toBe(200);
  await expect(page.getByRole('combobox', { name: 'Nuevo responsable' })).toHaveText(/Comercial Demo/);

  await page.getByRole('link', { name: 'Inicio' }).click();
  await page.getByRole('link', { name: 'Agenda' }).click();
  const agendaSearchResponse = page.waitForResponse(response =>
    response.url().includes('/api/contacts/v1/contacts?q=E2E') && response.status() === 200);
  await page.getByRole('textbox', { name: 'Buscar contacto' }).fill('E2E Cliente');
  const agendaSearchResult = await (await agendaSearchResponse).json();
  expect(agendaSearchResult.items.map((item: { name: string }) => item.name)).toContain('E2E Cliente');
  await page.getByRole('combobox', { name: 'Contacto', exact: true }).click();
  await page.getByRole('option', { name: /E2E Cliente/ }).click();
  await page.getByRole('textbox', { name: 'Asunto' }).fill('Seguimiento E2E');
  await page.getByLabel('Fecha y hora (Bogotá)').fill(
    DateTime.now().setZone('America/Bogota').plus({ days: 2 }).toFormat("yyyy-LL-dd'T'HH:mm"));
  await page.getByRole('button', { name: 'Programar seguimiento' }).click();
  await expect(page.getByText('Seguimiento E2E')).toBeVisible();
  const completeResponse = page.waitForResponse(response =>
    response.url().endsWith('/complete') && response.request().method() === 'POST');
  await page.getByRole('button', { name: 'Completar' }).click();
  expect((await completeResponse).status()).toBe(200);
  await page.getByRole('button', { name: 'Completados' }).click();
  await expect(page.getByText('Seguimiento E2E')).toBeVisible();

  await login(page, 'COMMERCIAL');
  const requestPromise = page.waitForRequest(request =>
    request.url().includes('/api/sales/v1/opportunities') && !!request.headers()['authorization']);
  await page.getByRole('link', { name: 'Oportunidades' }).click();
  const bearer = (await requestPromise).headers()['authorization'];
  await page.locator('.opportunity-list button').filter({ hasText: 'Venta E2E' }).click();
  await expect(page.getByRole('heading', { name: 'Venta E2E' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Reasignar' })).toHaveCount(0);
  const response = await page.request.patch(
    '/api/sales/v1/opportunities/019a6030-0000-7000-8000-000000000001/owner',
    { headers: { Authorization: bearer },
      data: { ownerUserId: '019a6010-0000-7000-8000-000000000001', version: 1 } });
  expect(response.status()).toBe(403);
});

test('presenta navegación móvil sin desbordamiento global y tablero desplazable', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await login(page);
  for (const section of ['Dashboard', 'Contactos', 'Oportunidades', 'Agenda', 'Tablero']) {
    if (section !== 'Dashboard') await page.getByRole('link', { name: section }).first().click();
    const width = await page.evaluate(() => document.documentElement.scrollWidth);
    expect(width, `${section} desborda el viewport`).toBeLessThanOrEqual(391);
    if (section !== 'Dashboard' && section !== 'Tablero')
      await page.getByRole('link', { name: 'Inicio' }).click();
  }
  const board = page.locator('.board');
  await expect(board).toBeVisible();
  const sizes = await board.evaluate(element => ({ scroll: element.scrollWidth, client: element.clientWidth }));
  expect(sizes.scroll).toBeGreaterThan(sizes.client);
});
