import { Injectable, signal } from '@angular/core';

export type ToastType = 'success' | 'error' | 'info';

export interface Toast {
  id: string;
  message: string;
  type: ToastType;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly _toasts = signal<Toast[]>([]);
  readonly toasts = this._toasts.asReadonly();

  success(message: string): void { this._add(message, 'success'); }
  error(message: string): void   { this._add(message, 'error'); }
  info(message: string): void    { this._add(message, 'info'); }

  dismiss(id: string): void {
    this._toasts.update(list => list.filter(t => t.id !== id));
  }

  private _add(message: string, type: ToastType): void {
    const id = crypto.randomUUID();
    this._toasts.update(list => [...list, { id, message, type }]);
    setTimeout(() => this.dismiss(id), 4000);
  }
}
