# Guion para el video de entrega

Duración máxima: **15 minutos**. Graba con OBS Studio o QuickTime (Mac: Cmd+Shift+5 →
"Grabar toda la pantalla" o una ventana).

Este guion está escrito como **texto para leer en voz alta** mientras grabas, con
vocabulario técnico. Las líneas en cursiva entre paréntesis indican qué mostrar en
pantalla en ese momento. Practícalo una vez en voz alta antes de grabar.

---

## 1. Introducción (30 s)

*(Muestra el proyecto abierto en el editor)*

> "Buenas, en este video voy a presentar el proyecto eshop-services, una arquitectura
> de microservicios desarrollada en .NET 9 siguiendo el patrón CQRS, es decir,
> separación entre comandos y consultas. Voy a explicar la arquitectura del código,
> mostrar el catálogo de productos y el carrito de compras funcionando en producción,
> y el proceso paso a paso de cómo publiqué las bases de datos, las APIs, la caché y
> el frontend en la nube."

## 2. Arquitectura y código (2-3 min)

*(Abre el editor y muestra los archivos mientras hablas)*

> "El proyecto tiene dos microservicios. Catalog.API expone un API REST para la
> gestión de productos: búsqueda por nombre, filtrado por categoría y rango de
> precio, inserción, actualización, eliminación y consulta paginada. Basket.API
> gestiona el carrito de compras con una caché distribuida sobre Redis."

*(Abre `GetProductsEndPoint.cs` y `GetProductsQueryHandler.cs`)*

> "Cada microservicio expone sus endpoints usando Carter, una librería de enrutamiento
> sobre Minimal APIs de ASP.NET Core, y la lógica de negocio se despacha con MediatR,
> implementando el patrón mediador: cada operación es un Command o una Query con su
> respectivo Handler. Este endpoint de consulta de productos acepta filtros
> combinables por nombre, categoría y rango de precio, todos opcionales, y devuelve
> el resultado paginado. La persistencia se maneja con Marten, un ORM de tipo
> document database sobre PostgreSQL: los objetos .NET se serializan directamente a
> columnas JSONB, sin necesidad de mapeo relacional explícito ni migraciones manuales."

*(Abre `CacheBasketRepository.cs` y `Program.cs` de Basket.API)*

> "Basket.API implementa el patrón decorador para el manejo de caché: la clase
> `BasketRepository` accede directamente a PostgreSQL, y `CacheBasketRepository` la
> envuelve agregando una capa de caché distribuida con Redis, a través de la interfaz
> estándar `IDistributedCache` de .NET. Esto se registra en el contenedor de
> inyección de dependencias con el método `Decorate`, de la librería Scrutor, sin
> modificar el contrato de la interfaz `IBasketRepository`. Con esto, el servicio es
> completamente compatible con Redis como caché distribuida: las lecturas del carrito
> primero consultan Redis, y solo si no hay dato en caché se consulta la base
> relacional, reduciendo la carga sobre PostgreSQL."

*(Abre `App.vue`)*

> "El frontend está construido con Vue 3, usando la Composition API y `<script setup>`,
> y Vite como herramienta de build. Consume ambas APIs mediante `fetch`, con manejo
> reactivo de estado para el catálogo, los filtros, la paginación y el carrito."

## 3. Demo funcional en producción (3-4 min)

*(Abre en el navegador la URL real de Netlify, por ejemplo https://alexisbbya.netlify.app)*

> "Ahora una demostración funcional completa, directamente contra el entorno de
> producción: el sitio publicado en Netlify, que consume las dos APIs publicadas en
> Render, las cuales a su vez persisten en las bases de datos publicadas en Neon, y
> en el caso del carrito, también en la caché publicada en Upstash."

*(Inserción de producto)*

> "Inserción de un producto nuevo: nombre, descripción, categorías, imagen y precio."

*(Búsqueda por nombre)*

> "Búsqueda por nombre."

*(Filtro combinado)*

> "Filtro combinado por categoría y rango de precio, aplicados simultáneamente."

*(Actualización)*

> "Actualización de un producto existente."

*(Eliminación)*

> "Eliminación por nombre."

*(Paginación)*

> "Y la consulta paginada: el listado completo se pagina desde el backend con los
> parámetros `pageIndex` y `pageSize`."

*(Sección Carrito: cargar carrito, agregar productos, quitar uno, vaciar)*

> "Y aquí el carrito de compras: cargo el carrito de un usuario, agrego productos del
> catálogo, quito uno, y vacío el carrito por completo. Cada una de estas operaciones
> viaja hasta Basket.API en Render, que lee y escribe tanto en PostgreSQL como en la
> caché de Redis en Upstash."

## 4. Publicar las bases de datos: Neon (2 min)

*(Navegador: neon.tech)*

> "Para publicar las bases de datos en un dominio propio en la nube, usé Neon, un
> servicio de PostgreSQL serverless. Creé un proyecto, y dentro de él, dos bases de
> datos: una para el catálogo y otra para el carrito."

*(Muestra la pantalla de "Connect", cambiando el selector de base de datos)*

> "Desde el botón 'Connect' del proyecto se obtiene el connection string de conexión
> a PostgreSQL para cada base —oculto la contraseña por seguridad—. Ese host,
> terminado en `neon.tech`, es el dominio en la nube de las bases de datos."

## 5. Publicar la caché: Redis en Upstash (1-2 min)

*(Navegador: upstash.com)*

> "Para la caché distribuida de Basket.API usé Upstash, un servicio de Redis
> administrado. Creé una base de datos Redis de tipo regional, y desde la sección
> 'Connect' obtuve el connection string en el formato que necesita el cliente
> StackExchange.Redis de .NET."

## 6. Publicar las APIs: Render (3 min)

*(Navegador: GitHub, mostrando el repositorio)*

> "El código está versionado en un repositorio de GitHub. Para publicar ambas APIs en
> la nube usé Render, conectado directamente a ese repositorio: cada microservicio es
> un 'Web Service' independiente, cada uno con su propio Dockerfile dentro del mismo
> repositorio."

*(Muestra la configuración de uno de los servicios: runtime Docker, Dockerfile path, variables de entorno)*

> "En cada servicio configuré el runtime como Docker, la ruta del Dockerfile
> correspondiente, y las variables de entorno: el connection string de Neon para la
> base de datos, el de Upstash para Redis en el caso de Basket.API, el origen
> permitido para CORS, y el entorno de ejecución en modo Production."

*(Muestra el log de deploy terminando en éxito y ambas URLs públicas)*

> "Al desplegar, Render construye la imagen Docker de cada servicio automáticamente y
> los publica en URLs propias sobre HTTPS. Cada `git push` al repositorio dispara un
> nuevo despliegue automático en ambos servicios."

*(Terminal: `curl` a `/products` y a `/health`)*

> "Aquí confirmo que la API de catálogo responde correctamente, y que la de carrito
> reporta sus dos dependencias, PostgreSQL y Redis, en estado saludable."

## 7. Publicar el frontend: Netlify (2 min)

*(Navegador: netlify.com)*

> "Para el frontend usé Netlify. El proceso: crear una cuenta, conectarla a GitHub, e
> importar el mismo repositorio."

*(Muestra la configuración de build y las variables de entorno)*

> "En la configuración de build indiqué el directorio `frontend` como base, el
> comando `npm run build` con Vite, y `dist` como directorio de publicación. Las URLs
> de ambas APIs publicadas en Render se inyectan en tiempo de compilación mediante
> las variables de entorno `VITE_API_URL` y `VITE_BASKET_API_URL`."

*(Muestra el deploy terminado y la URL pública)*

> "Al desplegar, Netlify genera la URL pública final, y también aquí cada nuevo
> `git push` dispara un despliegue automático."

## 8. Cierre (30 s)

> "En resumen: las bases de datos están publicadas en Neon, la caché distribuida en
> Upstash, las dos APIs —catálogo y carrito— publicadas en Render, y el frontend
> publicado en Netlify, todo consumiéndose entre sí en producción. Gracias."

---

### Checklist antes de grabar
- [ ] `git push` hecho y repo visible en GitHub.
- [ ] Ambos servicios en Render desplegados y respondiendo
      (`/products` y `/health` antes de grabar).
- [ ] Sitio en Netlify desplegado y probado end-to-end: catálogo completo
      (crear/buscar/filtrar/actualizar/eliminar/paginar) y carrito
      (cargar/agregar/quitar/vaciar).
- [ ] `Cors__AllowedOrigins` en ambos servicios de Render actualizado con la URL
      final de Netlify.
- [ ] Contraseñas/connection strings tapadas si el video se va a compartir públicamente.
- [ ] Practica el guion en voz alta una vez, cronometrado, para que quede bajo 15 min
      y no suene leído palabra por palabra.
