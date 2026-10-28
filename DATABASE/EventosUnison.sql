IF DB_ID('EventosUnison') IS NULL CREATE DATABASE EventosUnison;
GO
USE EventosUnison;
GO

CREATE TABLE Usuarios (
    Id INT IDENTITY PRIMARY KEY,
    NombreCompleto NVARCHAR(150) NOT NULL,
    CorreoInstitucional NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(500) NOT NULL,
    Rol NVARCHAR(20) NOT NULL DEFAULT 'Alumno',
    Departamento NVARCHAR(150) NOT NULL,
    Carrera NVARCHAR(150) NULL,
    FechaRegistro DATE NOT NULL DEFAULT GETDATE(),
    CONSTRAINT CK_Usuarios_Rol CHECK (Rol IN ('Alumno', 'Profesor', 'Administrador'))
);

CREATE TABLE Eventos (
    Id INT IDENTITY PRIMARY KEY,
    Nombre NVARCHAR(150) NOT NULL,
    Descripcion NVARCHAR(500) NOT NULL,
    Departamento NVARCHAR(150) NOT NULL,
    FechaHora DATETIME NOT NULL,
    Lugar NVARCHAR(150) NOT NULL,
    Organizador NVARCHAR(150) NOT NULL,
    OrganizadorId INT NOT NULL REFERENCES Usuarios(Id),
    Estado NVARCHAR(20) NOT NULL DEFAULT 'Propuesto',
    PuntosCulturest FLOAT NOT NULL DEFAULT 0.25,
    CONSTRAINT CK_Eventos_Estado CHECK (Estado IN ('Propuesto', 'Confirmado', 'Rechazado'))
);

CREATE TABLE Proyectos (
    Id INT IDENTITY PRIMARY KEY,
    Nombre NVARCHAR(150) NOT NULL,
    Descripcion NVARCHAR(1000) NOT NULL,
    Carrera NVARCHAR(150) NULL,
    Departamento NVARCHAR(150) NOT NULL,
    FechaDePublicacion DATETIME NOT NULL DEFAULT GETDATE(),
    UsuarioId INT NOT NULL REFERENCES Usuarios(Id),
    ArchivoNombre NVARCHAR(255) NULL,
    ArchivoRuta NVARCHAR(100) NULL,
    ArchivoTamano BIGINT NULL
);

CREATE TABLE RegistroEventos (
    Id INT IDENTITY PRIMARY KEY,
    UsuarioId INT NOT NULL REFERENCES Usuarios(Id),
    EventoId INT NOT NULL REFERENCES Eventos(Id),
    FechaRegistro DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT UQ_RegistroEventos UNIQUE (UsuarioId, EventoId)
);

CREATE TABLE InteresEventos (
    Id INT IDENTITY PRIMARY KEY,
    UsuarioId INT NOT NULL REFERENCES Usuarios(Id),
    EventoId INT NOT NULL REFERENCES Eventos(Id),
    Respuesta NVARCHAR(10) NOT NULL,
    FechaRespuesta DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT UQ_InteresEventos UNIQUE (UsuarioId, EventoId),
    CONSTRAINT CK_InteresEventos_Respuesta CHECK (Respuesta IN ('Si', 'TalVez', 'No'))
);

CREATE TABLE Notificaciones (
    Id INT IDENTITY PRIMARY KEY,
    UsuarioId INT NOT NULL REFERENCES Usuarios(Id),
    ActorId INT NOT NULL REFERENCES Usuarios(Id),
    EventoId INT NOT NULL REFERENCES Eventos(Id),
    Tipo NVARCHAR(20) NOT NULL,
    Detalle NVARCHAR(20) NULL,
    Leida BIT NOT NULL DEFAULT 0,
    Fecha DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT CK_Notificaciones_Tipo CHECK (Tipo IN ('Interes', 'Registro', 'Estado'))
);
CREATE INDEX IX_Notificaciones_Usuario ON Notificaciones (UsuarioId, Leida);
GO