import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClaimStatusBadgeComponent } from './claim-status-badge.component';

describe('ClaimStatusBadgeComponent', () => {
  let component: ClaimStatusBadgeComponent;
  let fixture: ComponentFixture<ClaimStatusBadgeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ClaimStatusBadgeComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ClaimStatusBadgeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
