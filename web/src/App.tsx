import { Route, Routes } from 'react-router'
import { RequireAuth } from './auth/RequireAuth'
import { Topbar } from './components/Topbar'
import { Home } from './pages/Home'
import { LongTips } from './pages/LongTips'
import { Matches } from './pages/Matches'
import { Me } from './pages/Me'
import { Team } from './pages/Team'
import { TeamJoin } from './pages/TeamJoin'
import { TipSubmit } from './pages/TipSubmit'

export default function App() {
  return (
    <div className="min-h-svh bg-stone-100 text-stone-900">
      <Topbar />
      <Routes>
        <Route path="/" element={<Home />} />
        <Route
          path="/matches"
          element={
            <RequireAuth>
              <Matches />
            </RequireAuth>
          }
        />
        <Route
          path="/matches/:matchId/tip"
          element={
            <RequireAuth>
              <TipSubmit />
            </RequireAuth>
          }
        />
        <Route
          path="/long-tips"
          element={
            <RequireAuth>
              <LongTips />
            </RequireAuth>
          }
        />
        <Route
          path="/team"
          element={
            <RequireAuth>
              <Team />
            </RequireAuth>
          }
        />
        <Route
          path="/team/join/:token"
          element={
            <RequireAuth>
              <TeamJoin />
            </RequireAuth>
          }
        />
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
