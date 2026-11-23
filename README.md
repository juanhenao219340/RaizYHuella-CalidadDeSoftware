# Sistema de gestión de una clínica veterinaria (MVP)

Este repositorio contiene el MVP de un sistema web para gestionar la agenda de una clínica veterinaria. Para ejecutarlo solo necesita Docker y Git: el backend, el frontend y la base de datos se levantan con Docker Compose.

## 1. Descripción del proyecto

El sistema centraliza propietarios, mascotas, servicios y citas. Antes, la agenda se llevaba por WhatsApp, cuaderno o Excel, y eso causaba cruces de horarios y pérdida de información. Las reglas de negocio viven en el backend.

Hay dos tipos de usuario:

- Administrador: administra la clínica. Crea cuentas de propietario, gestiona servicios, consulta la agenda completa y cancela o reprograma citas con un motivo.
- Propietario: dueño de una o más mascotas. Gestiona sus mascotas, consulta la disponibilidad y agenda citas para ellas. Solo ve sus propias citas.

### Alcance del MVP

| Incluye | No incluye |
|---|---|
| Inicio de sesión con roles (administrador y propietario) | Auditoría de cambios |
| CRUD de propietarios (administrador) y de mascotas (propietario) | Historial de citas |
| Gestión de servicios (administrador) | Paginación en los listados |
| Agenda con cupos por franja y servicio | Cambio ni recuperación de contraseña |
| Cancelación y reprogramación de citas (administrador) | Registro público de usuarios |
| Bloqueo de cuenta tras intentos fallidos | Notificaciones por correo |

## 2. Tecnologías y versiones

| Componente | Versión | Dónde se declara |
|---|---|---|
| .NET (backend) | 10 (`net10.0`) | `backend\*\*.csproj` |
| ASP.NET Core Identity | 10.0.15 | `Veterinaria.Infrastructure.csproj` |
| Entity Framework Core | 10.0.15 (y `Design` 10.0.15) | `Veterinaria.Infrastructure.csproj` |
| Npgsql (proveedor de PostgreSQL para EF Core) | 10.0.3 | `Veterinaria.Infrastructure.csproj` |
| JWT Bearer (autenticación con tokens) | 10.0.15 | `Veterinaria.Api.csproj` |
| PostgreSQL | 16 (imagen `postgres:16`) | `docker-compose.yml` |
| React | 19 (`^19.1.0`) | `frontend\package.json` |
| Vite | 6 (`^6.3.0`) | `frontend\package.json` |
| React Router | 7 (`^7.6.0`) | `frontend\package.json` |
| Tailwind CSS | 4 (`^4.1.0`) | `frontend\package.json` |
| Source Sans 3 (fuente) | 5 (`@fontsource/source-sans-3` `^5.2.0`) | `frontend\package.json` |
| Node.js (imagen de build del frontend) | 20 (`node:20-alpine`) | `frontend\Dockerfile` |
| nginx (imagen que sirve el frontend) | `nginx:alpine` (sin versión fija) | `frontend\Dockerfile` |
| .NET SDK / runtime (imágenes del backend) | 10.0 (`dotnet/sdk:10.0`, `dotnet/aspnet:10.0`) | `backend\Dockerfile` |
| Docker Compose | v2 | `docker-compose.yml` |

Las versiones de React, Vite, React Router, Tailwind y la fuente son los rangos declarados en `package.json`. La versión exacta instalada depende de `package-lock.json`.

## 3. Estructura del repositorio

```text
repositorio/
├── backend/                     Solución .NET 10 en capas (Veterinaria.sln)
│   ├── Veterinaria.Api/         API ASP.NET Core
│   ├── Veterinaria.Application/ Casos de uso
│   ├── Veterinaria.Domain/      Entidades y reglas de negocio
│   ├── Veterinaria.Infrastructure/  EF Core, PostgreSQL, Identity, migraciones y datos iniciales
│   ├── Veterinaria.Domain.Tests/    Pruebas automatizadas del dominio
│   └── Dockerfile               Imagen del backend
├── frontend/                    Aplicación React + Vite (JavaScript)
│   ├── src/                     Código de la aplicación
│   ├── public/                  Archivos estáticos
│   ├── nginx.conf               Configuración de nginx para servir la app
│   └── Dockerfile               Imagen del frontend
├── docker-compose.yml           Servicios: PostgreSQL, backend y frontend
├── .env                         Configuración local (no se versiona; ver sección 5)
└── README.md                    Este documento
```

### Backend

- `Veterinaria.Api`: la capa de entrada. Tiene los controladores, el manejo de errores (en español), la autenticación (Basic Auth para el login y JWT para el resto), los contratos de entrada y salida (DTO) y las opciones de configuración.
- `Veterinaria.Application`: los casos de uso (crear una cita, cancelarla, reprogramar, etc.). Coordina el dominio y la persistencia.
- `Veterinaria.Domain`: el corazón del sistema. Contiene las entidades (propietario, mascota, servicio, cita) y las reglas de negocio: fechas, horas, cupos, motivo, radicado y bloqueo. No depende de la base de datos.
- `Veterinaria.Infrastructure`: todo lo que toca la base de datos. Incluye el contexto de EF Core, la configuración de ASP.NET Core Identity, las migraciones y la creación del administrador inicial.
- `Veterinaria.Domain.Tests`: pruebas de las reglas de dominio (ver sección 8).

### Frontend

- `Pages`: una pantalla por ruta (inicio de sesión, citas, mascotas, propietarios, servicios, perfil y reserva de cita).
- `Components`: piezas reutilizables, como tablas, formularios, diálogos de confirmación o motivo, el selector de disponibilidad y los componentes que protegen las rutas según el rol.
- `Services`: la comunicación con la API (cliente HTTP, un servicio por recurso) y funciones de validación, formato y mensajes de error.
- `Context`: la sesión del usuario (token y rol), compartida por toda la aplicación.
- `public`: archivos estáticos que se copian tal cual al build.

## 4. Reglas de negocio

### Reglas obligatorias

| Regla | Qué establece | Si se incumple |
|---|---|---|
| RN-01 Fecha | La fecha de la cita no puede ser anterior a hoy (zona America/Bogota). | 422. No se guarda la cita. |
| RN-02 Cupos | Cupos disponibles = capacidad (4) − citas confirmadas de la misma fecha, hora y servicio. Las citas anuladas no ocupan cupo. | No es un error por sí mismo. Cuando el cupo llega a 0, aplica RN-03. |
| RN-03 Franja llena | Si una franja no tiene cupos, no admite nuevas citas. Al anular una cita, el cupo se libera. | 409. |
| RN-04 Motivo | Cancelar o reprogramar exige un motivo no vacío (después de quitar espacios), de máximo 500 caracteres. Solo el administrador puede hacerlo. | Motivo vacío o de más de 500 caracteres: 422. Propietario que intenta cancelar o reprogramar: 403. |
| RN-05 Bloqueo | Tras 3 intentos fallidos de contraseña, la cuenta queda bloqueada 2 minutos. Aplica a todos los roles. Los intentos durante el bloqueo no alargan el tiempo. | Durante el bloqueo: 423, con cabecera `Retry-After` y el tiempo restante en el mensaje. Un intento fallido sin bloqueo: 401. |
| P-01 Hora de hoy | Si la cita es para hoy, la hora de inicio debe ser posterior a la hora actual. Se aplica junto con RN-01. | 422 (la misma respuesta que RN-01). |
| P-02 Mascota | Una mascota no puede tener dos citas no anuladas a la misma fecha, hora y servicio. Sí puede tener citas a la misma hora en servicios distintos. | 409. |

### Otras reglas que ya están implementadas

- Franjas: 10 franjas de 1 hora por día y servicio, con inicio en punto de 08:00 a 17:00.
- Zona horaria: America/Bogota, para validar fechas y generar radicados.
- Radicado: formato `RAD-AAAAMMDD-####`, único en toda la base de datos. El consecutivo `####` empieza en `0001` cada día.
- Disponibilidad del administrador: solo ve y reprograma a franjas con cupo y de fecha futura. El backend vuelve a validarlo al reprogramar.
- Anulación de citas al eliminar un servicio: eliminar un servicio anula de inmediato sus citas confirmadas y futuras, con el motivo "Servicio eliminado por la clínica.". Las citas pasadas y las ya anuladas no cambian.
- Cita ya anulada: anularla o reprogramarla de nuevo responde 409. Una cita anulada no se reactiva.
- Conflicto de versión: si dos operaciones modifican la misma cita, la segunda responde 409 con el mensaje "La cita fue modificada por otra operación; vuelva a consultarla.".
- Borrado lógico: eliminar usuarios, mascotas o servicios los marca como eliminados y no borra la fila. No hay restauración en el MVP.
- Usuario eliminado: no puede iniciar sesión (401), y un token que ya tenía deja de servir en su siguiente petición (401).
- Propietario y su propia cuenta: un propietario no puede eliminar su propia cuenta (403). Solo el administrador elimina cuentas de propietario.
- Prefijo `DEL-`: está reservado para las identificaciones de propietarios eliminados. Una identificación que empiece por `DEL-` responde 400.
- Bloqueo de cuenta: el mensaje indica cuánto falta para desbloquear, en minutos y segundos.
- Mascotas sin identificador propio: las mascotas no tienen un campo de identificación propio (se quitó del MVP). Se distinguen por su identificador interno de la base de datos y por su propietario.
- Visibilidad del propietario: el administrador ve el nombre del propietario en el listado de mascotas y en la agenda. El propietario solo ve sus mascotas y sus citas, y no tiene filtro por identificación.
- Búsqueda de citas: ambos roles pueden filtrar por nombre de mascota, con coincidencia parcial sin distinguir mayúsculas y minúsculas; el filtro se combina con los demás.
- Errores de validación: usan `ProblemDetails`, con las claves del campo `errors` en camelCase.

## 5. Archivo `.env`

El archivo `.env` está en la raíz de `repositorio\`. Docker Compose lo lee al construir y arrancar los servicios. Concentra toda la configuración del MVP: la base de datos, los datos del administrador inicial, las credenciales, los puertos y la dirección de la API.

**El archivo `.env` contiene credenciales.** Cada persona debe definir sus propios valores, y no debe compartirlos ni subirlos a un repositorio público. Los nombres de las variables que aparecen abajo son los que usa el proyecto; los valores no se incluyen en este documento.

### Formato de ejemplo (los valores son marcadores; defina los suyos):

```text
POSTGRES_DB=<nombre de la base>
POSTGRES_USER=<usuario de PostgreSQL>
POSTGRES_PASSWORD=<contraseña de PostgreSQL>
ASPNETCORE_ENVIRONMENT=Development
BACKEND_PORT=5000
FRONTEND_PORT=5173
VITE_API_BASE_URL=http://localhost:5000/api
BusinessRules__SlotCapacity=4
BusinessRules__TimeZone=America/Bogota
Lockout__MaxFailedAttempts=3
Lockout__LockoutMinutes=2
SEED_ADMIN_EMAIL=<correo del administrador>
SEED_ADMIN_PASSWORD=<contraseña del administrador>
SEED_ADMIN_IDENTIFICATION=<identificación del administrador>
SEED_ADMIN_NAME=<nombre del administrador>
Cors__AllowedOrigin=http://localhost:5173
Jwt__Key=<clave de al menos 32 caracteres>
BasicAuth__Username=<usuario de Basic Auth>
BasicAuth__Password=<contraseña de Basic Auth>
```

### Variables por grupo

#### Base de datos

| Variable | Para qué sirve |
|---|---|
| `POSTGRES_DB` | Nombre de la base de datos. |
| `POSTGRES_USER` | Usuario de PostgreSQL. |
| `POSTGRES_PASSWORD` | Contraseña de PostgreSQL. Sin ella, el contenedor no arranca. |

La cadena de conexión del backend (`ConnectionStrings__Veterinary`) no está en `.env`: `docker-compose.yml` la construye con las variables anteriores.

#### Backend

| Variable | Para qué sirve |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | Entorno de ejecución. El proyecto usa un único entorno local (`Development`). |
| `BusinessRules__SlotCapacity` | Capacidad de citas confirmadas por franja y servicio (4). |
| `BusinessRules__TimeZone` | Zona horaria para validar fechas y generar radicados (`America/Bogota`). |
| `Lockout__MaxFailedAttempts` | Intentos fallidos permitidos antes de bloquear la cuenta (3). |
| `Lockout__LockoutMinutes` | Duración del bloqueo, en minutos (2). |

#### Semilla del administrador inicial

| Variable | Para qué sirve |
|---|---|
| `SEED_ADMIN_EMAIL` | Correo del administrador inicial. |
| `SEED_ADMIN_PASSWORD` | Contraseña del administrador inicial. |
| `SEED_ADMIN_IDENTIFICATION` | Identificación del administrador inicial. |
| `SEED_ADMIN_NAME` | Nombre del administrador inicial. |

Al arrancar, el administrador se crea solo si no existe. Cambiar `SEED_ADMIN_EMAIL` crea otro administrador; no renombra ni borra el anterior. Si falta alguna de estas cuatro variables, la API no arranca.

#### CORS y JWT

| Variable | Para qué sirve |
|---|---|
| `Cors__AllowedOrigin` | Origen del frontend que puede llamar a la API (por ejemplo, `http://localhost:5173`). |
| `Jwt__Key` | Clave con la que se firman los tokens de sesión. Debe tener al menos 32 caracteres. |

#### Basic Auth del login

| Variable | Para qué sirve |
|---|---|
| `BasicAuth__Username` | Usuario que exige el login, además de correo y contraseña. |
| `BasicAuth__Password` | Contraseña de esa autenticación Basic. |

Estas dos variables también alimentan el build del frontend (`VITE_BASIC_AUTH_USERNAME` y `VITE_BASIC_AUTH_PASSWORD` se toman de ellas en `docker-compose.yml`). No hace falta definirlas por separado.

#### Frontend y puertos

| Variable | Para qué sirve |
|---|---|
| `VITE_API_BASE_URL` | Dirección de la API que usa el frontend (por ejemplo, `http://localhost:5000/api`). |
| `BACKEND_PORT` | Puerto del equipo por donde se expone la API (5000 por defecto en la guía). |
| `FRONTEND_PORT` | Puerto del equipo por donde se expone el frontend (5173 por defecto en la guía). |

**Importante:** las variables `VITE_*` se incorporan al código del frontend en el momento de construir la imagen. Si cambia `VITE_API_BASE_URL` (o las credenciales de Basic Auth), hay que **reconstruir** la imagen del frontend con `docker compose build frontend`. Reiniciar no basta.

## 6. Requisitos

- Docker Desktop (en Windows o macOS) o Docker Engine con Compose v2 (en Linux), en ejecución.
- Git, para clonar el repositorio.

No hace falta instalar .NET, Node.js ni PostgreSQL en el equipo para ejecutar el sistema con Compose: todos corren dentro de contenedores.

Solo si quiere ejecutar las pruebas de dominio, necesita el .NET SDK 10.

### Puertos que usa el sistema

| Puerto | Servicio | Notas |
|---|---|---|
| 5000 | Backend (API) | Se configura con `BACKEND_PORT`. |
| 5173 | Frontend | Se configura con `FRONTEND_PORT`. Es la dirección que abre en el navegador. |
| No se publica | PostgreSQL | Solo es accesible desde la red interna de Compose. |

## 7. Cómo ejecutar el proyecto

1. Clone el repositorio y entre a la carpeta `repositorio`:

   ```powershell
   git clone (https://github.com/juanhenao219340/RaizYHuella-CalidadDeSoftware.git)
   cd RaizYHuella-CalidadDeSoftware
   ```

2. Compruebe la existencia del archivo `.env` en esa carpeta, con las variables de la sección 5 y sus propios valores.

3. Construya las imágenes:

   ```powershell
   docker compose build
   ```

4. Levante los servicios en segundo plano:

   ```powershell
   docker compose up -d
   ```

   La primera vez, la API aplica las migraciones y crea el administrador inicial antes de quedar lista.

5. Compruebe que los servicios están activos:

   ```powershell
   docker compose ps
   ```

   Los tres servicios (`postgres`, `backend` y `frontend`) deben aparecer como `running`, y `postgres` como `healthy`.

6. Abra la aplicación en el navegador: `http://localhost:5173`. Inicie sesión con las credenciales del administrador inicial (`SEED_ADMIN_EMAIL` y `SEED_ADMIN_PASSWORD`).

### Ver registros

```powershell
docker compose logs -f backend
docker compose logs -f frontend
docker compose logs -f postgres
```

Si la API no arranca, revise primero el registro de `backend`. Si falta una variable obligatoria, el mensaje indica su nombre.

### Parar sin borrar datos

```powershell
docker compose stop
```

`stop` detiene los contenedores y conserva los datos de PostgreSQL. Para volver a arrancarlos, use `docker compose start`.

**No use `docker compose down -v`.** Borra el volumen de PostgreSQL y, con él, toda la base de datos.

`docker compose down` detiene y elimina los contenedores, pero no borra el volumen de datos. Úselo solo cuando sea necesario, por ejemplo para recrear los contenedores después de cambiar la configuración.

Reiniciar el entorno completamente desde cero

Si desea reiniciar el MVP desde cero para realizar nuevamente las pruebas, eliminando los contenedores, las imágenes construidas localmente y los datos almacenados en PostgreSQL, ejecute los siguientes comandos desde la raíz del repositorio:

```powershell
docker compose down -v --rmi local --remove-orphans
docker compose build --no-cache
docker compose up -d
```

Esto realiza lo siguiente:

Elimina los contenedores del proyecto.

Elimina el volumen postgres_data, borrando todos los datos de la base de datos.

Elimina las imágenes construidas localmente por Docker Compose.

Elimina contenedores huérfanos asociados al proyecto.

Reconstruye las imágenes sin utilizar la caché de construcción.

Levanta nuevamente PostgreSQL, backend y frontend, ejecutando el proceso de inicialización del sistema como en una instalación nueva.

Advertencia: esta operación elimina definitivamente los datos almacenados en la base de datos del entorno. No use estos comandos si desea conservar propietarios, mascotas, servicios o citas existentes.

## 8. Pruebas

El proyecto `backend\Veterinaria.Domain.Tests` contiene pruebas automatizadas (xUnit) de las reglas de dominio: radicado, fecha y hora, cupos, franja no disponible, motivo, P-02 y horario de 08:00 a 17:00. Incluye también una prueba del bloqueo de cuenta (RN-05) que usa ASP.NET Core Identity en memoria.

Para ejecutarlas, desde la raíz del repositorio:

```powershell
dotnet test backend\Veterinaria.Domain.Tests
```

No hay pruebas de integración automatizadas. La base de datos, Docker, la concurrencia real y los flujos de la API y de la interfaz se verifican de forma manual.

## 9. Solución de problemas

a) El build del backend falla con "invalid file request Dockerfile" (Windows con OneDrive). Es un problema del constructor de Docker con archivos en OneDrive. Como solución temporal, use el constructor clásico solo en la sesión de PowerShell actual:

```powershell
$env:DOCKER_BUILDKIT = "0"
$env:COMPOSE_DOCKER_CLI_BUILD = "0"
docker compose build
```

Estas variables duran solo mientras esa ventana de PowerShell esté abierta.

b) Aviso sobre `libgssapi_krb5` al arrancar el backend. Es benigno. Viene de la imagen base y no afecta al funcionamiento de la API.

c) Aviso "Overriding HTTP_PORTS". Es benigno. La imagen base de ASP.NET lo muestra porque define `HTTP_PORTS`. El puerto correcto lo fija el `Dockerfile` (`ASPNETCORE_URLS=http://+:8080`).

d) Un puerto ya está ocupado. Cambie `BACKEND_PORT` o `FRONTEND_PORT` en `.env` por otro libre, y vuelva a levantar los servicios con `docker compose up -d`. Si cambia `FRONTEND_PORT`, no hace falta reconstruir la imagen. Si cambia `VITE_API_BASE_URL`, sí.

## 10. Límites conocidos

Estos puntos son decisiones aceptadas para un MVP local, de un solo entorno. Revísenlos antes de usar el sistema en otro contexto.

- Sin auditoría: el sistema no registra quién cambió qué ni cuándo, salvo el motivo y la fecha del último cambio de una cita.
- Sin cambio ni recuperación de contraseña: no hay pantalla ni flujo para cambiarla o recuperarla.
- Credenciales de Basic Auth en el frontend: el usuario y la contraseña de Basic Auth quedan dentro del código que descarga el navegador. Cualquiera que vea ese código puede leerlos. **Riesgo aceptado.**
- Token JWT en `localStorage`: el navegador guarda el token en `localStorage`, donde un script malicioso podría leerlo. **Riesgo aceptado.** El token dura 60 minutos y no hay revocación en el servidor: cerrar sesión solo borra el token del navegador.
- Entorno de desarrollo: el proyecto usa `ASPNETCORE_ENVIRONMENT=Development` como único entorno. No hay configuración de producción.
- Claves de DataProtection sin volumen: las claves se generan dentro del contenedor del backend. Si el contenedor se recrea, se regeneran. El impacto es bajo porque el sistema no usa cookies de sesión ni recuperación de contraseña.

## 11. Documentación del proyecto

- Especificación: `specifications\sistema-gestion-canina-mvp\specification.md`, en la carpeta `specifications\` que está junto a `repositorio\`. Define el alcance, los requisitos funcionales, las reglas de negocio y el modelo de datos.
- Documentos de tareas: se organizan en la misma carpeta `specifications\`. Este README no los describe en detalle.
- Cambios por fases: los cambios se proponen, se revisan y se aprueban por fases. La especificación lleva un número de revisión (r1, r2 … r19) y cada cambio indica qué decisión lo aprobó.

## 12. Referencia rápida de la API

Todas las rutas usan el prefijo `/api`, reciben y devuelven JSON en camelCase, y responden en español cuando hay un error. Salvo el login, todas exigen `Authorization: Bearer <token>`. El token dura 60 minutos.

| Método | Ruta | Rol |
|---|---|---|
| `POST` | `/api/auth/login` | Público, con Basic Auth |
| `GET` | `/api/auth/me` | Propietario o administrador |
| `GET`, `POST` | `/api/users` | Administrador |
| `GET`, `PUT` | `/api/users/me` | Propietario o administrador |
| `GET`, `PUT` | `/api/users/{id}` | Propietario (solo la propia cuenta) o administrador (propietarios) |
| `DELETE` | `/api/users/{id}` | Administrador |
| `GET`, `POST` | `/api/pets` | Propietario (sus mascotas); el administrador solo lee |
| `GET`, `PUT`, `DELETE` | `/api/pets/{id}` | Propietario (la suya); el administrador solo lee |
| `GET`, `POST` | `/api/services` | Propietario o administrador (lectura); administrador (crear) |
| `PUT`, `DELETE` | `/api/services/{id}` | Administrador |
| `GET` | `/api/appointments` | Propietario (sus citas) o administrador (todas) |
| `GET` | `/api/appointments/availability` | Propietario (para agendar) o administrador (para reprogramar) |
| `POST` | `/api/appointments` | Propietario |
| `PUT` | `/api/appointments/{id}` | Administrador (reprogramar, con motivo) |
| `POST` | `/api/appointments/{id}/cancel` | Administrador (cancelar, con motivo) |

Notas:

- Las fechas van en formato `yyyy-MM-dd` y las horas de inicio en `HH:mm`, de 08:00 a 17:00.
- Los tipos de servicio son `Clinic` (clínica), `Aesthetics` (estética) y `Recreation` (recreación).
- `GET /api/appointments` admite el parámetro opcional `petName` (hasta 100 caracteres); espacios exteriores se ignoran y `%`, `_` y `\` se buscan literalmente.
- Un propietario que pide un recurso ajeno recibe 404 genérico. En el listado de citas, un filtro con datos ajenos devuelve una lista vacía.
- Las respuestas nunca incluyen contraseñas ni hashes.
