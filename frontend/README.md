# Login de ConecsIA

La SPA se comunica con la API; el navegador nunca se conecta directamente a PostgreSQL.

```bash
npm ci
npm run dev
```

En despliegues separados usa `VITE_API_BASE_URL`. En desarrollo, el proxy de Vite usa
`API_PROXY_TARGET`; consulta `.env.example`.

El login realiza `POST /api/auth/login` con `{ correo, password }`. El access token JWT se conserva
solo en memoria y las solicitudes posteriores envían `Authorization: Bearer <token>`. Un `401`
limpia la sesión y un `403` conserva la sesión e informa la falta de permisos. No se guardan tokens
en `localStorage`.

Comprobaciones locales:

```bash
npm run build
npm test -- --run --reporter=verbose --maxWorkers=1
```
