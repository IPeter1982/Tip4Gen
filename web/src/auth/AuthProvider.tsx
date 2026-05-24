import { Auth0Provider } from '@auth0/auth0-react'
import type { ReactNode } from 'react'
import { authConfig, isAuthConfigured } from './authConfig'

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
        audience: authConfig.audience,
      }}
      cacheLocation="localstorage"
    >
      {children}
    </Auth0Provider>
  )
}
