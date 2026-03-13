import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { IdeaService, Idea } from '../../../core/services/idea.service';
import { IdeaCardComponent } from '../idea-card/idea-card.component';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-idea-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, IdeaCardComponent],
  templateUrl: './idea-list.component.html',
  styleUrls: ['./idea-list.component.scss']
})
export class IdeaListComponent implements OnInit {
  ideas: Idea[] = [];
  filteredIdeas: Idea[] = [];
  loading = true;
  error = '';

  searchTerm = '';
  selectedIndustry = '';
  selectedStage = '';

  industries: string[] = [
    'AI/ML', 'EdTech', 'FinTech', 'HealthTech', 'SaaS', 
    'E-commerce', 'Social Media', 'Gaming', 'CleanTech', 'Other'
  ];
  
  stages: string[] = ['Idea', 'MVP', 'Prototype', 'Launched'];

  constructor(
    private ideaService: IdeaService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadIdeas();
  }

  loadIdeas(): void {
    this.loading = true;
    this.error = '';

    this.ideaService.getAllIdeas(this.selectedIndustry, this.selectedStage, this.searchTerm).subscribe({
      next: (data: Idea[]) => {
        this.ideas = data;
        this.filteredIdeas = data;
        this.loading = false;
      },
      error: (err: any) => {
        console.error('Error loading ideas:', err);
        this.error = 'Failed to load ideas. Please try again.';
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    this.loadIdeas();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedIndustry = '';
    this.selectedStage = '';
    this.loadIdeas();
  }

  onIdeaClick(ideaId: string): void {
    this.router.navigate(['/ideas', ideaId]);
  }

  createNewIdea(): void {
    this.router.navigate(['/ideas/new']);
  }

  canCreateIdea(): boolean {
    return this.authService.isAuthenticated();
  }
}