import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DocumentVerificationBadgeComponent } from './document-verification-badge.component';

describe('DocumentVerificationBadgeComponent', () => {
  let component: DocumentVerificationBadgeComponent;
  let fixture: ComponentFixture<DocumentVerificationBadgeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DocumentVerificationBadgeComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DocumentVerificationBadgeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
