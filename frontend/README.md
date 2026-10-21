# Login de ConecsIA

Esta carpeta contiene únicamente la pantalla de inicio de sesión ajustada y el cliente HTTP
necesario para el contrato de autenticación real.

```bash
npm ci
npm run dev
```

La API se configura con `VITE_API_BASE_URL` en despliegues separados. En desarrollo, el proxy de
Vite usa `API_PROXY_TARGET`; consulta `.env.example`. El login realiza `POST /api/v1/auth/login`,
obtiene el token CSRF desde `GET /api/v1/auth/csrf` y conserva la sesión mediante la cookie del
servidor. No incluye autenticación simulada.

Comprobaciones locales:

```bash
npm run build
npm test -- --run --reporter=verbose --maxWorkers=1
```
