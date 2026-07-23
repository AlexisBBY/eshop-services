# Guion para el video de entrega

Duración máxima: **15 minutos**. Graba con OBS Studio o QuickTime (Mac: Cmd+Shift+5 →
"Grabar toda la pantalla" o una ventana).

Este guion está escrito como **texto para leer en voz alta** mientras grabas — con
vocabulario técnico, tal como pidió el profesor. Los tiempos entre paréntesis son
sugeridos. Practícalo una vez en voz alta antes de grabar para que no suene leído.

---

## 1. Introducción (30 s)

> "Buenas, en este video voy a presentar el proyecto eshop-services, una arquitectura
> de microservicios desarrollada en .NET 9 siguiendo el patrón CQRS, es decir,
> separación entre comandos y consultas. El proyecto tiene dos microservicios:
> Catalog.API, que expone un API REST para la gestión de productos —búsqueda por
> nombre, filtrado por categoría y rango de precio, inserción, actualización,
> eliminación y consulta paginada—, y Basket.API, que gestiona el carrito de compras
> con una caché distribuida sobre Redis. El frontend está construido con Vue 3 y
> Vite, y consume ambos servicios vía HTTP."

## 2. Recorrido del código (1-2 min)

*(Abre el editor y muestra los archivos mientras hablas)*

> "Empecemos por la arquitectura del backend. Cada microservicio expone sus
> endpoints usando Carter, una librería de enrutamiento sobre Minimal APIs de
> ASP.NET Core, y la lógica de negocio se despacha con MediatR, implementando el
> patrón mediador propio de CQRS: cada operación es un Command o una Query con su
> respectivo Handler."

*(Abre `GetProductsEndPoint.cs` y `GetProductsQueryHandler.cs`)*

> "Aquí está el endpoint de consulta de productos. Acepta filtros combinables por
> nombre, categoría y rango de precio, todos opcionales, y devuelve el resultado
> paginado usando un objeto genérico `PaginatedResult`. La persistencia se maneja con
> Marten, que es un ORM de tipo document database sobre PostgreSQL: los objetos .NET
> se serializan directamente a columnas JSONB, sin necesidad de mapeo relacional
> explícito ni migraciones manuales."

*(Abre `CacheBasketRepository.cs`)*

> "En Basket.API se implementa el patrón decorador para la caché: la clase
> `BasketRepository` accede directamente a PostgreSQL, y `CacheBasketRepository` la
> envuelve, agregando una capa de caché distribuida con Redis a través de la
> interfaz `IDistributedCache`. Esto se registra en el contenedor de inyección de
> dependencias con `Decorate`, de la librería Scrutor, sin modificar el contrato de
> la interfaz `IBasketRepository`. El resultado: las lecturas del carrito primero
> consultan Redis, y solo si no hay dato en caché se consulta la base relacional."

*(Abre `App.vue`)*

> "En el frontend, con Vue 3 usando la Composition API y `<script setup>`, se
> consumen ambos servicios mediante `fetch`, con manejo reactivo de estado para el
> catálogo, los filtros y el carrito."

## 3. Demo en local — Catálogo (2 min)

*(Terminal)*
```bash
docker compose up -d catalogdb
dotnet run --project src/Catalog.API/Catalog.API.csproj --urls http://localhost:5201
cd frontend && npm run dev
```

> "Levanto la base de datos PostgreSQL en un contenedor Docker, inicio la API de
> catálogo en el puerto 5201, y el frontend con el servidor de desarrollo de Vite."

*(En el navegador, `http://localhost:5173`)*

> "Voy a demostrar las operaciones CRUD completas. Primero, inserción de un producto
> nuevo con nombre, descripción, categorías, imagen y precio."

*(Crea un producto)*

> "Ahora, búsqueda por nombre."

*(Busca)*

> "Filtrado combinado: por categoría y rango de precio, aplicados simultáneamente."

*(Filtra)*

> "Actualización de un producto existente."

*(Edita y guarda)*

> "Eliminación por nombre."

*(Elimina)*

> "Y finalmente, la consulta paginada: con varios productos cargados, el listado se
> pagina desde el backend usando los parámetros `pageIndex` y `pageSize`."

*(Navega entre páginas)*

## 4. Demo en local — Basket.API + Redis (2 min)

*(Terminal)*
```bash
docker compose up -d basketdb redis basket.api
curl http://localhost:8082/health
```

> "Levanto la base de datos del carrito, el contenedor de Redis, y el microservicio
> Basket.API. Con este `curl` al endpoint de healthcheck confirmo que ambas
> dependencias están operativas: pueden ver en la respuesta que tanto la entrada
> `npgsql`, correspondiente a PostgreSQL, como la entrada `redis` reportan estado
> `Healthy`. Esto demuestra que el servicio tiene una caché real y funcional con
> Redis, no simulada."

*(En el navegador, sección Carrito)*

> "En el frontend, cargo el carrito asociado a un usuario, agrego productos del
> catálogo, lo que dispara una escritura tanto en PostgreSQL como en la caché de
> Redis, remuevo un ítem, y finalmente vacío el carrito por completo."

## 5. Publicar la base de datos en Neon (1-2 min)

*(Navegador, dashboard de Neon)*

> "Para el despliegue en la nube, la base de datos PostgreSQL está alojada en Neon,
> un servicio de Postgres serverless. Aquí se ve el proyecto creado y el connection
> string de conexión —oculto la contraseña por seguridad—. Esto le da a la base de
> datos un dominio propio en la nube, cumpliendo con el requisito de publicación de
> la base de datos."

## 6. Publicar la API en Render (1-2 min)

*(Navegador, GitHub y luego Render)*

> "El código fuente está versionado en un repositorio de GitHub. El servicio de
> Catalog.API se despliega en Render como un Web Service basado en contenedor
> Docker, apuntando al Dockerfile del proyecto. La configuración se inyecta mediante
> variables de entorno: la cadena de conexión a Neon, el origen permitido para CORS,
> y el entorno de ejecución en modo Production."

*(Muestra el log de deploy y la URL)*

> "El pipeline de build y despliegue termina exitosamente, y el servicio queda
> disponible en una URL pública sobre HTTPS."

*(Terminal: `curl https://tu-api.onrender.com/products`)*

## 7. Publicar el frontend en Netlify (1 min)

*(Navegador, configuración de Netlify)*

> "El frontend se despliega en Netlify, configurado para tomar el directorio
> `frontend` como base, ejecutar `npm run build` con Vite, y publicar el directorio
> de salida `dist`. La URL de la API se inyecta en tiempo de build mediante la
> variable de entorno `VITE_API_URL`."

## 8. Demo final en producción (1-2 min)

*(Navegador, la URL real de Netlify)*

> "Ahora, contra el entorno de producción real: búsqueda por nombre, filtro
> combinado, inserción, actualización, eliminación y paginación, todo operando
> contra la API desplegada en Render y la base de datos en Neon."

*(Abre DevTools → Network)*

> "En la pestaña de Network se confirma que las peticiones viajan hacia el dominio
> de Render y responden con código 200. En esta entrega, el microservicio de carrito
> con Redis se demostró en el entorno local; el catálogo es el que está totalmente
> desplegado en la nube."

## 9. Cierre (15 s)

> "En resumen: base de datos en Neon, API de catálogo desplegada en Render, frontend
> en Netlify, y un segundo microservicio de carrito con caché distribuida en Redis,
> demostrado en entorno local. Gracias."

---

### Checklist antes de grabar
- [ ] `git push` hecho y repo visible en GitHub.
- [ ] Servicio en Render desplegado y respondiendo (probar `/products` antes de grabar).
- [ ] Sitio en Netlify desplegado y probado end-to-end (crear/buscar/filtrar/actualizar/eliminar/paginar).
- [ ] `Cors__AllowedOrigins` en Render actualizado con la URL final de Netlify.
- [ ] `docker compose up -d catalogdb basketdb redis basket.api` corriendo antes de
      grabar la sección 4, y `curl http://localhost:8082/health` probado.
- [ ] Contraseñas/connection strings tapadas si el video se va a compartir públicamente.
- [ ] Practica el guion en voz alta una vez, cronometrado, para que quede bajo 15 min
      y no suene leído palabra por palabra.
