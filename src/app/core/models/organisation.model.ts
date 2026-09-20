export interface Site {
  id: string;
  name: string;
  address: string | null;
}

export interface SiteSpend {
  siteId: string;
  siteName: string;
  totalSpend: number;
  completedRequests: number;
}

export interface SpendReport {
  fromUtc: string;
  /** Exclusive — the server treats the range as half-open. */
  toUtc: string;
  grandTotal: number;
  sites: SiteSpend[];
}
