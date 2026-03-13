import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Idea } from '../../../core/services/idea.service';

@Component({
  selector: 'app-idea-card',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './idea-card.component.html',
  styleUrls: ['./idea-card.component.scss']
})
export class IdeaCardComponent {
  @Input() idea!: Idea;
  @Output() ideaClick = new EventEmitter<string>();

  onClick(): void {
    this.ideaClick.emit(this.idea.id);
  }

  getShortDescription(): string {
    if (!this.idea.description) return '';
    return this.idea.description.length > 100 
      ? this.idea.description.substring(0, 100) + '...' 
      : this.idea.description;
  }

  getTimeAgo(date: Date): string {
    const now = new Date();
    const past = new Date(date);
    const diffMs = now.getTime() - past.getTime();
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
    const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
    const diffMins = Math.floor(diffMs / (1000 * 60));

    if (diffDays > 7) {
      return past.toLocaleDateString();
    } else if (diffDays > 0) {
      return `${diffDays}d ago`;
    } else if (diffHours > 0) {
      return `${diffHours}h ago`;
    } else if (diffMins > 0) {
      return `${diffMins}m ago`;
    } else {
      return 'Just now';
    }
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
}