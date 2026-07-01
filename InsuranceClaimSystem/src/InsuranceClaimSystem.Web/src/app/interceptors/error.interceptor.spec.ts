import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { HttpClient, HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { Router } from '@angular/router';
import { errorInterceptor } from './error.interceptor';

describe('errorInterceptor', () => {
  let httpTestingController: HttpTestingController;
  let httpClient: HttpClient;
  let mockRouter: any;
  let consoleErrorSpy: jasmine.Spy;

  beforeEach(() => {
    mockRouter = { navigate: jasmine.createSpy('navigate') };
    consoleErrorSpy = spyOn(console, 'error');

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: mockRouter }
      ]
    });

    httpTestingController = TestBed.inject(HttpTestingController);
    httpClient = TestBed.inject(HttpClient);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('should pass through successful requests', () => {
    httpClient.get('/test').subscribe();
    const req = httpTestingController.expectOne('/test');
    req.flush({});
    expect(consoleErrorSpy).not.toHaveBeenCalled();
  });

  it('should redirect to /unauthorized on 403 Forbidden', () => {
    httpClient.get('/test').subscribe({
      error: (error: HttpErrorResponse) => {
        expect(error.status).toBe(403);
      }
    });

    const req = httpTestingController.expectOne('/test');
    req.flush('Forbidden', { status: 403, statusText: 'Forbidden' });

    expect(mockRouter.navigate).toHaveBeenCalledWith(['/unauthorized']);
  });

  it('should log error on 500 Internal Server Error', () => {
    httpClient.get('/test').subscribe({
      error: (error: HttpErrorResponse) => {
        expect(error.status).toBe(500);
      }
    });

    const req = httpTestingController.expectOne('/test');
    req.flush('Server Error', { status: 500, statusText: 'Server Error' });

    expect(consoleErrorSpy).toHaveBeenCalledWith('An unexpected error occurred. Please try again later.');
  });

  it('should log error on 0 Network Error', () => {
    httpClient.get('/test').subscribe({
      error: (error: HttpErrorResponse) => {
        expect(error.status).toBe(0);
      }
    });

    const req = httpTestingController.expectOne('/test');
    req.flush('Network Error', { status: 0, statusText: 'Network Error' });

    expect(consoleErrorSpy).toHaveBeenCalledWith('An unexpected error occurred. Please try again later.');
  });
});
