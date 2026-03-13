import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface MatchCardUser {
  id: string;
  fullName: string;
  headline: string;
  avatarUrl?: string;
  matchScore: number;
  matchedAt: Date;
}

@Component({
  selector: 'app-match-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="card h-100 shadow-sm">
      <div class="card-body text-center">
        <!-- Avatar -->
        <img 
          [src]="match.avatarUrl || 'https://ui-avatars.com/api/?name=' + match.fullName + '&background=667eea&color=fff&size=128'" 
          class="rounded-circle mb-3" 
          width="100" 
          height="100"
          [alt]="match.fullName"
          style="object-fit: cover; border: 3px solid #667eea;">
        
        <!-- User Info -->
        <h5 class="card-title fw-bold">{{ match.fullName }}</h5>
        <p class="card-text text-muted small">{{ match.headline || 'No headline' }}</p>
        
        <!-- Match Score -->
        <div class="mb-3">
          <span class="badge" [ngClass]="{
            'bg-success': match.matchScore >= 70,
            'bg-warning': match.matchScore >= 40 && match.matchScore < 70,
            'bg-danger': match.matchScore < 40
          }">
            {{ match.matchScore }}% Match
          </span>
        </div>
        
        <!-- Match Date -->
        <p class="small text-muted mb-3">
          <i class="bi bi-calendar"></i> 
          Matched on {{ match.matchedAt | date:'mediumDate' }}
        </p>
        
        <!-- Action Buttons -->
        <div class="d-flex justify-content-center gap-2">
          <button class="btn btn-primary btn-sm" (click)="onMessage.emit(match.id)">
            <i class="bi bi-chat"></i> Message
          </button>
          <button class="btn btn-outline-secondary btn-sm" (click)="onView.emit(match.id)">
            <i class="bi bi-person"></i> View Profile
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .card {
      border: none;
      border-radius: 15px;
      transition: all 0.3s ease;
      overflow: hidden;
    }
    .card:hover {
      transform: translateY(-5px);
      box-shadow: 0 15px 30px rgba(0,0,0,0.1) !important;
    }
    .badge {
      padding: 8px 15px;
      border-radius: 20px;
    }
    .btn-sm {
      padding: 8px 15px;
      border-radius: 20px;
    }
  `]
})
export class MatchCardComponent {
  @Input() match!: MatchCardUser;
  @Output() onMessage = new EventEmitter<string>();
  @Output() onView = new EventEmitter<string>();
}