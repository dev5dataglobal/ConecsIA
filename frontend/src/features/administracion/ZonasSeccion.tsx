import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { Icono } from '../../shared/ui/Icono'
import { useAcciones } from '../modulos/apiModulos'
import { esAdministradorGeneral, leerOrganizacionActiva } from '../sesion/sesion'
import { cambiarEstadoZona, consultarZonas, crearZona, editarZona, type ZonaAdmin } from './apiAdministracion'
import {
  BotonIcono,
  BotonNuevo,
  BotonesFormulario,
  Buscador,
  Campo,
  Confirmar,
  Dialogo,
  Estado,
  EstadoConsulta,
  Estatus,
  Filtro,
  Interruptor,
  Marco,
} from './componentes'
import { mensajeError, useAviso, useDiferido } from './utilidades'
import type { SeccionProps } from './secciones'

const ESTADOS_ZONA = [
  { valor: '', texto: 'Todos los estados' },
  { valor: 'true', texto: 'Activas' },
  { valor: 'false', texto: 'Inactivas' },
]

/**
 * Zonas empresariales (ADR-028): agrupación de sedes, sin vínculo con organizaciones. La plataforma administra el catálogo;
 * cada organización asigna la zona en sus sedes y solo ve sus propias sedes dentro de la zona.
 */
export function ZonasSeccion({ pestanas }: SeccionProps) {
  const acciones = useAcciones('zonas_empresariales')
  const plataforma = esAdministradorGeneral()
  const [buscar, setBuscar] = useState('')
  const [activa, setActiva] = useState('')
  const [formulario, setFormulario] = useState<ZonaAdmin | 'nueva' | null>(null)
  const [confirmar, setConfirmar] = useState<ZonaAdmin | null>(null)
  const { avisar, aviso } = useAviso()
  const queryClient = useQueryClient()
  const texto = useDiferido(buscar)
  const zonas = useQuery({
    queryKey: ['admin-zonas', leerOrganizacionActiva(), texto, activa],
    queryFn: () => consultarZonas({ buscar: texto, activa }),
    enabled: acciones.consultar,
    placeholderData: keepPreviousData,
  })
  const estadoMutation = useMutation({
    mutationFn: (z: ZonaAdmin) => cambiarEstadoZona(z.idZonaEmpresarial, !z.activa),
    onSuccess: (z) => {
      void queryClient.invalidateQueries({ queryKey: ['admin-zonas'] })
      void queryClient.invalidateQueries({ queryKey: ['zonas-sede'] })
      setConfirmar(null)
      avisar(z.activa ? 'Zona empresarial activada.' : 'Zona empresarial inactivada.')
    },
  })

  return (
    <Marco
      titulo="Zonas empresariales"
      descripcion="Agrupaciones de sedes (parques, zonas francas…). Cada sede pertenece a lo sumo a una zona y sus áreas o puntos comunes los administra la organización dueña de la sede."
      pestanas={pestanas}
      accion={acciones.anadir && plataforma && <BotonNuevo texto="Nueva zona empresarial" onClick={() => setFormulario('nueva')} />}
    >
      <div className="ad-herramientas">
        <Buscador valor={buscar} onChange={setBuscar} etiqueta="Buscar zonas" placeholder="Buscar por nombre o descripción" />
        <Filtro etiqueta="Filtrar por estado" valor={activa} onChange={setActiva} opciones={ESTADOS_ZONA} />
      </div>
      {!acciones.consultar ? (
        <Estado tipo="permiso" texto="Tu perfil no tiene permiso para consultar zonas empresariales." />
      ) : (
        <EstadoConsulta loading={zonas.isLoading} error={zonas.error} empty={!zonas.data?.length}
          vacio={buscar || activa ? 'Ninguna zona coincide con los filtros.' : plataforma ? 'Aún no hay zonas empresariales.' : 'Ninguna sede de tu organización pertenece a una zona empresarial.'} />
      )}
      {zonas.data && zonas.data.length > 0 && (
        <div className="ad-zonas">
          {zonas.data.map((zona) => (
            <article className="ad-zona" key={zona.idZonaEmpresarial} data-inactivo={!zona.activa || undefined}>
              <header>
                <span className="ad-perfil-icono" aria-hidden="true"><Icono nombre="administracion" /></span>
                <div>
                  <h2>{zona.nombre}</h2>
                  {zona.descripcion && <small>{zona.descripcion}</small>}
                </div>
                <Estatus activo={zona.activa} texto={zona.activa ? 'Activa' : 'Inactiva'} />
              </header>
              <div className="ad-zona-empresas">
                <span className="ad-zona-titulo">{plataforma ? 'Sedes de la zona' : 'Tus sedes en la zona'} ({zona.sedes.length})</span>
                {zona.sedes.length ? (
                  <ul className="ad-etiquetas">
                    {zona.sedes.map((s) => (
                      <li key={s.idSede} className="ad-etiqueta" data-inactiva={!s.activo || undefined}>{plataforma ? `${s.sede} · ${s.organizacion}` : s.sede}</li>
                    ))}
                  </ul>
                ) : (
                  <span className="ad-tenue">Ninguna sede pertenece a esta zona.</span>
                )}
              </div>
              {acciones.editar && plataforma && (
                <footer>
                  <span />
                  <span className="ad-perfil-acciones">
                    <BotonIcono etiqueta={`Editar ${zona.nombre}`} icono="editar" onClick={() => setFormulario(zona)} />
                    <BotonIcono etiqueta={`${zona.activa ? 'Inactivar' : 'Activar'} ${zona.nombre}`} icono={zona.activa ? 'cerrar' : 'check'}
                      peligro={zona.activa} onClick={() => { estadoMutation.reset(); setConfirmar(zona) }} />
                  </span>
                </footer>
              )}
            </article>
          ))}
        </div>
      )}

      {formulario && (
        <ZonaFormulario zona={formulario === 'nueva' ? null : formulario} onClose={() => setFormulario(null)}
          onGuardado={(m) => { setFormulario(null); avisar(m) }} />
      )}
      {confirmar && (
        <Confirmar
          titulo={confirmar.activa ? '¿Inactivar zona empresarial?' : '¿Activar zona empresarial?'}
          texto={confirmar.activa
            ? 'La zona dejará de ofrecerse para nuevas sedes. Las sedes que ya pertenecen a ella, sus áreas y puntos comunes y su historial se conservan.'
            : 'La zona volverá a estar disponible para asignarla a sedes.'}
          accion={confirmar.activa ? 'Inactivar' : 'Activar'}
          peligro={confirmar.activa}
          pendiente={estadoMutation.isPending}
          error={estadoMutation.error ? mensajeError(estadoMutation.error) : null}
          onConfirmar={() => estadoMutation.mutate(confirmar)}
          onCancelar={() => setConfirmar(null)}
        />
      )}
      {aviso}
    </Marco>
  )
}

function ZonaFormulario({ zona, onClose, onGuardado }: { zona: ZonaAdmin | null; onClose: () => void; onGuardado: (mensaje: string) => void }) {
  const queryClient = useQueryClient()
  const [nombre, setNombre] = useState(zona?.nombre ?? '')
  const [descripcion, setDescripcion] = useState(zona?.descripcion ?? '')
  const [activa, setActiva] = useState(zona?.activa ?? true)
  const [errorNombre, setErrorNombre] = useState<string>()
  const guardar = useMutation({
    mutationFn: () => {
      const datos = { nombre: nombre.trim(), descripcion: descripcion.trim() || null, activa }
      return zona ? editarZona(zona.idZonaEmpresarial, datos) : crearZona(datos)
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin-zonas'] })
      void queryClient.invalidateQueries({ queryKey: ['zonas-sede'] })
      onGuardado(zona ? 'Zona empresarial actualizada.' : 'Zona empresarial creada.')
    },
  })

  function enviar(event: FormEvent) {
    event.preventDefault()
    setErrorNombre(nombre.trim() ? undefined : 'El nombre de la zona es obligatorio.')
    if (nombre.trim() && !guardar.isPending) guardar.mutate()
  }

  return (
    <Dialogo titulo={zona ? 'Editar zona empresarial' : 'Nueva zona empresarial'} eyebrow="Zonas empresariales" icono="administracion"
      onClose={onClose} onSubmit={enviar} pie={<BotonesFormulario onCancelar={onClose} pendiente={guardar.isPending} />}>
      <div className="ad-rejilla">
        <Campo etiqueta="Nombre de la zona" error={errorNombre} ancho="completo">
          <input required autoFocus maxLength={200} value={nombre} aria-invalid={Boolean(errorNombre)} onChange={(e) => setNombre(e.target.value)} />
        </Campo>
        <Campo etiqueta="Descripción (opcional)" ancho="completo">
          <textarea rows={2} maxLength={2000} value={descripcion} onChange={(e) => setDescripcion(e.target.value)} />
        </Campo>
      </div>
      <p className="ad-ayuda">Las organizaciones asignan la zona en el formulario de cada sede.</p>
      <Interruptor etiqueta="Zona activa" ayuda={activa ? 'Disponible para asignarla a sedes.' : 'No se ofrecerá para nuevas sedes; las actuales la conservan.'} checked={activa} onChange={setActiva} />
      {guardar.error && <p className="ad-mensaje" role="alert">{mensajeError(guardar.error, 'No se pudo guardar la zona empresarial.')}</p>}
    </Dialogo>
  )
}
