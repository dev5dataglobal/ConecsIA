import { useT } from '../../shared/i18n/traductor'
import { useRoles } from '../roles/apiRoles'
import { useSedes } from '../sedes/apiSedes'

/** Casillas de roles activos de la organización (HU-016). El servidor aplica el anti-escalamiento. */
export function SelectorRoles({
  elegidos,
  onCambiar,
}: {
  elegidos: Set<string>
  onCambiar: (roles: Set<string>) => void
}) {
  const t = useT()
  const { data: roles } = useRoles()
  return (
    <fieldset className="space-y-2">
      <legend className="text-sm font-medium">{t('usuarios.form.roles')}</legend>
      {roles
        ?.filter((r) => r.estado === 'Activo')
        .map((rol) => (
          <label key={rol.id} className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={elegidos.has(rol.id)}
              onChange={() => {
                const nuevos = new Set(elegidos)
                if (nuevos.has(rol.id)) nuevos.delete(rol.id)
                else nuevos.add(rol.id)
                onCambiar(nuevos)
              }}
            />
            {rol.nombre}
          </label>
        ))}
    </fieldset>
  )
}

/**
 * Alcance por sede (HU-018): toda la organización o sedes concretas. Vacío = sin acceso
 * territorial. El servidor exige que quien asigna tenga ese alcance.
 */
export function SelectorAlcance({
  total,
  sedes,
  onCambiar,
}: {
  total: boolean
  sedes: Set<string>
  onCambiar: (total: boolean, sedes: Set<string>) => void
}) {
  const t = useT()
  const { data } = useSedes('Activa', 1)
  return (
    <fieldset className="space-y-2">
      <legend className="text-sm font-medium">{t('usuarios.form.alcance')}</legend>
      <label className="flex items-center gap-2 text-sm">
        <input type="checkbox" checked={total} onChange={() => onCambiar(!total, sedes)} />
        {t('usuarios.form.alcanceTotal')}
      </label>
      {!total &&
        data?.elementos.map((sede) => (
          <label key={sede.id} className="flex items-center gap-2 pl-4 text-sm">
            <input
              type="checkbox"
              checked={sedes.has(sede.id)}
              onChange={() => {
                const nuevas = new Set(sedes)
                if (nuevas.has(sede.id)) nuevas.delete(sede.id)
                else nuevas.add(sede.id)
                onCambiar(total, nuevas)
              }}
            />
            {sede.nombre}
          </label>
        ))}
      {!total && sedes.size === 0 && (
        <p className="text-xs text-tinta-suave">{t('usuarios.form.alcanceVacio')}</p>
      )}
    </fieldset>
  )
}
