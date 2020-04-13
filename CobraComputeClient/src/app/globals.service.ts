import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class GlobalsService {
  private token : string = '';

  constructor() { }

  getToken (): string {
    return this.token;
  }

  setToken (_token) {
    this.token = _token;
  }

}
