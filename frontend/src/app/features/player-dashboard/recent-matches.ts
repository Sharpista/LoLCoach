import { DatePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { RecentMatch } from '../../core/models/dashboard';

/** Histórico de partidas recentes. */
@Component({
  selector: 'app-recent-matches',
  imports: [DatePipe],
  templateUrl: './recent-matches.html',
  styleUrl: './recent-matches.scss',
})
export class RecentMatches {
  readonly matches = input.required<RecentMatch[]>();
}
