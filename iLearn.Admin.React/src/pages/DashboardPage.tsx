import { Activity, Database, ShieldCheck } from 'lucide-react'
import { PageHeader } from '../components/ui/PageHeader'
import { SidePanel } from '../components/ui/SidePanel'
import { StatusText } from '../components/ui/StatusText'
import { Toolbar } from '../components/ui/Toolbar'
import { appConfig } from '../config/appConfig'

const metrics = [
  { label: 'Admin API', value: 'Ready', note: 'Existing endpoints remain the source of truth.' },
  { label: 'Auth Mode', value: 'Windows', note: 'Requests include credentials for Negotiate.' },
  { label: 'Deploy Mode', value: 'Side by side', note: 'No link to the MVC Admin project.' },
]

export function DashboardPage() {
  return (
    <>
      <PageHeader
        title="Admin Dashboard"
        eyebrow="iLearn React Console"
        description="Operational shell for the new React Admin experience, backed by the existing iLearn API contracts."
        actions={
          <Toolbar align="end">
            <StatusText tone="success">Isolated SPA</StatusText>
          </Toolbar>
        }
      />

      <section className="admin-dashboard-grid" aria-label="React console status">
        {metrics.map((metric) => (
          <article className="admin-card" key={metric.label}>
            <h2 className="admin-card-title">{metric.label}</h2>
            <div className="admin-metric-value">{metric.value}</div>
            <p className="admin-card-note">{metric.note}</p>
          </article>
        ))}
      </section>

      <section className="admin-dashboard-stack">
        <article className="admin-card">
          <h2 className="admin-card-title">Runtime Contract</h2>
          <dl className="admin-meta-list mt-4">
            <div className="admin-meta-row">
              <dt>App base path</dt>
              <dd>{appConfig.appBasePath}</dd>
            </div>
            <div className="admin-meta-row">
              <dt>API base URL</dt>
              <dd>{appConfig.apiBaseUrl}</dd>
            </div>
            <div className="admin-meta-row">
              <dt>SignalR base URL</dt>
              <dd>{appConfig.signalRBaseUrl}</dd>
            </div>
          </dl>
        </article>

        <SidePanel title="Migration Guardrails" note="Contracts preserved during side-by-side rollout.">
          <dl className="admin-meta-list">
            <div className="admin-meta-row">
              <dt>
                <ShieldCheck size={16} aria-hidden="true" /> Security
              </dt>
              <dd>API authorization and division isolation stay server-side.</dd>
            </div>
            <div className="admin-meta-row">
              <dt>
                <Database size={16} aria-hidden="true" /> Data
              </dt>
              <dd>DevExtreme stores use existing DataSourceLoadOptions endpoints.</dd>
            </div>
            <div className="admin-meta-row">
              <dt>
                <Activity size={16} aria-hidden="true" /> Rollout
              </dt>
              <dd>MVC Admin remains untouched while React pages are migrated module by module.</dd>
            </div>
          </dl>
        </SidePanel>
      </section>
    </>
  )
}