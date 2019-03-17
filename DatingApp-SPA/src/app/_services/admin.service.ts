import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { HttpClient } from '@angular/common/http';
import { User } from '../_models/user';
import { Observable } from 'rxjs';
import { Photo } from '../_models/photo';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  getUsersWithRoles() {
    return this.http.get(this.baseUrl + 'admin/usersWithRoles');
  }

  updateUserRoles(user: User, roles: {}) {
    return this.http.post(this.baseUrl + 'admin/editRoles/' + user.userName, roles);
  }

  getPhotosForModeration(): Observable<Photo[]> {
    return this.http.get<Photo[]>(this.baseUrl + 'admin/photosForModeration');
  }

  approvePhoto(id: number) {
    return this.http.post(this.baseUrl + 'admin/approve/' + id, {});
  }

}
