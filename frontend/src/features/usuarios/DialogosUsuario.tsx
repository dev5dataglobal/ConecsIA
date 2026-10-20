import { useState, type FormEvent } from 'react'
import { ApiError } from '../../shared/api/problemDetails'
import { useT } from '../../shared/i18n/traductor'
import { Dialogo } from '../../shared/ui/Dialogo'
import { ProblemDetailsAlert } from '../../shared/ui/ProblemDetailsAlert'
import {
  useAsignarAlcance,
  useAsignarRoles,
  useInvitar,
  type UsuarioOrganizacion,
} from './apiUsuarios'
import { SelectorAlcance, SelectorRoles } from './SelectorAccesos'

const campo = 'w-full rounded-lg border border-linea bg-fondo px-3 py-2 text-sm'

function Botones({ onCerrar, ocupado }: { onCerrar: () => void; ocupado: boolean }) {
  const t = useT()
  return (
    <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
      <button
        type="button"
        onClick={onCerrar}
        className="rounded-lg border border-linea px-4 py-2 text-sm hover:bg-superficie"
      >
        {t('usuarios.form.cancelar')}
      </button>
      <button
        type="submit"
        disabled={ocupado}
        className="rounded-lg bg-marca px-4 py-2 text-sm font-medium text-white hover:bg-marca-oscura disabled:opacity-60"
      >
        {t('usuarios.form.guardar')}
      </button>
    </div>
  )
}

/** HU-016: invitar con roles y alcance inicial. En desarrollo muestra el enlace de aceptación. */
export function DialogoInvitar({ onCerrar }: { onCerrar: () => void }) {
  const t = useT()
  const invitar = useInvitar()
  const [correo, setCorreo] = useState('')
  const [nombre, setNombre] = useState('')
  const [roles, setRoles] = useState<Set<string>>(new Set())
  const [total, setTotal] = useState(false)
  const [sedes, setSedes] = useState<Set<string>>(new Set())

  function enviar(evento: FormEvent) {
    evento.preventDefault()
    invitar.mutate({
      correo,
      nombre,
      roles: [...roles],
      alcanceTotalSedes: total,
      sedes: [...sedes],
    })
  }

  if (invitar.data) {
    return (
      <Dialogo titulo={t('usuarios.invitar.titulo')} onCerrar={onCerrar}>
        <p className="text-sm">{t('usuarios.invitar.enviada', { correo })}</p>
        {invitar.data.enlaceAceptacion && (
          <p className="break-all rounded-lg bg-superficie p-3 font-mono text-xs">
            {t('usuarios.invitar.enlaceDesarrollo')} {invitar.data.enlaceAceptacion}
          </p>
        )}
        <div className="flex justify-end">
          <button
            type="button"
            onClick={onCerrar}
            className="rounded-lg bg-marca px-4 py-2 text-sm font-medium text-white"
          >
            {t('usuarios.form.cerrar')}
          </button>
        </div>
      </Dialogo>
    )
  }

  return (
    <Dialogo titulo={t('usuarios.invitar.titulo')} onCerrar={onCerrar}>
      <form onSubmit={enviar} className="space-y-4">
        {invitar.error instanceof ApiError && (
          <ProblemDetailsAlert problem={invitar.error.problem} />
        )}
        <div className="space-y-1">
          <label htmlFor="inv-correo" className="text-sm font-medium">
            {t('usuarios.form.correo')}
          </label>
          <input
            id="inv-correo"
            type="email"
            required
            value={correo}
            onChange={(e) => setCorreo(e.target.value)}
            className={campo}
          />
        </div>
        <div className="space-y-1">
          <label htmlFor="inv-nombre" className="text-sm font-medium">
            {t('usuarios.form.nombre')}
          </label>
          <input
            id="inv-nombre"
            required
            value={nombre}
            onChange={(e) => setNombre(e.target.value)}
            className={campo}
          />
        </div>
        <SelectorRoles elegidos={roles} onCambiar={setRoles} />
        <SelectorAlcance
          total={total}
          sedes={sedes}
          onCambiar={(t2, s) => {
            setTotal(t2)
            setSedes(s)
          }}
        />
        <Botones onCerrar={onCerrar} ocupado={invitar.isPending} />
      </form>
    </Dialogo>
  )
}

/** HU-016 y HU-018: roles y alcance de un usuario existente. */
export function DialogoAccesos({
  usuario,
  onCerrar,
}: {
  usuario: UsuarioOrganizacion
  onCerrar: () => void
}) {
  const t = useT()
  const asignarRoles = useAsignarRoles(usuario.usuarioId)
  const asignarAlcance = useAsignarAlcance(usuario.usuarioId)
  const [roles, setRoles] = useState(new Set(usuario.roles))
  const [total, setTotal] = useState(usuario.alcanceTotalSedes)
  const [sedes, setSedes] = useState(new Set(usuario.sedes))
  const error = asignarRoles.error ?? asignarAlcance.error

  async function enviar(evento: FormEvent) {
    evento.preventDefault()
    try {
      await asignarRoles.mutateAsync([...roles])
      await asignarAlcance.mutateAsync({ alcanceTotalSedes: total, sedes: [...sedes] })
      onCerrar()
    } catch {
      // El error se muestra con ProblemDetailsAlert.
    }
  }

  return (
    <Dialogo titulo={t('usuarios.accesos.titulo', { nombre: usuario.nombre })} onCerrar={onCerrar}>
      <form onSubmit={(e) => void enviar(e)} className="space-y-4">
        {error instanceof ApiError && <ProblemDetailsAlert problem={error.problem} />}
        <SelectorRoles elegidos={roles} onCambiar={setRoles} />
        <SelectorAlcance
          total={total}
          sedes={sedes}
          onCambiar={(t2, s) => {
            setTotal(t2)
            setSedes(s)
          }}
        />
        <Botones onCerrar={onCerrar} ocupado={asignarRoles.isPending || asignarAlcance.isPending} />
      </form>
    </Dialogo>
  )
}
