import os

# Try to delete nul file using os.remove
nul_path = r"D:\CodeWork\WithVs2022\KaiWork\Kai.Client\Kai.Client.CallCenter\nul"

try:
    if os.path.exists(nul_path):
        os.remove(nul_path)
        print(f"Successfully deleted: {nul_path}")
    else:
        print(f"File not found: {nul_path}")
except Exception as e:
    print(f"Error: {e}")

    # Try with \\?\ prefix for long path support
    try:
        extended_path = r"\\?\ "[:-1] + nul_path
        os.remove(extended_path)
        print(f"Successfully deleted with extended path: {extended_path}")
    except Exception as e2:
        print(f"Extended path also failed: {e2}")
