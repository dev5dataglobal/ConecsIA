import { useState, type FormEvent } from 'react'
import { ApiError } from '../../shared/api/problemDetails'
import { useT } from '../../shared/i18n/traductor'
import { Dialogo } from '../../shared/ui/Dialogo'
import { ProblemDetailsAlert } from '../../shared/ui/ProblemDetailsAlert'
import { useGuardarOrganizacion, useTiposOrganizacion, type Organizacion } from './apiSaas'

/** Alta y edición de organizaciones desde la consola SaaS (HU-009). El servidor valida todo. */
export function FormularioOrganizacion({
  organizacion,
  onCerrar,
}: {
  organizacion: Organizacion | null
  onCerrar: () => void
}) {
  const t = useT()
  const { data: tipos } = useTiposOrganizacion()
  const guardar = useGuardarOrganizacion(organizacion)
  const [datos, setDatos] = useState({
    nombre: organizacion?.nombre ?? '',
    tipo: organizacion?.tipo ?? 'otro',
    identificacionLegal: organizacion?.identificacionLegal ?? '',
    zonaHoraria: organizacion?.zonaHoraria ?? '',
    contactoAdministrativo: organizacion?.contactoAdministrativo ?? '',
  })
  const [administrador, setAdministrador] = useState({ correo: '', nombre: '' })

  function enviar(evento: FormEvent) {
    evento.preventDefault()
    guardar.mutate(organizacion ? datos : { ...datos, administrador }, { onSuccess: onCerrar })
  }

  const campo = 'w-full rounded-lg border border-linea bg-fondo px-3 py-2 text-sm'
  const texto = (
    id: keyof typeof datos,
    clave: Parameters<typeof t>[0],
    opciones: { requerido?: boolean; max?: number } = {},
  ) => (
    <div className="space-y-1">
      <label htmlFor={`org-${id}`} className="text-sm font-medium">
        {t(clave)}
      </label>
      <input
        id={`org-${id}`}
        value={datos[id]}
        onChange={(e) => setDatos({ ...datos, [id]: e.target.value })}
        required={opciones.requerido}
        maxLength={opciones.max ?? 200}
        className={campo}
      />
    </div>
  )

  return (
    <Dialogo
      titulo={organizacion ? t('saas.form.editar') : t('saas.form.nueva')}
      onCerrar={onCerrar}
    >
      <form onSubmit={enviar} className="space-y-4">
        {guardar.error instanceof ApiError && (
          <ProblemDetailsAlert problem={guardar.error.problem} />
        )}
        {texto('nombre', 'saas.form.nombre', { requerido: true })}
        <div className="space-y-1">
          <label htmlFor="org-tipo" className="text-sm font-medium">
            {t('saas.form.tipo')}
          </label>
          <select
            id="org-tipo"
            value={datos.tipo}
            onChange={(e) => setDatos({ ...datos, tipo: e.target.value })}
            className={campo}
          >
            {tipos?.map((tipo) => (
              <option key={tipo.clave} value={tipo.clave}>
                {tipo.nombre}
              </option>
            ))}
          </select>
        </div>
        {texto('identificacionLegal', 'saas.form.identificacion', { max: 50 })}
        {texto('zonaHoraria', 'saas.form.zonaHoraria', { max: 64 })}
        {texto('contactoAdministrativo', 'saas.form.contacto')}

        {!organizacion && (
          <fieldset className="space-y-3 rounded-lg border border-linea p-3">
            <legend className="px-1 text-sm font-medium">{t('saas.form.administrador')}</legend>
            <div className="space-y-1">
              <label htmlFor="org-admin-correo" className="text-sm font-medium">
                {t('saas.form.adminCorreo')}
              </label>
              <input
                id="org-admin-correo"
                type="email"
                required
                value={administrador.correo}
                onChange={(e) => setAdministrador({ ...administrador, correo: e.target.value })}
                className={campo}
              />
            </div>
            <div className="space-y-1">
              <label htmlFor="org-admin-nombre" className="text-sm font-medium">
                {t('saas.form.adminNombre')}
              </label>
              <input
                id="org-admin-nombre"
                required
                value={administrador.nombre}
                onChange={(e) => setAdministrador({ ...administrador, nombre: e.target.value })}
                className={campo}
              />
            </div>
          </fieldset>
        )}

        <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <button
            type="button"
            onClick={onCerrar}
            className="rounded-lg border border-linea px-4 py-2 text-sm hover:bg-superficie"
          >
            {t('saas.form.cancelar')}
          </button>
          <button
            type="submit"
            disabled={guardar.isPending}
            className="rounded-lg bg-marca px-4 py-2 text-sm font-medium text-white hover:bg-marca-oscura disabled:opacity-60"
          >
            {t('saas.form.guardar')}
          </button>
        </div>
      </form>
    </Dialogo>
  )
}
