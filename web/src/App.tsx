import { Route, Routes } from 'react-router'
import { RequireAuth } from './auth/RequireAuth'
import { Topbar } from './components/Topbar'
import { Home } from './pages/Home'
import { Me } from './pages/Me'

export default function App() {
  return (
    <div className="min-h-svh bg-stone-100 text-stone-900">
      <Topbar />
      <Routes>
        <Route path="/" element={<Home />} />
        <Route
          path="/me"
          element={
            <RequireAuth>
              <Me />
            </RequireAuth>
          }
        />
      </Routes>
    </div>
  )
}
