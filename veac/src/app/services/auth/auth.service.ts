import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, Subject, tap, firstValueFrom } from 'rxjs';
import { ConfigService } from '../api/config.service';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private tokenKey = 'authToken';
  private apiUrl = "";

  private loginEvent = new Subject<void>();
  public loginNotifier$ = this.loginEvent.asObservable(); 

  constructor(private http: HttpClient, private router: Router, private configService: ConfigService) {
      this.apiUrl = `${this.configService.getApiBaseUrl()}/api/v1/auth/login`;
  }

  login(username: string, password: string): Observable<{ token: string }> {
    return this.http.post<{ token: string }>(this.apiUrl, { username, password }).pipe(
      tap(response => {
        localStorage.setItem(this.tokenKey, response.token);
        this.loginEvent.next();
      })
    );
  }

 refreshTokenForVessel(vesselId: number): Promise<any> {
   const url = `${this.configService.getApiBaseUrl()}/api/v1/vessel/set-current-vessel`;
   return firstValueFrom(
     this.http.post<any>(url, vesselId).pipe(
       tap({
         next: (response) => {
          
           const newToken = response.token || response.Token;
           if (newToken) {
             localStorage.setItem(this.tokenKey, newToken);
           } else {
             console.error('❌ No token in response:', response);
           }
         },
         error: (error) => {
           console.error("❌ Error updating vessel:", error);
         }
       })
     )
   );
 }

  logout() {
    localStorage.removeItem(this.tokenKey);
    this.router.navigate(['/login']);
  }

  isAuthenticated(): boolean {
    const token = this.getToken();
    return token !== null && !this.isTokenExpired(token);
  }

  getToken(): string | null {
    const token = localStorage.getItem(this.tokenKey);
    if (token && this.isTokenExpired(token)) {
      this.logout();  
      return null;
    }
    return token;
  }

  private isTokenExpired(token: string): boolean {
    try {
      const payload = JSON.parse(atob(token.split('.')[1])); // Decode JWT payload
      const expiry = payload.exp * 1000; // Convert expiry to milliseconds
      return Date.now() > expiry; // Check if token is expired
    } catch (e) {
      return true; 
    }
  }
}
