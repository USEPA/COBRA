import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class GlobalsService {
  private token : string = '';
  private mode : string = 'COBRA';
  private queue: {};

  constructor() { }

  getToken (): string {
    return this.token;
  }

  setToken (_token) {
    this.token = _token;
  }

  setMode (_mode) {
    this.mode = _mode;
  }

  getQeue(): {} {
    return this.queue;
  }

  setQueue (_queue) {
    this.queue = _queue;
  }

  getMode (): string {
    return this.mode;
  }



}
