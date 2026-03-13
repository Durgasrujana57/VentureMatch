import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MessageService, ChatMatch, Message } from '../../../core/services/message.service'; // Add Message here
import { AuthService } from '../../../core/services/auth.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-chat-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './chat-list.component.html',
  styleUrls: ['./chat-list.component.scss']
})
export class ChatListComponent implements OnInit, OnDestroy {
  matches: ChatMatch[] = [];
  filteredMatches: ChatMatch[] = [];
  loading = true;
  searchTerm = '';
  currentUserId: string = '';
  
  private subscriptions: Subscription[] = [];

  constructor(
    private messageService: MessageService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.currentUserId = this.authService.getCurrentUserId() || '';
    
    // Subscribe to matches
    this.subscriptions.push(
      this.messageService.matches$.subscribe(matches => {
        this.matches = matches;
        this.filteredMatches = matches;
        this.loading = false;
      })
    );

    // Subscribe to unread count updates
    this.subscriptions.push(
      this.messageService.unreadCount$.subscribe(() => {
        // Force change detection for unread badges
        this.filteredMatches = [...this.filteredMatches];
      })
    );

    // Load matches
    this.messageService.loadMatches();
    
    // Start SignalR connection
    this.messageService.startConnection();
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
    this.messageService.disconnect();
  }

  filterMatches(): void {
    if (!this.searchTerm.trim()) {
      this.filteredMatches = this.matches;
      return;
    }
    
    const term = this.searchTerm.toLowerCase().trim();
    this.filteredMatches = this.matches.filter(match => 
      match.user.fullName.toLowerCase().includes(term) ||
      match.user.headline?.toLowerCase().includes(term)
    );
  }

  selectMatch(matchId: string): void {
    this.router.navigate(['/messages', matchId]);
  }

  getLastMessageTime(date: Date): string {
    const now = new Date();
    const messageDate = new Date(date);
    const diffMs = now.getTime() - messageDate.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m`;
    if (diffHours < 24) return `${diffHours}h`;
    if (diffDays === 1) return 'Yesterday';
    return messageDate.toLocaleDateString();
  }

  getLastMessagePreview(message?: Message): string {
    if (!message) return 'No messages yet';
    return message.content.length > 30 
      ? message.content.substring(0, 30) + '...' 
      : message.content;
  }
}