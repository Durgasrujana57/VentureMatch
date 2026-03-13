import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { IdeaService, Idea, Application } from '../../../core/services/idea.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-idea-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './idea-detail.component.html',
  styleUrls: ['./idea-detail.component.scss']
})
export class IdeaDetailComponent implements OnInit {
  ideaId: string = '';
  idea: Idea | null = null;
  loading = true;
  error = '';
  isOwner = false;
  showApplyForm = false;
  applicationMessage = '';
  submitting = false;
  applications: Application[] = [];
  showApplications = false;
  selectedTab: 'details' | 'applications' = 'details';
  currentUserId: string = '';
  
  // Properties for user application tracking
  userApplication: Application | null = null;
  applicationStatus: string = '';
  showSuccessMessage = false;
  successMessage = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private ideaService: IdeaService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.ideaId = this.route.snapshot.paramMap.get('id') || '';
    this.currentUserId = this.authService.getCurrentUserId() || '';
    
    if (!this.ideaId) {
      this.router.navigate(['/ideas']);
      return;
    }

    this.loadIdea();
  }

  loadIdea(): void {
    this.loading = true;
    this.error = '';

    this.ideaService.getIdeaById(this.ideaId).subscribe({
      next: (data: Idea) => {
        this.idea = data;
        this.isOwner = data.userId === this.currentUserId;
        this.loading = false;
        
        if (this.isOwner) {
          this.loadApplications();
        } else {
          // Check if user has applied only if they're not the owner
          this.checkUserApplication();
        }
      },
      error: (err: any) => {
        console.error('Error loading idea:', err);
        this.error = 'Failed to load idea. Please try again.';
        this.loading = false;
      }
    });
  }

  loadApplications(): void {
    this.ideaService.getIdeaApplications(this.ideaId).subscribe({
      next: (data: Application[]) => {
        this.applications = data;
      },
      error: (err: any) => {
        console.error('Error loading applications:', err);
      }
    });
  }

  // ✅ UPDATED: Method to check if current user has applied using the new endpoint
  checkUserApplication(): void {
    if (!this.isOwner && this.authService.isAuthenticated() && this.currentUserId) {
      console.log('📡 Checking my application for idea:', this.ideaId);
      
      this.ideaService.getMyApplication(this.ideaId).subscribe({
        next: (application: Application | null) => {
          console.log('✅ My application response:', application);
          if (application) {
            this.userApplication = application;
            this.applicationStatus = application.status;
            console.log('✅ Application status:', this.applicationStatus);
          } else {
            console.log('❌ No application found');
            this.applicationStatus = '';
          }
        },
        error: (err: any) => {
          console.error('❌ Error checking application:', err);
        }
      });
    }
  }

  // Updated applyToIdea method
  applyToIdea(): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: `/ideas/${this.ideaId}` } });
      return;
    }

    this.submitting = true;
    
    this.ideaService.applyToIdea({
      ideaId: this.ideaId,
      message: this.applicationMessage
    }).subscribe({
      next: (response: any) => {
        this.successMessage = 'Application submitted successfully!';
        this.showSuccessMessage = true;
        this.showApplyForm = false;
        this.applicationMessage = '';
        this.submitting = false;
        this.applicationStatus = 'pending';
        
        // Create a temporary application object for immediate feedback
        this.userApplication = {
          id: response.applicationId || 'temp',
          ideaId: this.ideaId,
          userId: this.currentUserId,
          message: this.applicationMessage,
          status: 'pending',
          appliedAt: new Date()
        };
        
        // Auto-hide success message after 3 seconds
        setTimeout(() => {
          this.showSuccessMessage = false;
        }, 3000);

        // Refresh applications if needed
        if (this.isOwner) {
          this.loadApplications();
        }
      },
      error: (err: any) => {
        console.error('Error applying to idea:', err);
        this.error = 'Failed to submit application. Please try again.';
        this.submitting = false;
      }
    });
  }

  updateApplicationStatus(applicationId: string, status: string): void {
    this.ideaService.updateApplicationStatus(applicationId, status).subscribe({
      next: () => {
        this.loadApplications();
        // Update userApplication if it's the current user's application
        if (this.userApplication && this.userApplication.id === applicationId) {
          this.userApplication.status = status;
          this.applicationStatus = status;
          
          // Show success message for status change
          this.successMessage = status === 'accepted' 
            ? 'Application accepted! The user has been notified.' 
            : 'Application rejected.';
          this.showSuccessMessage = true;
          
          setTimeout(() => {
            this.showSuccessMessage = false;
          }, 3000);
        }
      },
      error: (err: any) => {
        console.error('Error updating application:', err);
        alert('Failed to update application status. Please try again.');
      }
    });
  }

  deleteIdea(): void {
    if (confirm('Are you sure you want to delete this idea?')) {
      this.ideaService.deleteIdea(this.ideaId).subscribe({
        next: () => {
          this.router.navigate(['/ideas']);
        },
        error: (err: any) => {
          console.error('Error deleting idea:', err);
          alert('Failed to delete idea. Please try again.');
        }
      });
    }
  }

  editIdea(): void {
    this.router.navigate(['/ideas/edit', this.ideaId]);
  }

  goBack(): void {
    this.router.navigate(['/ideas']);
  }

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  }

  getStageClass(stage: string): string {
    const stageMap: {[key: string]: string} = {
      'Idea': 'stage-idea',
      'MVP': 'stage-mvp',
      'Prototype': 'stage-prototype',
      'Launched': 'stage-launched'
    };
    return stageMap[stage] || 'stage-idea';
  }

  getStatusClass(status: string): string {
    const statusMap: {[key: string]: string} = {
      'pending': 'status-pending',
      'accepted': 'status-accepted',
      'rejected': 'status-rejected'
    };
    return statusMap[status] || 'status-pending';
  }

  viewProfile(userId: string): void {
    this.router.navigate(['/profile', userId]);
  }

  // Helper method to check if user can apply
  canApply(): boolean {
    return !this.isOwner && 
           !this.applicationStatus && 
           this.authService.isAuthenticated();
  }

  // Helper method to get status message
  getStatusMessage(): string {
    switch(this.applicationStatus) {
      case 'pending':
        return 'Your application is pending review. The idea owner will respond soon.';
      case 'accepted':
        return '🎉 Congratulations! Your application has been accepted. You can now message the idea owner.';
      case 'rejected':
        return 'Your application was not accepted. Keep looking for other great ideas!';
      default:
        return '';
    }
  }
}