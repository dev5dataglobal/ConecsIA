import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent, type ReactNode } from 'react'
import { ApiError } from '../../shared/api/problemDetails'
import { Icono } from '../../shared/ui/Icono'
import { Selector } from '../../shared/ui/Selector'
import { useAcciones } from '../modulos/apiModulos'
import { leerOrganizacionActiva } from '../sesion/sesion'
import {
  BotonIcono, BotonNuevo, BotonesFormulario, Buscador, Campo, Confirmar, Dialogo, Estado, EstadoConsulta, Estatus, Filtro, Interruptor, Marco,
} from '../administracion/componentes'
import { ESTADOS, iniciales, mensajeError, useAviso, useDiferido, useUrlObjeto } from '../administracion/utilidades'
import '../administracion/administracion.css'
import './personas.css'
import { DispositivosPersona } from './DispositivosPersona'
import {
  agregarTelefono, agregarVehiculo, buscarPorDocumento, cambiarEstadoPersona, cambiarEstadoTelefono, cambiarEstadoTipoPersona, cambiarEstadoVehiculo, cargarFoto,
  consultarCatalogosPersona, consultarPersona, consultarPersonas, consultarTiposPersona, crearPersona, crearTipoPersona, descargarFoto,
  editarPersona, editarTelefono, editarTipoPersona, editarVehiculo, quitarFoto,
  type CatalogosPersona, type PersonaEntrada, type PersonaLista, type Telefono, type TelefonoEntrada, type TipoPersona, type VehiculoEntrada, type VehiculoPersona,
} from './apiPersonas'
import { normalizarDocumento, validarPersona, validarTelefono, validarVehiculo, type Errores } from './validacion'

const TIPOS_FOTO = ['image/png', 'image/jpeg', 'image/webp']
const MAX_FOTO = 2 * 1024 * 1024
const CLAVE = 'personas'

type Pestana = 'personas' | 'tipos'

export function PersonasPage() {
  const [pestana, setPestana] = useState<Pestana>('personas')
  const pestanas = (
    <div className="ad-tabs" role="tablist" aria-label="Secciones de personas">
      <button type="button" role="tab" aria-selected={pestana === 'personas'} onClick={() => setPestana('personas')}><Icono nombre="personas" /> Personas</button>
      <button type="button" role="tab" aria-selected={pestana === 'tipos'} onClick={() => setPestana('tipos')}><Icono nombre="configuracion" /> Tipos de persona</button>
    </div>
  )
  return pestana === 'personas' ? <ListaPersonas pestanas={pestanas} /> : <TiposPersonaSeccion pestanas={pestanas} />
}

function useCatalogos() {
  return useQuery({ queryKey: ['catalogos-persona', leerOrganizacionActiva()], queryFn: consultarCatalogosPersona, staleTime: 60_000 })
}

// ---------- Listado ----------

function ListaPersonas({ pestanas }: { pestanas: ReactNode }) {
  const acciones = useAcciones('personas')
  const [buscar, setBuscar] = useState('')
  const [estado, setEstado] = useState('')
  const [tipo, setTipo] = useState('')
  const [formulario, setFormulario] = useState<PersonaLista | 'nueva' | null>(null)
  const [detalle, setDetalle] = useState<number | null>(null)
  const [confirmar, setConfirmar] = useState<PersonaLista | null>(null)
  const { avisar, aviso } = useAviso()
  const queryClient = useQueryClient()
  const texto = useDiferido(buscar)
  const catalogos = useCatalogos()
  const personas = useQuery({
    queryKey: [CLAVE, leerOrganizacionActiva(), texto, estado, tipo],
    queryFn: () => consultarPersonas({ buscar: texto, estado, idTipoPersona: tipo }),
    enabled: acciones.consultar,
    placeholderData: keepPreviousData,
  })
  const estadoMutation = useMutation({
    mutationFn: (p: PersonaLista) => cambiarEstadoPersona(p.idPersona, !p.activo),
    onSuccess: (p) => {
      void queryClient.invalidateQueries({ queryKey: [CLAVE] })
      void queryClient.invalidateQueries({ queryKey: ['persona', p.idPersona] })
      setConfirmar(null)
      avisar(p.activo ? 'Persona activada.' : 'Persona inactivada. Su historial se conserva.')
    },
  })
  const filtrando = Boolean(buscar || estado || tipo)

  return (
    <Marco
      titulo="Personas"
      migas="Operación"
      descripcion="Registro de personas de la organización activa: identificación, contacto, teléfonos y vehículos."
      pestanas={pestanas}
      accion={acciones.anadir && <BotonNuevo texto="Nueva persona" onClick={() => setFormulario('nueva')} />}
    >
      <div className="ad-herramientas">
        <Buscador valor={buscar} onChange={setBuscar} etiqueta="Buscar personas" placeholder="Buscar por nombre, documento o correo" />
        <Filtro etiqueta="Filtrar por tipo de persona" valor={tipo} onChange={setTipo}
          opciones={[{ valor: '', texto: 'Todos los tipos' }, ...(catalogos.data?.tiposPersona ?? []).map((t) => ({ valor: String(t.id), texto: t.nombre }))]} />
        <Filtro etiqueta="Filtrar por estado" valor={estado} onChange={setEstado} opciones={ESTADOS} />
      </div>
      {!acciones.consultar ? (
        <Estado tipo="permiso" texto="Tu perfil no tiene permiso para consultar personas." />
      ) : (
        <EstadoConsulta loading={personas.isLoading} error={personas.error} empty={!personas.data?.length}
          vacio={filtrando ? 'Ninguna persona coincide con los filtros.' : 'Aún no hay personas registradas.'} />
      )}
      {personas.data && personas.data.length > 0 && (
        <div className="ad-tabla-wrap">
          <table className="ad-tabla">
            <thead>
              <tr>
                <th scope="col">Persona</th>
                <th scope="col">Documento</th>
                <th scope="col">Tipo</th>
                <th scope="col">Teléfono</th>
                <th scope="col">Estado</th>
                <th scope="col"><span className="sr-only">Acciones</span></th>
              </tr>
            </thead>
            <tbody>
              {personas.data.map((p) => (
                <tr key={p.idPersona}>
                  <td>
                    <button type="button" className="pe-nombre" onClick={() => setDetalle(p.idPersona)}>
                      <span className="ad-persona">
                        <Foto persona={p} />
                        <span>
                          <strong>{p.nombres} {p.apellidos}</strong>
                          <small>{p.correo ?? '—'}</small>
                        </span>
                      </span>
                    </button>
                  </td>
                  <td><span className="ad-etiqueta">{p.tipoDocumento}</span> {p.numeroDocumento}</td>
                  <td>{p.tipoPersona ?? '—'}</td>
                  <td>{p.telefonoPrincipal ?? <span className="ad-tenue">—</span>}</td>
                  <td><Estatus activo={p.activo} /></td>
                  <td className="ad-acciones">
                    <BotonIcono etiqueta={`Ver detalle de ${p.nombres} ${p.apellidos}`} icono="flecha" onClick={() => setDetalle(p.idPersona)} />
                    {acciones.editar && (
                      <>
                        <BotonIcono etiqueta={`Editar ${p.nombres} ${p.apellidos}`} icono="editar" onClick={() => setFormulario(p)} />
                        <BotonIcono etiqueta={`${p.activo ? 'Inactivar' : 'Activar'} ${p.nombres} ${p.apellidos}`} icono={p.activo ? 'cerrar' : 'check'}
                          peligro={p.activo} onClick={() => { estadoMutation.reset(); setConfirmar(p) }} />
                      </>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {formulario && (
        <PersonaFormulario
          persona={formulario === 'nueva' ? null : formulario}
          catalogos={catalogos.data}
          onClose={() => setFormulario(null)}
          onVerExistente={(id) => { setFormulario(null); setDetalle(id) }}
          onGuardado={(id, mensaje) => { setFormulario(null); avisar(mensaje); if (id) setDetalle(id) }}
        />
      )}
      {detalle !== null && <PersonaDetalleDialogo idPersona={detalle} catalogos={catalogos.data} onClose={() => setDetalle(null)} onEditar={(p) => { setDetalle(null); setFormulario(p) }} />}
      {confirmar && (
        <Confirmar
          titulo={confirmar.activo ? '¿Inactivar persona?' : '¿Activar persona?'}
          texto={confirmar.activo
            ? `${confirmar.nombres} ${confirmar.apellidos} no podrá usarse en nuevos registros. Sus autorizaciones, visitas, movimientos, teléfonos y vehículos se conservan.`
            : `${confirmar.nombres} ${confirmar.apellidos} volverá a estar disponible.`}
          accion={confirmar.activo ? 'Inactivar' : 'Activar'}
          peligro={confirmar.activo}
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

function Foto({ persona, grande }: { persona: { idPersona: number; tieneFoto: boolean; nombres: string; apellidos: string }; grande?: boolean }) {
  const foto = useQuery({
    queryKey: ['foto-persona', persona.idPersona],
    queryFn: () => descargarFoto(persona.idPersona),
    enabled: persona.tieneFoto,
    staleTime: 5 * 60_000,
  })
  const url = useUrlObjeto(persona.tieneFoto ? foto.data : null)
  const clase = grande ? 'ad-avatar pe-foto ad-logo-grande' : 'ad-avatar pe-foto'
  return url
    ? <img className={clase} src={url} alt={`Foto de ${persona.nombres} ${persona.apellidos}`} />
    : <span className={grande ? 'ad-avatar ad-logo-grande' : 'ad-avatar'} aria-hidden="true">{iniciales(`${persona.nombres} ${persona.apellidos}`)}</span>
}

// ---------- Formulario de persona ----------

const personaVacia = (): PersonaEntrada => ({ idTipoPersona: 0, idTipoDocumento: 0, numeroDocumento: '', nombres: '', apellidos: '', correo: '', empresaProcedencia: null, observacion: null })

function PersonaFormulario({ persona, catalogos, onClose, onGuardado, onVerExistente }: {
  persona: PersonaLista | null
  catalogos: CatalogosPersona | undefined
  onClose: () => void
  onGuardado: (idPersona: number | null, mensaje: string) => void
  onVerExistente: (idPersona: number) => void
}) {
  const queryClient = useQueryClient()
  const [datos, setDatos] = useState<PersonaEntrada>(persona ? {
    idTipoPersona: persona.idTipoPersona, idTipoDocumento: persona.idTipoDocumento, numeroDocumento: persona.numeroDocumento,
    nombres: persona.nombres, apellidos: persona.apellidos, correo: persona.correo ?? '', empresaProcedencia: persona.empresaProcedencia, observacion: persona.observacion,
  } : personaVacia())
  const [errores, setErrores] = useState<Errores<PersonaEntrada>>({})
  const [existente, setExistente] = useState<PersonaLista | null>(null)
  const [foto, setFoto] = useState<File | null>(null)
  const [quitar, setQuitar] = useState(false)
  const [errorFoto, setErrorFoto] = useState<string | null>(null)
  const vista = useUrlObjeto(foto)
  const cambiar = (parcial: Partial<PersonaEntrada>) => setDatos((d) => ({ ...d, ...parcial }))

  // «Buscar primero»: al terminar de escribir el documento se consulta si ya existe en la organización.
  async function comprobarDocumento() {
    setExistente(null)
    const numero = normalizarDocumento(datos.numeroDocumento)
    if (persona || !datos.idTipoDocumento || !numero) return
    try {
      setExistente(await buscarPorDocumento(datos.idTipoDocumento, numero))
    } catch {
      // 404 = no existe: se puede registrar.
    }
  }

  const guardar = useMutation({
    mutationFn: async () => {
      const entrada: PersonaEntrada = {
        ...datos, numeroDocumento: normalizarDocumento(datos.numeroDocumento), nombres: datos.nombres.trim(), apellidos: datos.apellidos.trim(), correo: datos.correo.trim(),
        empresaProcedencia: datos.empresaProcedencia?.trim() || null, observacion: datos.observacion?.trim() || null,
      }
      let guardada = persona ? await editarPersona(persona.idPersona, entrada) : await crearPersona(entrada)
      // La foto se envía después de guardar los datos: si falla, la persona ya quedó registrada.
      if (foto) guardada = await cargarFoto(guardada.idPersona, foto)
      else if (quitar && guardada.tieneFoto) guardada = await quitarFoto(guardada.idPersona)
      return guardada
    },
    onSuccess: (guardada) => {
      void queryClient.invalidateQueries({ queryKey: [CLAVE] })
      void queryClient.invalidateQueries({ queryKey: ['persona', guardada.idPersona] })
      void queryClient.invalidateQueries({ queryKey: ['foto-persona', guardada.idPersona] })
      onGuardado(persona ? null : guardada.idPersona, persona ? 'Persona actualizada.' : 'Persona registrada. Ahora puedes agregar teléfonos y vehículos.')
    },
    onError: () => void queryClient.invalidateQueries({ queryKey: [CLAVE] }),
  })

  function elegirFoto(archivo: File | undefined) {
    setErrorFoto(null)
    if (!archivo) return
    if (!TIPOS_FOTO.includes(archivo.type)) { setErrorFoto('La foto debe ser PNG, JPEG o WebP.'); return }
    if (archivo.size > MAX_FOTO) { setErrorFoto('La foto no puede superar 2 MB.'); return }
    setFoto(archivo)
    setQuitar(false)
  }

  function enviar(event: FormEvent) {
    event.preventDefault()
    const nuevos = validarPersona(datos, catalogos?.tiposDocumento ?? [])
    setErrores(nuevos)
    if (Object.keys(nuevos).length === 0 && !errorFoto && !guardar.isPending) guardar.mutate()
  }

  const tiposPersona = catalogos?.tiposPersona ?? []
  // Un tipo inactivo que la persona ya tiene se sigue mostrando para no perder la referencia.
  const opcionesTipo = persona && !tiposPersona.some((t) => t.id === persona.idTipoPersona)
    ? [...tiposPersona, { id: persona.idTipoPersona, nombre: `${persona.tipoPersona ?? 'Tipo'} (inactivo)` }]
    : tiposPersona

  return (
    <Dialogo titulo={persona ? 'Editar persona' : 'Nueva persona'} eyebrow="Personas" icono="personas" ancho="amplio" onClose={onClose} onSubmit={enviar}
      pie={<BotonesFormulario onCancelar={onClose} pendiente={guardar.isPending} />}>
      <div className="ad-logo-campo">
        {vista ? <img className="ad-avatar pe-foto ad-logo-grande" src={vista} alt="Vista previa de la foto" />
          : persona && !quitar ? <Foto persona={persona} grande />
          : <span className="ad-avatar ad-logo-grande" aria-hidden="true"><Icono nombre="imagen" /></span>}
        <div>
          <strong>Foto (opcional)</strong>
          <small>PNG, JPEG o WebP de hasta 2 MB. Se guarda en almacenamiento privado.</small>
          <div className="ad-logo-botones">
            <label className="ad-enlace">
              <input type="file" accept={TIPOS_FOTO.join(',')} className="sr-only" aria-label="Seleccionar foto"
                onChange={(e) => { elegirFoto(e.target.files?.[0]); e.target.value = '' }} />
              {foto || (persona?.tieneFoto && !quitar) ? 'Cambiar' : 'Cargar'}
            </label>
            {/* Descartar la foto elegida conserva la actual; «Quitar» solo aplica a la foto ya guardada. */}
            {foto && <button type="button" className="ad-enlace" onClick={() => { setFoto(null); setErrorFoto(null) }}>Descartar selección</button>}
            {!foto && persona?.tieneFoto && !quitar && <button type="button" className="ad-enlace" onClick={() => setQuitar(true)}>Quitar</button>}
            {!foto && persona?.tieneFoto && quitar && <button type="button" className="ad-enlace" onClick={() => setQuitar(false)}>Conservar foto actual</button>}
          </div>
          {errorFoto && <small className="ad-campo-error" role="alert">{errorFoto}</small>}
        </div>
      </div>
      {!catalogos && <p className="ad-mensaje" role="status">Cargando catálogos…</p>}
      <div className="ad-rejilla">
        <Campo etiqueta="Tipo de documento" error={errores.idTipoDocumento}>
          <Selector className="sel-campo" etiqueta="Tipo de documento" valor={String(datos.idTipoDocumento || '')} invalido={Boolean(errores.idTipoDocumento)}
            onChange={(v) => { cambiar({ idTipoDocumento: Number(v) }); setExistente(null) }}
            opciones={[{ valor: '', texto: 'Selecciona…', deshabilitada: true }, ...(catalogos?.tiposDocumento ?? []).map((t) => ({ valor: String(t.id), texto: `${t.codigo} · ${t.nombre}` }))]} />
        </Campo>
        <Campo etiqueta="Número de documento" error={errores.numeroDocumento}>
          <input required value={datos.numeroDocumento} maxLength={30} autoComplete="off" aria-invalid={Boolean(errores.numeroDocumento)}
            onChange={(e) => cambiar({ numeroDocumento: e.target.value })} onBlur={() => void comprobarDocumento()} />
        </Campo>
        {existente && (
          <div className="ad-mensaje pe-existente" data-ancho="completo" role="alert">
            Ya existe {existente.nombres} {existente.apellidos} con este documento en la organización{existente.activo ? '' : ' (inactiva)'}.
            <button type="button" className="ad-enlace" onClick={() => onVerExistente(existente.idPersona)}>Ver registro</button>
          </div>
        )}
        <Campo etiqueta="Nombres" error={errores.nombres}>
          <input required value={datos.nombres} maxLength={100} autoComplete="off" aria-invalid={Boolean(errores.nombres)} onChange={(e) => cambiar({ nombres: e.target.value })} />
        </Campo>
        <Campo etiqueta="Apellidos" error={errores.apellidos}>
          <input required value={datos.apellidos} maxLength={100} autoComplete="off" aria-invalid={Boolean(errores.apellidos)} onChange={(e) => cambiar({ apellidos: e.target.value })} />
        </Campo>
        <Campo etiqueta="Tipo de persona" error={errores.idTipoPersona}>
          <Selector className="sel-campo" etiqueta="Tipo de persona" valor={String(datos.idTipoPersona || '')} invalido={Boolean(errores.idTipoPersona)}
            onChange={(v) => cambiar({ idTipoPersona: Number(v) })}
            opciones={[{ valor: '', texto: 'Selecciona…', deshabilitada: true }, ...opcionesTipo.map((t) => ({ valor: String(t.id), texto: t.nombre }))]} />
        </Campo>
        <Campo etiqueta="Correo electrónico" error={errores.correo}>
          <input required type="email" value={datos.correo} maxLength={320} aria-invalid={Boolean(errores.correo)} onChange={(e) => cambiar({ correo: e.target.value })} />
        </Campo>
        <Campo etiqueta="Empresa de procedencia (opcional)" ancho="completo">
          <input value={datos.empresaProcedencia ?? ''} maxLength={200} onChange={(e) => cambiar({ empresaProcedencia: e.target.value })} />
        </Campo>
        <Campo etiqueta="Observación (opcional)" ancho="completo">
          <textarea value={datos.observacion ?? ''} maxLength={1000} onChange={(e) => cambiar({ observacion: e.target.value })} />
        </Campo>
      </div>
      {guardar.error && <p className="ad-mensaje" role="alert">{mensajeError(guardar.error, 'No se pudo guardar la persona.')}</p>}
    </Dialogo>
  )
}

// ---------- Detalle: teléfonos y vehículos ----------

function PersonaDetalleDialogo({ idPersona, catalogos, onClose, onEditar }: {
  idPersona: number
  catalogos: CatalogosPersona | undefined
  onClose: () => void
  onEditar: (persona: PersonaLista) => void
}) {
  const acciones = useAcciones('personas')
  const vehiculos = useAcciones('vehiculos')
  const dispositivos = useAcciones('dispositivos')
  const detalle = useQuery({ queryKey: ['persona', idPersona], queryFn: () => consultarPersona(idPersona) })
  const p = detalle.data
  const nombre = p ? `${p.nombres} ${p.apellidos}` : 'Persona'

  return (
    <Dialogo titulo={nombre} eyebrow="Detalle de persona" icono="personas" ancho="matriz" onClose={onClose} onSubmit={(e) => e.preventDefault()}
      pie={(
        <>
          <button type="button" className="ad-boton-sec" onClick={onClose}>Cerrar</button>
          {p && acciones.editar && <button type="button" className="ad-boton" onClick={() => onEditar({ ...p, telefonoPrincipal: null })}>Editar datos</button>}
        </>
      )}>
      <EstadoConsulta loading={detalle.isLoading} error={detalle.error} empty={false} vacio="" />
      {p && (
        <>
          <div className="pe-resumen">
            <Foto persona={p} grande />
            <dl>
              <div><dt>Documento</dt><dd>{p.tipoDocumento} {p.numeroDocumento}</dd></div>
              <div><dt>Tipo</dt><dd>{p.tipoPersona ?? '—'}</dd></div>
              <div><dt>Correo</dt><dd>{p.correo ?? '—'}</dd></div>
              <div><dt>Empresa</dt><dd>{p.empresaProcedencia ?? '—'}</dd></div>
              <div><dt>Estado</dt><dd><Estatus activo={p.activo} /></dd></div>
              <div><dt>Registrado por</dt><dd>{p.registradoPor ?? '—'} · {new Date(p.creadoEn).toLocaleString('es-CO', { dateStyle: 'medium', timeStyle: 'short' })}</dd></div>
              {p.observacion && <div data-ancho="completo"><dt>Observación</dt><dd>{p.observacion}</dd></div>}
            </dl>
          </div>
          <TelefonosPersona idPersona={idPersona} nombre={nombre} telefonos={p.telefonos} tipos={catalogos?.tiposTelefono ?? []} editable={acciones.editar} />
          {vehiculos.consultar || vehiculos.anadir || vehiculos.editar ? (
            <VehiculosPersona idPersona={idPersona} nombre={nombre} vehiculos={p.vehiculos} catalogos={catalogos}
              puedeAnadir={acciones.editar && vehiculos.anadir} puedeEditar={acciones.editar && vehiculos.editar} />
          ) : (
            <section className="pe-seccion"><h3>Vehículos</h3><p className="ad-tenue">El módulo de vehículos no está habilitado para tu perfil.</p></section>
          )}
          {dispositivos.consultar ? (
            <DispositivosPersona idPersona={idPersona} nombre={nombre}
              puedeAnadir={acciones.editar && dispositivos.anadir} puedeEditar={acciones.editar && dispositivos.editar} />
          ) : (
            <section className="pe-seccion"><h3>Dispositivos</h3><p className="ad-tenue">El módulo de dispositivos no está habilitado para tu perfil.</p></section>
          )}
        </>
      )}
    </Dialogo>
  )
}

const telefonoVacio = (): TelefonoEntrada => ({ codigoPais: '+57', numero: '', tipo: 'Móvil', principal: false })

function TelefonosPersona({ idPersona, nombre, telefonos, tipos, editable }: { idPersona: number; nombre: string; telefonos: Telefono[]; tipos: string[]; editable: boolean }) {
  const queryClient = useQueryClient()
  const [edicion, setEdicion] = useState<Telefono | 'nuevo' | null>(null)
  const [datos, setDatos] = useState<TelefonoEntrada>(telefonoVacio())
  const [errores, setErrores] = useState<Errores<TelefonoEntrada>>({})
  const [confirmar, setConfirmar] = useState<Telefono | null>(null)
  const refrescar = () => { void queryClient.invalidateQueries({ queryKey: ['persona', idPersona] }); void queryClient.invalidateQueries({ queryKey: [CLAVE] }) }

  const guardar = useMutation({
    mutationFn: () => edicion && edicion !== 'nuevo' ? editarTelefono(idPersona, edicion.idTelefono, datos) : agregarTelefono(idPersona, datos),
    onSuccess: () => { refrescar(); setEdicion(null) },
  })
  const estado = useMutation({
    mutationFn: (t: Telefono) => cambiarEstadoTelefono(idPersona, t.idTelefono, !t.activo),
    onSuccess: () => { refrescar(); setConfirmar(null) },
  })

  function abrir(t: Telefono | 'nuevo') {
    guardar.reset()
    setErrores({})
    setDatos(t === 'nuevo' ? { ...telefonoVacio(), principal: !telefonos.some((x) => x.activo) } : { codigoPais: t.codigoPais, numero: t.numero, tipo: t.tipo ?? 'Otro', principal: t.principal })
    setEdicion(t)
  }
  function enviar() {
    const nuevos = validarTelefono(datos)
    setErrores(nuevos)
    if (Object.keys(nuevos).length === 0 && !guardar.isPending) guardar.mutate()
  }

  return (
    <section className="pe-seccion" aria-label={`Teléfonos de ${nombre}`}>
      <header>
        <h3>Teléfonos <small>de {nombre}</small></h3>
        {editable && edicion === null && <button type="button" className="ad-enlace" onClick={() => abrir('nuevo')}>+ Agregar teléfono</button>}
      </header>
      {telefonos.length === 0 && edicion === null && <p className="ad-tenue">Sin teléfonos registrados.</p>}
      {telefonos.length > 0 && (
        <ul className="pe-lista">
          {telefonos.map((t) => (
            <li key={t.idTelefono} data-inactivo={!t.activo || undefined}>
              <span>
                <strong>{t.codigoPais} {t.numero}</strong>
                <small>{t.tipo}{t.principal ? ' · Principal' : ''}</small>
              </span>
              {!t.activo && <Estatus activo={false} texto="Quitado" />}
              {editable && (
                <span>
                  {t.activo && <BotonIcono etiqueta={`Editar teléfono ${t.numero}`} icono="editar" onClick={() => abrir(t)} />}
                  <BotonIcono etiqueta={`${t.activo ? 'Quitar' : 'Restaurar'} teléfono ${t.numero}`} icono={t.activo ? 'cerrar' : 'check'} peligro={t.activo}
                    onClick={() => { estado.reset(); setConfirmar(t) }} />
                </span>
              )}
            </li>
          ))}
        </ul>
      )}
      {edicion !== null && (
        <div className="pe-subformulario" role="group" aria-label={edicion === 'nuevo' ? 'Nuevo teléfono' : 'Editar teléfono'}
          onKeyDown={(e) => { if (e.key === 'Enter' && (e.target as HTMLElement).tagName === 'INPUT') { e.preventDefault(); enviar() } }}>
          <div className="pe-rejilla-telefono">
            <Campo etiqueta="Indicativo" error={errores.codigoPais}>
              <input value={datos.codigoPais} maxLength={5} inputMode="tel" aria-invalid={Boolean(errores.codigoPais)} onChange={(e) => setDatos({ ...datos, codigoPais: e.target.value.trim() })} />
            </Campo>
            <Campo etiqueta="Número" error={errores.numero}>
              <input value={datos.numero} maxLength={30} inputMode="tel" autoFocus aria-invalid={Boolean(errores.numero)} onChange={(e) => setDatos({ ...datos, numero: e.target.value })} />
            </Campo>
            <Campo etiqueta="Tipo" error={errores.tipo}>
              <Selector className="sel-campo" etiqueta="Tipo de teléfono" valor={datos.tipo} onChange={(v) => setDatos({ ...datos, tipo: v })}
                opciones={tipos.map((t) => ({ valor: t, texto: t }))} />
            </Campo>
          </div>
          <Interruptor etiqueta="Teléfono principal" ayuda="Solo uno por persona; marcarlo desmarca el anterior." checked={datos.principal} onChange={(v) => setDatos({ ...datos, principal: v })} />
          {guardar.error && <p className="ad-mensaje" role="alert">{mensajeError(guardar.error, 'No se pudo guardar el teléfono.')}</p>}
          <div className="pe-subacciones">
            <button type="button" className="ad-boton-sec" onClick={() => setEdicion(null)}>Cancelar</button>
            <button type="button" className="ad-boton" onClick={enviar} disabled={guardar.isPending}>{guardar.isPending ? 'Guardando…' : 'Guardar teléfono'}</button>
          </div>
        </div>
      )}
      {confirmar && (
        <Confirmar titulo={confirmar.activo ? '¿Quitar teléfono?' : '¿Restaurar teléfono?'}
          texto={confirmar.activo ? `El teléfono ${confirmar.codigoPais} ${confirmar.numero} dejará de estar activo para ${nombre}. El registro se conserva.` : `El teléfono volverá a estar activo para ${nombre}.`}
          accion={confirmar.activo ? 'Quitar' : 'Restaurar'} peligro={confirmar.activo} pendiente={estado.isPending}
          error={estado.error ? mensajeError(estado.error) : null} onConfirmar={() => estado.mutate(confirmar)} onCancelar={() => setConfirmar(null)} />
      )}
    </section>
  )
}

const vehiculoVacio = (): VehiculoEntrada => ({ placa: '', tipo: 'Automóvil', marca: null, modelo: null, color: null, relacion: 'Responsable' })

function VehiculosPersona({ idPersona, nombre, vehiculos, catalogos, puedeAnadir, puedeEditar }: {
  idPersona: number; nombre: string; vehiculos: VehiculoPersona[]; catalogos: CatalogosPersona | undefined; puedeAnadir: boolean; puedeEditar: boolean
}) {
  const queryClient = useQueryClient()
  const [edicion, setEdicion] = useState<VehiculoPersona | 'nuevo' | null>(null)
  const [datos, setDatos] = useState<VehiculoEntrada>(vehiculoVacio())
  const [errores, setErrores] = useState<Errores<VehiculoEntrada>>({})
  const [confirmar, setConfirmar] = useState<VehiculoPersona | null>(null)
  const refrescar = () => void queryClient.invalidateQueries({ queryKey: ['persona', idPersona] })

  const guardar = useMutation({
    mutationFn: () => edicion && edicion !== 'nuevo' ? editarVehiculo(idPersona, edicion.idVehiculo, datos) : agregarVehiculo(idPersona, datos),
    onSuccess: () => { refrescar(); setEdicion(null) },
  })
  const estado = useMutation({
    mutationFn: (v: VehiculoPersona) => cambiarEstadoVehiculo(idPersona, v.idVehiculo, !v.activo),
    onSuccess: () => { refrescar(); setConfirmar(null) },
  })

  function abrir(v: VehiculoPersona | 'nuevo') {
    guardar.reset()
    setErrores({})
    setDatos(v === 'nuevo' ? vehiculoVacio() : { placa: v.placa, tipo: v.tipo ?? 'Otro', marca: v.marca, modelo: v.modelo, color: v.color, relacion: v.relacion })
    setEdicion(v)
  }
  function enviar() {
    const nuevos = validarVehiculo(datos)
    setErrores(nuevos)
    if (Object.keys(nuevos).length === 0 && !guardar.isPending) guardar.mutate()
  }
  const texto = (campo: 'marca' | 'modelo' | 'color', valor: string) => setDatos({ ...datos, [campo]: valor || null })

  return (
    <section className="pe-seccion" aria-label={`Vehículos de ${nombre}`}>
      <header>
        <h3>Vehículos <small>de {nombre}</small></h3>
        {puedeAnadir && edicion === null && <button type="button" className="ad-enlace" onClick={() => abrir('nuevo')}>+ Asociar vehículo</button>}
      </header>
      {vehiculos.length === 0 && edicion === null && <p className="ad-tenue">Sin vehículos asociados.</p>}
      {vehiculos.length > 0 && (
        <ul className="pe-lista">
          {vehiculos.map((v) => (
            <li key={v.idVehiculo} data-inactivo={!v.activo || undefined}>
              <span>
                <strong className="pe-placa">{v.placa}</strong>
                <small>{[v.tipo, v.marca, v.modelo, v.color].filter(Boolean).join(' · ')} · {v.relacion}{v.otrasPersonas > 0 ? ` · compartido con ${v.otrasPersonas} persona(s) más` : ''}</small>
              </span>
              {!v.activo && <Estatus activo={false} texto="Desasociado" />}
              {puedeEditar && (
                <span>
                  {v.activo && <BotonIcono etiqueta={`Editar vehículo ${v.placa}`} icono="editar" onClick={() => abrir(v)} />}
                  <BotonIcono etiqueta={`${v.activo ? 'Desasociar' : 'Volver a asociar'} vehículo ${v.placa}`} icono={v.activo ? 'cerrar' : 'check'} peligro={v.activo}
                    onClick={() => { estado.reset(); setConfirmar(v) }} />
                </span>
              )}
            </li>
          ))}
        </ul>
      )}
      {edicion !== null && (
        <div className="pe-subformulario" role="group" aria-label={edicion === 'nuevo' ? 'Asociar vehículo' : 'Editar vehículo'}
          onKeyDown={(e) => { if (e.key === 'Enter' && (e.target as HTMLElement).tagName === 'INPUT') { e.preventDefault(); enviar() } }}>
          {edicion === 'nuevo' && <p className="ad-tenue pe-nota">Si la placa ya existe en la organización se asocia el mismo vehículo sin cambiar sus datos.</p>}
          {edicion !== 'nuevo' && edicion.otrasPersonas > 0 && <p className="ad-tenue pe-nota">Los datos del vehículo se actualizan también para las otras personas asociadas.</p>}
          <div className="ad-rejilla">
            <Campo etiqueta="Placa" error={errores.placa}>
              <input value={datos.placa} maxLength={15} autoFocus autoComplete="off" aria-invalid={Boolean(errores.placa)} onChange={(e) => setDatos({ ...datos, placa: e.target.value.toUpperCase() })} />
            </Campo>
            <Campo etiqueta="Tipo de vehículo" error={errores.tipo}>
              <Selector className="sel-campo" etiqueta="Tipo de vehículo" valor={datos.tipo} onChange={(v) => setDatos({ ...datos, tipo: v })}
                opciones={(catalogos?.tiposVehiculo ?? []).map((t) => ({ valor: t, texto: t }))} />
            </Campo>
            <Campo etiqueta="Marca (opcional)"><input value={datos.marca ?? ''} maxLength={60} onChange={(e) => texto('marca', e.target.value)} /></Campo>
            <Campo etiqueta="Modelo (opcional)"><input value={datos.modelo ?? ''} maxLength={60} onChange={(e) => texto('modelo', e.target.value)} /></Campo>
            <Campo etiqueta="Color (opcional)"><input value={datos.color ?? ''} maxLength={40} onChange={(e) => texto('color', e.target.value)} /></Campo>
            <Campo etiqueta="Relación con la persona">
              <Selector className="sel-campo" etiqueta="Relación con la persona" valor={datos.relacion} onChange={(v) => setDatos({ ...datos, relacion: v })}
                opciones={(catalogos?.relacionesVehiculo ?? []).map((t) => ({ valor: t, texto: t }))} />
            </Campo>
          </div>
          {guardar.error && <p className="ad-mensaje" role="alert">{mensajeError(guardar.error, 'No se pudo guardar el vehículo.')}</p>}
          <div className="pe-subacciones">
            <button type="button" className="ad-boton-sec" onClick={() => setEdicion(null)}>Cancelar</button>
            <button type="button" className="ad-boton" onClick={enviar} disabled={guardar.isPending}>{guardar.isPending ? 'Guardando…' : 'Guardar vehículo'}</button>
          </div>
        </div>
      )}
      {confirmar && (
        <Confirmar titulo={confirmar.activo ? '¿Desasociar vehículo?' : '¿Volver a asociar vehículo?'}
          texto={confirmar.activo ? `El vehículo ${confirmar.placa} dejará de estar asociado a ${nombre}. El vehículo, sus otras asociaciones y su historial se conservan.` : `El vehículo ${confirmar.placa} volverá a asociarse a ${nombre}.`}
          accion={confirmar.activo ? 'Desasociar' : 'Asociar'} peligro={confirmar.activo} pendiente={estado.isPending}
          error={estado.error ? mensajeError(estado.error) : null} onConfirmar={() => estado.mutate(confirmar)} onCancelar={() => setConfirmar(null)} />
      )}
    </section>
  )
}

// ---------- Catálogo: tipos de persona ----------

function TiposPersonaSeccion({ pestanas }: { pestanas: ReactNode }) {
  const acciones = useAcciones('personas')
  const queryClient = useQueryClient()
  const { avisar, aviso } = useAviso()
  const [formulario, setFormulario] = useState<TipoPersona | 'nuevo' | null>(null)
  const [confirmar, setConfirmar] = useState<TipoPersona | null>(null)
  const tipos = useQuery({ queryKey: ['tipos-persona', leerOrganizacionActiva()], queryFn: consultarTiposPersona, enabled: acciones.consultar })
  const refrescar = () => { void queryClient.invalidateQueries({ queryKey: ['tipos-persona'] }); void queryClient.invalidateQueries({ queryKey: ['catalogos-persona'] }) }
  const estado = useMutation({
    mutationFn: (t: TipoPersona) => cambiarEstadoTipoPersona(t.idTipoPersona, !t.activo),
    onSuccess: (t) => { refrescar(); setConfirmar(null); avisar(t.activo ? 'Tipo activado.' : 'Tipo inactivado. Las personas que lo usan lo conservan.') },
  })

  return (
    <Marco titulo="Tipos de persona" migas="Operación" descripcion="Clasificación de personas de tu organización. Inactivar un tipo no cambia a quienes ya lo tienen."
      pestanas={pestanas} accion={acciones.anadir && <BotonNuevo texto="Nuevo tipo" onClick={() => setFormulario('nuevo')} />}>
      {!acciones.consultar ? <Estado tipo="permiso" texto="Tu perfil no tiene permiso para consultar tipos de persona." />
        : <EstadoConsulta loading={tipos.isLoading} error={tipos.error} empty={!tipos.data?.length} vacio="No hay tipos de persona configurados." />}
      {tipos.data && tipos.data.length > 0 && (
        <div className="ad-tabla-wrap">
          <table className="ad-tabla">
            <thead><tr><th scope="col">Tipo</th><th scope="col">Descripción</th><th scope="col">Estado</th>{acciones.editar && <th scope="col"><span className="sr-only">Acciones</span></th>}</tr></thead>
            <tbody>
              {tipos.data.map((t) => (
                <tr key={t.idTipoPersona}>
                  <td><strong>{t.nombre}</strong></td>
                  <td>{t.descripcion ?? <span className="ad-tenue">—</span>}</td>
                  <td><Estatus activo={t.activo} /></td>
                  {acciones.editar && (
                    <td className="ad-acciones">
                      <BotonIcono etiqueta={`Editar ${t.nombre}`} icono="editar" onClick={() => setFormulario(t)} />
                      <BotonIcono etiqueta={`${t.activo ? 'Inactivar' : 'Activar'} ${t.nombre}`} icono={t.activo ? 'cerrar' : 'check'} peligro={t.activo} onClick={() => { estado.reset(); setConfirmar(t) }} />
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      {formulario && <TipoFormulario tipo={formulario === 'nuevo' ? null : formulario} onClose={() => setFormulario(null)} onGuardado={(m) => { refrescar(); setFormulario(null); avisar(m) }} />}
      {confirmar && (
        <Confirmar titulo={confirmar.activo ? '¿Inactivar tipo?' : '¿Activar tipo?'}
          texto={confirmar.activo ? `«${confirmar.nombre}» no podrá asignarse a personas nuevas. Quienes ya lo tienen lo conservan.` : `«${confirmar.nombre}» volverá a estar disponible.`}
          accion={confirmar.activo ? 'Inactivar' : 'Activar'} peligro={confirmar.activo} pendiente={estado.isPending}
          error={estado.error ? mensajeError(estado.error) : null} onConfirmar={() => estado.mutate(confirmar)} onCancelar={() => setConfirmar(null)} />
      )}
      {aviso}
    </Marco>
  )
}

function TipoFormulario({ tipo, onClose, onGuardado }: { tipo: TipoPersona | null; onClose: () => void; onGuardado: (mensaje: string) => void }) {
  const [nombre, setNombre] = useState(tipo?.nombre ?? '')
  const [descripcion, setDescripcion] = useState(tipo?.descripcion ?? '')
  const [error, setError] = useState<string | undefined>()
  const guardar = useMutation({
    mutationFn: () => {
      const datos = { nombre: nombre.trim(), descripcion: descripcion.trim() || null }
      return tipo ? editarTipoPersona(tipo.idTipoPersona, datos) : crearTipoPersona(datos)
    },
    onSuccess: () => onGuardado(tipo ? 'Tipo actualizado.' : 'Tipo creado.'),
  })
  function enviar(event: FormEvent) {
    event.preventDefault()
    const falta = nombre.trim() ? undefined : 'El nombre es obligatorio.'
    setError(falta)
    if (!falta && !guardar.isPending) guardar.mutate()
  }
  return (
    <Dialogo titulo={tipo ? 'Editar tipo de persona' : 'Nuevo tipo de persona'} eyebrow="Tipos de persona" icono="configuracion" onClose={onClose} onSubmit={enviar}
      pie={<BotonesFormulario onCancelar={onClose} pendiente={guardar.isPending} />}>
      <Campo etiqueta="Nombre" error={error}><input required value={nombre} maxLength={80} aria-invalid={Boolean(error)} onChange={(e) => setNombre(e.target.value)} /></Campo>
      <Campo etiqueta="Descripción (opcional)"><textarea value={descripcion} maxLength={300} onChange={(e) => setDescripcion(e.target.value)} /></Campo>
      {guardar.error && <p className="ad-mensaje" role="alert">{guardar.error instanceof ApiError && guardar.error.status === 409 ? 'Ya existe un tipo con ese nombre.' : mensajeError(guardar.error)}</p>}
    </Dialogo>
  )
}
