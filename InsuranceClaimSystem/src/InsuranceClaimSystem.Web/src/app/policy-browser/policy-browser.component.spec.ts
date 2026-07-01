import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PolicyBrowserComponent } from './policy-browser.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';

describe('PolicyBrowserComponent', () => {
  let component: PolicyBrowserComponent;
  let fixture: ComponentFixture<PolicyBrowserComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PolicyBrowserComponent, HttpClientTestingModule, RouterTestingModule]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PolicyBrowserComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
