# Guion para el video de entrega

Duración máxima: **15 minutos**. Graba con OBS Studio o QuickTime (Mac: Cmd+Shift+5 →
"Grabar toda la pantalla" o una ventana). Habla mientras muestras cada paso — no hace
falta editar, solo grabar en orden. Los tiempos son sugeridos, ajusta si te sobra o
falta tiempo, pero no te pases de los 15 min.

## 1. Introducción (30 s)
- Muestra el proyecto abierto en el editor.
- Di brevemente: "Este es un proyecto de microservicios en .NET: Catalog.API maneja
  productos (buscar por nombre, filtrar por categoría, insertar, actualizar, eliminar,
  paginación) y Basket.API maneja el carrito con caché en Redis. Frontend en Vue y Vite."

## 2. Recorrido del código (1-2 min)
- Abre `src/Catalog.API/Models/Products/GetProducts/GetProductsEndPoint.cs` — endpoint
  `/products` que busca por nombre y pagina.
- Abre `src/Catalog.API/Models/Products/GetProductByCategory/GetProductByCategoryEndPoint.cs`
  — el filtro por categoría.
- Abre `src/Basket/Basket.API/Data/CacheBasketRepository.cs` — señala el patrón
  decorador: `BasketRepository` (Postgres) envuelto por `CacheBasketRepository` (Redis,
  vía `IDistributedCache`). Menciona que esto se registra en `Program.cs` con
  `.Decorate<IBasketRepository, CacheBasketRepository>()`.
- Abre `frontend/src/App.vue` — muestra el formulario de productos, el filtro de
  categoría y la sección de Carrito.

## 3. Demo en local — Catálogo (2 min)
- Terminal: `docker compose up -d catalogdb`.
- Terminal: `dotnet run --project src/Catalog.API/Catalog.API.csproj --urls http://localhost:5201`.
- Terminal: `cd frontend && npm run dev`.
- Abre `http://localhost:5173` y, en vivo:
  1. Crea un producto.
  2. Búscalo por nombre.
  3. Filtra por categoría.
  4. Actualízalo.
  5. Elimínalo por nombre.
  6. Crea varios productos y muestra la paginación.

## 4. Demo en local — Basket.API + Redis (2 min)
- Terminal: `docker compose up -d basketdb redis basket.api` (o `dotnet run` si lo
  corres fuera de Docker).
- `curl http://localhost:8082/health` — muestra en pantalla que responde
  `"status":"Healthy"` con las entradas `npgsql` y `redis` ambas en `Healthy`. Explica:
  "Esto confirma que Basket.API está usando Redis como caché real, no simulado."
- En el navegador, sección **Carrito**: escribe un usuario, "Cargar carrito", agrega
  1-2 productos con "Agregar al carrito", muestra el total, quita uno, y vacía el carrito.

## 5. Publicar la base de datos en Neon (1-2 min)
- Entra a Neon, muestra el proyecto y el **connection string** (tapa la contraseña con
  el cursor o un recuadro si vas a compartir el video públicamente).
- Explica: "Esto le da un dominio en la nube a la base de datos: `xxxx.neon.tech`."

## 6. Publicar la API en Render (1-2 min)
- Muestra el repo ya subido a GitHub.
- Muestra la configuración del Web Service en Render: Dockerfile path, variables de
  entorno (`ConnectionStrings__Database`, `Cors__AllowedOrigins__0`, `ASPNETCORE_ENVIRONMENT`).
- Muestra el log de build/deploy terminando en éxito y la URL pública.
- Prueba en vivo: `curl https://tu-api.onrender.com/products`.

## 7. Publicar el frontend en Netlify (1 min)
- Muestra la configuración del sitio: base directory `frontend`, build command
  `npm run build`, publish directory `frontend/dist`, variable `VITE_API_URL`.
- Muestra el deploy terminando y la URL pública.

## 8. Demo final en producción (1-2 min)
- Abre la URL de Netlify (no localhost).
- Repite búsqueda por nombre, filtro por categoría, crear, actualizar, eliminar y
  paginación, ahora contra Render + Neon.
- Muestra la pestaña **Network** de DevTools para confirmar que las peticiones van a
  `https://tu-api.onrender.com` y responden 200 OK.
- Menciona: "El carrito con Redis se mostró en local en la sección anterior; en esta
  entrega Basket.API no está desplegado a la nube, solo Catalog.API."

## 9. Cierre (15 s)
- Resume: "Base de datos en Neon, API de catálogo en Render, frontend en Netlify, y
  Basket.API con caché en Redis funcionando en local." Menciona las URLs finales en
  pantalla o en la descripción del video.

---

### Checklist antes de grabar
- [ ] `git push` hecho y repo visible en GitHub.
- [ ] Servicio en Render desplegado y respondiendo (probar `/products` antes de grabar).
- [ ] Sitio en Netlify desplegado y probado end-to-end (crear/buscar/filtrar/actualizar/eliminar/paginar).
- [ ] `Cors__AllowedOrigins` en Render actualizado con la URL final de Netlify.
- [ ] `docker compose up -d catalogdb basketdb redis basket.api` corriendo antes de
      grabar la sección 4, y `curl http://localhost:8082/health` probado.
- [ ] Contraseñas/connection strings tapadas si el video se va a compartir públicamente.
- [ ] Cronómetro corriendo mientras ensayas una vez — que quede bajo 15 min.
