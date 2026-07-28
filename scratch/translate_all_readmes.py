import os
import re

# Comprehensive dictionary for Spanish to English translation across all documentation files
REPLACEMENTS = [
    # Headers & Section Titles
    ("## Prerrequisitos", "## Prerequisites"),
    ("## Referencia Técnica (API)", "## Technical Reference (API)"),
    ("### Puerto/Interfaz", "### Port/Interface"),
    ("### Clase", "### Class"),
    ("### Interfaz", "### Interface"),
    ("#### Métodos", "#### Methods"),
    ("#### Propiedades", "#### Properties"),
    ("* **Parámetros:**", "* **Parameters:**"),
    ("- **Parámetros:**", "- **Parameters:**"),
    ("* **Retorno:**", "* **Return:**"),
    ("- **Retorno:**", "- **Return:**"),
    ("### Adaptador de Infraestructura", "### Infrastructure Adapter"),
    ("#### Ejemplo de Uso", "#### Usage Example"),
    ("### Modelo", "### Model"),
    ("### Modelos", "### Models"),

    # Common Phrases & Explanations
    ("Librería de manipulación WMI pre-empaquetada para extracción de métricas de Windows.", "Pre-packaged WMI manipulation library for Windows metrics extraction."),
    ("Proyecto de pruebas unitarias para validar las funcionalidades de las librerías base.", "Unit testing project to validate base library functionality."),
    ("Lectura local", "Local reading"),
    ("Realiza consultas WQL a un host local o remoto.", "Executes WQL queries against a local or remote host."),
    ("ID del equipo (para bloqueo de tráfico).", "Device ID (for traffic locking)."),
    ("Dirección IP del host.", "Host IP address."),
    ("Usuario con privilegios de administración/WMI.", "User with administrative/WMI privileges."),
    ("Contraseña de red.", "Network password."),
    ("Estructura de consulta a ejecutar.", "Query structure to execute."),
    ("Salida de mensaje de error si la consulta falla.", "Error message output if the query fails."),
    ("Puerto alternativo opcional.", "Optional alternative port."),
    ("Lista de diccionarios donde cada elemento es una fila con los campos requeridos.", "List of dictionaries where each element is a row containing requested fields."),
    ("Clase DTO para estructurar las peticiones WMI:", "DTO class to structure WMI requests:"),
    ("Namespace WMI (ej. `root\\cimv2`).", "WMI Namespace (e.g. `root\\cimv2`)."),
    ("Clase WMI (ej. `Win32_OperatingSystem`).", "WMI Class (e.g. `Win32_OperatingSystem`)."),
    ("Diccionario de mapeo de alias y propiedad (`<Alias, Property>`).", "Mapping dictionary for alias and property (`<Alias, Property>`)."),
    
    # Generic term replacements
    (r"\bLibrería de\b", "Library for"),
    (r"\bLibrería para\b", "Library for"),
    (r"\bCliente de\b", "Client for"),
    (r"\bCliente para\b", "Client for"),
    (r"\bMotor de\b", "Engine for"),
    (r"\bServicio de\b", "Service for"),
    (r"\bSoporte para\b", "Support for"),
    (r"\bProvee\b", "Provides"),
    (r"\bPermite\b", "Allows"),
    (r"\bRealiza\b", "Performs"),
    (r"\bObtiene\b", "Gets"),
    (r"\bGuarda\b", "Saves"),
    (r"\bElimina\b", "Deletes"),
    (r"\bActualiza\b", "Updates"),
    (r"\bEjecuta\b", "Executes"),
    (r"\bConsulta\b", "Queries"),
    (r"\bEstructura\b", "Structure"),
    (r"\bDescripción\b", "Description"),
    (r"\bEjemplo\b", "Example"),
    (r"\bNotas\b", "Notes"),
    (r"\bUso\b", "Usage"),
]

docs_dir = r"c:\Users\Axl\Desktop\Projects\Libraries\Docs"
base_dir = r"c:\Users\Axl\Desktop\Projects\Libraries"

for fname in os.listdir(docs_dir):
    if not fname.endswith(".md"):
        continue
    
    doc_path = os.path.join(docs_dir, fname)
    with open(doc_path, "r", encoding="utf-8") as f:
        text = f.read()

    # Apply all replacements
    for pattern, repl in REPLACEMENTS:
        if pattern.startswith(r"\b"):
            text = re.sub(pattern, repl, text)
        else:
            text = text.replace(pattern, repl)

    proj_name = fname[:-3]
    target_readme = os.path.join(base_dir, proj_name, "README.md")
    
    if os.path.exists(os.path.dirname(target_readme)):
        with open(target_readme, "w", encoding="utf-8") as f:
            f.write(text)
        print(f"Fully updated: {target_readme}")
