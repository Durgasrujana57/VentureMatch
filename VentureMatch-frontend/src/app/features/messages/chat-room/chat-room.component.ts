import { Component, OnInit, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MessageService, Message, ChatMatch } from '../../../core/services/message.service';
import { AuthService } from '../../../core/services/auth.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-chat-room',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './chat-room.component.html',
  styleUrls: ['./chat-room.component.scss']
})
export class ChatRoomComponent implements OnInit, OnDestroy {
  @ViewChild('messagesContainer') private messagesContainer!: ElementRef;
  @ViewChild('messageInput') private messageInput!: ElementRef;

  matchId: string = '';
  match: ChatMatch | null = null;
  messages: Message[] = [];
  newMessage = '';
  loading = true;
  sending = false;
  currentUserId: string = '';
  otherUserTyping = false;
  typingTimeout: any;

  private subscriptions: Subscription[] = [];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private messageService: MessageService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.matchId = this.route.snapshot.paramMap.get('matchId') || '';
    this.currentUserId = this.authService.getCurrentUserId() || '';

    if (!this.matchId) {
      this.router.navigate(['/messages']);
      return;
    }

    // Subscribe to messages
    this.subscriptions.push(
      this.messageService.messages$.subscribe(messages => {
        this.messages = messages;
        this.loading = false;
        this.scrollToBottom();
      })
    );

    // Subscribe to matches to find current match info
    this.subscriptions.push(
      this.messageService.matches$.subscribe(matches => {
        this.match = matches.find(m => m.matchId === this.matchId) || null;
      })
    );

    // Subscribe to typing indicators
    this.subscriptions.push(
      this.messageService.typing$.subscribe(indicator => {
        if (indicator.userId !== this.currentUserId) {
          this.otherUserTyping = indicator.isTyping;
          this.scrollToBottom();
        }
      })
    );

    // Load matches if needed
    this.messageService.loadMatches();
    
    // Start SignalR connection and join room
    this.messageService.startConnection().then(() => {
      this.messageService.joinMatchRoom(this.matchId);
    });
  }

  ngAfterViewInit(): void {
    this.scrollToBottom();
  }

  ngOnDestroy(): void {
    // Clear typing indicator
    if (this.typingTimeout) {
      clearTimeout(this.typingTimeout);
    }
    
    // Leave match room
    this.messageService.leaveMatchRoom(this.matchId);
    
    // Clear messages
    this.messageService.clearMessages();
    
    // Unsubscribe
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }

  sendMessage(): void {
    if (!this.newMessage.trim() || !this.match) return;

    this.sending = true;
    const content = this.newMessage.trim();
    this.newMessage = '';

    this.messageService.sendMessage(this.matchId, this.match.user.id, content)
      .then(() => {
        this.sending = false;
        this.scrollToBottom();
      })
      .catch(err => {
        console.error('Error sending message:', err);
        this.sending = false;
      });
  }

  onTyping(): void {
    if (!this.match) return;

    // Send typing indicator
    this.messageService.sendTyping(this.matchId, true);

    // Clear previous timeout
    if (this.typingTimeout) {
      clearTimeout(this.typingTimeout);
    }

    // Set timeout to stop typing indicator
    this.typingTimeout = setTimeout(() => {
      this.messageService.sendTyping(this.matchId, false);
    }, 1000);
  }

  onKeyPress(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  markAsRead(message: Message): void {
    if (!message.isRead && message.receiverId === this.currentUserId) {
      this.messageService.markAsRead(this.matchId, message.id);
    }
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      try {
        this.messagesContainer.nativeElement.scrollTop = 
          this.messagesContainer.nativeElement.scrollHeight;
      } catch (err) {
        console.error('Error scrolling:', err);
      }
    }, 100);
  }

  goBack(): void {
    this.router.navigate(['/messages']);
  }

  viewProfile(): void {
    if (this.match) {
      this.router.navigate(['/profile', this.match.user.id]);
    }
  }

  formatTime(date: Date): string {
    const messageDate = new Date(date);
    const now = new Date();
    const diffDays = Math.floor((now.getTime() - messageDate.getTime()) / (1000 * 60 * 60 * 24));

    if (diffDays === 0) {
      return messageDate.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    } else if (diffDays === 1) {
      return 'Yesterday';
    } else {
      return messageDate.toLocaleDateString();
    }
  }

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString([], { 
      weekday: 'long', 
      year: 'numeric', 
      month: 'long', 
      day: 'numeric' 
    });
  }

  shouldShowDate(index: number): boolean {
    if (index === 0) return true;
    
    const currentDate = new Date(this.messages[index].createdAt).toDateString();
    const prevDate = new Date(this.messages[index - 1].createdAt).toDateString();
    
    return currentDate !== prevDate;
  }

  getMessageDate(date: Date): string {
    const messageDate = new Date(date);
    const today = new Date().toDateString();
    const yesterday = new Date(Date.now() - 86400000).toDateString();

    if (messageDate.toDateString() === today) {
      return 'Today';
    } else if (messageDate.toDateString() === yesterday) {
      return 'Yesterday';
    } else {
      return this.formatDate(date);
    }
  }
}