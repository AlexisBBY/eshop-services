# Guion para el video de entrega

Duración sugerida: 6-10 min. Graba con OBS Studio o QuickTime (Mac: Cmd+Shift+5 →
"Grabar toda la pantalla" o una ventana). Habla mientras muestras cada paso — no hace
falta editar, solo grabar en orden.

## 1. Introducción (30 s)
- Muestra el proyecto abierto en el editor (VS Code o VS).
- Di brevemente: "Este es un proyecto de microservicios en .NET con un catálogo de
  productos: buscar por nombre, insertar, actualizar, eliminar y consultar con
  paginación, con frontend en Vue y Vite."

## 2. Recorrido del código (1-2 min)
- Abre `src/Catalog.API/Models/Products/GetProducts/GetProductsEndPoint.cs` — muestra
  el endpoint `/products` que busca por nombre y pagina.
- Abre `frontend/src/App.vue` — muestra el formulario de crear/editar y la tabla con
  paginación.
- Menciona que usan CQRS (`CreateProductCommand`, `GetProductsQuery`, etc.) y Marten
  como ORM sobre PostgreSQL.

## 3. Demo en local (2 min)
- Terminal: `docker compose up -d catalogdb`.
- Terminal: `dotnet run --project src/Catalog.API/Catalog.API.csproj --urls http://localhost:5201`.
- Terminal: `cd frontend && npm run dev`.
- Abre `http://localhost:5173` en el navegador y, en vivo:
  1. Crea un producto.
  2. Búscalo por nombre.
  3. Actualízalo.
  4. Elimínalo por nombre.
  5. Crea varios productos y muestra la paginación funcionando.

## 4. Publicar la base de datos en Neon (1-2 min)
- Entra a neon.tech, muestra el dashboard, el proyecto creado y el **connection
  string** (tapa la contraseña con el cursor o un recuadro si vas a compartir el
  video públicamente).
- Explica: "Esto le da un dominio en la nube a la base de datos: `xxxx.neon.tech`."

## 5. Publicar la API en Render (1-2 min)
- Muestra el repo ya subido a GitHub.
- Muestra la pantalla de creación del Web Service en Render: Dockerfile path, las
  variables de entorno (`ConnectionStrings__Database`, `Cors__AllowedOrigins__0`,
  `ASPNETCORE_ENVIRONMENT`).
- Muestra el log de build/deploy terminando en éxito y la URL pública.
- Prueba en vivo: `curl https://tu-api.onrender.com/products` o ábrelo en el navegador.

## 6. Publicar el frontend en Netlify (1-2 min)
- Muestra la configuración del sitio en Netlify: base directory `frontend`, build
  command `npm run build`, publish directory `frontend/dist`, variable
  `VITE_API_URL`.
- Muestra el deploy terminando y la URL pública.

## 7. Demo final en producción (1-2 min)
- Abre la URL de Netlify en el navegador (no localhost).
- Repite las 5 operaciones del paso 3, ahora contra la API en Render y la base en Neon.
- Muestra la pestaña **Network** de DevTools para demostrar que las peticiones van a
  `https://tu-api.onrender.com` y responden 200 OK.

## 8. Cierre (15 s)
- Resume: "Base de datos en Neon, API en Render, frontend en Netlify, todo
  funcionando en producción." Menciona las URLs finales en pantalla o en la
  descripción del video.

---

### Checklist antes de grabar
- [ ] `git push` hecho y repo visible en GitHub.
- [ ] Servicio en Render desplegado y respondiendo (probar `/products` antes de grabar).
- [ ] Sitio en Netlify desplegado y probado end-to-end (crear/buscar/actualizar/eliminar/paginar).
- [ ] `Cors__AllowedOrigins` en Render actualizado con la URL final de Netlify.
- [ ] Contraseñas/connection strings tapadas si el video se va a compartir públicamente.
