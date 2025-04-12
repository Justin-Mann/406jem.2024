import { NgIf } from '@angular/common';
import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, NgIf],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css'
})
export class HeaderComponent {
 menuValue : boolean = false;
 menu_icon : string ='bi bi-list';

 openMenu(){
    this.menuValue =! this.menuValue ;
    this.menu_icon = this.menuValue ? 'bi bi-x' : 'bi bi-list';
  }
 closeMenu() {
    this.menuValue = false;
    this.menu_icon = 'bi bi-list';
  }
  isResumePage(): boolean {
    return window.location.pathname.endsWith('/digitalresume');
  }
}
