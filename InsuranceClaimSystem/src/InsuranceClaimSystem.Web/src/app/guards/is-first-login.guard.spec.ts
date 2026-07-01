import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { isFirstLoginGuard } from './is-first-login.guard';
import { AuthService } from '../services/auth.service';

describe('isFirstLoginGuard', () => {
  let mockRouter: any;
  let mockAuthService: any;

  beforeEach(() => {
    mockRouter = { navigate: jasmine.createSpy('navigate') };
    mockAuthService = { currentUserValue: null };

    TestBed.configureTestingModule({
      providers: [
        { provide: Router, useValue: mockRouter },
        { provide: AuthService, useValue: mockAuthService }
      ]
    });
  });

  it('should allow access if user is not logged in', () => {
    TestBed.runInInjectionContext(() => {
      expect(isFirstLoginGuard({} as any, { url: '/some-url' } as any)).toBe(true);
    });
  });

  it('should redirect to /set-password if user is logged in and isFirstLogin is true', () => {
    mockAuthService.currentUserValue = { isFirstLogin: true };
    TestBed.runInInjectionContext(() => {
      expect(isFirstLoginGuard({} as any, { url: '/dashboard' } as any)).toBe(false);
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/set-password']);
    });
  });

  it('should allow access to /set-password if isFirstLogin is true', () => {
    mockAuthService.currentUserValue = { isFirstLogin: true };
    TestBed.runInInjectionContext(() => {
      expect(isFirstLoginGuard({} as any, { url: '/set-password' } as any)).toBe(true);
      expect(mockRouter.navigate).not.toHaveBeenCalled();
    });
  });

  it('should redirect to / if user is on /set-password and isFirstLogin is false', () => {
    mockAuthService.currentUserValue = { isFirstLogin: false };
    TestBed.runInInjectionContext(() => {
      expect(isFirstLoginGuard({} as any, { url: '/set-password' } as any)).toBe(false);
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/']);
    });
  });

  it('should allow access to normal routes if isFirstLogin is false', () => {
    mockAuthService.currentUserValue = { isFirstLogin: false };
    TestBed.runInInjectionContext(() => {
      expect(isFirstLoginGuard({} as any, { url: '/dashboard' } as any)).toBe(true);
      expect(mockRouter.navigate).not.toHaveBeenCalled();
    });
  });
});
