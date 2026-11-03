import type { CSSProperties, ReactNode } from 'react'
import { Icono, type NombreIcono } from '../../shared/ui/Icono'
import { SERVICIOS } from './contenido'

/**
 * Vistas ilustrativas dentro de cada tarjeta. Solo etiquetas genéricas: sin nombres, documentos,
 * fechas ni cantidades; los valores se representan con barras neutras. Decorativas (aria-hidden).
 */
const MODALIDADES = ['Única', 'Diaria', 'Temporal', 'Recurrente', 'Permanente']

function Barra({ w }: { w: string }) {
  return <i className="lp-sv-barra" style={{ width: w }} />
}

const VISTAS: Partial<Record<NombreIcono, ReactNode>> = {
  personas: (
    <div className="lp-sv-panel">
      <div className="lp-sv-buscar">
        <Icono nombre="buscar" className="lp-icono" /> Buscar por documento…
      </div>
      {['58%', '46%', '52%'].map((w) => (
        <div key={w} className="lp-sv-fila">
          <span className="lp-sv-avatar">
            <Icono nombre="perfil" className="lp-icono" />
          </span>
          <span className="lp-sv-lineas">
            <Barra w={w} />
            <Barra w="34%" />
          </span>
          <span className="lp-sv-chip lp-sv-ok">Registrada</span>
        </div>
      ))}
    </div>
  ),
  sede: (
    <ol className="lp-sv-arbol">
      {(
        [
          ['sede', 'Sede'],
          ['administracion', 'Área'],
          ['dashboard', 'Zona'],
          ['accesos', 'Punto de acceso'],
        ] as const
      ).map(([i, t]) => (
        <li key={t}>
          <Icono nombre={i} className="lp-icono" /> {t}
        </li>
      ))}
    </ol>
  ),
  visitas: (
    <div className="lp-sv-fila lp-sv-tarjeta">
      <span className="lp-sv-avatar">
        <Icono nombre="visitas" className="lp-icono" />
      </span>
      <span className="lp-sv-lineas">
        <b>Visita programada</b>
        <Barra w="45%" />
      </span>
      <Icono nombre="flecha" className="lp-icono lp-sv-tenue" />
    </div>
  ),
  accesos: (
    <div className="lp-sv-acc-cuerpo">
      <div className="lp-sv-eventos">
        {(
          [
            ['entrada', 'Entrada registrada', 'lp-sv-in'],
            ['salida', 'Salida registrada', 'lp-sv-out'],
          ] as const
        ).map(([i, t, c]) => (
          <div key={t} className={`lp-sv-evento ${c}`}>
            <span className="lp-sv-evicono">
              <Icono nombre={i} className="lp-icono" />
            </span>
            <span className="lp-sv-lineas">
              <b>{t}</b>
              <small>Punto de acceso · persona · instante</small>
            </span>
            <span className="lp-sv-operador">
              <Icono nombre="perfil" className="lp-icono" />
              <small>Operador</small>
            </span>
          </div>
        ))}
      </div>
      <div className="lp-sv-torno">
        <span className="lp-sv-pase">
          <Icono nombre="perfil" className="lp-icono" />
          <b>Acceso registrado</b>
          <Icono nombre="check" className="lp-icono lp-sv-check" />
        </span>
        <span className="lp-sv-poste" />
        <span className="lp-sv-poste lp-sv-poste-2" />
        <span className="lp-sv-luz" />
      </div>
    </div>
  ),
  dashboard: (
    <ul className="lp-sv-lista">
      {(
        [
          ['En sede ahora', 'lp-sv-p-lima', '70%'],
          ['Sede', 'lp-sv-p-lima', '45%'],
          ['Sede', 'lp-sv-p-cian', '30%'],
          ['Sede', 'lp-sv-p-violeta', '18%'],
        ] as const
      ).map(([t, c, w], i) => (
        <li key={i}>
          <span className={`lp-sv-punto ${c}`} />
          {i === 0 ? <b>{t}</b> : <Barra w="40%" />}
          <Barra w={w} />
        </li>
      ))}
    </ul>
  ),
  reportes: (
    <ol className="lp-sv-tiempo">
      {(
        [
          ['Solicitud creada', 'lp-sv-p-lima'],
          ['Autorización aprobada', 'lp-sv-p-violeta'],
          ['Entrada registrada', 'lp-sv-p-cian'],
        ] as const
      ).map(([t, c]) => (
        <li key={t}>
          <span className={`lp-sv-punto ${c}`} />
          <span className="lp-sv-lineas">
            <b>{t}</b>
            <Barra w="38%" />
          </span>
        </li>
      ))}
    </ol>
  ),
  administracion: (
    <div className="lp-sv-multi">
      <ul className="lp-sv-orgs">
        <li data-actual="">
          <Icono nombre="sede" className="lp-icono" /> Organización actual
        </li>
        <li>
          <Icono nombre="sede" className="lp-icono" /> Otra organización
        </li>
        <li>
          <Icono nombre="sede" className="lp-icono" /> Otra organización
        </li>
      </ul>
      <ul className="lp-sv-roles">
        <li className="lp-sv-roles-t">Permisos del rol</li>
        {['Consultar', 'Añadir', 'Editar'].map((r) => (
          <li key={r}>
            <span className="lp-sv-caja" data-on="">
              <Icono nombre="check" className="lp-icono" />
            </span>
            {r}
          </li>
        ))}
        <li>
          <span className="lp-sv-caja" /> Por sede
        </li>
      </ul>
    </div>
  ),
  vehiculos: (
    <div className="lp-sv-panel lp-sv-panel-plano">
      {(
        [
          ['vehiculos', 'Vehículo autorizado'],
          ['dispositivos', 'Dispositivo autorizado'],
        ] as const
      ).map(([i, t]) => (
        <div key={t} className="lp-sv-fila lp-sv-tarjeta">
          <span className="lp-sv-avatar">
            <Icono nombre={i} className="lp-icono" />
          </span>
          <span className="lp-sv-lineas">
            <Barra w="40%" />
            <b className="lp-sv-ok-txt">{t}</b>
          </span>
          <span className="lp-sv-ok-redondo">
            <Icono nombre="check" className="lp-icono" />
          </span>
        </div>
      ))}
    </div>
  ),
}

/** Tono de acento por servicio. */
const TONO: Partial<Record<NombreIcono, string>> = {
  accesos: 'lima',
  dashboard: 'cian',
  reportes: 'cian',
  vehiculos: 'cian',
}

/** «Servicios»: composición asimétrica con tarjeta principal para entradas y salidas. */
export function ServiciosBento() {
  return (
    <>
      <ul className="lp-sv">
        {SERVICIOS.map((s, i) => (
          <li
            key={s.titulo}
            className={`lp-sv-bloque lp-sv-${s.icono} lp-sv-tono-${TONO[s.icono] ?? 'violeta'} lp-revela`}
            style={{ '--k': i % 3 } as CSSProperties}
          >
            <div className="lp-sv-cab">
              <span className="lp-sv-icono">
                <Icono nombre={s.icono} className="lp-icono" />
              </span>
              <div>
                <h3>{s.titulo}</h3>
                <p>{s.texto}</p>
              </div>
              {s.opcional && (
                <span className="lp-sv-opcional">
                  Módulo opcional según el plan de la organización
                </span>
              )}
            </div>
            {s.icono === 'autorizaciones' && (
              <ul className="lp-sv-modalidades" aria-label="Modalidades de autorización">
                {MODALIDADES.map((m, j) => (
                  <li key={m} data-activa={j === 0 || undefined}>
                    {m}
                  </li>
                ))}
              </ul>
            )}
            {VISTAS[s.icono] && (
              <div className="lp-sv-vista" aria-hidden="true">
                {VISTAS[s.icono]}
              </div>
            )}
            {s.icono === 'accesos' && (
              <span className="lp-sello lp-sv-sello">Ejemplo ilustrativo</span>
            )}
          </li>
        ))}
      </ul>
    </>
  )
}
