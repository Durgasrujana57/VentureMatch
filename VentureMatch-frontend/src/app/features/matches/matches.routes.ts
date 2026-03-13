import { Routes } from '@angular/router';
import { MatchesListComponent } from './matches-list/matches-list.component';
import { MatchDetailsComponent } from './match-details/match-details.component';

export const MATCHES_ROUTES: Routes = [
  { path: '', component: MatchesListComponent },
  { path: ':id', component: MatchDetailsComponent }
];