import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { ApiError } from '../../shared/api/problemDetails'
import {
  CLAVE_MIS_MODULOS,
  consultarCatalogoModulos,
  consultarModulosDeRol,
  reemplazarModulosDeRol,
  useMisModulos,
  type Accion,
  type AsignacionModulo,
} from '../modulos/apiModulos'
import { esAdministradorGeneral } from '../sesion/sesion'
import type { PerfilAdmin } from './apiAdministracion'
import {
  Dialogo,
} from './componentes'

const ACCIONES: { clave: Accion; etiqueta: string; ayuda: string }[] = [
  { clave: 'consultar', etiqueta: 'Consultar', ayuda: 'Ver listados y detalles' },
  { clave: 'anadir', etiqueta: 'Añadir', ayuda: 'Crear registros' },
  { clave: 'editar', etiqueta: 'Editar', ayuda: 'Modificar, activar o inactivar' },
]

type Matriz = Record<number, Record<Accion, boolean>>

/**
 * Matriz de acciones por módulo de un perfil. Cada acción es independiente: Añadir o Editar no conceden Consultar.
 * El servidor valida el alcance de organización y que no se otorguen acciones que el administrador no tiene;
 * aquí esas casillas se muestran deshabilitadas solo como ayuda.
 */
export function ModulosRolModal({ perfil, editable, onClose, onGuardado }: { perfil: PerfilAdmin; editable: boolean; onClose: () => void; onGuardado?: () => void }) {
  const queryClient = useQueryClient()
  const catalogo = useQuery({ queryKey: ['catalogo-modulos'], queryFn: consultarCatalogoModulos })
  const asignados = useQuery({ queryKey: ['modulos-rol', perfil.idRol], queryFn: () => consultarModulosDeRol(perfil.idRol) })
  const propios = useMisModulos()
  const [cambios, setCambios] = useState<Matriz | null>(null)
  const plataforma = esAdministradorGeneral()

  const inicial: Matriz = Object.fromEntries((asignados.data ?? []).map((m) => [m.idAppModulo, { consultar: Boolean(m.consultar), anadir: Boolean(m.anadir), editar: Boolean(m.editar) }]))
  const matriz = cambios ?? inicial
  const valor = (id: number, accion: Accion) => Boolean(matriz[id]?.[accion])
  /** Sin ser administrador de plataforma solo se tocan acciones que uno mismo tiene en ese módulo. */
  const puedeCambiar = (codigo: string, accion: Accion) =>
    editable && (plataforma || Boolean(propios.data?.find((m) => m.codigo === codigo)?.[accion]))

  const guardar = useMutation({
    mutationFn: () => {
      const asignaciones: AsignacionModulo[] = Object.entries(matriz)
        .map(([id, a]) => ({ idAppModulo: Number(id), ...a }))
        .filter((a) => a.consultar || a.anadir || a.editar)
      return reemplazarModulosDeRol(perfil.idRol, asignaciones)
    },
    onSuccess: (data) => {
      queryClient.setQueryData(['modulos-rol', perfil.idRol], data)
      // El usuario actual puede tener este perfil: se refresca su menú y sus acciones.
      void queryClient.invalidateQueries({ queryKey: CLAVE_MIS_MODULOS })
      void queryClient.invalidateQueries({ queryKey: ['admin-perfiles'] })
      onGuardado?.()
      onClose()
    },
  })

  function poner(id: number, accion: Accion, activo: boolean) {
    setCambios({ ...matriz, [id]: { consultar: false, anadir: false, editar: false, ...matriz[id], [accion]: activo } })
  }

  function ponerFila(id: number, codigo: string, activo: boolean) {
    const fila = { consultar: false, anadir: false, editar: false, ...matriz[id] }
    for (const { clave } of ACCIONES) if (puedeCambiar(codigo, clave)) fila[clave] = activo
    setCambios({ ...matriz, [id]: fila })
  }

  function enviar(event: FormEvent) {
    event.preventDefault()
    if (!guardar.isPending) guardar.mutate()
  }

  const cargando = catalogo.isLoading || asignados.isLoading || propios.isLoading
  const error = catalogo.error ?? asignados.error ?? guardar.error
  const detalle = !error
    ? null
    : !(error instanceof ApiError)
      ? 'No se pudo completar la operación.'
      : error.status === 403
        ? error.problem.detail ?? 'No tienes permiso para gestionar los módulos de este perfil.'
        : error.status === 404
          ? 'El perfil no pertenece a la organización activa.'
          : error.problem.detail ?? 'No se pudo completar la operación.'
  const modulos = [...(catalogo.data?.filter((m) => m.activo) ?? [])].sort((a, b) => a.orden - b.orden)
  const totalConAcceso = modulos.filter((m) => ACCIONES.some((a) => valor(m.idAppModulo, a.clave))).length

  return (
    <Dialogo
      titulo={perfil.nombre}
      eyebrow="Permisos por módulo"
      icono="dashboard"
      ancho="matriz"
      onClose={onClose}
      onSubmit={enviar}
      pie={
        <>
          <span className="ad-matriz-resumen">{totalConAcceso} de {modulos.length} módulos con acceso</span>
          <button type="button" className="ad-boton-sec" onClick={onClose}>{editable ? 'Cancelar' : 'Cerrar'}</button>
          {editable && (
            <button className="ad-boton" type="submit" disabled={guardar.isPending || cargando || Boolean(catalogo.error ?? asignados.error)}>
              {guardar.isPending ? 'Guardando…' : 'Guardar permisos'}
            </button>
          )}
        </>
      }
    >
      <p className="ad-ayuda">
        Cada acción se concede por separado: <strong>Añadir</strong> o <strong>Editar</strong> no permiten <strong>Consultar</strong>.
        Inactivar y reactivar registros requiere <strong>Editar</strong>. Un módulo sin acciones queda sin acceso, aunque se llame la API directamente.
      </p>

      {cargando && <div className="ad-cargando" aria-busy="true" aria-label="Cargando"><div /><div /><div /></div>}
      {!cargando && modulos.length > 0 && (
        <div className="ad-matriz-wrap">
          <table className="ad-matriz">
            <caption className="sr-only">Acciones por módulo del perfil {perfil.nombre}</caption>
            <thead>
              <tr>
                <th scope="col">Módulo</th>
                {ACCIONES.map((a) => <th key={a.clave} scope="col" title={a.ayuda}>{a.etiqueta}</th>)}
                {editable && <th scope="col">Todas</th>}
              </tr>
            </thead>
            <tbody>
              {modulos.map((modulo) => {
                const todas = ACCIONES.every((a) => valor(modulo.idAppModulo, a.clave))
                const algunaEditable = ACCIONES.some((a) => puedeCambiar(modulo.codigo, a.clave))
                return (
                  <tr key={modulo.idAppModulo} data-con-acceso={ACCIONES.some((a) => valor(modulo.idAppModulo, a.clave)) || undefined}>
                    <th scope="row">
                      <strong>{modulo.nombre}</strong>
                      <small>{modulo.descripcion ?? modulo.codigo}</small>
                    </th>
                    {ACCIONES.map((a) => {
                      const permitido = puedeCambiar(modulo.codigo, a.clave)
                      return (
                        <td key={a.clave} data-etiqueta={a.etiqueta}>
                          <label className="ad-matriz-celda" title={!permitido && editable ? 'No puedes otorgar ni retirar una acción que no tienes' : a.ayuda}>
                            <input
                              type="checkbox"
                              checked={valor(modulo.idAppModulo, a.clave)}
                              disabled={!permitido || guardar.isPending}
                              onChange={(e) => poner(modulo.idAppModulo, a.clave, e.target.checked)}
                              aria-label={`${a.etiqueta} en ${modulo.nombre}`}
                            />
                            <span aria-hidden="true">{a.etiqueta}</span>
                          </label>
                        </td>
                      )
                    })}
                    {editable && (
                      <td data-etiqueta="Todas">
                        <label className="ad-matriz-celda">
                          <input type="checkbox" checked={todas} disabled={!algunaEditable || guardar.isPending}
                            onChange={(e) => ponerFila(modulo.idAppModulo, modulo.codigo, e.target.checked)} aria-label={`Todas las acciones en ${modulo.nombre}`} />
                          <span aria-hidden="true">Todas</span>
                        </label>
                      </td>
                    )}
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
      {detalle && <p className="ad-mensaje" role="alert">{detalle}</p>}
    </Dialogo>
  )
}
