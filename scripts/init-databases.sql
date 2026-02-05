-- Initialize databases for PMS and Keycloak
-- Run this after MSSQL container is healthy

-- Create PMS database if not exists
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'pms_db')
BEGIN
    CREATE DATABASE pms_db;
    PRINT 'Created pms_db database';
END
GO

-- Create Keycloak database if not exists
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'keycloak_db')
BEGIN
    CREATE DATABASE keycloak_db;
    PRINT 'Created keycloak_db database';
END
GO

PRINT 'Database initialization complete';
