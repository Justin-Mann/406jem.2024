import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { DigitalResumeComponent } from './digital-resume/digital-resume.component';
import { ProjectsComponent } from './projects/projects.component';

export const routes: Routes = [
    { path: 'home', component: HomeComponent },
    { path: 'digitalresume', component: DigitalResumeComponent },
    { path: 'projects', component: ProjectsComponent },
    { path: 'login', loadComponent: () => import('./auth/login/login.component').then(m => m.LoginComponent) },
    { path: 'register', loadComponent: () => import('./auth/register/register.component').then(m => m.RegisterComponent) },
    { path: 'testimonials', loadComponent: () => import('./testimonials/testimonials.component').then(m => m.TestimonialsComponent) },
    { path: 'resume-posters', loadComponent: () => import('./resume-posters/resume-posters.component').then(m => m.ResumePostersComponent) },
    { path: 'admin/resumes', loadComponent: () => import('./manage-resumes/manage-resumes.component').then(m => m.ManageResumesComponent) },
    { path: '', redirectTo: '/home', pathMatch: 'full' },
];
