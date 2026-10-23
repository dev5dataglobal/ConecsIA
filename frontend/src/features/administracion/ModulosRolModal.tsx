import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState, type FormEvent } from 'react'
import { ApiError } from '../../shared/api/problemDetails'
import { Icono } from '../../shared/ui/Icono'
import {
  CLAVE_MIS_MODULOS,
  consultarCatalogoModulos,
  consultarModulosDeRol,
  reemplazarModulosDeRol,
} from '../modulos/apiModulos'
import type { PerfilAdmin } from './apiAdministracion'

/**
 * Asignación de módulos a un perfil (rol). El servidor valida alcance de organización,
 * existencia de módulos y que no se otorguen módulos ajenos al administrador.
 */
export function ModulosRolModal({ perfil, editable, onClose }: { perfil: PerfilAdmin; editable: boolean; onClose: () => void }) {
  const queryClient = useQueryClient()
  const catalogo = useQuery({ queryKey: ['catalogo-modulos'], queryFn: consultarCatalogoModulos })
  const asignados = useQuery({ queryKey: ['modulos-rol', perfil.idRol], queryFn: () => consultarModulosDeRol(perfil.idRol) })
  const [seleccion, setSeleccion] = useState<Set<number> | null>(null)
  const elegidos = seleccion ?? new Set(asignados.data?.map((m) => m.idAppModulo) ?? [])

  const guardar = useMutation({
    mutationFn: () => reemplazarModulosDeRol(perfil.idRol, [...elegidos]),
    onSuccess: (data) => {
      queryClient.setQueryData(['modulos-rol', perfil.idRol], data)
      // El usuario actual puede tener este rol: se refresca su menú.
      void queryClient.invalidateQueries({ queryKey: CLAVE_MIS_MODULOS })
      onClose()
    },
  })

  useEffect(() => {
    const alPresionarTecla = (event: KeyboardEvent) => { if (event.key === 'Escape') onClose() }
    document.addEventListener('keydown', alPresionarTecla)
    return () => document.removeEventListener('keydown', alPresionarTecla)
  }, [onClose])

  function alternar(id: number) {
    const siguiente = new Set(elegidos)
    if (siguiente.has(id)) siguiente.delete(id)
    else siguiente.add(id)
    setSeleccion(siguiente)
  }

  function enviar(event: FormEvent) {
    event.preventDefault()
    if (!guardar.isPending) guardar.mutate()
  }

  const cargando = catalogo.isLoading || asignados.isLoading
  const error = catalogo.error ?? asignados.error ?? guardar.error
  const detalle = !error
    ? null
    : !(error instanceof ApiError)
      ? 'No se pudo completar la operación.'
      : error.status === 403
        ? error.problem.detail ?? 'No tienes permiso para gestionar los módulos de este perfil.'
        : error.status === 404
          ? 'El perfil no pertenece a tu organización.'
          : error.problem.detail ?? 'No se pudo completar la operación.'
  const modulosActivos = catalogo.data?.filter((m) => m.activo) ?? []

  return (
    <div className="ad-velo" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) onClose() }}>
      <form className="ad-modal ad-modal-ancho" onSubmit={enviar} role="dialog" aria-modal="true" aria-labelledby="ad-modulos-titulo">
        <header>
          <span className="ad-modal-icono" aria-hidden="true"><Icono nombre="dashboard" /></span>
          <div>
            <span className="ad-modal-eyebrow">Módulos del perfil</span>
            <h2 id="ad-modulos-titulo">{perfil.nombre}</h2>
          </div>
          <button type="button" className="ad-cerrar" onClick={onClose} aria-label="Cerrar"><Icono nombre="cerrar" /></button>
        </header>

        <p className="ad-ayuda">Sin un módulo asignado, los usuarios de este perfil no pueden usarlo aunque llamen la API directamente.</p>

        {cargando && <div className="ad-cargando" aria-busy="true" aria-label="Cargando"><div /><div /><div /></div>}
        {!cargando && modulosActivos.length > 0 && (
          <fieldset className="ad-modulos" disabled={!editable || guardar.isPending}>
            <legend className="sr-only">Módulos asignados</legend>
            {modulosActivos.map((modulo) => (
              <label key={modulo.idAppModulo} className="ad-modulo">
                <input type="checkbox" checked={elegidos.has(modulo.idAppModulo)} onChange={() => alternar(modulo.idAppModulo)} />
                <span>
                  <strong>{modulo.nombre}</strong>
                  <small>{modulo.descripcion ?? modulo.codigo}</small>
                </span>
              </label>
            ))}
          </fieldset>
        )}
        {detalle && <p className="ad-mensaje" role="alert">{detalle}</p>}

        <footer>
          <button type="button" className="ad-boton-sec" onClick={onClose}>{editable ? 'Cancelar' : 'Cerrar'}</button>
          {editable && (
            <button className="ad-boton" type="submit" disabled={guardar.isPending || cargando || Boolean(catalogo.error ?? asignados.error)}>
              {guardar.isPending ? 'Guardando…' : 'Guardar módulos'}
            </button>
          )}
        </footer>
      </form>
    </div>
  )
}
