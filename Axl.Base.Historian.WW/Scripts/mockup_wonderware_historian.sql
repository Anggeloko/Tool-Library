-- ====================================================================================
-- SCRIPT DE MOCKUP PARA WONDERWARE HISTORIAN (AVEVA HISTORIAN) - DATABASE [Runtime]
-- Genera las tablas y vistas requeridas para realizar pruebas locales con la librería Axl.Base.Historian.WW
-- Incluye más de 100 señales de prueba (tags) de una planta industrial simulada.
-- ====================================================================================

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'Runtime')
BEGIN
    CREATE DATABASE [Runtime];
END
GO

USE [Runtime];
GO

-- ====================================================================================
-- 1. ESTRUCTURA DE TABLAS (DDL)
-- ====================================================================================

-- 1.1 Tabla EngineeringUnit (Unidades de Ingeniería)
IF OBJECT_ID('dbo.EngineeringUnit', 'U') IS NOT NULL DROP TABLE dbo.EngineeringUnit;
CREATE TABLE dbo.EngineeringUnit (
    EUKey INT NOT NULL PRIMARY KEY,
    Unit NVARCHAR(50) NOT NULL
);

-- 1.2 Tabla AnalogTag (Definición de Etiquetas Analógicas)
IF OBJECT_ID('dbo.AnalogTag', 'U') IS NOT NULL DROP TABLE dbo.AnalogTag;
CREATE TABLE dbo.AnalogTag (
    TagKey INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    TagName NVARCHAR(256) NOT NULL UNIQUE,
    EUKey INT NOT NULL,
    Description NVARCHAR(256) NULL
);

-- 1.3 Tabla v_AnalogLive (Valores en Tiempo Real / Live Values)
-- En el Historian real es una vista sobre memoria/runtime, aquí se modela como tabla para pruebas locales.
IF OBJECT_ID('dbo.v_AnalogLive', 'U') IS NOT NULL DROP TABLE dbo.v_AnalogLive;
CREATE TABLE dbo.v_AnalogLive (
    TagName NVARCHAR(256) NOT NULL PRIMARY KEY,
    DateTime DATETIME2 NOT NULL,
    Value FLOAT NOT NULL,
    Quality INT NOT NULL,
    QualityDetail INT NOT NULL
);

-- 1.4 Tabla History (Historial de Valores)
-- En el Historian real es una tabla virtual OLE DB, aquí se crea para satisfacer las consultas T-SQL con filtros de modo.
IF OBJECT_ID('dbo.History', 'U') IS NOT NULL DROP TABLE dbo.History;
CREATE TABLE dbo.History (
    HistoryId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    TagName NVARCHAR(256) NOT NULL,
    DateTime DATETIME2 NOT NULL,
    Value FLOAT NOT NULL,
    Quality INT NOT NULL,
    QualityDetail INT NOT NULL,
    StartDateTime DATETIME2 NOT NULL,
    wwRetrievalMode NVARCHAR(50) NOT NULL DEFAULT 'Cyclic',
    wwCycleCount INT NOT NULL DEFAULT 1,
    wwQualityRule NVARCHAR(50) NOT NULL DEFAULT 'Extended',
    wwVersion NVARCHAR(50) NOT NULL DEFAULT 'Latest'
);
CREATE NONCLUSTERED INDEX IX_History_TagName_DateTime ON dbo.History(TagName, DateTime);

-- 1.5 Tabla v_Live (Estado y Configuración en Tiempo Real para Chequeo de Salud)
IF OBJECT_ID('dbo.v_Live', 'U') IS NOT NULL DROP TABLE dbo.v_Live;
CREATE TABLE dbo.v_Live (
    TagName NVARCHAR(256) NOT NULL PRIMARY KEY,
    wwTagKey INT NOT NULL,
    wwRetrievalMode NVARCHAR(50) NOT NULL DEFAULT 'Cyclic',
    wwTimeDeadband NVARCHAR(50) NOT NULL DEFAULT '0',
    wwValueDeadband NVARCHAR(50) NOT NULL DEFAULT '0.0',
    wwTimeZone NVARCHAR(50) NOT NULL DEFAULT 'UTC',
    wwParameters NVARCHAR(256) NOT NULL DEFAULT '',
    wwValueSelector NVARCHAR(50) NOT NULL DEFAULT 'Value',
    Quality INT NOT NULL
);
GO

-- ====================================================================================
-- 2. CARGA DE DATOS MAESTROS Y METADATOS (DML)
-- ====================================================================================

-- 2.1 Carga de Unidades de Ingeniería
INSERT INTO dbo.EngineeringUnit (EUKey, Unit) VALUES
(1, '°C'),
(2, 'bar'),
(3, 'PSI'),
(4, 'RPM'),
(5, 'MW'),
(6, 'MVAR'),
(7, 'kV'),
(8, 'V'),
(9, 'A'),
(10, 'Hz'),
(11, 'm³/h'),
(12, '%'),
(13, 'mm/s'),
(14, 'm'),
(15, 'kg/s'),
(16, 'ppm'),
(17, 'pH'),
(18, 'µS/cm'),
(19, 'mg/m³');
GO

-- 2.2 Carga de Definición de Etiquetas Analógicas (109 Señales Industriales)
INSERT INTO dbo.AnalogTag (TagName, EUKey, Description) VALUES
-- Boiler 01 (Caldera)
('Boiler01.Drum_Level', 12, 'Nivel de domo de vapor Boiler 01'),
('Boiler01.Drum_Pressure', 2, 'Presion de domo de vapor Boiler 01'),
('Boiler01.Furnace_Temp', 1, 'Temperatura de horno Boiler 01'),
('Boiler01.Feedwater_Flow', 11, 'Caudal de agua de alimentacion Boiler 01'),
('Boiler01.Steam_Flow', 15, 'Flujo de vapor principal Boiler 01'),
('Boiler01.Steam_Temp', 1, 'Temperatura de vapor principal Boiler 01'),
('Boiler01.Fuel_Gas_Flow', 11, 'Flujo de gas combustible Boiler 01'),
('Boiler01.Combustion_Air_Flow', 11, 'Flujo de aire de combustion Boiler 01'),
('Boiler01.Flue_Gas_O2', 12, 'Porcentaje de oxigeno en gases de escape Boiler 01'),
('Boiler01.Economizer_Temp_Out', 1, 'Temperatura salida economizador Boiler 01'),
('Boiler01.Superheater_Temp_1', 1, 'Temperatura sobrecalentador etapa 1 Boiler 01'),
('Boiler01.Superheater_Temp_2', 1, 'Temperatura sobrecalentador etapa 2 Boiler 01'),

-- Turbine 01 (Turbina de Vapor)
('Turbine01.Speed_RPM', 4, 'Velocidad de giro Turbina 01'),
('Turbine01.Inlet_Steam_Pressure', 2, 'Presion de vapor entrada Turbina 01'),
('Turbine01.Inlet_Steam_Temp', 1, 'Temperatura de vapor entrada Turbina 01'),
('Turbine01.Exhaust_Temp', 1, 'Temperatura de escape Turbina 01'),
('Turbine01.Vibration_Brg1_X', 13, 'Vibracion cojinete 1 eje X Turbina 01'),
('Turbine01.Vibration_Brg1_Y', 13, 'Vibracion cojinete 1 eje Y Turbina 01'),
('Turbine01.Vibration_Brg2_X', 13, 'Vibracion cojinete 2 eje X Turbina 01'),
('Turbine01.Vibration_Brg2_Y', 13, 'Vibracion cojinete 2 eje Y Turbina 01'),
('Turbine01.Lube_Oil_Pressure', 2, 'Presion aceite lubricacion Turbina 01'),
('Turbine01.Lube_Oil_Temp', 1, 'Temperatura aceite lubricacion Turbina 01'),
('Turbine01.Hydraulic_Oil_Press', 2, 'Presion aceite hidraulico Turbina 01'),
('Turbine01.Governor_Valve_Pos', 12, 'Posicion valvula gobernadora Turbina 01'),

-- Generator 01 (Generador Electrico)
('Generator01.Active_Power', 5, 'Potencia activa Generador 01'),
('Generator01.Reactive_Power', 6, 'Potencia reactiva Generador 01'),
('Generator01.Stator_Voltage', 7, 'Voltaje de estator Generador 01'),
('Generator01.Stator_Current_R', 9, 'Corriente fase R Generador 01'),
('Generator01.Stator_Current_S', 9, 'Corriente fase S Generador 01'),
('Generator01.Stator_Current_T', 9, 'Corriente fase T Generador 01'),
('Generator01.Frequency', 10, 'Frecuencia electrica Generador 01'),
('Generator01.Power_Factor', 12, 'Factor de potencia Generador 01'),
('Generator01.Excitation_Voltage', 8, 'Voltaje de excitacion Generador 01'),
('Generator01.Excitation_Current', 9, 'Corriente de excitacion Generador 01'),
('Generator01.Stator_Winding_Temp1', 1, 'Devanado estator temperatura 1 Generador 01'),
('Generator01.Stator_Winding_Temp2', 1, 'Devanado estator temperatura 2 Generador 01'),

-- Cooling Tower 01 (Torre de Enfriamiento)
('CoolingTower01.Basin_Level', 14, 'Nivel de piscina Torre Enfriamiento 01'),
('CoolingTower01.Water_Inlet_Temp', 1, 'Temperatura agua entrada Torre 01'),
('CoolingTower01.Water_Outlet_Temp', 1, 'Temperatura agua salida Torre 01'),
('CoolingTower01.Fan1_Speed', 4, 'Velocidad ventilador 1 Torre 01'),
('CoolingTower01.Fan1_Vibration', 13, 'Vibracion ventilador 1 Torre 01'),
('CoolingTower01.Fan2_Speed', 4, 'Velocidad ventilador 2 Torre 01'),
('CoolingTower01.Fan2_Vibration', 13, 'Vibracion ventilador 2 Torre 01'),
('CoolingTower01.Makeup_Water_Flow', 11, 'Caudal de agua de reposicion Torre 01'),
('CoolingTower01.Blowdown_Flow', 11, 'Caudal de purga Torre 01'),
('CoolingTower01.Water_pH', 17, 'pH del agua de refrigeracion Torre 01'),
('CoolingTower01.Water_Conductivity', 18, 'Conductividad del agua Torre 01'),

-- Condenser 01 (Condensador)
('Condenser01.Vacuum_Pressure', 2, 'Presion de vacio Condensador 01'),
('Condenser01.Hotwell_Level', 14, 'Nivel de pozo caliente Condensador 01'),
('Condenser01.Cooling_Water_Flow', 11, 'Caudal agua refrigeracion Condensador 01'),
('Condenser01.Cooling_Water_TempIn', 1, 'Temperatura entrada agua Condensador 01'),
('Condenser01.Cooling_Water_TempOut', 1, 'Temperatura salida agua Condensador 01'),
('Condenser01.Condensate_Pump1_Amps', 9, 'Amperaje bomba condensado 1 Condensador 01'),
('Condenser01.Condensate_Pump2_Amps', 9, 'Amperaje bomba condensado 2 Condensador 01'),
('Condenser01.Condensate_Disch_Press', 2, 'Presion descarga condensado Condensador 01'),
('Condenser01.Air_Ejector_Flow', 15, 'Flujo eyector de aire Condensador 01'),
('Condenser01.Hotwell_Temp', 1, 'Temperatura pozo caliente Condensador 01'),

-- BFP01 (Bombas de Agua de Alimentacion)
('BFP01.PumpA_Suction_Press', 2, 'Presion succion bomba BFP A'),
('BFP01.PumpA_Discharge_Press', 2, 'Presion descarga bomba BFP A'),
('BFP01.PumpA_Motor_Amps', 9, 'Corriente motor bomba BFP A'),
('BFP01.PumpA_Motor_Temp', 1, 'Temperatura motor bomba BFP A'),
('BFP01.PumpA_Vibration', 13, 'Vibracion bomba BFP A'),
('BFP01.PumpB_Suction_Press', 2, 'Presion succion bomba BFP B'),
('BFP01.PumpB_Discharge_Press', 2, 'Presion descarga bomba BFP B'),
('BFP01.PumpB_Motor_Amps', 9, 'Corriente motor bomba BFP B'),
('BFP01.PumpB_Motor_Temp', 1, 'Temperatura motor bomba BFP B'),
('BFP01.PumpB_Vibration', 13, 'Vibracion bomba BFP B'),
('BFP01.Total_Discharge_Flow', 11, 'Caudal total descarga BFP'),
('BFP01.Deaerator_Storage_Level', 12, 'Nivel tanque desaireador'),

-- Compressor 01 (Compresor de Gas)
('Compressor01.Suction_Pressure', 2, 'Presion succion Compresor de Gas 01'),
('Compressor01.Suction_Temp', 1, 'Temperatura succion Compresor 01'),
('Compressor01.Discharge_Pressure', 2, 'Presion descarga Compresor 01'),
('Compressor01.Discharge_Temp', 1, 'Temperatura descarga Compresor 01'),
('Compressor01.Motor_Power', 5, 'Potencia motor Compresor 01'),
('Compressor01.Motor_RPM', 4, 'RPM motor Compresor 01'),
('Compressor01.Lube_Oil_Temp', 1, 'Temperatura aceite Compresor 01'),
('Compressor01.Lube_Oil_Press', 2, 'Presion aceite Compresor 01'),
('Compressor01.Surge_Valve_Pos', 12, 'Posicion valvula anti-surge Compresor 01'),
('Compressor01.Intercooler_Temp', 1, 'Temperatura intercooler Compresor 01'),

-- Substation 01 (Subestacion Electrica)
('Substation01.Transformer1_Temp', 1, 'Temperatura aceite transformador 1'),
('Substation01.Transformer1_Oil_Level', 12, 'Nivel aceite transformador 1'),
('Substation01.Bus_Voltage_138kV', 7, 'Voltaje barra 138kV Subestacion 01'),
('Substation01.Line1_Current', 9, 'Corriente linea 1 Subestacion 01'),
('Substation01.Line1_ActivePower', 5, 'Potencia activa linea 1'),
('Substation01.Line2_Current', 9, 'Corriente linea 2 Subestacion 01'),
('Substation01.Line2_ActivePower', 5, 'Potencia activa linea 2'),
('Substation01.Battery_Bank_Voltage', 8, 'Voltaje banco baterias 125VDC'),
('Substation01.Aux_Transformer_Temp', 1, 'Temperatura transformador auxiliar'),
('Substation01.SF6_Gas_Pressure', 2, 'Presion gas SF6 interruptores'),

-- WTP 01 (Planta de Tratamiento de Agua)
('WTP01.Raw_Water_Tank_Level', 14, 'Nivel tanque agua cruda WTP'),
('WTP01.Raw_Water_Pump_Flow', 11, 'Flujo bomba agua cruda WTP'),
('WTP01.RO_Inlet_Pressure', 2, 'Presion entrada osmosis inversa WTP'),
('WTP01.RO_Permeate_Flow', 11, 'Flujo permeado osmosis inversa WTP'),
('WTP01.RO_Reject_Flow', 11, 'Flujo rechazo osmosis inversa WTP'),
('WTP01.Demin_Tank_Level', 12, 'Nivel tanque agua desmineralizada'),
('WTP01.Silica_Analyzer', 16, 'Analizador de silice WTP'),
('WTP01.Conductivity_Analyzer', 18, 'Analizador de conductividad WTP'),
('WTP01.Acid_Dosing_Tank_Level', 12, 'Nivel tanque quimico acido'),
('WTP01.Caustic_Dosing_Tank_Level', 12, 'Nivel tanque quimico caustico'),

-- CEMS 01 (Monitoreo de Emisiones / Chimenea)
('CEMS01.Stack_Gas_Flow', 11, 'Flujo de gas en chimenea CEMS'),
('CEMS01.Stack_Gas_Temp', 1, 'Temperatura de gases de chimenea'),
('CEMS01.NOx_Concentration', 16, 'Concentracion de NOx CEMS'),
('CEMS01.SO2_Concentration', 16, 'Concentracion de SO2 CEMS'),
('CEMS01.CO_Concentration', 16, 'Concentracion de CO CEMS'),
('CEMS01.CO2_Concentration', 12, 'Porcentaje de CO2 CEMS'),
('CEMS01.Opacity', 12, 'Opacidad en chimenea CEMS'),
('CEMS01.Particulate_Matter', 19, 'Material particulado CEMS'),
('CEMS01.O2_Concentration', 12, 'Oxigeno residual chimenea CEMS'),
('CEMS01.Moisture', 12, 'Humedad en chimenea CEMS'),

-- SEÑALES CON ESTADOS DE CALIDAD ANORMALES (Para pruebas de GetHealthStatusAsync / Health Check)
('Boiler01.Furnace_Temp_Redundant', 1, 'Sensor redundante de temperatura (Sensor Failure)'),
('Turbine01.Speed_SensorB', 4, 'Sensor de velocidad secundario (Comm Failure)'),
('Generator01.Stator_Temp_Backup', 1, 'Sensor de respaldo estator (Out of Service)'),
('Compressor01.Suction_Press_Aux', 2, 'Presion auxiliar succion (Uncertain Quality)'),
('WTP01.Silica_Analyzer_Old', 16, 'Analizador antiguo de silice (Bad Quality)');
GO

-- ====================================================================================
-- 3. POBLADO DE VALORES EN TIEMPO REAL (v_AnalogLive y v_Live)
-- ====================================================================================

-- 3.1 Poblado de v_AnalogLive (Valores actuales con calidad buena = 192, y anormales)
INSERT INTO dbo.v_AnalogLive (TagName, DateTime, Value, Quality, QualityDetail)
SELECT 
    TagName,
    GETDATE(),
    CASE 
        WHEN TagName LIKE '%_Temp%' THEN ROUND(RAND(CHECKSUM(NEWID())) * 100 + 50, 2)
        WHEN TagName LIKE '%_Pressure%' OR TagName LIKE '%_Press%' THEN ROUND(RAND(CHECKSUM(NEWID())) * 30 + 5, 2)
        WHEN TagName LIKE '%_Level%' OR TagName LIKE '%_Pos%' THEN ROUND(RAND(CHECKSUM(NEWID())) * 50 + 40, 2)
        WHEN TagName LIKE '%_Flow%' THEN ROUND(RAND(CHECKSUM(NEWID())) * 200 + 100, 2)
        WHEN TagName LIKE '%_RPM%' OR TagName LIKE '%_Speed%' THEN ROUND(RAND(CHECKSUM(NEWID())) * 600 + 3000, 0)
        WHEN TagName LIKE '%_Voltage%' THEN ROUND(RAND(CHECKSUM(NEWID())) * 5 + 13.8, 2)
        WHEN TagName LIKE '%_Current%' OR TagName LIKE '%_Amps%' THEN ROUND(RAND(CHECKSUM(NEWID())) * 150 + 200, 1)
        WHEN TagName LIKE '%_Power%' THEN ROUND(RAND(CHECKSUM(NEWID())) * 80 + 20, 1)
        WHEN TagName LIKE '%_Frequency%' THEN 60.0
        ELSE ROUND(RAND(CHECKSUM(NEWID())) * 50 + 10, 2)
    END,
    CASE 
        WHEN TagName = 'Boiler01.Furnace_Temp_Redundant' THEN 0  -- Bad
        WHEN TagName = 'Turbine01.Speed_SensorB' THEN 24         -- Bad Comm Failure
        WHEN TagName = 'Generator01.Stator_Temp_Backup' THEN 28  -- Out of Service
        WHEN TagName = 'Compressor01.Suction_Press_Aux' THEN 64  -- Uncertain
        WHEN TagName = 'WTP01.Silica_Analyzer_Old' THEN 0        -- Bad
        ELSE 192                                                 -- 192 = Good Quality en Wonderware Historian
    END,
    CASE 
        WHEN TagName IN ('Boiler01.Furnace_Temp_Redundant', 'WTP01.Silica_Analyzer_Old') THEN 0
        WHEN TagName = 'Turbine01.Speed_SensorB' THEN 24
        WHEN TagName = 'Generator01.Stator_Temp_Backup' THEN 28
        WHEN TagName = 'Compressor01.Suction_Press_Aux' THEN 64
        ELSE 4352
    END
FROM dbo.AnalogTag;
GO

-- 3.2 Poblado de v_Live (Metadatos y estado de salud actual)
INSERT INTO dbo.v_Live (TagName, wwTagKey, Quality)
SELECT 
    at.TagName,
    at.TagKey,
    val.Quality
FROM dbo.AnalogTag at
INNER JOIN dbo.v_AnalogLive val ON at.TagName = val.TagName;
GO

-- ====================================================================================
-- 4. POBLADO DE VALORES HISTÓRICOS (History)
-- ====================================================================================

-- Insertar 5 puntos en el tiempo hacia atrás (-60m, -45m, -30m, -15m, -5m) para cada una de las 109 señales
-- Esto asegura que GetHistoricalValuesAsync devuelva series de tiempo ricas en cualquier consulta de los últimos 60 minutos.

-- Punto 1: Hace 60 minutos
INSERT INTO dbo.History (TagName, DateTime, Value, Quality, QualityDetail, StartDateTime)
SELECT 
    TagName,
    DATEADD(MINUTE, -60, DateTime),
    Value * (1.0 + (RAND(CHECKSUM(NEWID())) * 0.1 - 0.05)), -- Variación de +/- 5%
    Quality,
    QualityDetail,
    DATEADD(MINUTE, -60, DateTime)
FROM dbo.v_AnalogLive;

-- Punto 2: Hace 45 minutos
INSERT INTO dbo.History (TagName, DateTime, Value, Quality, QualityDetail, StartDateTime)
SELECT 
    TagName,
    DATEADD(MINUTE, -45, DateTime),
    Value * (1.0 + (RAND(CHECKSUM(NEWID())) * 0.1 - 0.05)),
    Quality,
    QualityDetail,
    DATEADD(MINUTE, -45, DateTime)
FROM dbo.v_AnalogLive;

-- Punto 3: Hace 30 minutos
INSERT INTO dbo.History (TagName, DateTime, Value, Quality, QualityDetail, StartDateTime)
SELECT 
    TagName,
    DATEADD(MINUTE, -30, DateTime),
    Value * (1.0 + (RAND(CHECKSUM(NEWID())) * 0.1 - 0.05)),
    Quality,
    QualityDetail,
    DATEADD(MINUTE, -30, DateTime)
FROM dbo.v_AnalogLive;

-- Punto 4: Hace 15 minutos
INSERT INTO dbo.History (TagName, DateTime, Value, Quality, QualityDetail, StartDateTime)
SELECT 
    TagName,
    DATEADD(MINUTE, -15, DateTime),
    Value * (1.0 + (RAND(CHECKSUM(NEWID())) * 0.1 - 0.05)),
    Quality,
    QualityDetail,
    DATEADD(MINUTE, -15, DateTime)
FROM dbo.v_AnalogLive;

-- Punto 5: Hace 5 minutos
INSERT INTO dbo.History (TagName, DateTime, Value, Quality, QualityDetail, StartDateTime)
SELECT 
    TagName,
    DATEADD(MINUTE, -5, DateTime),
    Value * (1.0 + (RAND(CHECKSUM(NEWID())) * 0.1 - 0.05)),
    Quality,
    QualityDetail,
    DATEADD(MINUTE, -5, DateTime)
FROM dbo.v_AnalogLive;

-- Punto 6: Tiempo Actual (Coincidente con el valor en vivo)
INSERT INTO dbo.History (TagName, DateTime, Value, Quality, QualityDetail, StartDateTime)
SELECT 
    TagName,
    DateTime,
    Value,
    Quality,
    QualityDetail,
    DateTime
FROM dbo.v_AnalogLive;
GO

PRINT '===================================================================================='
PRINT 'MOCKUP DE WONDERWARE HISTORIAN [Runtime] CREADO EXITOSAMENTE CON 109 SEÑALES DE PRUEBA'
PRINT '===================================================================================='
