# Guía de despliegue — eshop-services (Catalog.API + Frontend Vue)

Este documento explica, paso a paso, cómo publicar:
1. La base de datos en **Neon** (Postgres en la nube, con su propio dominio).
2. La API (**Catalog.API**, .NET 9) en **Render**.
3. El frontend (Vue 3 + Vite) en **Netlify**.

Requisito: el proyecto debe estar en un repositorio de **GitHub**, porque tanto Render
como Netlify despliegan automáticamente cuando conectas un repo (cada `git push`
dispara un nuevo deploy).

---

## 0. Subir el proyecto a GitHub

El repo local ya está inicializado y con un primer commit (rama `main`). Solo falta
crear el repo remoto y subirlo:

1. Entra a https://github.com/new y crea un repositorio **vacío** (sin README, sin
   .gitignore, sin licencia — ya los tenemos localmente). Por ejemplo, llámalo
   `eshop-services`.
2. Copia la URL que te da GitHub (HTTPS), algo como
   `https://github.com/TU_USUARIO/eshop-services.git`.
3. En la terminal, dentro de la carpeta del proyecto:
   ```bash
   git remote add origin https://github.com/TU_USUARIO/eshop-services.git
   git push -u origin main
   ```
   Te pedirá iniciar sesión (usuario + token personal, o el navegador si tienes
   GitHub CLI/Desktop configurado).

> Nota: git detectó automáticamente tu nombre/usuario del equipo para los commits.
> Si quieres que aparezca tu nombre real en el historial (recomendado para la
> entrega), antes de otro commit corre:
> `git config user.name "Tu Nombre"` y `git config user.email "tu@correo.com"`.

---

## 1. Base de datos: Neon.tech

1. Crea una cuenta gratuita en https://neon.tech e inicia sesión.
2. **New Project** → nombre `catalogdb` (o el que prefieras), región cercana a ti,
   Postgres versión 15+ (coincide con la imagen que usas en local).
3. Neon crea automáticamente una base de datos y te muestra un **Connection string**
   como:
   ```
   postgres://usuario:password@ep-xxxx-xxxx.region.aws.neon.tech/neondb?sslmode=require
   ```
   Cópialo — lo vas a pegar como variable de entorno en Render (paso 2), **no** lo
   escribas en `appsettings.json` (ese archivo se sube a GitHub y quedaría público).
4. Marten (el ORM que usa `Catalog.API`) crea el esquema de tablas automáticamente
   la primera vez que la API se conecta y guarda un documento — no necesitas correr
   migraciones a mano.

Esto ya cumple el requisito de "publicar la base de datos en un dominio": el host
`ep-xxxx.region.aws.neon.tech` es el dominio de tu base de datos en la nube.

---

## 2. API: Render

1. Crea una cuenta en https://render.com y conéctala a tu cuenta de GitHub.
2. **New +** → **Web Service** → selecciona el repo `eshop-services`.
3. Configuración del servicio:
   - **Runtime**: Docker
   - **Root Directory**: (déjalo vacío / raíz del repo — el Dockerfile necesita ver
     tanto `src/Catalog.API` como `src/BuildingBlocks`)
   - **Dockerfile Path**: `src/Catalog.API/Dockerfile`
   - **Instance Type**: Free
4. Variables de entorno (**Environment** → **Add Environment Variable**):
   | Key | Value |
   |---|---|
   | `ASPNETCORE_ENVIRONMENT` | `Production` |
   | `ConnectionStrings__Database` | *(el connection string de Neon del paso 1)* |
   | `Cors__AllowedOrigins__0` | `http://localhost:5173` *(temporal, se corrige en el paso 4)* |
5. **Create Web Service**. Render construye la imagen Docker y la despliega. Al
   terminar te da una URL pública tipo `https://catalog-api-xxxx.onrender.com`.
6. Verifica que responde:
   ```bash
   curl https://catalog-api-xxxx.onrender.com/products
   ```
   (El plan free de Render "duerme" el servicio tras inactividad; el primer request
   puede tardar ~30s en despertar — coméntalo en el video para que no parezca un error.)

---

## 3. Frontend: Netlify

1. Crea una cuenta en https://netlify.com y conéctala a GitHub.
2. **Add new site** → **Import an existing project** → selecciona el repo.
3. Configuración de build:
   - **Base directory**: `frontend`
   - **Build command**: `npm run build`
   - **Publish directory**: `frontend/dist`
4. Variables de entorno (**Site settings** → **Environment variables**):
   | Key | Value |
   |---|---|
   | `VITE_API_URL` | `https://catalog-api-xxxx.onrender.com` *(la URL de Render del paso 2)* |
5. **Deploy site**. Netlify construye con Vite y publica. Te da una URL tipo
   `https://tu-sitio-123abc.netlify.app` (puedes cambiar el subdominio en
   **Site settings → Domain management → Options → Edit site name**).

`frontend/netlify.toml` ya incluye la regla de redirect para que las rutas de la SPA
funcionen correctamente, así que no hay que configurar nada extra ahí.

---

## 4. Cerrar el círculo: CORS

Ahora que tienes la URL real de Netlify, vuelve a Render:

1. **Environment** → edita `Cors__AllowedOrigins__0` y ponle tu URL de Netlify
   (`https://tu-sitio-123abc.netlify.app`).
2. Si quieres seguir probando también desde local, agrega una segunda variable
   `Cors__AllowedOrigins__1` = `http://localhost:5173`.
3. Guarda — Render vuelve a desplegar automáticamente con las nuevas variables.

---

## 5. Prueba end-to-end en producción

Desde la URL de Netlify, en el navegador:
1. Buscar un producto por nombre.
2. Crear un producto nuevo.
3. Actualizarlo.
4. Eliminarlo por nombre.
5. Verificar la paginación (crea 6+ productos y cambia de página).

Si algo falla, abre las **DevTools → Network** del navegador: un error de CORS se ve
como "blocked by CORS policy" en la consola — revisa que la URL de Netlify esté
exactamente en `Cors__AllowedOrigins` en Render (con `https://`, sin `/` al final).

---

## Referencia rápida — desarrollo local

1. `docker compose up -d catalogdb`
2. `dotnet run --project src/Catalog.API/Catalog.API.csproj --urls http://localhost:5201`
3. `cd frontend && npm install && npm run dev` → http://localhost:5173

## Referencia rápida — variables de entorno en producción

| Servicio | Variable | Valor |
|---|---|---|
| Render | `ASPNETCORE_ENVIRONMENT` | `Production` |
| Render | `ConnectionStrings__Database` | connection string de Neon |
| Render | `Cors__AllowedOrigins__0` | URL de Netlify |
| Netlify | `VITE_API_URL` | URL de Render |
