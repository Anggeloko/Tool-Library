import os

RAW_TRANSLATE = [
    ("Debe estar instalado el conjunto de herramientas de Foxboro (ej. `omgetimp`, `omsetimp`, `omcrt`, `omfnd`).", "Foxboro toolset must be installed (e.g. `omgetimp`, `omsetimp`, `omcrt`, `omfnd`)."),
    ("Requiere `ksh.exe` disponible en el sistema.", "Requires `ksh.exe` available on the system."),
    ("Ruta por defecto:", "Default path:"),
    ("Inicializa el servicio verificando que la ruta de las herramientas exista.", "Initializes the service verifying that the toolset path exists."),
    ("Ruta opcional a las herramientas. Si no se provee, usa la ruta estándar de Foxboro en el disco D.", "Optional path to tools. If not provided, uses standard Foxboro path on drive D."),
    ("Escribe un valor en la base de datos. Si la variable no existe, la crea automáticamente con el tipo especificado.", "Writes a value to the database. If the variable does not exist, it creates it automatically with the specified type."),
    ("Lee el valor de una variable.", "Reads the value of a variable."),
    ("El valor convertido al tipo genérico `T`.", "The value converted to generic type `T`."),
    ("`exists` indica si la variable fue encontrada.", "`exists` indicates whether the variable was found."),
    ("Lee un bit específico de una variable de tipo Packed Long (`pl`).", "Reads a specific bit of a Packed Long (`pl`) variable."),
    ("Nombre de la variable PL.", "PL variable name."),
    ("Índice del bit (0-31).", "Bit index (0-31)."),
    ("Crea una secuencia de variables con formato `PREFIJO_001`, `PREFIJO_002`, etc.", "Creates a sequence of variables formatted as `PREFIX_001`, `PREFIX_002`, etc."),
    ("Versión:", "Version:")
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
        
        for old, new in RAW_TRANSLATE:
            content = content.replace(old, new)
            
        with open(target_readme, "w", encoding="utf-8") as f:
            f.write(content)

print("Exact raw translations patched.")
