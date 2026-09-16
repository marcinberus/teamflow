import { TestBed } from '@angular/core/testing';
import { TokenStorage } from './token-storage';

describe('TokenStorage', () => {
  let storage: TokenStorage;

  beforeEach(() => {
    window.localStorage.clear();
    TestBed.configureTestingModule({});
    storage = TestBed.inject(TokenStorage);
  });

  afterEach(() => window.localStorage.clear());

  it('stores, reads, and clears the access token', () => {
    expect(storage.get()).toBeNull();

    storage.set('test-token');
    expect(storage.get()).toBe('test-token');

    storage.clear();
    expect(storage.get()).toBeNull();
  });
});
