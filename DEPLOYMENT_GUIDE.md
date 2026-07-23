# Guía rápida de despliegue

## 1. Backend API

1. Asegúrate de tener .NET 9 SDK instalado.
2. Inicia la API de Catalog:
   - dotnet run --project src/Catalog.API/Catalog.API.csproj
3. La API queda disponible en:
   - http://localhost:5201

## 2. Base de datos

1. Levanta PostgreSQL con Docker Compose:
   - docker compose up -d catalogdb basketdb
2. Verifica que la base esté disponible en el puerto 5433.

## 3. Frontend Vue + Vite

1. Entra a la carpeta frontend:
   - cd frontend
2. Instala dependencias:
   - npm install
3. Ejecuta en desarrollo:
   - npm run dev
4. La app queda en:
   - http://localhost:5173

## 4. Publicar en Netlify

1. Sube la carpeta frontend a GitHub.
2. En Netlify, crea un nuevo sitio desde GitHub.
3. Selecciona la carpeta frontend como directorio de publicación.
4. Build command: npm run build
5. Publish directory: dist
6. Añade la variable de entorno:
   - VITE_API_URL=https://tu-api-en-produccion

## 5. Publicar la API en la nube

Opciones recomendadas:
- Azure App Service
- Render
- Railway
- Fly.io

Para Azure App Service:
1. Publica el proyecto de la API.
2. Define la cadena de conexión a PostgreSQL.
3. Configura CORS para permitir tu dominio de Netlify.

## 6. Publicar la base de datos

Opciones recomendadas:
- Neon.tech
- Supabase
- Azure Database for PostgreSQL

En producción, cambia la cadena de conexión en la API para apuntar a la base remota.
