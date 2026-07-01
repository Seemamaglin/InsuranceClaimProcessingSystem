import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ApplyPolicyComponent } from './apply-policy.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';

describe('ApplyPolicyComponent', () => {
  let component: ApplyPolicyComponent;
  let fixture: ComponentFixture<ApplyPolicyComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ApplyPolicyComponent, HttpClientTestingModule, RouterTestingModule]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ApplyPolicyComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
