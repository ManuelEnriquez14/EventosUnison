# Eventos UNISON

Plataforma web para la gestión de eventos y proyectos universitarios de la Universidad de Sonora.
Reto de programación: *Página web para la gestión de eventos*.

## Funcionalidades
- Registro e inicio de sesión (contraseñas con hash)
- Roles: Alumno, Profesor y Administrador
- Consulta de eventos confirmados y propuesta de nuevos eventos, con aprobación del administrador
- Interés (Sí / Tal vez / No) y registro en eventos; el organizador no puede votar en el suyo
- Panel de interesados por evento y notificaciones para el organizador
- Publicación de proyectos académicos con archivo adjunto
- Puntos Culturest (0.25 por evento)

## Tecnologías
ASP.NET Core MVC, Entity Framework Core, SQL Server y Bootstrap 5.

## Cómo ejecutarlo
1. Instala Visual Studio con la carga de trabajo de ASP.NET y SQL Server (o SQL Server Express).
2. Ejecuta `Database/EventosUnison.sql` en SQL Server Management Studio.
3. En `appsettings.json`, ajusta `ConnectionStrings:Default` con el nombre de tu servidor.
4. Abre la solución en Visual Studio y presiona **F5**.
5. Para crear el primer administrador, regístrate desde la página y luego ejecuta:
   ```sql
   UPDATE Usuarios SET Rol = 'Administrador' WHERE CorreoInstitucional = 'tu_correo@unison.mx';
   ```

## Autor
Manuel Enriquez Grijalva