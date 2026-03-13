import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import * as signalR from '@microsoft/signalr';

export interface Message {
  id: string;
  matchId: string;
  senderId: string;
  receiverId: string;
  content: string;
  isRead: boolean;
  createdAt: Date;
  isMine: boolean;
}

export interface ChatMatch {
  matchId: string;
  user: {
    id: string;
    fullName: string;
    headline: string;
    avatarUrl?: string;
  };
  lastMessage?: Message;
  unreadCount: number;
  matchedAt: Date;
}

export interface UnreadCount {
  count: number;
}

export interface TypingIndicator {
  userId: string;
  isTyping: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class MessageService {
  private apiUrl = `${environment.apiUrl}/Messages`;
  private hubConnection!: signalR.HubConnection;
  private messagesSubject = new BehaviorSubject<Message[]>([]);
  public messages$ = this.messagesSubject.asObservable();
  
  private matchesSubject = new BehaviorSubject<ChatMatch[]>([]);
  public matches$ = this.matchesSubject.asObservable();
  
  private unreadCountSubject = new BehaviorSubject<number>(0);
  public unreadCount$ = this.unreadCountSubject.asObservable();
  
  private typingSubject = new Subject<TypingIndicator>();
  public typing$ = this.typingSubject.asObservable();

  private currentMatchId: string = '';
  private currentUserId: string | null = null;

  constructor(private http: HttpClient) {
    this.loadCurrentUser();
    this.loadMatches();
    this.loadUnreadCount();
  }

  // ========== Helper Methods ==========

  private loadCurrentUser(): void {
    const userStr = localStorage.getItem('user');
    if (userStr) {
      try {
        const user = JSON.parse(userStr);
        this.currentUserId = user.id;
      } catch (e) {
        console.error('Error parsing user:', e);
      }
    }
  }

  private setMessageOwnership(messages: Message[]): Message[] {
    return messages.map(msg => ({
      ...msg,
      isMine: msg.senderId === this.currentUserId
    }));
  }

  // ========== SignalR Connection ==========

  async startConnection(): Promise<void> {
    const token = localStorage.getItem('token');
    
    if (!token) {
      console.error('❌ No token found for SignalR connection');
      return;
    }

    this.loadCurrentUser(); // Reload current user

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl.replace('/api', '')}/hubs/chat`, {
        accessTokenFactory: () => token,
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 20000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.registerEvents();

    try {
      await this.hubConnection.start();
      console.log('✅ SignalR connected');
      
      // Join current match room if any
      if (this.currentMatchId) {
        this.joinMatchRoom(this.currentMatchId);
      }
    } catch (err) {
      console.error('❌ SignalR connection error:', err);
      setTimeout(() => this.startConnection(), 5000);
    }
  }

  private registerEvents(): void {
    this.hubConnection.on('ReceiveMessage', (message: Message) => {
      console.log('📩 New message received:', message);
      
      // Set isMine property based on current user
      const messageWithMine = {
        ...message,
        isMine: message.senderId === this.currentUserId
      };
      
      const currentMessages = this.messagesSubject.value;
      this.messagesSubject.next([...currentMessages, messageWithMine]);
      
      this.loadMatches();
      this.loadUnreadCount();
    });

    this.hubConnection.on('NewMessage', (data: any) => {
      console.log('🔔 New message notification:', data);
      this.loadUnreadCount();
      this.loadMatches();
    });

    this.hubConnection.on('MessageRead', (data: any) => {
      console.log('👁️ Message read:', data);
      const messages = this.messagesSubject.value.map(m => 
        m.id === data.messageId ? { ...m, isRead: true } : m
      );
      this.messagesSubject.next(messages);
    });

    this.hubConnection.on('UserTyping', (data: TypingIndicator) => {
      this.typingSubject.next(data);
    });

    this.hubConnection.onreconnecting(error => {
      console.log('🔄 SignalR reconnecting...', error);
    });

    this.hubConnection.onreconnected(connectionId => {
      console.log('✅ SignalR reconnected:', connectionId);
      if (this.currentMatchId) {
        this.joinMatchRoom(this.currentMatchId);
      }
    });
  }

  async joinMatchRoom(matchId: string): Promise<void> {
    this.currentMatchId = matchId;
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      try {
        await this.hubConnection.invoke('JoinMatchRoom', matchId);
        console.log(`✅ Joined match room: ${matchId}`);
        this.loadMessagesForMatch(matchId);
      } catch (err) {
        console.error('❌ Error joining match room:', err);
      }
    }
  }

  async leaveMatchRoom(matchId: string): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      try {
        await this.hubConnection.invoke('LeaveMatchRoom', matchId);
        console.log(`✅ Left match room: ${matchId}`);
        this.currentMatchId = '';
      } catch (err) {
        console.error('❌ Error leaving match room:', err);
      }
    }
  }

  async sendMessage(matchId: string, receiverId: string, content: string): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      try {
        await this.hubConnection.invoke('SendMessage', matchId, receiverId, content);
      } catch (err) {
        console.error('❌ Error sending message:', err);
      }
    } else {
      console.error('❌ SignalR not connected');
    }
  }

  async markAsRead(matchId: string, messageId: string): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      try {
        await this.hubConnection.invoke('MarkAsRead', matchId, messageId);
      } catch (err) {
        console.error('❌ Error marking message as read:', err);
      }
    }
  }

  async sendTyping(matchId: string, isTyping: boolean): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      try {
        await this.hubConnection.invoke('Typing', matchId, isTyping);
      } catch (err) {
        console.error('❌ Error sending typing indicator:', err);
      }
    }
  }

  // ========== REST API Calls ==========

  loadMessagesForMatch(matchId: string): void {
    this.http.get<Message[]>(`${this.apiUrl}/match/${matchId}`).subscribe({
      next: (messages) => {
        console.log(`✅ Loaded ${messages.length} messages for match ${matchId}`);
        
        // Set isMine property for all messages
        const messagesWithMine = this.setMessageOwnership(messages);
        this.messagesSubject.next(messagesWithMine);
      },
      error: (err) => console.error('❌ Error loading messages:', err)
    });
  }

  loadMatches(): void {
    this.http.get<ChatMatch[]>(`${environment.apiUrl}/Matching/matches`).subscribe({
      next: (matches) => {
        console.log(`✅ Loaded ${matches.length} chat matches`);
        this.matchesSubject.next(matches);
      },
      error: (err) => console.error('❌ Error loading matches:', err)
    });
  }

  loadUnreadCount(): void {
    this.http.get<UnreadCount>(`${this.apiUrl}/unread/count`).subscribe({
      next: (data) => {
        this.unreadCountSubject.next(data.count);
      },
      error: (err) => console.error('❌ Error loading unread count:', err)
    });
  }

  disconnect(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
    }
  }

  clearMessages(): void {
    this.messagesSubject.next([]);
  }

  getCurrentUserId(): string | null {
    return this.currentUserId;
  }
}