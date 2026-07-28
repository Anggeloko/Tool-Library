# Tools.Tests

Proyecto centralizado de pruebas unitarias y de integración para todo el ecosistema.

## Prerrequisitos
- **Framework:** .NET Framework 4.7.2 (Para compatibilidad con todos los módulos).

## Dependencias de NuGet
- `NUnit` (v3.13.3)
- `Moq` (v4.0.10827)
- `NUnit3TestAdapter`

## Dependencias Internas
- Referencias directas a todos los proyectos `Tools.*` para garantizar la cobertura total.

## Cobertura de Pruebas
- Pruebas de lógica de negocio (Common, Metrics).
- Pruebas de integración simulada (Bases de Datos con Moq).
- Pruebas funcionales de protocolos (MQTT, SNMP, WMI, OPC).

## Ejemplo de Estructura de Test

```csharp
[TestFixture]
public class MyFeatureTests
{
    [Test]
    public void Test_Success_Condition()
    {
        // 1. Arrange (Preparar)
        var service = new MyService();
        
        // 2. Act (Ejecutar)
        var result = service.DoSomething();
        
        // 3. Assert (Verificar)
        Assert.IsTrue(result.IsSuccess);
    }
}
```
