import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { MatchingService } from '../../../core/services/matching.service';

@Component({
  selector: 'app-match-details',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="container mt-4">
      <div *ngIf="loading" class="text-center my-5">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">Loading...</span>
        </div>
        <p class="mt-2">Loading match details...</p>
      </div>

      <div *ngIf="!loading && match" class="match-details">
        <div class="row">
          <div class="col-md-4">
            <div class="card text-center">
              <div class="card-body">
                <img 
                  [src]="match.user.avatarUrl || 'https://ui-avatars.com/api/?name=' + match.user.fullName + '&background=667eea&color=fff&size=128'" 
                  class="rounded-circle mb-3" 
                  width="150" 
                  height="150"
                  [alt]="match.user.fullName"
                  style="object-fit: cover; border: 3px solid #667eea;">
                
                <h3>{{ match.user.fullName }}</h3>
                <p class="text-muted">{{ match.user.headline || 'No headline' }}</p>
                
                <div class="mb-3">
                  <span class="badge bg-success fs-6 p-3">{{ match.matchScore }}% Match</span>
                </div>
                
                <p class="text-muted">
                  <i class="bi bi-calendar"></i> 
                  Matched on {{ match.matchedAt | date:'fullDate' }}
                </p>
                
                <div class="d-flex gap-2 justify-content-center">
                  <a [routerLink]="['/messages', match.matchId]" class="btn btn-primary">
                    <i class="bi bi-chat"></i> Send Message
                  </a>
                  <a routerLink="/matches" class="btn btn-outline-secondary">
                    <i class="bi bi-arrow-left"></i> Back
                  </a>
                </div>
              </div>
            </div>
          </div>
          
          <div class="col-md-8">
            <div class="card">
              <div class="card-body">
                <h4>About {{ match.user.fullName }}</h4>
                <p>{{ match.user.bio || 'No bio provided' }}</p>
                
                <h5 class="mt-4">Skills</h5>
                <div class="skills-list">
                  <span *ngFor="let skill of match.user.skills" class="skill-tag">
                    {{ skill }}
                  </span>
                  <span *ngIf="!match.user.skills?.length" class="text-muted">
                    No skills listed
                  </span>
                </div>
                
                <h5 class="mt-4">Interests</h5>
                <div class="interests-list">
                  <span *ngFor="let interest of match.user.interests" class="interest-tag">
                    {{ interest }}
                  </span>
                  <span *ngIf="!match.user.interests?.length" class="text-muted">
                    No interests listed
                  </span>
                </div>
                
                <h5 class="mt-4">Location</h5>
                <p><i class="bi bi-geo-alt"></i> {{ match.user.location || 'Not specified' }}</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .badge {
      border-radius: 25px;
    }
    .skills-list, .interests-list {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
    }
    .skill-tag {
      background: linear-gradient(135deg, #e3f2fd 0%, #bbdefb 100%);
      color: #1976d2;
      padding: 5px 12px;
      border-radius: 20px;
      font-size: 0.9rem;
    }
    .interest-tag {
      background: linear-gradient(135deg, #f3e5f5 0%, #e1bee7 100%);
      color: #7b1fa2;
      padding: 5px 12px;
      border-radius: 20px;
      font-size: 0.9rem;
    }
    .card {
      border: none;
      border-radius: 15px;
      box-shadow: 0 5px 20px rgba(0,0,0,0.1);
      margin-bottom: 20px;
    }
  `]
})
export class MatchDetailsComponent implements OnInit {
  matchId: string = '';
  match: any = null;
  loading = true;

  constructor(
    private route: ActivatedRoute,
    private matchingService: MatchingService
  ) {}

  ngOnInit(): void {
    this.matchId = this.route.snapshot.paramMap.get('id') || '';
    this.loadMatchDetails();
  }

  loadMatchDetails(): void {
    // First get all matches
    this.matchingService.getMatches().subscribe({
      next: (matches) => {
        this.match = matches.find(m => m.matchId === this.matchId);
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading match details:', error);
        this.loading = false;
      }
    });
  }
}