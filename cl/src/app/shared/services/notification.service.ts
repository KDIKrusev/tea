import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

/**
 * The app's four notification shapes, in one place.
 *
 * There were sixteen `snackBar.open(...)` call sites, ten of which repeated
 * `{ duration: 5000, panelClass: ['error-snackbar'] }` verbatim. Nothing was wrong with any of
 * them individually — but a duration or a panel class could drift on one of ten, and nobody would
 * notice until two snackbars behaved differently in the same session.
 *
 * The durations below are the ones the call sites already used: errors linger longest because they
 * carry something to read, confirmations are brief because they only acknowledge.
 */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly snackBar = inject(MatSnackBar);

  /** Something failed and the user needs to read why. */
  error(message: string): void {
    this.snackBar.open(message, 'Close', { duration: 5000, panelClass: ['error-snackbar'] });
  }

  /** Something worked; the user only needs to know it happened. */
  success(message: string): void {
    this.snackBar.open(message, 'OK', { duration: 2500 });
  }

  /** A success worth a slightly longer look — an import, a restore. */
  successDetailed(message: string): void {
    this.snackBar.open(message, 'OK', { duration: 3000 });
  }

  /** A quick acknowledgement of a small action — a rename, a delete. */
  acknowledge(message: string): void {
    this.snackBar.open(message, 'OK', { duration: 2000 });
  }
}
