import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule, Router } from '@angular/router';
import { MatchingService } from '../../../core/services/matching.service';
import { UserService } from '../../../core/services/user.service'; 

@Component({
  selector: 'app-profile-view',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './profile-view.component.html',
  styleUrls: ['./profile-view.component.scss']
})
export class ProfileViewComponent implements OnInit {
  userId: string = '';
  user: any = null;
  loading = true;
  debugInfo: any = {};

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private matchingService: MatchingService,
    private userService: UserService // Add UserService
  ) {}

  ngOnInit(): void {
    this.userId = this.route.snapshot.paramMap.get('id') || '';
    console.log('🔍 Profile View - User ID from URL:', this.userId);
    
    if (!this.userId) {
      console.error('❌ No user ID found in URL');
      this.loading = false;
      return;
    }
    
    this.loadUserProfile();
  }

  loadUserProfile(): void {
    console.log('📡 Fetching user profile for ID:', this.userId);
    
    // Try to get full user profile from UserService
    this.userService.getUserById(this.userId).subscribe({
      next: (userData: any) => {
        console.log('✅ User profile received:', userData);
        this.user = userData;
        this.loading = false;
      },
      error: (error: any) => {
        console.error('❌ Error loading user profile:', error);
        // Fallback to matches data if user service fails
        this.loadFromMatches();
      }
    });
  }

  loadFromMatches(): void {
    console.log('📡 Falling back to matches data...');
    this.matchingService.getMatches().subscribe({
      next: (matches: any[]) => {
        const match = matches.find(m => m.user && m.user.id === this.userId);
        if (match) {
          console.log('✅ User found in matches:', match.user);
          this.user = match.user;
        } else {
          console.log('❌ User not found in matches');
        }
        this.loading = false;
      },
      error: (error: any) => {
        console.error('❌ Error loading matches:', error);
        this.loading = false;
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/matches']);
  }
}