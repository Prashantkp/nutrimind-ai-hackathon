import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private authService: AuthService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Add auth token to all requests
    const authToken = this.authService.getToken();
    
    let authReq = req;
    if (authToken) {
      authReq = req.clone({
        setHeaders: {
          Authorization: `Bearer ${authToken}`
        }
      });
    }

    return next.handle(authReq).pipe(
      catchError((error: HttpErrorResponse) => {
        // Handle 401 Unauthorized - try to refresh token
        if (error.status === 401 && authToken) {
          return this.handle401Error(authReq, next);
        }
        
        // Handle other errors
        return throwError(error);
      })
    );
  }

  private handle401Error(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return this.authService.refreshToken().pipe(
      switchMap(() => {
        // Retry the failed request with new token
        const newToken = this.authService.getToken();
        const newReq = req.clone({
          setHeaders: {
            Authorization: `Bearer ${newToken}`
          }
        });
        return next.handle(newReq);
      }),
      catchError((refreshError) => {
        // Refresh failed, logout user
        this.authService.logout();
        return throwError(refreshError);
      })
    );
  }
}
