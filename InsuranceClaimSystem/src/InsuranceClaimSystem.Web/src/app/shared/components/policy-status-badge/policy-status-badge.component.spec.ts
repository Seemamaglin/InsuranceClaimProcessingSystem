import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PolicyStatusBadgeComponent } from './policy-status-badge.component';

describe('PolicyStatusBadgeComponent', () => {
  let component: PolicyStatusBadgeComponent;
  let fixture: ComponentFixture<PolicyStatusBadgeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PolicyStatusBadgeComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PolicyStatusBadgeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
