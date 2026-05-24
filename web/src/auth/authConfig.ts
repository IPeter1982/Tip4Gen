export const authConfig = {
  domain: import.meta.env.VITE_AUTH0_DOMAIN ?? '',
  clientId: import.meta.env.VITE_AUTH0_CLIENT_ID ?? '',
  audience: import.meta.env.VITE_AUTH0_AUDIENCE ?? '',
}

// Audience is optional: without it, the SPA can still log in (ID token),
// but the backend won't accept the access token until an Auth0 API
// (audience) is created and set on both sides.
export const isAuthConfigured = authConfig.domain.length > 0 && authConfig.clientId.length > 0
