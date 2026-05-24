export const authConfig = {
  domain: import.meta.env.VITE_AUTH0_DOMAIN ?? '',
  clientId: import.meta.env.VITE_AUTH0_CLIENT_ID ?? '',
  audience: import.meta.env.VITE_AUTH0_AUDIENCE ?? '',
}

export const isAuthConfigured =
  authConfig.domain.length > 0 && authConfig.clientId.length > 0 && authConfig.audience.length > 0
