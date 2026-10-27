import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { Icono } from '../../shared/ui/Icono'
import { useAcciones } from '../modulos/apiModulos'
import { esAdministradorGeneral, leerOrganizacionActiva } from '../sesion/sesion'
import {
  cambiarEstadoZona,
  consultarEmpresasDisponibles,
  consultarZonas,
  crearZona,
  editarZona,
  type EmpresaZona,
  type OrganizacionEntrada,
  type ZonaAdmin,
} from './apiAdministracion'
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
import { CamposOrganizacion } from './OrganizacionesSeccion'
import { limpiarOrganizacion, organizacionAEntrada, organizacionVacia, validarOrganizacion } from './organizacion'
import type { SeccionProps } from './secciones'

const ESTADOS_ZONA = [
  { valor: '', texto: 'Todos los estados' },
  { valor: 'true', texto: 'Activas' },
  { valor: 'false', texto: 'Inactivas' },
]

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
      void queryClient.invalidateQueries({ queryKey: ['admin-organizaciones'] })
      setConfirmar(null)
      avisar(z.activa ? 'Zona empresarial activada.' : 'Zona empresarial inactivada.')
    },
  })

  return (
    <Marco
      titulo="Zonas empresariales"
      descripcion="Organizaciones superiores que agrupan empresas. La zona administra los accesos de sus áreas comunes; cada empresa conserva los suyos."
      pestanas={pestanas}
      accion={acciones.anadir && plataforma && <BotonNuevo texto="Nueva zona empresarial" onClick={() => setFormulario('nueva')} />}
    >
      <div className="ad-herramientas">
        <Buscador valor={buscar} onChange={setBuscar} etiqueta="Buscar zonas" placeholder="Buscar por zona u organización superior" />
        <Filtro etiqueta="Filtrar por estado" valor={activa} onChange={setActiva} opciones={ESTADOS_ZONA} />
      </div>
      {!acciones.consultar ? (
        <Estado tipo="permiso" texto="Tu perfil no tiene permiso para consultar zonas empresariales." />
      ) : (
        <EstadoConsulta loading={zonas.isLoading} error={zonas.error} empty={!zonas.data?.length}
          vacio={buscar || activa ? 'Ninguna zona coincide con los filtros.' : plataforma ? 'Aún no hay zonas empresariales.' : 'Tu organización no participa en ninguna zona empresarial.'} />
      )}
      {zonas.data && zonas.data.length > 0 && (
        <div className="ad-zonas">
          {zonas.data.map((zona) => (
            <article className="ad-zona" key={zona.idZonaEmpresarial} data-inactivo={!zona.activa || undefined}>
              <header>
                <span className="ad-perfil-icono" aria-hidden="true"><Icono nombre="administracion" /></span>
                <div>
                  <h2>{zona.nombre}</h2>
                  <small>Organización superior: <strong>{zona.organizacionSuperior?.nombre ?? '—'}</strong></small>
                </div>
                <Estatus activo={zona.activa} texto={zona.activa ? 'Activa' : 'Inactiva'} />
              </header>
              {zona.descripcion && <p>{zona.descripcion}</p>}
              <div className="ad-zona-empresas">
                <span className="ad-zona-titulo">Empresas ({zona.empresas.length})</span>
                {zona.empresas.length ? (
                  <ul className="ad-etiquetas">
                    {zona.empresas.map((e) => <li key={e.idOrganizacion} className="ad-etiqueta" data-inactiva={e.estado !== 'Activa' || undefined}>{e.nombre}</li>)}
                  </ul>
                ) : (
                  <span className="ad-tenue">{plataforma || zona.organizacionSuperior?.idOrganizacion === leerOrganizacionActiva() ? 'Sin empresas asociadas.' : 'Solo la organización superior ve las empresas de la zona.'}</span>
                )}
              </div>
              <footer>
                <span>{zona.totalSedes} {zona.totalSedes === 1 ? 'sede común' : 'sedes comunes'}</span>
                {acciones.editar && plataforma && (
                  <span className="ad-perfil-acciones">
                    <BotonIcono etiqueta={`Editar ${zona.nombre}`} icono="editar" onClick={() => setFormulario(zona)} />
                    <BotonIcono etiqueta={`${zona.activa ? 'Inactivar' : 'Activar'} ${zona.nombre}`} icono={zona.activa ? 'cerrar' : 'check'}
                      peligro={zona.activa} onClick={() => { estadoMutation.reset(); setConfirmar(zona) }} />
                  </span>
                )}
              </footer>
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
            ? `Se inactivarán la zona y su organización superior (${confirmar.organizacionSuperior?.nombre ?? '—'}). Las empresas asociadas siguen activas con sus propios accesos; no se borran vínculos, sedes ni historial.`
            : 'Se reactivarán la zona y su organización superior.'}
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
  const [organizacion, setOrganizacion] = useState<OrganizacionEntrada>(zona?.organizacionSuperior ? organizacionAEntrada(zona.organizacionSuperior) : organizacionVacia())
  const [empresas, setEmpresas] = useState<EmpresaZona[]>(zona?.empresas ?? [])
  const [filtro, setFiltro] = useState('')
  const [errores, setErrores] = useState<Partial<Record<keyof OrganizacionEntrada, string>>>({})
  const [errorNombre, setErrorNombre] = useState<string>()
  const disponibles = useQuery({ queryKey: ['empresas-disponibles', zona?.idZonaEmpresarial], queryFn: () => consultarEmpresasDisponibles(zona?.idZonaEmpresarial) })
  const guardar = useMutation({
    mutationFn: () => {
      const datos = { nombre: nombre.trim(), descripcion: descripcion.trim() || null, activa, organizacionSuperior: limpiarOrganizacion(organizacion), idEmpresas: empresas.map((e) => e.idOrganizacion) }
      return zona ? editarZona(zona.idZonaEmpresarial, datos) : crearZona(datos)
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin-zonas'] })
      void queryClient.invalidateQueries({ queryKey: ['admin-organizaciones'] })
      void queryClient.invalidateQueries({ queryKey: ['catalogos-organizacion'] })
      void queryClient.invalidateQueries({ queryKey: ['empresas-disponibles'] })
      onGuardado(zona ? 'Zona empresarial actualizada.' : 'Zona empresarial creada.')
    },
  })

  const opciones = (disponibles.data ?? []).filter((e) => !empresas.some((s) => s.idOrganizacion === e.idOrganizacion)
    && e.nombre.toLowerCase().includes(filtro.trim().toLowerCase()))

  function enviar(event: FormEvent) {
    event.preventDefault()
    const nuevos = validarOrganizacion(organizacion)
    setErrores(nuevos)
    setErrorNombre(nombre.trim() ? undefined : 'El nombre de la zona es obligatorio.')
    if (nombre.trim() && Object.keys(nuevos).length === 0 && !guardar.isPending) guardar.mutate()
  }

  return (
    <Dialogo titulo={zona ? 'Editar zona empresarial' : 'Nueva zona empresarial'} eyebrow="Zonas empresariales" icono="administracion" ancho="amplio"
      onClose={onClose} onSubmit={enviar} pie={<BotonesFormulario onCancelar={onClose} pendiente={guardar.isPending} />}>
      <fieldset className="ad-grupo">
        <legend>Zona</legend>
        <div className="ad-rejilla">
          <Campo etiqueta="Nombre de la zona" error={errorNombre} ancho="completo">
            <input required autoFocus maxLength={200} value={nombre} aria-invalid={Boolean(errorNombre)} onChange={(e) => setNombre(e.target.value)} />
          </Campo>
          <Campo etiqueta="Descripción (opcional)" ancho="completo">
            <textarea rows={2} maxLength={2000} value={descripcion} onChange={(e) => setDescripcion(e.target.value)} />
          </Campo>
        </div>
      </fieldset>

      <fieldset className="ad-grupo">
        <legend>Organización superior</legend>
        <p className="ad-ayuda">Organización que administra la zona y los accesos de sus áreas comunes.</p>
        <CamposOrganizacion datos={organizacion} cambiar={(p) => setOrganizacion((o) => ({ ...o, ...p }))} errores={errores} plataforma superior={false} />
      </fieldset>

      <fieldset className="ad-grupo">
        <legend>Empresas de la zona ({empresas.length})</legend>
        <p className="ad-ayuda">Una empresa solo puede pertenecer a una zona empresarial activa. Retirarla conserva su historial y sus propios accesos.</p>
        {empresas.length > 0 && (
          <ul className="ad-seleccion">
            {empresas.map((e) => (
              <li key={e.idOrganizacion}>
                <span>{e.nombre}{e.identificacion && <small> · {e.identificacion}</small>}</span>
                <button type="button" className="ad-quitar" aria-label={`Retirar ${e.nombre}`} onClick={() => setEmpresas((l) => l.filter((x) => x.idOrganizacion !== e.idOrganizacion))}>
                  <Icono nombre="cerrar" />
                </button>
              </li>
            ))}
          </ul>
        )}
        <Buscador valor={filtro} onChange={setFiltro} etiqueta="Buscar empresas disponibles" placeholder="Buscar empresas para asociar" />
        {disponibles.isLoading && <div className="ad-cargando" aria-busy="true" aria-label="Cargando empresas"><div /></div>}
        {disponibles.error && <p className="ad-mensaje" role="alert">{mensajeError(disponibles.error, 'No se pudieron cargar las empresas.')}</p>}
        {!disponibles.isLoading && (
          <ul className="ad-disponibles">
            {opciones.slice(0, 50).map((e) => (
              <li key={e.idOrganizacion}>
                <button type="button" onClick={() => setEmpresas((l) => [...l, e])}>
                  <span>{e.nombre}{e.identificacion && <small> · {e.identificacion}</small>}</span>
                  <span aria-hidden="true">+</span>
                </button>
              </li>
            ))}
            {opciones.length === 0 && <li className="ad-tenue">No hay más empresas disponibles.</li>}
          </ul>
        )}
      </fieldset>

      <Interruptor etiqueta="Zona activa" ayuda={activa ? 'La zona y su organización superior operan.' : 'La zona y su organización superior quedarán inactivas.'} checked={activa} onChange={setActiva} />
      {guardar.error && <p className="ad-mensaje" role="alert">{mensajeError(guardar.error, 'No se pudo guardar la zona empresarial.')}</p>}
    </Dialogo>
  )
}
