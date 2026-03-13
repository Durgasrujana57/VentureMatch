import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { MatchingService, Match } from '../../../core/services/matching.service';
import { MatchCardComponent } from '../match-card/match-card.component';

@Component({
  selector: 'app-matches-list',
  standalone: true,
  imports: [CommonModule, RouterModule, MatchCardComponent],
  template: `
    <div class="container mt-4">
      <h2 class="mb-4">Your Matches</h2>
      
      <!-- Error Message -->
      <div *ngIf="error" class="alert alert-danger">
        <i class="bi bi-exclamation-triangle-fill"></i>
        {{ error }}
        <button class="btn btn-sm btn-outline-danger ms-3" (click)="loadMatches()">
          <i class="bi bi-arrow-repeat"></i> Retry
        </button>
      </div>

      <!-- Loading State -->
      <div *ngIf="loading" class="text-center my-5">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">Loading...</span>
        </div>
        <p class="mt-2">Loading your matches...</p>
      </div>

      <!-- No Matches -->
      <div *ngIf="!loading && !error && matches.length === 0" class="text-center my-5 py-5">
        <i class="bi bi-heart" style="font-size: 4rem; color: #ccc;"></i>
        <h4 class="mt-3">No matches yet</h4>
        <p class="text-muted">When someone likes you back, they'll appear here!</p>
        <a routerLink="/dashboard" class="btn btn-primary">
          <i class="bi bi-search"></i> Find Matches
        </a>
      </div>

      <!-- Matches Grid -->
      <div *ngIf="!loading && !error && matches.length > 0" class="row g-4">
        <div *ngFor="let match of matches" class="col-md-4 col-sm-6">
          <app-match-card 
            [match]="{
              id: match.user.id,
              fullName: match.user.fullName,
              headline: match.user.headline,
              avatarUrl: match.user.avatarUrl,
              matchScore: match.matchScore,
              matchedAt: match.matchedAt
            }"
            (onMessage)="goToMessages(match.matchId, match.user.fullName)"
            (onView)="goToProfile(match.user.id)">
          </app-match-card>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .spinner-border {
      width: 3rem;
      height: 3rem;
    }
    .alert {
      margin-bottom: 20px;
    }
  `]
})
export class MatchesListComponent implements OnInit {
  matches: Match[] = [];
  loading = true;
  error: string = '';

  constructor(
    private matchingService: MatchingService,
    private router: Router
  ) {}

  ngOnInit(): void {
    console.log('MatchesListComponent initialized');
    this.loadMatches();
  }

  loadMatches(): void {
    this.loading = true;
    this.error = '';
    
    console.log('Fetching matches...');
    this.matchingService.getMatches().subscribe({
      next: (data: Match[]) => {
        console.log('Matches received:', data);
        this.matches = data;
        this.loading = false;
      },
      error: (error: any) => {
        console.error('Error loading matches:', error);
        this.error = 'Failed to load matches. Please try again.';
        this.loading = false;
      }
    });
  }

  // FIXED: Now navigates to actual chat room instead of showing alert
  goToMessages(matchId: string, userName: string): void {
    console.log('Navigating to chat room for match:', matchId);
    this.router.navigate(['/messages', matchId]);
  }

  goToProfile(userId: string): void {
    console.log('Navigating to profile for user:', userId);
    this.router.navigate(['/profile', userId]);
  }
}