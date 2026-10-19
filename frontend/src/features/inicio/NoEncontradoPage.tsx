import { Link } from 'react-router'
import { useT } from '../../shared/i18n/traductor'

export function NoEncontradoPage() {
  const t = useT()
  return (
    <section className="space-y-3">
      <h1 className="text-2xl font-semibold">{t('noEncontrado.titulo')}</h1>
      <Link to="/" className="text-marca underline underline-offset-2 hover:text-marca-oscura">
        {t('noEncontrado.volver')}
      </Link>
    </section>
  )
}
