import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { DigitalResumeComponent } from './digital-resume/digital-resume.component';
import { ProjectsComponent } from './projects/projects.component';

export const routes: Routes = [
    { path: 'home', component: HomeComponent },
    { path: 'digitalresume', component: DigitalResumeComponent },
    { path: 'projects', component: ProjectsComponent },
    { path: '', redirectTo: '/home', pathMatch: 'full' },
];
