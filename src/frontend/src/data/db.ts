import Dexie, { Table } from 'dexie';

export interface MetaKV { key: string; value: any }
export interface DatasetRow { key: string; category: string; hash: string; data: any[]; ts: number }
export interface IndexRow { key: string; category: string; hash: string; index: any; ts: number }

export class BfDb extends Dexie {
  meta!: Table<MetaKV, string>;
  datasets!: Table<DatasetRow, string>;
  indexes!: Table<IndexRow, string>;

  constructor() {
    super('bfdb');
    this.version(1).stores({
      meta: '&key',
      datasets: '&key, category, hash',
      indexes: '&key, category, hash'
    });
  }
}

export const db = new BfDb();

