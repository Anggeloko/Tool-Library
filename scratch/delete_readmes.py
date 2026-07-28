import os

base_dir = r"c:\Users\Axl\Desktop\Projects\Libraries"
root_readme = os.path.join(base_dir, "README.md")

for root, dirs, files in os.walk(base_dir):
    for f in files:
        if f.lower() == "readme.md":
            full_path = os.path.join(root, f)
            if os.path.abspath(full_path) != os.path.abspath(root_readme):
                os.remove(full_path)
                print(f"Deleted: {full_path}")
