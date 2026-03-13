import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface RoleNeeded {
  role: string;
  skills: string[];
  commitment: string;
}

export interface Idea {
  id: string;
  userId: string;
  title: string;
  description: string;
  problemStatement: string;
  solution: string;
  industry: string;
  stage: string;
  rolesNeeded: RoleNeeded[];
  lookingForSkills: string[];
  views: number;
  applications: number;
  createdAt: Date;
  user?: {
    id: string;
    fullName: string;
    headline: string;
    avatarUrl?: string;
  };
}

export interface CreateIdeaRequest {
  title: string;
  description: string;
  problemStatement?: string;
  solution?: string;
  industry?: string;
  stage?: string;
  rolesNeeded?: RoleNeeded[];
  lookingForSkills?: string[];
}

export interface ApplyToIdeaRequest {
  ideaId: string;
  message: string;
}

export interface Application {
  id: string;
  ideaId: string;
  userId: string;
  message: string;
  status: string;
  appliedAt: Date;
  user?: {
    id: string;
    fullName: string;
    headline: string;
    avatarUrl?: string;
  };
}

@Injectable({
  providedIn: 'root'
})
export class IdeaService {
  private apiUrl = `${environment.apiUrl}/Ideas`;

  constructor(private http: HttpClient) {
    console.log('🔧 IDEA SERVICE INITIALIZED');
  }

  // ========== IDEA METHODS ==========

  getAllIdeas(industry?: string, stage?: string, search?: string): Observable<Idea[]> {
    let url = this.apiUrl;
    const params: string[] = [];
    
    if (industry) params.push(`industry=${encodeURIComponent(industry)}`);
    if (stage) params.push(`stage=${encodeURIComponent(stage)}`);
    if (search) params.push(`search=${encodeURIComponent(search)}`);
    
    if (params.length > 0) {
      url += '?' + params.join('&');
    }
    
    console.log('📡 Fetching ideas with filters:', { industry, stage, search });
    return this.http.get<Idea[]>(url);
  }

  getIdeaById(id: string): Observable<Idea> {
    console.log('📡 Fetching idea by ID:', id);
    return this.http.get<Idea>(`${this.apiUrl}/${id}`);
  }

  getUserIdeas(userId: string): Observable<Idea[]> {
    console.log('📡 Fetching ideas for user:', userId);
    return this.http.get<Idea[]>(`${this.apiUrl}/user/${userId}`);
  }

  createIdea(idea: CreateIdeaRequest): Observable<any> {
    console.log('📡 Creating new idea:', idea.title);
    return this.http.post(this.apiUrl, idea);
  }

  updateIdea(id: string, idea: Partial<CreateIdeaRequest>): Observable<any> {
    console.log('📡 Updating idea:', id);
    return this.http.put(`${this.apiUrl}/${id}`, idea);
  }

  deleteIdea(id: string): Observable<any> {
    console.log('📡 Deleting idea:', id);
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  // ========== APPLICATION METHODS ==========

  applyToIdea(application: ApplyToIdeaRequest): Observable<any> {
    console.log('📡 Applying to idea:', application.ideaId);
    return this.http.post(`${this.apiUrl}/apply`, application);
  }

  getIdeaApplications(ideaId: string): Observable<Application[]> {
    console.log('📡 Fetching applications for idea:', ideaId);
    return this.http.get<Application[]>(`${this.apiUrl}/${ideaId}/applications`);
  }

  // Get applications by user ID
  getUserApplications(userId: string): Observable<Application[]> {
    console.log('📡 Fetching applications for user:', userId);
    return this.http.get<Application[]>(`${this.apiUrl}/applications/user/${userId}`);
  }

  // Check user's own application status for an idea
  getMyApplication(ideaId: string): Observable<Application | null> {
    console.log('📡 Checking my application for idea:', ideaId);
    return this.http.get<Application | null>(`${this.apiUrl}/${ideaId}/my-application`);
  }

  // ✅ FIXED: Update application status - sends as object, not raw string
  updateApplicationStatus(applicationId: string, status: string): Observable<any> {
    console.log('📡 Updating application status:', applicationId, status);
    
    // IMPORTANT: Send as an object with a status property
    const body = { status: status };
    
    return this.http.put(`${this.apiUrl}/applications/${applicationId}`, body, {
      headers: { 'Content-Type': 'application/json' }
    });
  }
}