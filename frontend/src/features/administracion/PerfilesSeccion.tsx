import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { Icono } from '../../shared/ui/Icono'
import { CLAVE_MIS_MODULOS, useAcciones } from '../modulos/apiModulos'
import { leerOrganizacionActiva } from '../sesion/sesion'
import { cambiarEstadoPerfil, consultarPerfiles, crearPerfil, editarPerfil, type PerfilAdmin } from './apiAdministracion'
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
import { ESTADOS, mensajeError, useAviso, useDiferido } from './utilidades'
import { ModulosRolModal } from './ModulosRolModal'
import type { SeccionProps } from './secciones'

export function PerfilesSeccion({ pestanas }: SeccionProps) {
  const acciones = useAcciones('perfiles')
  const [buscar, setBuscar] = useState('')
  const [estado, setEstado] = useState('')
  const [formulario, setFormulario] = useState<PerfilAdmin | 'nuevo' | null>(null)
  const [modulos, setModulos] = useState<PerfilAdmin | null>(null)
  const [confirmar, setConfirmar] = useState<PerfilAdmin | null>(null)
  const { avisar, aviso } = useAviso()
  const queryClient = useQueryClient()
  const texto = useDiferido(buscar)
  const perfiles = useQuery({
    queryKey: ['admin-perfiles', leerOrganizacionActiva(), texto, estado],
    queryFn: () => consultarPerfiles({ buscar: texto, estado }),
    enabled: acciones.consultar,
    placeholderData: keepPreviousData,
  })
  const estadoMutation = useMutation({
    mutationFn: (p: PerfilAdmin) => cambiarEstadoPerfil(p.idRol, !p.activo),
    onSuccess: (p) => {
      void queryClient.invalidateQueries({ queryKey: ['admin-perfiles'] })
      void queryClient.invalidateQueries({ queryKey: CLAVE_MIS_MODULOS })
      setConfirmar(null)
      avisar(p.activo ? 'Perfil activado.' : 'Perfil inactivado.')
    },
  })

  return (
    <Marco
      titulo="Perfiles"
      descripcion="Perfiles de acceso de la organización activa y sus permisos por módulo."
      pestanas={pestanas}
      accion={acciones.anadir && <BotonNuevo texto="Nuevo perfil" onClick={() => setFormulario('nuevo')} />}
    >
      <div className="ad-herramientas">
        <Buscador valor={buscar} onChange={setBuscar} etiqueta="Buscar perfiles" placeholder="Buscar por nombre" />
        <Filtro etiqueta="Filtrar por estado" valor={estado} onChange={setEstado} opciones={ESTADOS} />
      </div>
      {!acciones.consultar ? (
        <Estado tipo="permiso" texto="Tu perfil no tiene permiso para consultar perfiles." />
      ) : (
        <EstadoConsulta loading={perfiles.isLoading} error={perfiles.error} empty={!perfiles.data?.length}
          vacio={buscar || estado ? 'Ningún perfil coincide con los filtros.' : 'Esta organización aún no tiene perfiles.'} />
      )}
      {perfiles.data && perfiles.data.length > 0 && (
        <div className="ad-perfiles">
          {perfiles.data.map((perfil) => (
            <article className="ad-perfil" key={perfil.idRol} data-inactivo={!perfil.activo || undefined}>
              <header>
                <span className="ad-perfil-icono" aria-hidden="true"><Icono nombre="autorizaciones" /></span>
                <Estatus activo={perfil.activo} />
              </header>
              <h2>{perfil.nombre}</h2>
              <p>{perfil.descripcion ?? 'Sin descripción'}</p>
              <footer>
                <span>
                  <strong>{perfil.totalModulos}</strong> {perfil.totalModulos === 1 ? 'módulo' : 'módulos'} · <strong>{perfil.totalUsuarios}</strong> {perfil.totalUsuarios === 1 ? 'usuario' : 'usuarios'}
                </span>
                <span className="ad-perfil-acciones">
                  <button type="button" className="ad-enlace" onClick={() => setModulos(perfil)} aria-label={`Módulos de ${perfil.nombre}`}>
                    <Icono nombre="dashboard" /> Módulos
                  </button>
                  {acciones.editar && (
                    <>
                      <BotonIcono etiqueta={`Editar ${perfil.nombre}`} icono="editar" onClick={() => setFormulario(perfil)} />
                      <BotonIcono etiqueta={`${perfil.activo ? 'Inactivar' : 'Activar'} ${perfil.nombre}`} icono={perfil.activo ? 'cerrar' : 'check'}
                        peligro={perfil.activo} onClick={() => { estadoMutation.reset(); setConfirmar(perfil) }} />
                    </>
                  )}
                </span>
              </footer>
            </article>
          ))}
        </div>
      )}

      {formulario && (
        <PerfilFormulario
          perfil={formulario === 'nuevo' ? null : formulario}
          onClose={() => setFormulario(null)}
          onGuardado={(perfil, nuevo) => {
            setFormulario(null)
            avisar(nuevo ? 'Perfil creado. Configura sus módulos.' : 'Perfil actualizado.')
            if (nuevo) setModulos(perfil)
          }}
        />
      )}
      {modulos && <ModulosRolModal perfil={modulos} editable={acciones.editar} onClose={() => setModulos(null)} onGuardado={() => avisar('Permisos del perfil guardados.')} />}
      {confirmar && (
        <Confirmar
          titulo={confirmar.activo ? '¿Inactivar perfil?' : '¿Activar perfil?'}
          texto={confirmar.activo
            ? `Los ${confirmar.totalUsuarios} usuarios con «${confirmar.nombre}» perderán de inmediato los permisos de este perfil. Su configuración se conserva para reactivarlo.`
            : `Los usuarios con «${confirmar.nombre}» recuperarán sus permisos.`}
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

function PerfilFormulario({ perfil, onClose, onGuardado }: { perfil: PerfilAdmin | null; onClose: () => void; onGuardado: (perfil: PerfilAdmin, nuevo: boolean) => void }) {
  const queryClient = useQueryClient()
  const [nombre, setNombre] = useState(perfil?.nombre ?? '')
  const [descripcion, setDescripcion] = useState(perfil?.descripcion ?? '')
  const [activo, setActivo] = useState(perfil?.activo ?? true)
  const [error, setError] = useState<string>()
  const guardar = useMutation({
    mutationFn: () => {
      const datos = { nombre: nombre.trim(), descripcion: descripcion.trim() || null, activo }
      return perfil ? editarPerfil(perfil.idRol, datos) : crearPerfil(datos)
    },
    onSuccess: (guardado) => {
      void queryClient.invalidateQueries({ queryKey: ['admin-perfiles'] })
      void queryClient.invalidateQueries({ queryKey: CLAVE_MIS_MODULOS })
      onGuardado(guardado, !perfil)
    },
  })

  function enviar(event: FormEvent) {
    event.preventDefault()
    setError(nombre.trim() ? undefined : 'El nombre es obligatorio.')
    if (nombre.trim() && !guardar.isPending) guardar.mutate()
  }

  return (
    <Dialogo titulo={perfil ? 'Editar perfil' : 'Nuevo perfil'} eyebrow="Perfiles" icono="autorizaciones" onClose={onClose} onSubmit={enviar}
      pie={<BotonesFormulario onCancelar={onClose} pendiente={guardar.isPending} />}>
      <Campo etiqueta="Nombre" error={error}>
        <input required autoFocus maxLength={120} value={nombre} aria-invalid={Boolean(error)} onChange={(e) => setNombre(e.target.value)} />
      </Campo>
      <Campo etiqueta="Descripción (opcional)">
        <textarea rows={3} maxLength={500} value={descripcion} onChange={(e) => setDescripcion(e.target.value)} />
      </Campo>
      <Interruptor etiqueta="Perfil activo" ayuda={activo ? 'Sus usuarios reciben sus permisos.' : 'Sus usuarios no reciben sus permisos.'} checked={activo} onChange={setActivo} />
      {guardar.error && <p className="ad-mensaje" role="alert">{mensajeError(guardar.error, 'No se pudo guardar el perfil.')}</p>}
    </Dialogo>
  )
}
