import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

export interface MatchUser {
  id: string;
  fullName: string;
  headline: string;
  avatarUrl?: string;
}

export interface Match {
  matchId: string;
  user: MatchUser;
  matchedAt: Date;
  matchScore: number;
}

export interface LikeResponse {
  isMatch: boolean;
  matchId?: string;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class MatchingService {
  private apiUrl = `${environment.apiUrl}/Matching`;

  constructor(private http: HttpClient) {
    console.log('🔧 MATCHING SERVICE INITIALIZED');
  }

  getPotentialMatches(): Observable<any[]> {
    console.log('📡 Fetching potential matches');
    return this.http.get<any[]>(`${this.apiUrl}/potential`).pipe(
      tap({
        next: (data) => console.log('✅ Potential matches received:', data?.length || 0),
        error: (err) => console.error('❌ Error fetching potential matches:', err)
      })
    );
  }

  likeUser(userId: string): Observable<LikeResponse> {
    console.log('📡 Liking user:', userId);
    return this.http.post<LikeResponse>(`${this.apiUrl}/like/${userId}`, {}).pipe(
      tap({
        next: (response) => console.log('✅ Like response:', response),
        error: (err) => console.error('❌ Error liking user:', err)
      })
    );
  }

  passUser(userId: string): Observable<any> {
    console.log('📡 Passing user:', userId);
    return this.http.post<any>(`${this.apiUrl}/pass/${userId}`, {}).pipe(
      tap({
        next: () => console.log('✅ Pass successful'),
        error: (err) => console.error('❌ Error passing user:', err)
      })
    );
  }

  getMatches(): Observable<Match[]> {
    console.log('📡 Fetching accepted matches');
    return this.http.get<Match[]>(`${this.apiUrl}/matches`).pipe(
      tap({
        next: (matches) => console.log('✅ Accepted matches received:', matches?.length || 0, matches),
        error: (err) => console.error('❌ Error fetching matches:', err)
      })
    );
  }
}