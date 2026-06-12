import { Link } from 'react-router-dom'
import { AlertTriangle, ArrowLeft } from 'lucide-react'

type NotFoundStateProps = {
  title: string
  message: string
  backTo: string
  backLabel: string
  tone?: 'warning' | 'danger'
}

export function NotFoundState({ title, message, backTo, backLabel, tone = 'warning' }: NotFoundStateProps) {
  return (
    <div className="text-center py-12">
      <AlertTriangle className={`h-12 w-12 mx-auto ${tone === 'danger' ? 'text-red-500' : 'text-amber-500'}`} />
      <h2 className="text-lg font-bold text-slate-700 mt-4">{title}</h2>
      <p className="text-slate-400 mt-2">{message}</p>
      <Link to={backTo} className="mt-6 inline-flex items-center text-indigo-500 font-semibold hover:underline">
        <ArrowLeft className="h-4 w-4 mr-1" /> {backLabel}
      </Link>
    </div>
  )
}
