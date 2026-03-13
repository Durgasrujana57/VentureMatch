import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';

import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { MatchesListComponent } from './features/matches/matches-list/matches-list.component';
import { ProfileViewComponent } from './features/profile/profile-view/profile-view.component';
import { ProfileEditComponent } from './features/profile/profile-edit/profile-edit.component';
import { ChatListComponent } from './features/messages/chat-list/chat-list.component';
import { ChatRoomComponent } from './features/messages/chat-room/chat-room.component';
import { IdeaListComponent } from './features/ideas/idea-list/idea-list.component';
import { IdeaDetailComponent } from './features/ideas/idea-detail/idea-detail.component';
import { IdeaFormComponent } from './features/ideas/idea-form/idea-form.component';

const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'dashboard', component: DashboardComponent, canActivate: [AuthGuard] },
  { path: 'matches', component: MatchesListComponent, canActivate: [AuthGuard] },
  { path: 'profile/:id', component: ProfileViewComponent, canActivate: [AuthGuard] },
  { path: 'profile/edit/:id', component: ProfileEditComponent, canActivate: [AuthGuard] },
  { path: 'messages', component: ChatListComponent, canActivate: [AuthGuard] },
  { path: 'messages/:matchId', component: ChatRoomComponent, canActivate: [AuthGuard] },
  { path: 'ideas', component: IdeaListComponent },
  { path: 'ideas/new', component: IdeaFormComponent, canActivate: [AuthGuard] },
  { path: 'ideas/edit/:id', component: IdeaFormComponent, canActivate: [AuthGuard] },
  { path: 'ideas/:id', component: IdeaDetailComponent },
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: '**', redirectTo: '/dashboard' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }