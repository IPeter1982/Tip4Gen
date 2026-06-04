import { Auth0Provider, type AppState } from '@auth0/auth0-react'
import type { ReactNode } from 'react'
import { authConfig, isAuthConfigured } from './authConfig'

function onRedirectCallback(appState?: AppState) {
  const target = appState?.returnTo ?? window.location.pathname
  window.history.replaceState({}, document.title, target)
}

export function AuthProvider({ children }: { children: ReactNode }) {
  if (!isAuthConfigured) {
    return <>{children}</>
  }

  return (
    <Auth0Provider
      domain={authConfig.domain}
      clientId={authConfig.clientId}
      authorizationParams={{
        redirect_uri: window.location.origin,
        ...(authConfig.audience ? { audience: authConfig.audience } : {}),
      }}
      cacheLocation="localstorage"
      onRedirectCallback={onRedirectCallback}
    >
      {children}
    </Auth0Provider>
  )
}
