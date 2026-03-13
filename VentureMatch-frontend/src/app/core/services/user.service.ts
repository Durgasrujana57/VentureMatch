import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface User {
  id: string;
  email: string;
  fullName: string;
  headline: string;
  bio: string;
  location: string;
  avatarUrl?: string;
  skills: string[];
  interests: string[];
  availability: string;
  lookingFor: string[];
  githubUrl?: string;
  linkedinUrl?: string;
  portfolioUrl?: string;
  createdAt: Date;
  updatedAt: Date;
}

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private apiUrl = `${environment.apiUrl}/Users`;

  constructor(private http: HttpClient) {}

  getUserById(id: string): Observable<User> {
    console.log('📡 Fetching user by ID:', id);
    return this.http.get<User>(`${this.apiUrl}/${id}`);
  }

  getProfile(): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/profile`);
  }

  updateProfile(data: any): Observable<User> {
    return this.http.put<User>(`${this.apiUrl}/profile`, data);
  }

  getAllUsers(): Observable<User[]> {
    return this.http.get<User[]>(this.apiUrl);
  }
}