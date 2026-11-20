# Sistema de gestión de servicios caninos (MVP)

Aplicación web para un negocio pequeño que atiende perros en tres servicios: clínica, estética y recreación. Hasta ahora la agenda se llevaba por WhatsApp, cuaderno o Excel, lo que causaba cruces de horarios y pérdida de información. El MVP centraliza propietarios, mascotas, servicios y citas, y aplica las reglas de negocio en el backend.

## 1. Descripción del MVP

El sistema tiene dos roles:

- **Propietario:** dueño de una o más mascotas.
  - Gestiona sus datos y los de sus mascotas (listar, consultar, crear, editar y eliminar sus mascotas).
  - Consulta los servicios para elegir uno al agendar.
  - Ve la disponibilidad por fecha y franja, y agenda un servicio para una de sus mascotas.
  - Ve solo sus propias citas. No cancela ni reprograma citas.
  - No puede eliminar su propia cuenta.
- **Administrador:** administra el negocio.
  - Solo crea cuentas de propietario. No hay registro público.
  - Gestiona propietarios (listar, consultar, crear, editar y eliminar) y servicios (crear, editar, eliminar y consultar).
  - Consulta mascotas en modo solo lectura. No las crea, edita ni elimina.
  - Ve la agenda completa, incluidas las citas anuladas.
  - Cancela y reprograma citas con un motivo obligatorio. No crea citas nuevas.
  - Reprograma solo a franjas disponibles, y consulta la disponibilidad únicamente para ese fin.

### Reglas de negocio

- **RN-V1 (fecha de la cita):** la fecha debe ser igual o posterior a la fecha actual. Si no lo es, la API responde 422 y no guarda la cita.
- **P-01 (hora del día actual):** si la cita es para hoy, la hora de inicio debe ser posterior a la hora actual. RN-V1 y P-01 se aplican juntas.
- **RN-C2 (cupos):** cada franja de un servicio tiene capacidad para 4 citas confirmadas. Los cupos disponibles son la capacidad menos las citas confirmadas de esa fecha, hora y servicio. Las citas anuladas no ocupan cupo.
- **RN-D3 (franja no disponible):** si una franja no tiene cupos, no admite nuevas citas. Al anular una cita, el cupo se libera y la franja vuelve a estar disponible.
- **RN-R4 (cancelar y reprogramar):** solo el administrador puede cancelar o reprogramar una cita, y debe indicar un motivo. El motivo no puede quedar vacío después de quitar los espacios y tiene un máximo de 500 caracteres. Una cita anulada no se puede reactivar.
- **RN-S5 (bloqueo de cuenta):** tras 3 intentos fallidos de contraseña, la cuenta queda bloqueada durante 2 minutos. El bloqueo aplica a todos los roles, incluido el administrador.
- **P-02 (una mascota por franja):** una mascota no puede tener dos citas no anuladas a la misma fecha, hora y servicio. Sí puede tener citas a la misma hora en servicios distintos.

Horario de citas: 10 franjas de 1 hora por día y servicio, con inicio de 08:00 a 17:00. La zona horaria de referencia es America/Bogota.

## 2. Requisitos

- Docker con Docker Compose v2
- Git

No hace falta instalar .NET ni Node en el equipo para ejecutar el MVP: todo corre en contenedores.

## 3. Estructura del repositorio

```text
Veterinaria/
├── backend/                 Solución .NET 10 en capas (Veterinaria.sln)
│   ├── Veterinaria.Api/             API ASP.NET Core (controladores, autenticación, errores)
│   ├── Veterinaria.Application/     Casos de uso
│   ├── Veterinaria.Domain/          Entidades y reglas de negocio
│   ├── Veterinaria.Infrastructure/  EF Core, PostgreSQL e Identity
│   └── Dockerfile                   Imagen del backend
├── frontend/                React + Vite (JavaScript)
├── specifications/          Especificación del MVP
├── docker-compose.yml       Servicios de frontend, backend y PostgreSQL
├── .env                     Configuración única del MVP
└── README.md
```

## 4. Configuración

Existe **un único archivo de configuración: `.env`, en la raíz del repositorio.** Está versionado y Docker Compose lo lee automáticamente. No hay plantilla que copiar ni archivo que generar.

Si quiere cambiar algún valor, edítelo en `.env` antes de levantar los servicios.

> **Advertencia de seguridad:** `.env` contiene credenciales locales versionadas (contraseña de PostgreSQL, contraseña del administrador inicial, credenciales Basic del login y clave de firma de los tokens). Es un riesgo aceptado para este MVP local, de un solo entorno. **No use estos valores en producción** y cámbielos antes de exponer el sistema.

### Variables de entorno

Estas son las variables que usa el sistema. Ninguna tiene valor en este documento; los valores están en `.env`.

**Obligatorias para la API.** Si falta cualquiera de ellas, o está vacía, la API no arranca:

| Variable | Para qué sirve |
|---|---|
| `Jwt__Key` | Clave con la que se firman los tokens de sesión. Debe tener al menos 32 caracteres. |
| `BasicAuth__Username` | Usuario de la autenticación Basic que exige el login. |
| `BasicAuth__Password` | Contraseña de la autenticación Basic que exige el login. |
| `Cors__AllowedOrigin` | Origen del frontend permitido por CORS. |
| `SEED_ADMIN_EMAIL` | Correo del administrador inicial. |
| `SEED_ADMIN_PASSWORD` | Contraseña del administrador inicial. |
| `SEED_ADMIN_IDENTIFICATION` | Identificación del administrador inicial. |
| `SEED_ADMIN_NAME` | Nombre del administrador inicial. |
| `BusinessRules__SlotCapacity` | Capacidad de citas confirmadas por franja y servicio. |
| `BusinessRules__TimeZone` | Zona horaria para validar fechas de citas y generar radicados. |
| `Lockout__MaxFailedAttempts` | Intentos fallidos permitidos antes de bloquear la cuenta. |
| `Lockout__LockoutMinutes` | Duración del bloqueo, en minutos. |

**Para PostgreSQL, puertos y frontend:**

| Variable | Para qué sirve |
|---|---|
| `POSTGRES_DB` | Nombre de la base de datos. |
| `POSTGRES_USER` | Usuario de PostgreSQL. |
| `POSTGRES_PASSWORD` | Contraseña de PostgreSQL. Sin ella, el contenedor no arranca. |
| `ASPNETCORE_ENVIRONMENT` | Entorno de ejecución. El proyecto usa un único entorno local. |
| `BACKEND_PORT` | Puerto del equipo que expone la API. |
| `FRONTEND_PORT` | Puerto del equipo que expone el frontend. |
| `VITE_API_BASE_URL` | Dirección de la API que usa el frontend. |

`ConnectionStrings__Veterinary` no aparece en `.env`: `docker-compose.yml` la construye a partir de las variables `POSTGRES_*`, usando el servicio de base de datos.

### Entorno único y arranque de la base de datos

- Solo existe un entorno local. No hay configuración de producción.
- Al arrancar, la API aplica las migraciones de la base de datos y crea los datos iniciales. No hay opción para desactivarlo.
- El administrador inicial se crea solo si no existe. Volver a arrancar no crea otro. No se exige cambiar la contraseña en el primer inicio.
- Cambiar `SEED_ADMIN_EMAIL` crea un administrador nuevo; no renombra ni elimina el anterior.

## 5. Arranque

El MVP se ejecuta **únicamente con Docker Compose**. No se soporta ejecutar la API con `dotnet run` ni fuera de Docker: la aplicación no lee `.env`, así que sin la configuración que inyecta Compose la API falla al arrancar por falta de configuración.

1. Desde la raíz del repositorio, ejecute:

   ```bash
   docker compose up --build
   ```

2. Servicios que se levantan:
   - **PostgreSQL 16:** solo es accesible desde la red interna de Compose; su puerto no se publica en el equipo.
   - **Backend:** `http://localhost:5000` por defecto (puerto `BACKEND_PORT`).
   - **Frontend:** `http://localhost:5173` por defecto (puerto `FRONTEND_PORT`).

3. Si la API no arranca, revise el registro del contenedor del backend. Si falta una variable obligatoria, el mensaje indica su nombre.

Para detener los servicios: `docker compose down`. Para borrar también los datos de PostgreSQL: `docker compose down -v`.

## 6. API

Todas las rutas usan el prefijo `/api`, reciben y devuelven JSON en camelCase, y responden en español cuando hay un error.

Salvo el login, todas las rutas exigen el token en la cabecera `Authorization: Bearer <token>`. El token se obtiene en el login y dura 60 minutos. Al expirar, hay que iniciar sesión de nuevo. El cierre de sesión lo gestiona la aplicación cliente, que descarta el token.

### Autenticación

| Método | Ruta | Rol | Descripción |
|---|---|---|---|
| `POST` | `/api/auth/login` | Público, con Basic Auth | Inicia sesión. Exige la cabecera `Authorization: Basic` además de un cuerpo JSON con `email` y `password`. Responde `token`, `expiresAt` y `role`. |
| `GET` | `/api/auth/me` | Propietario o Administrador | Devuelve el identificador y el rol del usuario del token. Sirve para comprobar que la sesión sigue vigente. |

### Usuarios (propietarios)

| Método | Ruta | Rol | Descripción |
|---|---|---|---|
| `GET` | `/api/users` | Administrador | Lista los propietarios activos. |
| `POST` | `/api/users` | Administrador | Crea una cuenta de propietario. |
| `GET` | `/api/users/me` | Propietario o Administrador | Devuelve los datos de la propia cuenta. |
| `PUT` | `/api/users/me` | Propietario o Administrador | Actualiza la propia cuenta. |
| `GET` | `/api/users/{id}` | Propietario (solo la propia cuenta) o Administrador (propietarios) | Devuelve una cuenta. |
| `PUT` | `/api/users/{id}` | Propietario (solo la propia cuenta) o Administrador (propietarios) | Actualiza una cuenta. |
| `DELETE` | `/api/users/{id}` | Administrador | Elimina una cuenta de propietario. |

### Mascotas

| Método | Ruta | Rol | Descripción |
|---|---|---|---|
| `GET` | `/api/pets` | Propietario (las suyas) o Administrador (todas, solo lectura) | Lista las mascotas. |
| `GET` | `/api/pets/{id}` | Propietario (la suya) o Administrador (cualquiera, solo lectura) | Devuelve una mascota. |
| `POST` | `/api/pets` | Propietario | Registra una mascota. El propietario es el del token; el `ownerId` del cuerpo se ignora. |
| `PUT` | `/api/pets/{id}` | Propietario (la suya) | Actualiza una mascota. El identificador de la mascota no es editable. |
| `DELETE` | `/api/pets/{id}` | Propietario (la suya) | Elimina una mascota. |

### Servicios

| Método | Ruta | Rol | Descripción |
|---|---|---|---|
| `GET` | `/api/services` | Propietario o Administrador | Lista los servicios activos. |
| `GET` | `/api/services/{id}` | Propietario o Administrador | Devuelve un servicio. |
| `POST` | `/api/services` | Administrador | Crea un servicio. |
| `PUT` | `/api/services/{id}` | Administrador | Actualiza un servicio. |
| `DELETE` | `/api/services/{id}` | Administrador | Elimina un servicio. |

El tipo de servicio es uno de estos tres valores: `Clinic` (clínica), `Grooming` (estética) o `Recreation` (recreación).

### Citas

| Método | Ruta | Rol | Descripción |
|---|---|---|---|
| `GET` | `/api/appointments` | Propietario (solo sus citas) o Administrador (todas) | Lista las citas. Filtros opcionales, combinados con AND: `referenceNumber` (radicado, búsqueda parcial sin distinguir mayúsculas), `petIdentifier`, `ownerIdentification` y `serviceType`. |
| `GET` | `/api/appointments/availability` | Propietario (para agendar) o Administrador (solo para reprogramar) | Devuelve las 10 franjas de un servicio en una fecha, con su cupo restante y si están disponibles. Parámetros: `serviceId` y `date` (`yyyy-MM-dd`). |
| `POST` | `/api/appointments` | Propietario | Agenda una cita para una de sus mascotas. |
| `PUT` | `/api/appointments/{id}` | Administrador | Reprograma una cita a una franja disponible. Exige motivo. |
| `POST` | `/api/appointments/{id}/cancel` | Administrador | Cancela una cita. Exige motivo. |

Los datos de una cita incluyen su radicado, la mascota, el servicio, la fecha, la hora de inicio y el estado (`Confirmed` o `Cancelled`). La respuesta de la lista no incluye correos ni identificadores internos de los propietarios.

### Reglas de acceso

- **Datos ajenos:** un propietario que pide una cuenta, una mascota o una cita que no le pertenece recibe **404** genérico. En el listado de citas, un filtro con datos ajenos devuelve una lista vacía.
- **Administrador sobre administradores:** cualquier operación sobre otra cuenta de administrador, incluida la propia, responde **403**.
- **Propietario que intenta eliminar su cuenta:** responde **403**.
- **Usuario eliminado:** no puede iniciar sesión, y un token que ya tenía deja de servir en su siguiente petición (401).

### Formatos

- Fechas en formato `yyyy-MM-dd`. Horas de inicio en formato `HH:mm`, de 08:00 a 17:00, en punto.
- La identificación de usuario y el identificador de mascota no pueden empezar por `DEL-`. La API responde 400 si lo hacen.
- Las respuestas nunca incluyen contraseñas ni hashes.

## 7. Errores

Los errores se devuelven en formato `ProblemDetails`, con título y detalle en español.

| Código | Cuándo |
|---|---|
| **400** | Datos no válidos, por ejemplo campos obligatorios vacíos, formatos incorrectos o contraseña que no cumple la política. |
| **401** | Falta el token, es incorrecto o ha expirado; o bien las credenciales del login son incorrectas, la cuenta está bloqueada, o el usuario fue eliminado. El login responde siempre con el mismo mensaje genérico, sin revelar si el correo existe. |
| **403** | El usuario está autenticado pero no tiene el rol necesario para la operación. |
| **404** | El recurso no existe, fue eliminado o pertenece a otro usuario. |
| **409** | Conflicto con datos existentes: correo o identificación duplicados, cupo ocupado, mascota con otra cita en la misma franja, o cita modificada por otra operación. |
| **422** | Regla de negocio incumplida, por ejemplo una fecha pasada o una franja sin cupo. |
| **500** | Error inesperado. La respuesta es genérica y no incluye detalles internos. |

Política de contraseñas: mínimo 8 caracteres, con al menos una mayúscula, una minúscula y un dígito.

## 8. Radicado

Cada cita recibe un número de radicado con el formato **`RAD-AAAAMMDD-####`**:

- `AAAAMMDD` es la fecha de registro en la zona horaria configurada.
- `####` es un consecutivo que empieza en `0001` cada día. Se lleva con una tabla de consecutivos (`ReferenceNumberSequences`), de modo que dos creaciones simultáneas no producen el mismo número.
- Es único en toda la base de datos.

## 9. Pruebas

El proyecto `backend/Veterinaria.Domain.Tests` contiene **80 pruebas** (xUnit, aserciones nativas). Cubren las reglas de dominio: radicado, fecha y hora, cupos, derivación de no disponible, motivo y rol, P-02 y horario de 08:00 a 17:00. Incluyen también una prueba de bloqueo de cuenta con Identity en memoria (excepción R13-11).

Comando:

```powershell
dotnet test backend\Veterinaria.Domain.Tests
```

No hay pruebas de integración: la base de datos, Docker y la concurrencia real se verifican manualmente (T-R13-10).

## 10. Límites conocidos

- **Aviso de Kerberos en los registros:** al arrancar, el contenedor del backend puede mostrar un aviso sobre la librería `libgssapi_krb5`. Es un aviso de la imagen base, no afecta al funcionamiento de la API y se conserva tal cual.
- **Claves de DataProtection en el contenedor:** las claves se generan dentro del contenedor y no se guardan en un volumen. Si el contenedor se recrea, se regeneran. Como el sistema no usa cookies ni recuperación de contraseña, el impacto práctico es bajo.
- **Aviso `Overriding HTTP_PORTS`:** la imagen base de ASP.NET define `HTTP_PORTS` y el `Dockerfile` fija `ASPNETCORE_URLS=http://+:8080`. El aviso es benigno; la configuración de puertos es la correcta y no se define `HTTP_PORTS` en el repositorio.

## 11. Idioma

La especificación, la interfaz, los mensajes al usuario y la documentación están en español. El código, la base de datos y la API están en inglés. Los nombres de la solución y de los proyectos (`Veterinaria.*`) se mantienen.
