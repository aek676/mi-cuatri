interface ImportMetaEnv {
  readonly INTERNAL_API_BASE_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}

declare namespace App {
  interface Locals {
    sessionToken: string | null;
    user?: import('./lib/api').UserDetailDto | null;
  }
}
