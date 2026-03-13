import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { MatchingService } from '../../core/services/matching.service';

// Define interfaces for type safety
interface MatchUser {
  id: string;
  fullName: string;
  headline: string;
  bio?: string;
  location: string;
  avatarUrl?: string;
  skills: string[];
  interests: string[];
  availability: string;
}

interface PotentialMatch {
  user: MatchUser;
  matchScore: number;
  matchReason: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  user: any = null;
  potentialMatches: PotentialMatch[] = [];
  loading = true;
  errorMessage = '';
  showDebug = true; // Set to false in production

  constructor(
    private authService: AuthService,
    private matchingService: MatchingService,
    private cdr: ChangeDetectorRef,
    private router: Router
  ) {
    console.log('🔧 DASHBOARD CONSTRUCTOR');
  }

  ngOnInit(): void {
    console.log('🚀 DASHBOARD INIT');
    
    // Check authentication
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login']);
      return;
    }

    // Get current user from localStorage first (for immediate display)
    const userStr = localStorage.getItem('user');
    if (userStr) {
      try {
        this.user = JSON.parse(userStr);
      } catch (e) {
        console.error('Error parsing user:', e);
      }
    }

    // Get current user from service
    this.authService.currentUser$.subscribe({
      next: (user) => {
        console.log('👤 AuthService user:', user);
        if (user) {
          this.user = user;
          this.cdr.detectChanges();
        }
      },
      error: (err) => console.error('❌ AuthService error:', err)
    });

    // Load matches
    this.loadPotentialMatches();
  }

  loadPotentialMatches(): void {
    console.log('📡 LOADING MATCHES');
    
    this.matchingService.getPotentialMatches().subscribe({
      next: (matches) => {
        console.log('✅ MATCHES RECEIVED:', matches);
        this.potentialMatches = matches || [];
        this.loading = false;
        this.cdr.detectChanges();
        console.log('✅ Matches assigned:', this.potentialMatches.length);
      },
      error: (error) => {
        console.error('❌ Error loading matches:', error);
        if (error.status === 401) {
          this.errorMessage = 'Your session has expired. Please login again.';
          setTimeout(() => {
            this.authService.logout();
            this.router.navigate(['/login']);
          }, 2000);
        } else {
          this.errorMessage = 'Failed to load matches. Please try again.';
        }
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  likeUser(userId: string, userName: string): void {
    console.log('👍 Liking user:', userId, userName);
    
    this.matchingService.likeUser(userId).subscribe({
      next: (response) => {
        console.log('✅ Like response:', response);
        
        if (response.isMatch) {
          // Show match notification
          this.showMatchNotification(userName);
          
          // Also show alert
          alert(`🎉 It's a match with ${userName}!`);
        }
        
        // Remove from list
        this.potentialMatches = this.potentialMatches.filter(m => m.user.id !== userId);
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('❌ Like error:', error);
        if (error.status === 401) {
          alert('Session expired. Please login again.');
          this.authService.logout();
          this.router.navigate(['/login']);
        }
      }
    });
  }

  passUser(userId: string, userName: string): void {
    console.log('👎 Passing user:', userId, userName);
    
    this.matchingService.passUser(userId).subscribe({
      next: () => {
        console.log('✅ Pass successful');
        this.potentialMatches = this.potentialMatches.filter(m => m.user.id !== userId);
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('❌ Pass error:', error);
        if (error.status === 401) {
          alert('Session expired. Please login again.');
          this.authService.logout();
          this.router.navigate(['/login']);
        }
      }
    });
  }

  showMatchNotification(userName: string): void {
    // You can replace this with a toast notification later
    console.log(`🎉 New match with ${userName}!`);
  }

  getMatchScoreColor(score: number): string {
    if (score >= 80) return '#4CAF50'; // Green
    if (score >= 60) return '#FFC107'; // Yellow
    if (score >= 40) return '#FF9800'; // Orange
    return '#F44336'; // Red
  }

  refreshMatches(): void {
    this.loading = true;
    this.potentialMatches = [];
    this.loadPotentialMatches();
  }

  toggleDebug(): void {
    this.showDebug = !this.showDebug;
  }
}