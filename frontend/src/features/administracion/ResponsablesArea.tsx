import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Selector } from '../../shared/ui/Selector'
import { leerOrganizacionActiva } from '../sesion/sesion'
import {
  asignarResponsableArea,
  cambiarResponsableArea,
  consultarMiembrosOrganizacion,
  consultarResponsablesArea,
  retirarResponsableArea,
  type AreaAdmin,
  type ResponsableArea,
} from './apiSedes'
import { Campo, Dialogo, EstadoConsulta, Estatus, Interruptor } from './componentes'
import { mensajeError } from './utilidades'

const fecha = (iso: string) => new Date(iso).toLocaleString('es-CO', { dateStyle: 'medium', timeStyle: 'short' })

/**
 * Responsables y autorizadores de un destino (RF-04, HU-014). Con designados activos, solo ellos pueden ser anfitriones de
 * los accesos del destino. «Autorizador» queda registrado para la decisión D-04: hoy no concede por sí permiso de aprobar.
 * Retirar no borra: la designación queda en el historial y puede reactivarse. El servidor valida permisos y alcance.
 */
export function ResponsablesArea({ area, puedeEditar, onClose, onAviso }: { area: AreaAdmin; puedeEditar: boolean; onClose: () => void; onAviso: (m: string) => void }) {
  const queryClient = useQueryClient()
  const clave = ['admin-areas', 'responsables', leerOrganizacionActiva(), area.idArea]
  const responsables = useQuery({ queryKey: clave, queryFn: () => consultarResponsablesArea(area.idArea) })
  const miembros = useQuery({ queryKey: ['admin-areas', 'miembros', leerOrganizacionActiva()], queryFn: consultarMiembrosOrganizacion, enabled: puedeEditar, staleTime: 60_000 })
  const [idMembresia, setIdMembresia] = useState('')
  const [autorizador, setAutorizador] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const refrescar = () => {
    void queryClient.invalidateQueries({ queryKey: clave })
    // Las opciones de Control de accesos filtran anfitriones por estos responsables.
    void queryClient.invalidateQueries({ queryKey: ['accesos', 'opciones'] })
  }
  const asignar = useMutation({
    mutationFn: () => asignarResponsableArea(area.idArea, Number(idMembresia), autorizador),
    onSuccess: (r) => { refrescar(); setIdMembresia(''); setAutorizador(false); onAviso(`${r.usuario} designado en ${area.nombre}.`) },
  })
  const cambiar = useMutation({
    mutationFn: (r: ResponsableArea) => cambiarResponsableArea(area.idArea, r.idMembresia, !r.esAutorizador),
    onSuccess: (r) => { refrescar(); onAviso(r.esAutorizador ? `${r.usuario} marcado como autorizador.` : `${r.usuario} ya no es autorizador.`) },
    onError: refrescar,
  })
  const retirar = useMutation({
    mutationFn: (r: ResponsableArea) => retirarResponsableArea(area.idArea, r.idMembresia),
    onSuccess: (r) => { refrescar(); onAviso(`${r.usuario} retirado de ${area.nombre}. Su historial se conserva.`) },
    onError: refrescar,
  })
  const reactivar = useMutation({
    mutationFn: (r: ResponsableArea) => asignarResponsableArea(area.idArea, r.idMembresia, r.esAutorizador),
    onSuccess: (r) => { refrescar(); onAviso(`${r.usuario} designado nuevamente en ${area.nombre}.`) },
    onError: refrescar,
  })

  const activos = (responsables.data ?? []).filter((r) => r.activo)
  const retirados = (responsables.data ?? []).filter((r) => !r.activo)
  const yaActivos = new Set(activos.map((r) => r.idMembresia))
  const disponibles = (miembros.data ?? []).filter((m) => !yaActivos.has(m.idMembresia))
  const errorAccion = cambiar.error ?? retirar.error ?? reactivar.error
  const ocupado = cambiar.isPending || retirar.isPending || reactivar.isPending

  function enviar() {
    if (!idMembresia) { setError('Selecciona el usuario que vas a designar.'); return }
    setError(null)
    if (!asignar.isPending) asignar.mutate()
  }

  return (
    <Dialogo titulo={`Responsables de ${area.nombre}`} eyebrow="Sedes y áreas" icono="personas" ancho="amplio" onClose={onClose}
      onSubmit={(e) => e.preventDefault()} pie={<button type="button" className="ad-boton-sec" onClick={onClose}>Cerrar</button>}>
      <p className="ad-ayuda">
        {activos.length
          ? 'Solo los responsables activos pueden ser anfitriones de los accesos de este destino.'
          : 'Sin responsables designados, cualquier usuario activo puede ser anfitrión de este destino.'}
        {' '}La marca «Autorizador» queda registrada; aprobar accesos sigue dependiendo del perfil del usuario.
      </p>

      {puedeEditar && area.activo && (
        <fieldset className="ad-grupo">
          <legend>Designar responsable</legend>
          <div className="ad-rejilla">
            <Campo etiqueta="Usuario" error={error ?? undefined} ancho="completo">
              <Selector className="sel-campo" etiqueta="Usuario" valor={idMembresia} invalido={Boolean(error)} buscable placeholderBusqueda="Buscar usuario"
                deshabilitado={miembros.isLoading} onChange={(v) => { setIdMembresia(v); setError(null) }}
                opciones={[{ valor: '', texto: miembros.isLoading ? 'Cargando usuarios…' : disponibles.length ? 'Selecciona…' : 'No hay más usuarios activos', deshabilitada: true },
                  ...disponibles.map((m) => ({ valor: String(m.idMembresia), texto: m.usuario }))]} />
            </Campo>
          </div>
          <Interruptor etiqueta="También es autorizador" ayuda="Registra que puede decidir los accesos de este destino (pendiente de la política D-04)."
            checked={autorizador} onChange={setAutorizador} />
          {asignar.error && <p className="ad-mensaje" role="alert">{mensajeError(asignar.error)}</p>}
          <div className="pe-subacciones">
            <button type="button" className="ad-boton" disabled={asignar.isPending} onClick={enviar}>{asignar.isPending ? 'Guardando…' : 'Designar'}</button>
          </div>
        </fieldset>
      )}
      {puedeEditar && !area.activo && <p className="ad-mensaje" role="status">El área está inactiva: actívala para designar responsables.</p>}

      <EstadoConsulta loading={responsables.isLoading} error={responsables.error} empty={!responsables.data?.length} vacio="Este destino no tiene responsables designados." />
      {errorAccion && <p className="ad-mensaje" role="alert">{mensajeError(errorAccion)}</p>}
      {activos.length > 0 && (
        <ul className="ad-responsables" aria-label="Responsables activos">
          {activos.map((r) => (
            <li key={r.idMembresia}>
              <div>
                <strong>{r.usuario}</strong>
                <small>Designado {fecha(r.asignadoEn)}{r.asignadoPor ? ` por ${r.asignadoPor}` : ''}{r.usuarioActivo ? '' : ' · usuario inactivo'}</small>
              </div>
              <span className="ad-etiqueta" data-tono={r.esAutorizador ? 'violeta' : undefined}>{r.esAutorizador ? 'Autorizador' : 'Responsable'}</span>
              {puedeEditar && (
                <span className="ad-perfil-acciones">
                  <button type="button" className="ad-boton-sec" disabled={ocupado} onClick={() => cambiar.mutate(r)}>{r.esAutorizador ? 'Quitar autorizador' : 'Hacer autorizador'}</button>
                  <button type="button" className="ad-boton-sec au-peligro" disabled={ocupado} onClick={() => retirar.mutate(r)}>Retirar</button>
                </span>
              )}
            </li>
          ))}
        </ul>
      )}
      {retirados.length > 0 && (
        <details className="ad-responsables-retirados">
          <summary>Retirados ({retirados.length})</summary>
          <ul className="ad-responsables" aria-label="Responsables retirados">
            {retirados.map((r) => (
              <li key={r.idMembresia} data-inactivo>
                <div>
                  <strong>{r.usuario}</strong>
                  <small>Retirado {r.actualizadoEn ? fecha(r.actualizadoEn) : ''}{r.actualizadoPor ? ` por ${r.actualizadoPor}` : ''}</small>
                </div>
                <Estatus activo={false} texto="Retirado" />
                {puedeEditar && area.activo && r.usuarioActivo && (
                  <span className="ad-perfil-acciones">
                    <button type="button" className="ad-boton-sec" disabled={ocupado} onClick={() => reactivar.mutate(r)}>Designar de nuevo</button>
                  </span>
                )}
              </li>
            ))}
          </ul>
        </details>
      )}
    </Dialogo>
  )
}
