import { Route, Routes } from 'react-router'
import { RequireAdmin } from './auth/RequireAdmin'
import { RequireAuth } from './auth/RequireAuth'
import { LiveMatchBanner } from './components/LiveMatchBanner'
import { Topbar } from './components/Topbar'
import { Home } from './pages/Home'
import { Leaderboard } from './pages/Leaderboard'
import { LongTips } from './pages/LongTips'
import { Matches } from './pages/Matches'
import { Me } from './pages/Me'
import { NotFound } from './pages/NotFound'
import { Team } from './pages/Team'
import { TeamJoin } from './pages/TeamJoin'
import { TipSubmit } from './pages/TipSubmit'
import { UserTips } from './pages/UserTips'
import { AdminAiAvatar } from './pages/admin/AdminAiAvatar'
import { AdminAudit } from './pages/admin/AdminAudit'
import { AdminLongTips } from './pages/admin/AdminLongTips'
import { AdminMatchEditor } from './pages/admin/AdminMatchEditor'
import { AdminMatches } from './pages/admin/AdminMatches'

export default function App() {
  return (
    <div className="min-h-svh bg-stone-100 text-stone-900">
      <Topbar />
      <LiveMatchBanner />
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
          path="/leaderboard"
          element={
            <RequireAuth>
              <Leaderboard />
            </RequireAuth>
          }
        />
        <Route
          path="/leaderboard/user/:userId"
          element={
            <RequireAuth>
              <UserTips />
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
        <Route
          path="/admin"
          element={
            <RequireAuth>
              <RequireAdmin>
                <AdminMatches />
              </RequireAdmin>
            </RequireAuth>
          }
        />
        <Route
          path="/admin/matches/:matchId"
          element={
            <RequireAuth>
              <RequireAdmin>
                <AdminMatchEditor />
              </RequireAdmin>
            </RequireAuth>
          }
        />
        <Route
          path="/admin/audit"
          element={
            <RequireAuth>
              <RequireAdmin>
                <AdminAudit />
              </RequireAdmin>
            </RequireAuth>
          }
        />
        <Route
          path="/admin/long-tips"
          element={
            <RequireAuth>
              <RequireAdmin>
                <AdminLongTips />
              </RequireAdmin>
            </RequireAuth>
          }
        />
        <Route
          path="/admin/ai-avatar"
          element={
            <RequireAuth>
              <RequireAdmin>
                <AdminAiAvatar />
              </RequireAdmin>
            </RequireAuth>
          }
        />
        <Route path="*" element={<NotFound />} />
      </Routes>
    </div>
  )
}
