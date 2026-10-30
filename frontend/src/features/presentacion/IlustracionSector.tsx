import type { ReactNode } from 'react'

/**
 * Ilustraciones vectoriales propias (sin fotos ni marcas) para el carrusel «Para quién».
 * Escenas nocturnas con la paleta de ConecsIA: violeta, lima y cian. Decorativas (aria-hidden).
 */
export type EscenaSector =
  'empresas' | 'residencial' | 'educacion' | 'club' | 'coworking' | 'industrial' | 'otras'

const V = '#7c6cf2'
const L = '#c9f26b'
const C = '#56c4ff'

/** Rejilla de ventanas; algunas encendidas de forma determinista. */
function Ventanas({
  x,
  y,
  cols,
  filas,
  w = 6,
  h = 5,
  gx = 4,
  gy = 5,
  luz = V,
  semilla = 1,
}: {
  x: number
  y: number
  cols: number
  filas: number
  w?: number
  h?: number
  gx?: number
  gy?: number
  luz?: string
  semilla?: number
}) {
  const celdas: ReactNode[] = []
  for (let f = 0; f < filas; f++)
    for (let c = 0; c < cols; c++) {
      const on = (f * 7 + c * 3 + semilla) % 4 !== 0
      celdas.push(
        <rect
          key={`${f}-${c}`}
          x={x + c * (w + gx)}
          y={y + f * (h + gy)}
          width={w}
          height={h}
          rx={1}
          fill={on ? luz : '#232338'}
          opacity={on ? 0.75 : 1}
        />,
      )
    }
  return <>{celdas}</>
}

function Arbol({ x, y, s = 1 }: { x: number; y: number; s?: number }) {
  return (
    <g transform={`translate(${x} ${y}) scale(${s})`}>
      <rect x={-1} y={0} width={2} height={10} fill="#2a2a3a" />
      <circle cx={0} cy={-4} r={8} fill="#1f4a3a" />
      <circle cx={-3} cy={-7} r={5} fill="#2c6b4c" />
      <circle cx={0} cy={-4} r={8} fill={L} opacity={0.08} />
    </g>
  )
}

function Farola({ x, y, color = L }: { x: number; y: number; color?: string }) {
  return (
    <g>
      <rect x={x} y={y} width={1.5} height={26} fill="#3a3a50" />
      <circle cx={x + 0.75} cy={y} r={2.2} fill={color} />
      <circle cx={x + 0.75} cy={y} r={9} fill={color} opacity={0.15} />
    </g>
  )
}

const ESCENAS: Record<EscenaSector, ReactNode> = {
  empresas: (
    <>
      <rect x={40} y={40} width={60} height={110} fill="#1b1b2c" />
      <Ventanas x={46} y={48} cols={5} filas={9} semilla={2} />
      <rect x={110} y={20} width={70} height={130} fill="#22223a" />
      <rect x={110} y={20} width={70} height={3} fill={C} opacity={0.8} />
      <Ventanas x={116} y={30} cols={6} filas={11} w={6} luz={C} semilla={3} />
      <rect x={190} y={60} width={56} height={90} fill="#1b1b2c" />
      <Ventanas x={196} y={68} cols={4} filas={7} w={8} luz={L} semilla={5} />
      <rect x={256} y={80} width={40} height={70} fill="#18182a" />
      <Ventanas x={262} y={88} cols={3} filas={5} w={7} semilla={1} />
      <Arbol x={30} y={142} />
      <Arbol x={300} y={142} s={1.1} />
    </>
  ),
  residencial: (
    <>
      <rect x={30} y={60} width={80} height={90} fill="#1d1d30" />
      <polygon points="26,60 70,36 114,60" fill="#26263e" />
      <Ventanas x={38} y={70} cols={5} filas={6} w={9} h={7} gx={6} luz={L} semilla={4} />
      <rect x={130} y={50} width={90} height={100} fill="#22223a" />
      <Ventanas x={138} y={60} cols={6} filas={7} w={8} h={7} gx={6} luz={V} semilla={2} />
      <rect x={236} y={72} width={64} height={78} fill="#1d1d30" />
      <Ventanas x={244} y={82} cols={4} filas={5} w={9} h={7} gx={6} luz={C} semilla={6} />
      {/* Portería */}
      <rect x={112} y={122} width={18} height={28} fill="#2c2c46" />
      <rect x={115} y={127} width={12} height={8} fill={L} opacity={0.7} />
      <rect x={96} y={140} width={50} height={2} fill={L} opacity={0.6} />
      <Arbol x={20} y={142} s={1.1} />
      <Arbol x={226} y={144} />
    </>
  ),
  educacion: (
    <>
      <rect x={60} y={70} width={200} height={80} fill="#1f1f34" />
      <polygon points="50,70 160,32 270,70" fill="#2a2a46" />
      <polygon
        points="50,70 160,32 270,70"
        fill="none"
        stroke={V}
        strokeWidth={1.5}
        opacity={0.8}
      />
      {[0, 1, 2, 3, 4, 5].map((i) => (
        <rect key={i} x={78 + i * 32} y={78} width={8} height={66} fill="#34345a" />
      ))}
      <Ventanas
        x={92}
        y={86}
        cols={5}
        filas={3}
        w={12}
        h={10}
        gx={20}
        gy={10}
        luz={L}
        semilla={3}
      />
      <circle cx={160} cy={54} r={7} fill={C} opacity={0.8} />
      <rect x={0} y={148} width={320} height={4} fill="#1f4a3a" />
      <Arbol x={30} y={142} s={1.2} />
      <Arbol x={292} y={142} s={1.2} />
    </>
  ),
  club: (
    <>
      <rect x={30} y={78} width={110} height={56} fill="#22223a" />
      <polygon points="22,80 85,52 148,80" fill="#2a2a46" />
      <Ventanas x={40} y={90} cols={6} filas={2} w={12} h={10} gx={4} gy={8} luz={L} semilla={2} />
      {/* Piscina */}
      <rect x={160} y={120} width={130} height={22} rx={4} fill={C} opacity={0.35} />
      <path
        d="M166 128 q8 -4 16 0 t16 0 t16 0 t16 0 t16 0 t16 0 t16 0"
        stroke={C}
        strokeWidth={1.2}
        fill="none"
        opacity={0.9}
      />
      {/* Cancha */}
      <rect
        x={176}
        y={70}
        width={100}
        height={40}
        fill="none"
        stroke={L}
        strokeWidth={1.2}
        opacity={0.7}
      />
      <line x1={226} y1={70} x2={226} y2={110} stroke={L} strokeWidth={1} opacity={0.7} />
      <Farola x={160} y={90} color={V} />
      <Farola x={296} y={90} />
      <Arbol x={14} y={142} />
    </>
  ),
  coworking: (
    <>
      <rect x={0} y={0} width={320} height={160} fill="#17172a" />
      <Ventanas
        x={196}
        y={18}
        cols={4}
        filas={4}
        w={24}
        h={18}
        gx={4}
        gy={4}
        luz="#2a2a4c"
        semilla={1}
      />
      {/* Lámpara anular */}
      <ellipse cx={130} cy={46} rx={56} ry={8} fill="none" stroke={V} strokeWidth={3} />
      <ellipse
        cx={130}
        cy={46}
        rx={56}
        ry={8}
        fill="none"
        stroke={V}
        strokeWidth={10}
        opacity={0.2}
      />
      <line x1={130} y1={0} x2={130} y2={38} stroke="#3a3a50" />
      {/* Mesa y sillas */}
      <polygon points="54,104 206,104 226,120 34,120" fill="#3b2f2a" />
      <polygon points="54,104 206,104 226,120 34,120" fill={L} opacity={0.06} />
      {[48, 82, 116, 150, 184].map((x) => (
        <rect key={x} x={x} y={92} width={14} height={14} rx={3} fill="#24243a" />
      ))}
      <rect x={110} y={98} width={18} height={6} fill={C} opacity={0.7} />
      <rect x={34} y={120} width={4} height={30} fill="#2a2a3a" />
      <rect x={222} y={120} width={4} height={30} fill="#2a2a3a" />
      <Arbol x={268} y={140} s={1.4} />
      <Arbol x={20} y={140} s={1.2} />
    </>
  ),
  industrial: (
    <>
      {[0, 1, 2].map((i) => (
        <g key={i} transform={`translate(${150 + i * 50} ${60 - i * 6})`}>
          <rect width={56} height={90 + i * 6} fill={i % 2 ? '#22223a' : '#1b1b2c'} />
          <rect width={56} height={2} fill={L} opacity={0.9} />
          <rect x={14} y={60} width={26} height={30 + i * 6} fill="#2c2c46" />
        </g>
      ))}
      <rect x={30} y={88} width={100} height={62} fill="#1d1d30" />
      <rect x={30} y={88} width={100} height={2} fill={L} opacity={0.8} />
      <rect x={44} y={110} width={30} height={40} fill="#2c2c46" />
      {/* Camión genérico */}
      <rect x={84} y={124} width={50} height={20} fill="#d7d7e0" />
      <rect x={134} y={130} width={16} height={14} fill="#b9b9c6" />
      <circle cx={94} cy={146} r={4} fill="#111" />
      <circle cx={142} cy={146} r={4} fill="#111" />
      <Farola x={20} y={110} />
      <Farola x={144} y={104} />
      <Arbol x={8} y={142} />
    </>
  ),
  otras: (
    <>
      <rect x={70} y={56} width={180} height={94} fill="#1f1f34" />
      <rect x={70} y={56} width={180} height={3} fill={C} opacity={0.8} />
      <Ventanas x={82} y={68} cols={8} filas={3} w={14} h={10} gx={7} gy={8} luz={C} semilla={4} />
      {/* Acceso con torniquete */}
      <rect x={140} y={114} width={40} height={36} fill="#2c2c46" />
      <rect x={144} y={118} width={32} height={32} fill={L} opacity={0.18} />
      <rect x={124} y={132} width={6} height={18} rx={2} fill={V} />
      <rect x={190} y={132} width={6} height={18} rx={2} fill={V} />
      <rect x={130} y={136} width={12} height={2} fill={L} />
      <Arbol x={44} y={142} s={1.2} />
      <Arbol x={276} y={142} s={1.2} />
      <Farola x={30} y={112} color={C} />
    </>
  ),
}

export function IlustracionSector({ escena }: { escena: EscenaSector }) {
  const id = `cielo-${escena}`
  return (
    <svg
      className="lp-ilustracion"
      viewBox="0 0 320 160"
      preserveAspectRatio="xMidYMid slice"
      aria-hidden="true"
      focusable="false"
    >
      <defs>
        <linearGradient id={id} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#2a1f63" />
          <stop offset="0.65" stopColor="#141428" />
          <stop offset="1" stopColor="#0d0d18" />
        </linearGradient>
      </defs>
      <rect width={320} height={160} fill={`url(#${id})`} />
      <circle cx={260} cy={26} r={40} fill={V} opacity={0.18} />
      <circle cx={40} cy={30} r={30} fill={C} opacity={0.1} />
      {ESCENAS[escena]}
      <rect y={150} width={320} height={10} fill="#101019" />
      <rect y={150} width={320} height={1} fill={L} opacity={0.25} />
    </svg>
  )
}
