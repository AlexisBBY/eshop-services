# Guion para el video — Microservicio de Órdenes de Compra

Duración sugerida: **12-15 minutos** (el profesor priorizó que quede bien explicado
sobre el tiempo exacto). Si se te hace muy largo, puedes cortarlo en 2 videos: uno de
arquitectura/código y otro de demo. Graba con OBS o QuickTime (Mac: Cmd+Shift+5).

**Formato de cada bloque:**
- **QUÉ HACER** → pasos exactos de qué abrir, dónde hacer clic, qué escribir.
- **QUÉ DECIR** → el texto para leer en voz alta mientras haces lo anterior.

Practica una vez sin grabar para ubicar las pestañas/ventanas antes de empezar.

---

## 0. Prepara tus pestañas antes de grabar

**QUÉ HACER** (antes de darle "grabar"):
1. Abre estas pestañas en tu navegador, en este orden, y déjalas listas sin cerrarlas:
   - Pestaña 1: tu sitio de Netlify (`https://alexisbbya.netlify.app`).
   - Pestaña 2: MongoDB Atlas, ya con sesión iniciada, en la pantalla de tu cluster.
2. Abre tu editor de código (VS Code) con el proyecto `eshop-services` cargado.
3. En el editor, abre desde ya estos 6 archivos en pestañas separadas (para no perder tiempo buscando durante la grabación):
   - `src/Order.API/Models/PurchaseOrder.cs`
   - `src/Order.API/Data/MongoOrderRepository.cs`
   - `src/Order.API/Application/OrderService.cs`
   - `src/Order.API/Endpoints/OrdersEndpoint.cs`
   - `src/Order.API/Program.cs`
   - `frontend/src/App.vue`
4. En el sitio de Netlify, entra a la sección del carrito y agrega 1 producto (para que ya esté listo cuando llegues a esa parte).

---

## 1. Introducción (30 s)

**QUÉ HACER:**
- Muestra tu escritorio o el editor con el proyecto abierto (la carpeta `eshop-services` visible en el explorador de archivos de VS Code).
- No hace falta hacer clic en nada todavía, solo que se vea el proyecto.

**QUÉ DECIR:**
> "Buenas, en este video voy a explicar la segunda fase del proyecto: la
> implementación de un tercer microservicio, Order.API, encargado de generar
> órdenes de compra a partir del carrito. A diferencia de Catalog.API y Basket.API,
> que persisten en PostgreSQL, este microservicio usa MongoDB Atlas como base de
> datos documental. Voy a explicar la arquitectura del código, y luego una
> demostración completa: desde agregar productos al carrito hasta consultar la
> orden generada, cambiarle el estado, descargarla en PDF, y verificar la
> persistencia directamente en MongoDB Atlas."

---

## 2. Arquitectura y código (4-5 min)

### 2.1 Vista general

**QUÉ HACER:**
- En el explorador de archivos de VS Code (panel izquierdo), expande la carpeta `src/Order.API` para que se vea su estructura de subcarpetas: `Models`, `Data`, `Application`, `Endpoints`, `Services`.

**QUÉ DECIR:**
> "El microservicio sigue el mismo patrón que Catalog.API y Basket.API: ASP.NET
> Core Minimal API con Carter para el enrutamiento, pero con una separación clara
> de responsabilidades en cuatro capas."

### 2.2 Modelo de dominio

**QUÉ HACER:**
- Haz clic en la pestaña del archivo `Models/PurchaseOrder.cs` (ya la tenías abierta).
- Con el mouse o el cursor, señala/subraya visualmente las propiedades `Id`, `CustomerId`, `CreatedAt`, `Status`, `Items`, `Subtotal`, `Tax`, `Total` mientras hablas de cada una.
- Cambia a la pestaña `Models/OrderItem.cs` y señala `ProductId`, `Quantity`, `UnitPrice`.

**QUÉ DECIR:**
> "Primero, el dominio: la clase `PurchaseOrder` representa la orden, con un
> identificador único, el cliente, la fecha de creación, el estado, la lista de
> `OrderItem`, y los totales: subtotal, impuesto y total. Cada `OrderItem` guarda
> el producto, la cantidad, y el precio unitario *congelado* al momento de la
> compra — no se recalcula después, aunque el precio del catálogo cambie."

### 2.3 Persistencia (MongoDB)

**QUÉ HACER:**
- Cambia a la pestaña `Data/MongoOrderRepository.cs`.
- Haz scroll hasta el método privado `RunAsync` (aprox. línea 22-34) y señálalo mientras lo explicas.
- Baja un poco más y señala el `catch (Exception ex) when (ex is MongoException or TimeoutException)`.
- Sube hasta el constructor (línea 13) y señala el `MongoClient(settings.Value.ConnectionString)`.

**QUÉ DECIR:**
> "La capa de persistencia usa el driver oficial de MongoDB para .NET. Cada
> operación pasa por un método `RunAsync` que envuelve las llamadas al driver: si
> MongoDB no está disponible o hay un timeout, se captura la excepción interna del
> driver y se traduce a un error genérico. Esto es importante porque el driver por
> defecto expone detalles internos del clúster en el mensaje de error, y eso viola
> el requisito de no exponer información sensible al cliente. Aquí también hay un
> detalle técnico relevante: el driver de MongoDB en su versión 3 requiere declarar
> explícitamente cómo serializar los `Guid`, cosa que se configura una sola vez al
> inicio de `Program.cs`."

### 2.4 Lógica de negocio

**QUÉ HACER:**
- Cambia a la pestaña `Application/OrderService.cs`.
- Señala el diccionario `AllowedTransitions` (aprox. línea 25-29) cuando menciones la máquina de estados.
- Baja hasta el método `CreateOrderAsync` y señala, en orden, mientras hablas:
  1. El bloque `if (!string.IsNullOrWhiteSpace(idempotencyKey))` (idempotencia).
  2. La línea `var basket = await basketClient.GetBasketAsync(...)`.
  3. El `foreach (var basketItem in basket.Items)` con la llamada a `catalogClient.ProductExistsAsync`.

**QUÉ DECIR:**
> "La capa de aplicación es donde vive la lógica de negocio. Al crear una orden,
> primero se revisa si ya existe una orden con el mismo `Idempotency-Key` para ese
> cliente — si existe, se devuelve esa misma orden en lugar de crear una nueva.
> Esto implementa idempotencia: si el cliente reintenta la misma solicitud, por
> ejemplo por un error de red, no se genera una compra duplicada. Después, se
> obtiene el carrito llamando por HTTP a Basket.API, se valida que no esté vacío, y
> por cada producto se valida que exista consultando a Catalog.API. Si algo falla
> en cualquiera de estas validaciones, se lanza una excepción que se traduce a
> `400 Bad Request`. Aquí también está la máquina de estados: un diccionario define
> las transiciones válidas — de `Pending` se puede pasar a `Confirmed` o a
> `Cancelled`, pero desde esos dos estados finales no se puede transicionar a
> ningún otro."

### 2.5 Endpoints

**QUÉ HACER:**
- Cambia a la pestaña `Endpoints/OrdersEndpoint.cs`.
- Señala, de arriba a abajo, cada bloque `group.MapPost`, `group.MapGet` (x3) y `group.MapPatch` a medida que los nombras.

**QUÉ DECIR:**
> "Finalmente, los endpoints: `POST /api/orders` para crear, `GET /api/orders` para
> listar todas, `GET /api/orders/{id}` para consultar una específica, `GET
> /api/orders/customer/{customerId}` para las de un cliente, y `PATCH
> /api/orders/{id}/status` para cambiar el estado. Todo documentado con Swagger."

### 2.6 Ensamblado del servicio (Program.cs)

**QUÉ HACER:**
- Abre `src/Order.API/Program.cs` (agrégalo a tus pestañas si no lo tenías).
- Señala, en orden, mientras hablas:
  1. La línea `BsonSerializer.RegisterSerializer(...)` hasta arriba del archivo.
  2. `builder.Services.AddSingleton<IOrderRepository, MongoOrderRepository>()`.
  3. Los dos bloques `AddHttpClient<IBasketApiClient, ...>` y `AddHttpClient<ICatalogApiClient, ...>`.
  4. `builder.Services.AddExceptionHandler<CustomExceptionHandler>()`.
  5. `builder.Services.AddSwaggerGen()` y, más abajo, `app.UseSwaggerUI()`.

**QUÉ DECIR:**
> "Todo esto se ensambla en `Program.cs`, el punto de entrada del microservicio.
> Aquí se registra el repositorio de MongoDB como singleton, y dos clientes HTTP
> tipados —`IBasketApiClient` y `ICatalogApiClient`— apuntando a las URLs de los
> otros microservicios, configuradas por variables de entorno. También se reutiliza
> `CustomExceptionHandler`, el mismo manejador de errores centralizado que ya usan
> Catalog.API y Basket.API, para mantener consistencia en cómo se devuelven los
> errores en toda la solución. Y se habilita Swagger, que genera automáticamente
> la documentación interactiva de la API a partir de estos mismos endpoints."

### 2.7 Frontend

**QUÉ HACER:**
- Cambia a la pestaña `frontend/src/App.vue`.
- Haz scroll rápido por el `<template>` mostrando (sin detenerte mucho) las tres secciones: el `cart-drawer`, la vista `orders`, y la vista `order` (detalle).
- No hace falta explicar línea por línea aquí, es un vistazo general.

**QUÉ DECIR:**
> "Del lado del frontend, en Vue 3, agregué un carrito en un panel deslizable
> activado por un ícono, para no saturar la pantalla principal. Al confirmar la
> compra, se abre el detalle de la orden en una pestaña nueva —usando parámetros de
> URL para simular navegación sin agregar un router completo—, y desde ahí se
> puede cambiar el estado de la orden y descargar un comprobante en PDF generado
> en el navegador con la librería jsPDF. También agregué una vista de 'Pedidos' que
> lista todas las órdenes, con filtro por cliente y paginación."

---

## 3. Demo completa en producción (7-8 min)

### 3.1 Agregar al carrito y comprar

**QUÉ HACER:**
1. Cambia a la Pestaña 1 (tu sitio de Netlify), ya debería tener 1 producto en el carrito de la preparación inicial.
2. Haz clic en el ícono del carrito 🛒 (arriba a la derecha) para abrir el panel.
3. Haz clic en el botón azul **"Realizar compra"**.
4. Se abre una pestaña nueva automáticamente — cámbiate a ella.

**QUÉ DECIR** (mientras haces los clics):
> "Ahora una demostración completa, todo contra el entorno de producción: frontend
> en Netlify, y los tres microservicios —Catalog, Basket y Order— desplegados en
> Render, cada uno con su propia base de datos en la nube. Agrego un producto al
> carrito, y al hacer clic en 'Realizar compra', el frontend llama a `POST
> /api/orders` con el `customerId`, el `basketId`, y un `Idempotency-Key` generado
> automáticamente. La orden se abre en una pestaña nueva, mostrando el
> identificador único, la fecha, la hora, los productos con sus cantidades y
> precios, y el estado inicial: `Pending`."

### 3.2 Verificar en MongoDB Atlas

**QUÉ HACER:**
1. Copia el ID de la orden que se ve en pantalla (selecciónalo y Cmd+C).
2. Cambia a la Pestaña 2 (MongoDB Atlas).
3. En el menú izquierdo, haz clic en **"Clusters"**, luego en el botón **"Browse Collections"** de tu cluster.
4. En la lista de bases de datos de la izquierda, haz clic en **`OrdersDb`**, y dentro de ella en la colección **`orders`**.
5. En la barra de búsqueda/filtro de documentos (arriba de la tabla de resultados), pega el ID y busca, o simplemente desplázate hasta encontrar el documento más reciente (aparece primero si ordenas por `_id` descendente).
6. Haz clic sobre el documento para expandirlo y que se vean todos sus campos.

**QUÉ DECIR:**
> "Para comprobar que la persistencia es real y no solo en memoria, entro a MongoDB
> Atlas y busco el documento con el mismo identificador que acabamos de ver — aquí
> está, con exactamente los mismos datos: cliente, items, subtotal, impuesto,
> total y estado."

### 3.3 Cambiar el estado y descargar PDF

**QUÉ HACER:**
1. Regresa a la pestaña del detalle de la orden (la que se abrió en el paso 3.1).
2. Haz clic en el botón azul **"Confirmar orden"**.
3. Observa que el badge de estado cambia de amarillo (`Pending`) a verde (`Confirmed`).
4. Haz clic en el botón **"📄 Descargar PDF"**.
5. Abre el archivo PDF descargado (desde la carpeta de Descargas) y muéstralo brevemente en pantalla.

**QUÉ DECIR:**
> "Cambio el estado de la orden de `Pending` a `Confirmed`, usando el endpoint
> `PATCH /api/orders/{id}/status`, que valida que la transición sea permitida. Y
> descargo el comprobante en PDF, generado del lado del cliente con fecha, hora,
> productos, cantidades, precios y total."

### 3.4 Vista de Pedidos

**QUÉ HACER:**
1. Regresa a la Pestaña 1 (Netlify), a la página principal del catálogo.
2. Haz clic en el botón **"📋 Pedidos"** (arriba a la derecha, junto al carrito) — se abre otra pestaña nueva.
3. En esa pestaña, muestra la tabla completa desplazándote un poco.
4. En el campo "Filtrar por cliente", escribe el nombre del cliente que usaste (ej. `Alexis`) y haz clic en **"Buscar"**.
5. Haz clic en **"Ver todas"** para regresar al listado completo.

**QUÉ DECIR:**
> "En la vista de Pedidos se listan todas las órdenes generadas, con paginación, y
> puedo filtrar por cliente específico para ver solo sus compras. Cada fila muestra
> el cliente, la fecha y hora exactas, los productos, el total y el estado con un
> color distinto según si está pendiente, confirmada o cancelada; y haciendo clic
> en cualquier parte de la fila se llega al mismo detalle que vimos antes."

### 3.5 Cierre de la demo: los tres servicios trabajando juntos

**QUÉ HACER:**
- Vuelve a la pestaña de Netlify, catálogo principal.
- Abre brevemente el panel del carrito con el ícono, y ciérralo, solo para dejar claro que sigue disponible.

**QUÉ DECIR:**
> "Con esto se completa el flujo de punta a punta: el catálogo y el carrito, que ya
> existían de la primera fase del proyecto, ahora se conectan con este nuevo
> microservicio de órdenes sin que se haya tocado su lógica interna — Order.API
> simplemente los consume por HTTP, como un cliente más. Esa es la ventaja de la
> arquitectura de microservicios: cada servicio se puede extender o reemplazar sin
> afectar a los demás, mientras se respete el contrato de su API."

---

## 4. Cierre (30-40 s)

**QUÉ HACER:**
- Vuelve a mostrar el editor con el proyecto, o el sitio de Netlify en la pantalla principal.

**QUÉ DECIR:**
> "En resumen: Order.API es un microservicio independiente con Minimal API,
> persistencia en MongoDB Atlas, idempotencia mediante `Idempotency-Key`, control
> de estados con transiciones validadas, e integración HTTP con los
> microservicios existentes de catálogo y carrito. Reutiliza los mismos patrones de
> diseño del resto del proyecto —manejo de errores centralizado, separación por
> capas, variables de entorno para toda la configuración sensible— y quedó
> publicado en la nube, funcionando en conjunto con el frontend en un flujo de
> compra completo y verificable de principio a fin. Gracias."

---

### Checklist antes de grabar
- [ ] Las 2 pestañas del navegador abiertas y listas (Netlify, MongoDB Atlas).
- [ ] Los 6 archivos del editor abiertos en pestañas separadas.
- [ ] Los 3 servicios en Render respondiendo (`/products`, `/health` de basket-api, `/api/orders` de order-api).
- [ ] MongoDB Atlas con el cluster activo (no pausado por inactividad — revisa antes de grabar, si dice "paused" dale Resume y espera).
- [ ] Un producto ya agregado al carrito en Netlify antes de empezar a grabar.
- [ ] Contraseñas/connection strings tapadas si el video se comparte públicamente.
