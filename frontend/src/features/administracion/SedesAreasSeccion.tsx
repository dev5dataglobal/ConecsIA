import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState, type FormEvent } from 'react'
import { Icono } from '../../shared/ui/Icono'
import { Selector } from '../../shared/ui/Selector'
import { useAcciones } from '../modulos/apiModulos'
import { leerOrganizacionActiva } from '../sesion/sesion'
import {
  cambiarEstadoArea,
  cambiarEstadoSede,
  consultarAreas,
  consultarCiudadesSede,
  consultarSedes,
  crearArea,
  crearSede,
  editarArea,
  editarSede,
  type AreaAdmin,
  type CiudadOpcion,
  type SedeAdmin,
} from './apiSedes'
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
import { mensajeError, useAviso } from './utilidades'
import type { SeccionProps } from './secciones'

type Vista = 'sedes' | 'areas'
type Pendiente = { tipo: 'sede'; registro: SedeAdmin } | { tipo: 'area'; registro: AreaAdmin }

const ESTADOS = [
  { valor: '', texto: 'Todos los estados' },
  { valor: 'true', texto: 'Activas' },
  { valor: 'false', texto: 'Inactivas' },
]

const coincide = (texto: string, ...campos: (string | null | undefined)[]) => {
  const buscado = texto.trim().toLowerCase()
  return !buscado || campos.some((c) => c?.toLowerCase().includes(buscado))
}

const nombreCiudad = (c?: CiudadOpcion) => (c ? [c.nombre, c.departamento].filter(Boolean).join(', ') : '—')

export function SedesAreasSeccion({ pestanas }: SeccionProps) {
  const acciones = useAcciones('sedes')
  const [vista, setVista] = useState<Vista>('sedes')
  const [buscar, setBuscar] = useState('')
  const [activo, setActivo] = useState('')
  const [idSedeFiltro, setIdSedeFiltro] = useState('')
  const [formSede, setFormSede] = useState<SedeAdmin | 'nueva' | null>(null)
  const [formArea, setFormArea] = useState<AreaAdmin | 'nueva' | null>(null)
  const [confirmar, setConfirmar] = useState<Pendiente | null>(null)
  const { avisar, aviso } = useAviso()
  const queryClient = useQueryClient()
  const organizacion = leerOrganizacionActiva()

  const sedes = useQuery({ queryKey: ['admin-sedes', organizacion], queryFn: consultarSedes, enabled: acciones.consultar })
  const areas = useQuery({ queryKey: ['admin-areas', organizacion], queryFn: consultarAreas, enabled: acciones.consultar })
  const ciudades = useQuery({ queryKey: ['ciudades-sede'], queryFn: consultarCiudadesSede, enabled: acciones.consultar, staleTime: 5 * 60_000 })

  const ciudadPorId = useMemo(() => new Map((ciudades.data ?? []).map((c) => [c.idCiudad, c])), [ciudades.data])
  const sedePorId = useMemo(() => new Map((sedes.data ?? []).map((s) => [s.idSede, s])), [sedes.data])
  const areasPorSede = useMemo(() => {
    const conteo = new Map<number, number>()
    for (const a of areas.data ?? []) if (a.activo) conteo.set(a.idSede, (conteo.get(a.idSede) ?? 0) + 1)
    return conteo
  }, [areas.data])

  const filtroEstado = (x: { activo: boolean }) => !activo || String(x.activo) === activo
  const sedesVisibles = (sedes.data ?? []).filter((s) => filtroEstado(s)
    && coincide(buscar, s.nombre, s.direccion, ciudadPorId.get(s.idCiudad)?.nombre))
  const areasVisibles = (areas.data ?? []).filter((a) => filtroEstado(a)
    && (!idSedeFiltro || String(a.idSede) === idSedeFiltro)
    && coincide(buscar, a.nombre, a.descripcion, sedePorId.get(a.idSede)?.nombre))

  const estadoMutation = useMutation({
    mutationFn: (p: Pendiente): Promise<{ activo: boolean }> => p.tipo === 'sede'
      ? cambiarEstadoSede(p.registro.idSede, !p.registro.activo)
      : cambiarEstadoArea(p.registro.idArea, !p.registro.activo),
    onSuccess: (r, p) => {
      void queryClient.invalidateQueries({ queryKey: [p.tipo === 'sede' ? 'admin-sedes' : 'admin-areas'] })
      setConfirmar(null)
      const nombre = p.tipo === 'sede' ? 'Sede' : 'Área'
      avisar(r.activo ? `${nombre} activada.` : `${nombre} inactivada.`)
    },
  })

  const consulta = vista === 'sedes' ? sedes : areas
  const visibles = vista === 'sedes' ? sedesVisibles : areasVisibles
  const filtrando = Boolean(buscar || activo || (vista === 'areas' && idSedeFiltro))
  const sinSedesActivas = !(sedes.data ?? []).some((s) => s.activo)

  function cambiarVista(v: Vista) {
    setVista(v)
    setBuscar('')
    setActivo('')
  }

  return (
    <Marco
      titulo="Sedes y áreas"
      descripcion="Instalaciones de la organización y las áreas o destinos que reciben a las personas. Inactivar conserva el historial."
      pestanas={pestanas}
      accion={acciones.anadir && (vista === 'sedes'
        ? <BotonNuevo texto="Nueva sede" onClick={() => setFormSede('nueva')} />
        : <BotonNuevo texto="Nueva área" onClick={() => setFormArea('nueva')} disabled={sinSedesActivas} />)}
    >
      <div className="ad-vista" role="tablist" aria-label="Ver sedes o áreas">
        {(['sedes', 'areas'] as const).map((v) => (
          <button key={v} type="button" role="tab" aria-selected={vista === v} onClick={() => cambiarVista(v)}>
            {v === 'sedes' ? 'Sedes' : 'Áreas'}
            <span>{(v === 'sedes' ? sedes.data : areas.data)?.filter((x) => x.activo).length ?? 0}</span>
          </button>
        ))}
      </div>

      <div className="ad-herramientas">
        <Buscador valor={buscar} onChange={setBuscar} etiqueta={vista === 'sedes' ? 'Buscar sedes' : 'Buscar áreas'}
          placeholder={vista === 'sedes' ? 'Buscar por nombre, dirección o ciudad' : 'Buscar por nombre, descripción o sede'} />
        {vista === 'areas' && (
          <Filtro etiqueta="Filtrar por sede" valor={idSedeFiltro} onChange={setIdSedeFiltro}
            opciones={[{ valor: '', texto: 'Todas las sedes' }, ...(sedes.data ?? []).map((s) => ({ valor: String(s.idSede), texto: s.nombre }))]} />
        )}
        <Filtro etiqueta="Filtrar por estado" valor={activo} onChange={setActivo} opciones={ESTADOS} />
      </div>

      {!acciones.consultar ? (
        <Estado tipo="permiso" texto="Tu perfil no tiene permiso para consultar sedes y áreas." />
      ) : (
        <EstadoConsulta loading={consulta.isLoading} error={consulta.error} empty={!visibles.length}
          vacio={filtrando
            ? `Ninguna ${vista === 'sedes' ? 'sede' : 'área'} coincide con los filtros.`
            : vista === 'sedes' ? 'Aún no hay sedes registradas.' : sinSedesActivas ? 'Crea primero una sede activa para registrar áreas.' : 'Aún no hay áreas registradas.'} />
      )}

      {vista === 'sedes' && sedesVisibles.length > 0 && (
        <div className="ad-zonas">
          {sedesVisibles.map((sede) => (
            <article className="ad-zona" key={sede.idSede} data-inactivo={!sede.activo || undefined}>
              <header>
                <span className="ad-perfil-icono" aria-hidden="true"><Icono nombre="sede" /></span>
                <div>
                  <h2>{sede.nombre}</h2>
                  <small>Ciudad: <strong>{nombreCiudad(ciudadPorId.get(sede.idCiudad))}</strong></small>
                </div>
                <Estatus activo={sede.activo} texto={sede.activo ? 'Activa' : 'Inactiva'} />
              </header>
              <ul className="ad-etiquetas">
                <li className="ad-etiqueta">{sede.zonaHoraria}</li>
                {sede.idZonaEmpresarial && <li className="ad-etiqueta">Zona empresarial</li>}
              </ul>
              {(sede.direccion || sede.telefono || sede.correo) && (
                <p>{[sede.direccion, sede.telefono, sede.correo].filter(Boolean).join(' · ')}</p>
              )}
              <footer>
                <span>{areasPorSede.get(sede.idSede) ?? 0} {areasPorSede.get(sede.idSede) === 1 ? 'área activa' : 'áreas activas'}</span>
                {acciones.editar && (
                  <span className="ad-perfil-acciones">
                    <BotonIcono etiqueta={`Editar ${sede.nombre}`} icono="editar" onClick={() => setFormSede(sede)} />
                    <BotonIcono etiqueta={`${sede.activo ? 'Inactivar' : 'Activar'} ${sede.nombre}`} icono={sede.activo ? 'cerrar' : 'check'}
                      peligro={sede.activo} onClick={() => { estadoMutation.reset(); setConfirmar({ tipo: 'sede', registro: sede }) }} />
                  </span>
                )}
              </footer>
            </article>
          ))}
        </div>
      )}

      {vista === 'areas' && areasVisibles.length > 0 && (
        <div className="ad-zonas">
          {areasVisibles.map((area) => (
            <article className="ad-zona" key={area.idArea} data-inactivo={!area.activo || undefined}>
              <header>
                <span className="ad-perfil-icono" aria-hidden="true"><Icono nombre="accesos" /></span>
                <div>
                  <h2>{area.nombre}</h2>
                  <small>Sede: <strong>{sedePorId.get(area.idSede)?.nombre ?? '—'}</strong></small>
                </div>
                <Estatus activo={area.activo} texto={area.activo ? 'Activa' : 'Inactiva'} />
              </header>
              {area.descripcion && <p>{area.descripcion}</p>}
              {area.esComunZonaEmpresarial && (
                <ul className="ad-etiquetas"><li className="ad-etiqueta">Área común de la zona empresarial</li></ul>
              )}
              {acciones.editar && (
                <footer>
                  <span />
                  <span className="ad-perfil-acciones">
                    <BotonIcono etiqueta={`Editar ${area.nombre}`} icono="editar" onClick={() => setFormArea(area)} />
                    <BotonIcono etiqueta={`${area.activo ? 'Inactivar' : 'Activar'} ${area.nombre}`} icono={area.activo ? 'cerrar' : 'check'}
                      peligro={area.activo} onClick={() => { estadoMutation.reset(); setConfirmar({ tipo: 'area', registro: area }) }} />
                  </span>
                </footer>
              )}
            </article>
          ))}
        </div>
      )}

      {formSede && (
        <SedeFormulario sede={formSede === 'nueva' ? null : formSede} ciudades={ciudades.data ?? []} cargandoCiudades={ciudades.isLoading}
          errorCiudades={ciudades.error} onClose={() => setFormSede(null)} onGuardado={(m) => { setFormSede(null); avisar(m) }} />
      )}
      {formArea && (
        <AreaFormulario area={formArea === 'nueva' ? null : formArea} sedes={sedes.data ?? []}
          onClose={() => setFormArea(null)} onGuardado={(m) => { setFormArea(null); avisar(m) }} />
      )}
      {confirmar && (
        <Confirmar
          titulo={`¿${confirmar.registro.activo ? 'Inactivar' : 'Activar'} ${confirmar.tipo === 'sede' ? 'sede' : 'área'}?`}
          texto={confirmar.registro.activo
            ? `«${confirmar.registro.nombre}» dejará de estar disponible para nuevos registros. No se borran sus datos ni su historial.`
            : `«${confirmar.registro.nombre}» volverá a estar disponible.`}
          accion={confirmar.registro.activo ? 'Inactivar' : 'Activar'}
          peligro={confirmar.registro.activo}
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

function SedeFormulario({ sede, ciudades, cargandoCiudades, errorCiudades, onClose, onGuardado }: {
  sede: SedeAdmin | null
  ciudades: CiudadOpcion[]
  cargandoCiudades: boolean
  errorCiudades: unknown
  onClose: () => void
  onGuardado: (mensaje: string) => void
}) {
  const queryClient = useQueryClient()
  const [nombre, setNombre] = useState(sede?.nombre ?? '')
  const [idCiudad, setIdCiudad] = useState(sede ? String(sede.idCiudad) : '')
  const [direccion, setDireccion] = useState(sede?.direccion ?? '')
  const [telefono, setTelefono] = useState(sede?.telefono ?? '')
  const [correo, setCorreo] = useState(sede?.correo ?? '')
  const [errores, setErrores] = useState<{ nombre?: string; idCiudad?: string; correo?: string }>({})
  const ciudad = ciudades.find((c) => String(c.idCiudad) === idCiudad)
  const guardar = useMutation({
    mutationFn: () => {
      const datos = {
        nombre: nombre.trim(), idCiudad: Number(idCiudad), idZonaEmpresarial: sede?.idZonaEmpresarial ?? null,
        direccion: direccion.trim() || null, telefono: telefono.trim() || null, correo: correo.trim() || null,
      }
      return sede ? editarSede(sede.idSede, datos) : crearSede(datos)
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin-sedes'] })
      onGuardado(sede ? 'Sede actualizada.' : 'Sede creada.')
    },
  })

  function enviar(event: FormEvent) {
    event.preventDefault()
    const nuevos: typeof errores = {}
    if (!nombre.trim()) nuevos.nombre = 'El nombre de la sede es obligatorio.'
    if (!idCiudad) nuevos.idCiudad = 'Selecciona la ciudad de la sede.'
    if (correo.trim() && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(correo.trim())) nuevos.correo = 'El correo no tiene un formato válido.'
    setErrores(nuevos)
    if (Object.keys(nuevos).length === 0 && !guardar.isPending) guardar.mutate()
  }

  const sinCiudades = !cargandoCiudades && !errorCiudades && ciudades.length === 0

  return (
    <Dialogo titulo={sede ? 'Editar sede' : 'Nueva sede'} eyebrow="Sedes y áreas" icono="sede" ancho="amplio"
      onClose={onClose} onSubmit={enviar} pie={<BotonesFormulario onCancelar={onClose} pendiente={guardar.isPending} />}>
      <fieldset className="ad-grupo">
        <legend>Sede</legend>
        <div className="ad-rejilla">
          <Campo etiqueta="Nombre de la sede" error={errores.nombre} ancho="completo">
            <input required autoFocus maxLength={200} value={nombre} aria-invalid={Boolean(errores.nombre)} onChange={(e) => setNombre(e.target.value)} />
          </Campo>
          <Campo etiqueta="Ciudad" error={errores.idCiudad}>
            <Selector className="sel-campo" etiqueta="Ciudad" valor={idCiudad} invalido={Boolean(errores.idCiudad)}
              deshabilitado={cargandoCiudades || sinCiudades} onChange={setIdCiudad}
              buscable placeholderBusqueda="Buscar ciudad o departamento"
              opciones={[{ valor: '', texto: cargandoCiudades ? 'Cargando ciudades…' : 'Selecciona…', deshabilitada: true },
                ...ciudades.map((c) => ({ valor: String(c.idCiudad), texto: nombreCiudad(c) }))]} />
          </Campo>
          <Campo etiqueta="Zona horaria">
            <input value={ciudad?.zonaHoraria ?? sede?.zonaHoraria ?? ''} readOnly placeholder="Se toma de la ciudad" />
          </Campo>
        </div>
        {sinCiudades && <p className="ad-mensaje" role="alert">No hay ciudades activas registradas. Pide al administrador que cargue el catálogo de ciudades.</p>}
        {Boolean(errorCiudades) && <p className="ad-mensaje" role="alert">{mensajeError(errorCiudades, 'No se pudieron cargar las ciudades.')}</p>}
      </fieldset>

      <fieldset className="ad-grupo">
        <legend>Contacto</legend>
        <div className="ad-rejilla">
          <Campo etiqueta="Dirección (opcional)" ancho="completo">
            <input maxLength={500} value={direccion} onChange={(e) => setDireccion(e.target.value)} />
          </Campo>
          <Campo etiqueta="Teléfono (opcional)">
            <input type="tel" maxLength={30} value={telefono} onChange={(e) => setTelefono(e.target.value)} />
          </Campo>
          <Campo etiqueta="Correo electrónico (opcional)" error={errores.correo}>
            <input type="email" maxLength={320} value={correo} aria-invalid={Boolean(errores.correo)} onChange={(e) => setCorreo(e.target.value)} />
          </Campo>
        </div>
      </fieldset>
      {guardar.error && <p className="ad-mensaje" role="alert">{mensajeError(guardar.error, 'No se pudo guardar la sede.')}</p>}
    </Dialogo>
  )
}

function AreaFormulario({ area, sedes, onClose, onGuardado }: {
  area: AreaAdmin | null
  sedes: SedeAdmin[]
  onClose: () => void
  onGuardado: (mensaje: string) => void
}) {
  const queryClient = useQueryClient()
  const [nombre, setNombre] = useState(area?.nombre ?? '')
  const [idSede, setIdSede] = useState(area ? String(area.idSede) : '')
  const [descripcion, setDescripcion] = useState(area?.descripcion ?? '')
  const [comun, setComun] = useState(area?.esComunZonaEmpresarial ?? false)
  const [errores, setErrores] = useState<{ nombre?: string; idSede?: string }>({})
  const sede = sedes.find((s) => String(s.idSede) === idSede)
  const opcionesSede = sedes.filter((s) => s.activo || s.idSede === area?.idSede)
  const guardar = useMutation({
    mutationFn: () => {
      const datos = {
        nombre: nombre.trim(), idSede: Number(idSede), idAreaPadre: area?.idAreaPadre ?? null,
        descripcion: descripcion.trim() || null, esComunZonaEmpresarial: Boolean(sede?.idZonaEmpresarial) && comun,
      }
      return area ? editarArea(area.idArea, datos) : crearArea(datos)
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin-areas'] })
      onGuardado(area ? 'Área actualizada.' : 'Área creada.')
    },
  })

  function enviar(event: FormEvent) {
    event.preventDefault()
    const nuevos: typeof errores = {}
    if (!nombre.trim()) nuevos.nombre = 'El nombre del área es obligatorio.'
    if (!idSede) nuevos.idSede = 'Selecciona la sede del área.'
    setErrores(nuevos)
    if (Object.keys(nuevos).length === 0 && !guardar.isPending) guardar.mutate()
  }

  return (
    <Dialogo titulo={area ? 'Editar área' : 'Nueva área'} eyebrow="Sedes y áreas" icono="accesos"
      onClose={onClose} onSubmit={enviar} pie={<BotonesFormulario onCancelar={onClose} pendiente={guardar.isPending} />}>
      <div className="ad-rejilla">
        <Campo etiqueta="Sede" error={errores.idSede} ancho="completo">
          <Selector className="sel-campo" etiqueta="Sede" valor={idSede} invalido={Boolean(errores.idSede)} onChange={setIdSede}
            opciones={[{ valor: '', texto: 'Selecciona…', deshabilitada: true }, ...opcionesSede.map((s) => ({ valor: String(s.idSede), texto: s.nombre }))]} />
        </Campo>
        <Campo etiqueta="Nombre del área" error={errores.nombre} ancho="completo">
          <input required autoFocus maxLength={200} value={nombre} aria-invalid={Boolean(errores.nombre)} onChange={(e) => setNombre(e.target.value)} />
        </Campo>
        <Campo etiqueta="Descripción (opcional)" ancho="completo">
          <textarea rows={2} maxLength={2000} value={descripcion} onChange={(e) => setDescripcion(e.target.value)} />
        </Campo>
      </div>
      {sede?.idZonaEmpresarial && (
        <Interruptor etiqueta="Área común de la zona empresarial"
          ayuda={comun ? 'La zona empresarial administra los accesos de esta área.' : 'Los accesos los administra tu organización.'}
          checked={comun} onChange={setComun} />
      )}
      {guardar.error && <p className="ad-mensaje" role="alert">{mensajeError(guardar.error, 'No se pudo guardar el área.')}</p>}
    </Dialogo>
  )
}
