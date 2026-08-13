# Guía de despliegue — eshop-services (Catalog.API + Basket.API + Frontend Vue)

Este documento explica, paso a paso, cómo publicar el proyecto completo:
1. Dos bases de datos PostgreSQL en **Neon** (Catalog y Basket, cada una con su
   propio dominio en la nube).
2. Una caché **Redis** en **Upstash**.
3. Dos APIs .NET 9 en **Render**: `Catalog.API` (productos: búsqueda por nombre,
   filtro por categoría y precio, alta/edición/eliminación, paginación) y
   `Basket.API` (carrito, con caché distribuida sobre Redis).
4. El frontend (Vue 3 + Vite) en **Netlify**, consumiendo ambas APIs.

Requisito: el proyecto debe estar en un repositorio de **GitHub**, porque tanto Render
como Netlify despliegan automáticamente cuando conectas un repo (cada `git push`
dispara un nuevo deploy).

---

## 0. Subir el proyecto a GitHub

1. Entra a https://github.com/new y crea un repositorio **vacío** (sin README, sin
   .gitignore, sin licencia). Por ejemplo, llámalo `eshop-services`.
2. Copia la URL HTTPS que te da GitHub.
3. En la terminal, dentro de la carpeta del proyecto:
   ```bash
   git remote add origin https://github.com/TU_USUARIO/eshop-services.git
   git push -u origin main
   ```

---

## 1. Bases de datos: Neon.tech

1. Crea una cuenta gratuita en https://neon.tech e inicia sesión.
2. **New Project** → nombre `eshop-catalog` (o el que prefieras), región cercana a
   ti, Postgres 15+.
3. Neon crea automáticamente la primera base de datos (`neondb`, usada por
   `Catalog.API`) y te muestra un **Connection string** desde el botón **Connect**.
4. Para `Basket.API` se necesita una **segunda base de datos** en el mismo proyecto:
   entra a **SQL Editor** y ejecuta:
   ```sql
   CREATE DATABASE basketdb;
   ```
   Luego, en **Connect**, cambia el selector de base de datos a `basketdb` para
   obtener su connection string (mismo host/usuario, distinto nombre de base).
5. Los connection strings de Neon vienen en formato URI
   (`postgresql://usuario:password@host/db?sslmode=require`), pero Npgsql (el driver
   .NET que usa Marten) necesita formato clave=valor. Conviértelo así:
   ```
   Host=<host-de-neon>;Port=5432;Database=<neondb-o-basketdb>;Username=<usuario>;Password=<password>;Ssl Mode=Require
   ```
   Este valor va como variable de entorno en Render (paso 3), **nunca** en
   `appsettings.json` (se sube a GitHub y quedaría público).
6. Marten crea el esquema de tablas automáticamente al guardar el primer documento
   — no hace falta correr migraciones a mano.

Esto cumple el requisito de "publicar la base de datos en un dominio": el host
`ep-xxxx.region.aws.neon.tech` es el dominio en la nube de ambas bases.

---

## 2. Caché: Redis en Upstash

1. Crea una cuenta gratuita en https://upstash.com (con GitHub es más rápido).
2. **Create Database** → tipo **Regional** (no "Global"), región cercana (idealmente
   la misma familia de región que Neon, ej. `us-east-1`), plan **Free**.
3. En el dashboard de la base, pestaña **Connect**, copia el string en formato
   `rediss://default:<password>@<host>:<port>`.
4. Conviértelo al formato que espera StackExchange.Redis (el cliente .NET):
   ```
   <host>:<port>,password=<password>,ssl=True,abortConnect=False
   ```

---

## 3. APIs: Render (dos Web Services)

Se crean **dos servicios separados**, uno por microservicio, ambos apuntando al
mismo repositorio.

### 3.1 Catalog.API
1. **New +** → **Web Service** → selecciona el repo.
2. **Runtime**: Docker · **Root Directory**: vacío ·
   **Dockerfile Path**: `src/Catalog.API/Dockerfile` · **Instance Type**: Free.
3. Variables de entorno:
   | Key | Value |
   |---|---|
   | `ASPNETCORE_ENVIRONMENT` | `Production` |
   | `ConnectionStrings__Database` | connection string de Neon (`neondb`) |
   | `Cors__AllowedOrigins__0` | URL de Netlify (paso 5; provisional al inicio) |
4. **Create Web Service**. Al terminar, obtienes una URL pública
   (`https://catalog-api-xxxx.onrender.com`).

### 3.2 Basket.API
1. **New +** → **Web Service** → mismo repo.
2. **Runtime**: Docker · **Root Directory**: vacío ·
   **Dockerfile Path**: `src/Basket/Basket.API/Dockerfile` · **Instance Type**: Free.
3. Variables de entorno:
   | Key | Value |
   |---|---|
   | `ASPNETCORE_ENVIRONMENT` | `Production` |
   | `ConnectionStrings__Database` | connection string de Neon (`basketdb`) |
   | `ConnectionStrings__Redis` | connection string de Upstash (paso 2.4) |
   | `Cors__AllowedOrigins__0` | URL de Netlify (paso 5; provisional al inicio) |
4. **Create Web Service**. Obtienes otra URL pública
   (`https://basket-api-xxxx.onrender.com`).

Verifica ambos servicios:
```bash
curl https://catalog-api-xxxx.onrender.com/products
curl https://basket-api-xxxx.onrender.com/health
```
El segundo debe responder `"status":"Healthy"` con las entradas `npgsql` y `redis`
ambas en `Healthy` — esa es la prueba de que el caché es compatible con Redis.

> El plan free de Render "duerme" cada servicio tras inactividad; el primer request
> puede tardar ~30s en despertar.

---

## 4. Frontend: Netlify

1. Crea una cuenta en https://netlify.com y conéctala a GitHub.
2. **Add new site** → **Import an existing project** → selecciona el repo.
3. Configuración de build:
   - **Base directory**: `frontend`
   - **Build command**: `npm run build`
   - **Publish directory**: `frontend/dist`
4. Variables de entorno:
   | Key | Value |
   |---|---|
   | `VITE_API_URL` | URL de Render de Catalog.API |
   | `VITE_BASKET_API_URL` | URL de Render de Basket.API |
5. **Deploy site**. Te da una URL tipo `https://tu-sitio.netlify.app`.

`frontend/netlify.toml` ya incluye la regla de redirect para que las rutas de la SPA
funcionen correctamente.

> Si cambias una variable de entorno **después** de que el sitio ya se desplegó,
> tienes que forzar un nuevo build manualmente: **Deploys → Trigger deploy → Deploy
> site** (Vite incrusta las variables en tiempo de compilación, no en runtime).

---

## 5. Cerrar el círculo: CORS

Con las URLs reales de Netlify y de ambas APIs ya conocidas:

1. En **cada** servicio de Render (Catalog.API y Basket.API) → **Environment** →
   edita `Cors__AllowedOrigins__0` con la URL real de Netlify.
2. Guarda — cada servicio redespliega automáticamente.

---

## 6. Prueba end-to-end en producción

Desde la URL de Netlify, en el navegador:
1. Catálogo: buscar por nombre, filtrar por categoría y rango de precio (combinados),
   crear, actualizar, eliminar por nombre, y verificar la paginación.
2. Carrito: cargar carrito por usuario, agregar productos, quitarlos, vaciar el
   carrito — y recargar la página para confirmar que el carrito persiste (viene de
   Redis/Postgres, no de memoria local del navegador).

Si algo falla, abre **DevTools → Network**: un error de CORS se ve como "blocked by
CORS policy" en la consola — revisa que la URL de Netlify esté exactamente en
`Cors__AllowedOrigins` de cada servicio en Render (con `https://`, sin `/` al final).

---

## Referencia rápida — desarrollo local

```bash
docker compose up -d catalogdb basketdb redis
dotnet run --project src/Catalog.API/Catalog.API.csproj --urls http://localhost:5201
dotnet run --project src/Basket/Basket.API/Basket.API.csproj --urls http://localhost:8082
cd frontend && npm install && npm run dev   # http://localhost:5173
```

## Referencia rápida — variables de entorno en producción

| Servicio | Variable | Valor |
|---|---|---|
| Render (Catalog.API) | `ASPNETCORE_ENVIRONMENT` | `Production` |
| Render (Catalog.API) | `ConnectionStrings__Database` | connection string de Neon (`neondb`) |
| Render (Catalog.API) | `Cors__AllowedOrigins__0` | URL de Netlify |
| Render (Basket.API) | `ASPNETCORE_ENVIRONMENT` | `Production` |
| Render (Basket.API) | `ConnectionStrings__Database` | connection string de Neon (`basketdb`) |
| Render (Basket.API) | `ConnectionStrings__Redis` | connection string de Upstash |
| Render (Basket.API) | `Cors__AllowedOrigins__0` | URL de Netlify |
| Netlify | `VITE_API_URL` | URL de Render (Catalog.API) |
| Netlify | `VITE_BASKET_API_URL` | URL de Render (Basket.API) |
