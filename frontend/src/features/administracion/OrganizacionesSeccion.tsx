import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { Icono } from '../../shared/ui/Icono'
import { Selector } from '../../shared/ui/Selector'
import { useAcciones } from '../modulos/apiModulos'
import { esAdministradorGeneral, leerOrganizacionActiva } from '../sesion/sesion'
import {
  cambiarEstadoOrganizacion,
  cargarLogo,
  consultarCatalogosOrganizacion,
  consultarOrganizaciones,
  crearOrganizacion,
  descargarLogo,
  editarOrganizacion,
  quitarLogo,
  type OrganizacionAdmin,
  type OrganizacionEntrada,
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
import type { SeccionProps } from './secciones'
import { limpiarOrganizacion, organizacionAEntrada, organizacionVacia, validarOrganizacion } from './organizacion'

const ESTADOS_ORG = [
  { valor: '', texto: 'Todos los estados' },
  { valor: 'Activa', texto: 'Activas' },
  { valor: 'Inactiva', texto: 'Inactivas' },
]
const TIPOS_LOGO = ['image/png', 'image/jpeg', 'image/webp']
const MAX_LOGO = 2 * 1024 * 1024

export function OrganizacionesSeccion({ pestanas }: SeccionProps) {
  const acciones = useAcciones('organizaciones')
  const plataforma = esAdministradorGeneral()
  const [buscar, setBuscar] = useState('')
  const [estado, setEstado] = useState('')
  const [formulario, setFormulario] = useState<OrganizacionAdmin | 'nueva' | null>(null)
  const [confirmar, setConfirmar] = useState<OrganizacionAdmin | null>(null)
  const { avisar, aviso } = useAviso()
  const queryClient = useQueryClient()
  const texto = useDiferido(buscar)
  const organizaciones = useQuery({
    queryKey: ['admin-organizaciones', leerOrganizacionActiva(), texto, estado],
    queryFn: () => consultarOrganizaciones({ buscar: texto, estado }),
    enabled: acciones.consultar,
    placeholderData: keepPreviousData,
  })
  const estadoMutation = useMutation({
    mutationFn: (o: OrganizacionAdmin) => cambiarEstadoOrganizacion(o.idOrganizacion, o.estado !== 'Activa'),
    onSuccess: (o) => {
      void queryClient.invalidateQueries({ queryKey: ['admin-organizaciones'] })
      void queryClient.invalidateQueries({ queryKey: ['organizaciones-selector'] })
      setConfirmar(null)
      avisar(o.estado === 'Activa' ? 'Organización activada.' : 'Organización inactivada. Sus usuarios ya no pueden ingresar.')
    },
  })

  return (
    <Marco
      titulo="Organizaciones"
      descripcion={plataforma ? 'Organizaciones clientes de la plataforma.' : 'Datos de tu organización.'}
      pestanas={pestanas}
      accion={acciones.anadir && plataforma && <BotonNuevo texto="Nueva organización" onClick={() => setFormulario('nueva')} />}
    >
      {plataforma && (
        <div className="ad-herramientas">
          <Buscador valor={buscar} onChange={setBuscar} etiqueta="Buscar organizaciones" placeholder="Buscar por nombre, identificación o correo" />
          <Filtro etiqueta="Filtrar por estado" valor={estado} onChange={setEstado} opciones={ESTADOS_ORG} />
        </div>
      )}
      {!acciones.consultar ? (
        <Estado tipo="permiso" texto="Tu perfil no tiene permiso para consultar organizaciones." />
      ) : (
        <EstadoConsulta loading={organizaciones.isLoading} error={organizaciones.error} empty={!organizaciones.data?.length}
          vacio={buscar || estado ? 'Ninguna organización coincide con los filtros.' : 'No hay organizaciones registradas.'} />
      )}
      {organizaciones.data && organizaciones.data.length > 0 && (
        <div className="ad-tabla-wrap">
          <table className="ad-tabla">
            <thead>
              <tr>
                <th scope="col">Organización</th>
                <th scope="col">Identificación</th>
                <th scope="col">Organización superior</th>
                <th scope="col">Estado</th>
                {acciones.editar && <th scope="col"><span className="sr-only">Acciones</span></th>}
              </tr>
            </thead>
            <tbody>
              {organizaciones.data.map((org) => (
                <tr key={org.idOrganizacion}>
                  <td>
                    <span className="ad-persona">
                      <Logo organizacion={org} />
                      <span>
                        <strong>{org.nombre}</strong>
                        <small>{[org.tipoOrganizacion, org.correo].filter(Boolean).join(' · ') || '—'}</small>
                      </span>
                    </span>
                  </td>
                  <td>{org.identificacion ?? '—'}</td>
                  <td>
                    {org.esOrganizacionSuperior ? <span className="ad-etiqueta">Organización superior</span>
                      : org.organizacionSuperior ? <span>{org.organizacionSuperior}<small className="ad-sub">{org.zonaEmpresarial}</small></span>
                      : <span className="ad-tenue">—</span>}
                  </td>
                  <td><Estatus activo={org.estado === 'Activa'} texto={org.estado} /></td>
                  {acciones.editar && (
                    <td className="ad-acciones">
                      <BotonIcono etiqueta={`Editar ${org.nombre}`} icono="editar" onClick={() => setFormulario(org)} />
                      {plataforma && (
                        <BotonIcono etiqueta={`${org.estado === 'Activa' ? 'Inactivar' : 'Activar'} ${org.nombre}`} icono={org.estado === 'Activa' ? 'cerrar' : 'check'}
                          peligro={org.estado === 'Activa'} onClick={() => { estadoMutation.reset(); setConfirmar(org) }} />
                      )}
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {formulario && (
        <OrganizacionFormulario
          organizacion={formulario === 'nueva' ? null : formulario}
          onClose={() => setFormulario(null)}
          onGuardado={(mensaje) => { setFormulario(null); avisar(mensaje) }}
        />
      )}
      {confirmar && (
        <Confirmar
          titulo={confirmar.estado === 'Activa' ? '¿Inactivar organización?' : '¿Activar organización?'}
          texto={confirmar.estado === 'Activa'
            ? `Los usuarios de ${confirmar.nombre} no podrán ingresar y sus sesiones se cerrarán. Usuarios, sedes, autorizaciones e historial se conservan.`
            : `${confirmar.nombre} y sus usuarios activos podrán volver a operar.`}
          accion={confirmar.estado === 'Activa' ? 'Inactivar' : 'Activar'}
          peligro={confirmar.estado === 'Activa'}
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

/** URL temporal para un Blob/File; se libera al cambiar o desmontar. */
function useUrlObjeto(contenido: Blob | null | undefined) {
  const url = useMemo(() => (contenido ? URL.createObjectURL(contenido) : null), [contenido])
  useEffect(() => () => { if (url) URL.revokeObjectURL(url) }, [url])
  return url
}

export function Logo({ organizacion }: { organizacion: OrganizacionAdmin }) {
  const logo = useQuery({
    queryKey: ['logo', organizacion.idOrganizacion],
    queryFn: () => descargarLogo(organizacion.idOrganizacion),
    enabled: organizacion.tieneLogo,
    staleTime: 5 * 60_000,
  })
  const url = useUrlObjeto(organizacion.tieneLogo ? logo.data : null)
  return url
    ? <img className="ad-avatar ad-logo" src={url} alt="" />
    : <span className="ad-avatar" data-tipo="org" aria-hidden="true"><Icono nombre="sede" /></span>
}

/** Campos del modelo de organización. `superior`: muestra el selector de organización superior. */
export function CamposOrganizacion({ datos, cambiar, errores, plataforma, superior }: {
  datos: OrganizacionEntrada
  cambiar: (parcial: Partial<OrganizacionEntrada>) => void
  errores: Partial<Record<keyof OrganizacionEntrada, string>>
  plataforma: boolean
  superior: boolean
}) {
  const catalogos = useQuery({ queryKey: ['catalogos-organizacion'], queryFn: consultarCatalogosOrganizacion, staleTime: 60_000 })
  const soloPlataforma = plataforma ? undefined : 'Solo el administrador de plataforma puede cambiarlo.'
  return (
    <div className="ad-rejilla">
      {catalogos.error && <p className="ad-mensaje" data-ancho="completo" role="alert">{mensajeError(catalogos.error, 'No se pudieron cargar los catálogos.')}</p>}
      <Campo etiqueta="Nombre" error={errores.nombre} ancho="completo">
        <input required value={datos.nombre} maxLength={200} aria-invalid={Boolean(errores.nombre)} onChange={(e) => cambiar({ nombre: e.target.value })} />
      </Campo>
      <Campo etiqueta="Tipo de organización" error={errores.idTipoOrganizacion}>
        <Selector className="sel-campo" etiqueta="Tipo de organización" valor={String(datos.idTipoOrganizacion || '')} invalido={Boolean(errores.idTipoOrganizacion)}
          onChange={(v) => cambiar({ idTipoOrganizacion: Number(v) })}
          opciones={[{ valor: '', texto: 'Selecciona…', deshabilitada: true }, ...(catalogos.data?.tipos ?? []).map((t) => ({ valor: String(t.id), texto: t.nombre }))]} />
      </Campo>
      <Campo etiqueta="Plan" error={errores.idPlan ?? soloPlataforma}>
        <Selector className="sel-campo" etiqueta="Plan" valor={String(datos.idPlan || '')} deshabilitado={!plataforma} invalido={Boolean(errores.idPlan)}
          onChange={(v) => cambiar({ idPlan: Number(v) })}
          opciones={[{ valor: '', texto: 'Selecciona…', deshabilitada: true }, ...(catalogos.data?.planes ?? []).map((p) => ({ valor: String(p.id), texto: p.nombre }))]} />
      </Campo>
      <Campo etiqueta="Identificación" error={errores.identificacion}>
        <input required value={datos.identificacion} maxLength={50} aria-invalid={Boolean(errores.identificacion)} onChange={(e) => cambiar({ identificacion: e.target.value })} />
      </Campo>
      <Campo etiqueta="Teléfono" error={errores.telefono}>
        <input required type="tel" value={datos.telefono} maxLength={30} aria-invalid={Boolean(errores.telefono)} onChange={(e) => cambiar({ telefono: e.target.value })} />
      </Campo>
      <Campo etiqueta="Correo electrónico" error={errores.correo}>
        <input required type="email" value={datos.correo} maxLength={320} aria-invalid={Boolean(errores.correo)} onChange={(e) => cambiar({ correo: e.target.value })} />
      </Campo>
      <Campo etiqueta="Dirección (opcional)">
        <input value={datos.direccion ?? ''} maxLength={500} onChange={(e) => cambiar({ direccion: e.target.value })} />
      </Campo>
      {superior && (
        <Campo etiqueta="Organización superior (opcional)" ancho="completo" error={soloPlataforma}>
          <Selector className="sel-campo" etiqueta="Organización superior" valor={String(datos.idZonaEmpresarial ?? '')} deshabilitado={!plataforma}
            onChange={(v) => cambiar({ idZonaEmpresarial: v ? Number(v) : null })}
            opciones={[{ valor: '', texto: 'Ninguna' }, ...(catalogos.data?.zonas ?? []).map((z) => ({ valor: String(z.id), texto: z.nombre }))]} />
        </Campo>
      )}
    </div>
  )
}

function OrganizacionFormulario({ organizacion, onClose, onGuardado }: { organizacion: OrganizacionAdmin | null; onClose: () => void; onGuardado: (mensaje: string) => void }) {
  const plataforma = esAdministradorGeneral()
  const queryClient = useQueryClient()
  const [datos, setDatos] = useState<OrganizacionEntrada>(organizacion ? organizacionAEntrada(organizacion) : organizacionVacia())
  const [errores, setErrores] = useState<Partial<Record<keyof OrganizacionEntrada, string>>>({})
  const [logo, setLogo] = useState<File | null>(null)
  const [quitar, setQuitar] = useState(false)
  const [errorLogo, setErrorLogo] = useState<string | null>(null)
  const vista = useUrlObjeto(logo)
  const cambiar = (parcial: Partial<OrganizacionEntrada>) => setDatos((d) => ({ ...d, ...parcial }))

  const guardar = useMutation({
    mutationFn: async () => {
      const entrada = limpiarOrganizacion(datos)
      let guardada = organizacion ? await editarOrganizacion(organizacion.idOrganizacion, entrada) : await crearOrganizacion(entrada)
      // El logo se envía después de guardar los datos: si falla, la organización ya quedó registrada.
      if (logo) guardada = await cargarLogo(guardada.idOrganizacion, logo)
      else if (quitar && guardada.tieneLogo) guardada = await quitarLogo(guardada.idOrganizacion)
      return guardada
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin-organizaciones'] })
      void queryClient.invalidateQueries({ queryKey: ['organizaciones-selector'] })
      void queryClient.invalidateQueries({ queryKey: ['admin-zonas'] })
      void queryClient.invalidateQueries({ queryKey: ['logo'] })
      onGuardado(organizacion ? 'Organización actualizada.' : 'Organización creada.')
    },
    onError: () => void queryClient.invalidateQueries({ queryKey: ['admin-organizaciones'] }),
  })

  function elegirLogo(archivo: File | undefined) {
    setErrorLogo(null)
    if (!archivo) return
    if (!TIPOS_LOGO.includes(archivo.type)) { setErrorLogo('El logo debe ser PNG, JPEG o WebP.'); return }
    if (archivo.size > MAX_LOGO) { setErrorLogo('El logo no puede superar 2 MB.'); return }
    setLogo(archivo)
    setQuitar(false)
  }

  function enviar(event: FormEvent) {
    event.preventDefault()
    const nuevos = validarOrganizacion(datos)
    setErrores(nuevos)
    if (Object.keys(nuevos).length === 0 && !errorLogo && !guardar.isPending) guardar.mutate()
  }

  return (
    <Dialogo
      titulo={organizacion ? 'Editar organización' : 'Nueva organización'}
      eyebrow="Organizaciones"
      icono="sede"
      ancho="amplio"
      onClose={onClose}
      onSubmit={enviar}
      pie={<BotonesFormulario onCancelar={onClose} pendiente={guardar.isPending} />}
    >
      <div className="ad-logo-campo">
        {vista ? <img className="ad-avatar ad-logo ad-logo-grande" src={vista} alt="Vista previa del logo" />
          : organizacion && !quitar ? <span className="ad-logo-grande-marco"><Logo organizacion={organizacion} /></span>
          : <span className="ad-avatar ad-logo-grande" data-tipo="org" aria-hidden="true"><Icono nombre="imagen" /></span>}
        <div>
          <strong>Logo (opcional)</strong>
          <small>PNG, JPEG o WebP de hasta 2 MB.</small>
          <div className="ad-logo-botones">
            <label className="ad-enlace">
              <input type="file" accept={TIPOS_LOGO.join(',')} className="sr-only" onChange={(e) => elegirLogo(e.target.files?.[0])} />
              {logo || organizacion?.tieneLogo ? 'Cambiar' : 'Cargar'}
            </label>
            {(logo || (organizacion?.tieneLogo && !quitar)) && (
              <button type="button" className="ad-enlace" onClick={() => { setLogo(null); setQuitar(true) }}>Quitar</button>
            )}
          </div>
          {errorLogo && <small className="ad-campo-error" role="alert">{errorLogo}</small>}
        </div>
      </div>
      <CamposOrganizacion datos={datos} cambiar={cambiar} errores={errores} plataforma={plataforma} superior={!organizacion?.esOrganizacionSuperior} />
      <Interruptor
        etiqueta="Organización activa"
        ayuda={plataforma ? (datos.estado === 'Activa' ? 'Sus usuarios pueden ingresar.' : 'Sus usuarios no podrán ingresar; los registros se conservan.') : 'Solo el administrador de plataforma puede cambiarlo.'}
        checked={datos.estado === 'Activa'}
        disabled={!plataforma}
        onChange={(v) => cambiar({ estado: v ? 'Activa' : 'Inactiva' })}
      />
      {guardar.error && <p className="ad-mensaje" role="alert">{mensajeError(guardar.error, 'No se pudo guardar la organización.')}</p>}
    </Dialogo>
  )
}
