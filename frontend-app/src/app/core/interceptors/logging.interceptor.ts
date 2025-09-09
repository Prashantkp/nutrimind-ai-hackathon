import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpResponse, HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

@Injectable()
export class LoggingInterceptor implements HttpInterceptor {
  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    console.log('=== HTTP REQUEST ===');
    console.log('Method:', req.method);
    console.log('URL:', req.url);
    console.log('Headers:', req.headers.keys().map(key => `${key}: ${req.headers.get(key)}`));
    console.log('Body:', req.body);
    console.log('==================');

    return next.handle(req).pipe(
      tap(
        event => {
          if (event instanceof HttpResponse) {
            console.log('=== HTTP RESPONSE ===');
            console.log('Status:', event.status);
            console.log('Headers:', event.headers.keys().map(key => `${key}: ${event.headers.get(key)}`));
            console.log('Body:', event.body);
            console.log('===================');
          }
        },
        error => {
          if (error instanceof HttpErrorResponse) {
            console.log('=== HTTP ERROR ===');
            console.log('Status:', error.status);
            console.log('Error:', error.error);
            console.log('Message:', error.message);
            console.log('=================');
          }
        }
      )
    );
  }
}
