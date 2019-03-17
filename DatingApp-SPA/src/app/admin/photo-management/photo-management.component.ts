import { Component, OnInit } from '@angular/core';
import { AdminService } from 'src/app/_services/admin.service';
import { Photo } from 'src/app/_models/photo';
import { AlertifyService } from 'src/app/_services/alertify.service';

@Component({
  selector: 'app-photo-management',
  templateUrl: './photo-management.component.html',
  styleUrls: ['./photo-management.component.css']
})

export class PhotoManagementComponent implements OnInit {
  photos: Photo[];

  constructor(private adminService: AdminService, private alertify: AlertifyService) { }

  ngOnInit() {
    this.getPhotosForModeration();
  }

  getPhotosForModeration() {
    this.adminService.getPhotosForModeration().subscribe(data => {
      this.photos = data;
    }, error => {
      console.log(error);
      this.alertify.error(error);
    });
  }

  approve(id: number) {
    this.adminService.approvePhoto(id).subscribe(() => {
      this.photos.splice(this.photos.findIndex(p => p.id === id), 1);
      this.alertify.success('Photo approved');
    }, error => {
      this.alertify.error(error);
    });
  }

  reject(userId: number, id: number) {
    this.adminService.rejectPhoto(userId, id).subscribe(() => {
      this.photos.splice(this.photos.findIndex(p => p.id === id), 1);
      this.alertify.success('Photo rejected');
    }, error => {
      this.alertify.error(error);
    });
  }

}
