# Tools.Tests

Centralized unit and integration testing project for the entire ecosystem.

## Prerequisites
- **Framework:** .NET Framework 4.7.2 (Para compatibilidad con todos los módulos).

## NuGet Dependencies
- `NUnit` (v3.13.3)
- `Moq` (v4.0.10827)
- `NUnit3TestAdapter`

## Internal Dependencies
- Referencias directas a todos los proyectos `Tools.*` para garantizar la cobertura total.

## Test Coverage
- Pruebas de lógica de negocio (Common, Metrics).
- Pruebas de integración simulada (Bases de Datos con Moq).
- Pruebas funcionales de protocolos (MQTT, SNMP, WMI, OPC).

## Test Structure Example

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
