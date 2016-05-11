import { OpaqueToken } from '@angular/core';
import * as ngUniversal from 'angular2-universal';
import * as aspnet from 'aspnet-prerendering';

export class Cookie implements ngUniversal.Cookie {
  constructor(private _cookies: aspnet.Cookie[]) {
  }

  set(key: string, value: string, attributes?: ngUniversal.CookieAttributes): void { }

  get(key?: string): string {
    return key ? this._cookies.find(t => t.key == key).value : this._cookies.map(t => `${t.key}=${t.value}`).join('; ');
  }

  remove(key: string, attributes?: ngUniversal.CookieAttributes): void { };
}