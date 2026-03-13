import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

@Injectable()
export class JwtInterceptor implements HttpInterceptor {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {
    console.log('🔧 JwtInterceptor initialized');
  }

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const token = this.authService.getToken();
    
    console.log('🔄 Intercepting:', req.url);
    console.log('- Token present:', !!token);

    if (token) {
      req = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
      console.log('- Added Authorization header');
    }

    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        console.error('❌ Interceptor caught error:', error.status, error.url);
        
        if (error.status === 401) {
          console.log('⛔ 401 detected - redirecting to login');
          this.authService.logout();
          this.router.navigate(['/login']);
        }
        return throwError(() => error);
      })
    );
  }
}