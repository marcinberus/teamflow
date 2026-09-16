import { DOCUMENT } from '@angular/common';
import { inject, Injectable } from '@angular/core';

const TOKEN_KEY = 'teamflow.accessToken';

@Injectable({ providedIn: 'root' })
export class TokenStorage {
  private readonly document = inject(DOCUMENT);

  get(): string | null {
    return this.storage?.getItem(TOKEN_KEY) ?? null;
  }

  set(token: string): void {
    this.storage?.setItem(TOKEN_KEY, token);
  }

  clear(): void {
    this.storage?.removeItem(TOKEN_KEY);
  }

  private get storage(): Storage | null {
    try {
      return this.document.defaultView?.localStorage ?? null;
    } catch {
      return null;
    }
  }
}
