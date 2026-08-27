import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { HttpInterceptorFn } from '@angular/common/http';
import { AuthService } from './auth.service';
import { catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);


  if (req.url.includes('/api/v1/auth/login')) {
    return next(req); 
  }

  let token = authService.getToken();

  if (!token) {
    console.log("No valid token, forcing logout");
    authService.logout();
    router.navigate(['/login']);
    return throwError(() => new Error("Unauthorized: No token available"));
  }

  req = req.clone({
    setHeaders: { Authorization: `Bearer ${token}` }
  });

  return next(req).pipe(
    catchError((error) => {
      if (error.status === 401) {
        console.log("Token expired or invalid, forcing logout");
        authService.logout(); 
        router.navigate(['/login']); 
      }
      return throwError(() => error);
    })
  );
};
