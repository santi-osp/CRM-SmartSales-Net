import { Component, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Opportunity } from '../opportunities/opportunity.models';

export interface ClosePayload {
  result: 'Won' | 'Lost';
  finalAmount: number | null;
  lossReason: string | null;
}

@Component({
  selector: 'app-close-opportunity-dialog',
  imports: [ReactiveFormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule],
  template: `
    <h2 mat-dialog-title>Cerrar oportunidad</h2>
    <mat-dialog-content>
      <p>{{ item.title }}</p>
      <mat-form-field appearance="outline">
        <mat-label>Resultado</mat-label>
        <mat-select [formControl]="result">
          <mat-option value="Won">Ganada</mat-option>
          <mat-option value="Lost">Perdida</mat-option>
        </mat-select>
      </mat-form-field>
      @if (result.value === 'Won') {
        <mat-form-field appearance="outline">
          <mat-label>Monto final (COP)</mat-label>
          <input matInput [formControl]="finalAmount" type="number" min="0" step="0.01" />
        </mat-form-field>
      } @else {
        <mat-form-field appearance="outline">
          <mat-label>Motivo de pérdida</mat-label>
          <textarea matInput [formControl]="lossReason" maxlength="1000"></textarea>
        </mat-form-field>
      }
      @if (error()) { <p role="alert">{{ error() }}</p> }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-stroked-button type="button" (click)="dialog.close()">Cancelar</button>
      <button mat-flat-button type="button" (click)="confirm()">Confirmar cierre</button>
    </mat-dialog-actions>
  `,
  styles: `mat-form-field { width: 100%; } [role='alert'] { color: #b42318; }`
})
export class CloseOpportunityDialog {
  readonly item = inject<Opportunity>(MAT_DIALOG_DATA);
  readonly dialog = inject(MatDialogRef<CloseOpportunityDialog, ClosePayload>);
  readonly result = new FormControl<'Won' | 'Lost'>('Won', { nonNullable: true });
  readonly finalAmount = new FormControl<number | null>(this.item.estimatedAmount);
  readonly lossReason = new FormControl('', { nonNullable: true });
  readonly error = signal<string | null>(null);

  confirm(): void {
    if (this.result.value === 'Won') {
      const amount = this.finalAmount.value;
      if (amount === null || !Number.isFinite(amount) || amount < 0 ||
          Math.abs(Math.round(amount * 100) - amount * 100) > 1e-7) {
        this.error.set('Indica un monto final no negativo con máximo dos decimales.');
        return;
      }
      this.dialog.close({ result: 'Won', finalAmount: amount, lossReason: null });
    } else {
      const reason = this.lossReason.value.trim();
      if (!reason || reason.length > 1000) {
        this.error.set('Indica un motivo de pérdida de máximo 1000 caracteres.');
        return;
      }
      this.dialog.close({ result: 'Lost', finalAmount: null, lossReason: reason });
    }
  }
}
