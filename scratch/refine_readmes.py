import os
import re

FULL_DICTIONARY = [
    # General terms & Titles
    ("Proyecto centralizado de pruebas unitarias y de integración para todo el ecosistema.", "Centralized unit and integration testing project for the entire ecosystem."),
    ("Proyecto de pruebas unitarias para validar las funcionalidades de las librerías base.", "Unit testing project to validate base library functionalities."),
    ("Librería de manipulación WMI pre-empaquetada para extracción de métricas de Windows.", "Pre-packaged WMI manipulation library for Windows metrics extraction."),
    ("## Dependencias de NuGet", "## NuGet Dependencies"),
    ("## Dependencias Internas", "## Internal Dependencies"),
    ("## Cobertura de Pruebas", "## Test Coverage"),
    ("## Example de Structure de Test", "## Test Structure Example"),
    ("Referencias directas a todos los proyectos", "Direct references to all projects"),
    ("para garantizar la cobertura total.", "to ensure total coverage."),
    ("Pruebas de lógica de negocio", "Business logic tests"),
    ("Pruebas de integración simulada", "Mocked integration tests"),
    ("Bases de Datos con Moq", "Databases with Moq"),
    ("Pruebas funcionales de protocolos", "Protocol functional tests"),
    ("IP del host remoto o `localhost`.", "Remote host IP or `localhost`."),
    ("Credenciales. Si es local, se omiten automáticamente.", "Credentials. If local, they are automatically omitted."),
    ("Objeto `WmiQuery` que define el namespace, la clase y las propiedades a consultar.", "`WmiQuery` object defining namespace, class, and properties to query."),
    ("Mensaje en caso de fallo.", "Message in case of failure."),
    ("Puerto de conexión personalizado. Si es `0` o no es un puerto TCP válido (1-65535), se utiliza el comportamiento por defecto de WMI/DCOM.", "Custom connection port. If `0` or not a valid TCP port (1-65535), default WMI/DCOM behavior is used."),
    ("(Opcional)", "(Optional)"),
    ("Cada diccionario representa una instancia encontrada, con los mapeos definidos en", "Each dictionary represents a found instance, with mappings defined in"),
    ("Propiedades:", "Properties:"),
    ("Namespace de WMI", "WMI Namespace"),
    ("Clase WMI", "WMI Class"),
    ("donde la Clave es el nombre amigable y el Valor es la propiedad real de WMI.", "where the Key is the friendly name and Value is the actual WMI property."),
    ("Para compatibilidad con todos los módulos", "For compatibility across all modules"),
    ("Preparar", "Arrange"),
    ("Ejecutar", "Act"),
    ("Verificar", "Assert")
]

docs_dir = r"c:\Users\Axl\Desktop\Projects\Libraries\Docs"
base_dir = r"c:\Users\Axl\Desktop\Projects\Libraries"

for fname in os.listdir(docs_dir):
    if not fname.endswith(".md"):
        continue
    
    proj_name = fname[:-3]
    target_readme = os.path.join(base_dir, proj_name, "README.md")
    
    if os.path.exists(target_readme):
        with open(target_readme, "r", encoding="utf-8") as f:
            content = f.read()

        for old, new in FULL_DICTIONARY:
            content = content.replace(old, new)

        with open(target_readme, "w", encoding="utf-8") as f:
            f.write(content)
        print(f"Refined translation for: {target_readme}")
