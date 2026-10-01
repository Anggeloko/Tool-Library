# Bibliotecas para OTMSProbe 1.1.1.0

Implementación del 2026-09-30 en `anggelo_dev`. Mantiene los targets net40 y M2Mqtt 3.4; no modifica MqttNet ni la configuración de otros colectores.

- `Axl.Base.Mqtt 1.1.12`: publicación con QoS, retain y compresión por mensaje; las firmas existentes conservan sus opciones. Publicación confirmada con PUBACK para una parada controlada.
- `Axl.Base.Snmp 1.1.4`: SNMPv3 conserva excepciones/REPORT en `out error`; walks v1/v2c/v3 terminan con respuesta vacía, error, fin del árbol o falta de avance. Cierre de sockets en `finally`. Los noSuchName opcionales en GET v1 conservan evidencia de respuesta sin inventar un fallo de acceso.
- `Axl.Base.IloSnmp 1.0.6`: conserva errores por tabla y contacto válido por separado; detiene consultas tras autenticación fallida. HPE principal `.232.6.2.6.7.1.12` y alternativa `.232.6.2.6.6.1.7` se interpretan en RPM. La alternativa se utiliza solo si la principal responde sin error pero no proporciona velocidad válida. Evita doble conteo, preserva la salud principal y publica `fan_avg_rpm` en RawDetails, manteniendo FanAvgPct null para RPM.
- `Axl.Base.Ilo 1.0.4`: marca contacto HTTP válido y comienza con salud desconocida; un HTTP 401 ya no aparenta salud OK por los valores iniciales del modelo.

Las unidades/columnas HPE se contrastaron con `cpqhlth.mib` suministrada para ProLiant DL360 Gen10. No se infiere de la MIB que el firmware exponga todos los sensores: falta comparar la entrega en campo con un walk autenticado.

Pruebas aisladas: `dotnet test Axl.Base.Diagnostics.Tests/Axl.Base.Diagnostics.Tests.csproj -c Release -p:GeneratePackageOnBuild=false -p:LangVersion=latest`. Incluye regresiones HPE/Dell, tabla alternativa, índices compuestos, errores y autenticación, transporte MQTT TCP con retain/QoS/PUBACK y compresión previa, SNMP UDP y Redfish HTTP 401. La suite amplia `Axl.Base.Tests` no se modifica estructuralmente; solo se corrigen las expectativas de RPM del test de ventiladores existente. Los resultados específicos quedan también en el ZIP de Probe.
