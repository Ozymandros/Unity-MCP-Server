import subprocess
import json

# Obre el procés (igual que abans)
proc = subprocess.Popen(
    "unity-mcp",  # or ["unity-mcp"] if not using shell=True
    cwd="C:\\Projects\\Unity-MCP-Server",  # Adjust as needed
    stdin=subprocess.PIPE,
    stdout=subprocess.PIPE,
    stderr=subprocess.PIPE
)

# Preparar la crida a l'eina unity_list_assets
comanda = {
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
        "name": "unity_list_assets",
        "arguments": {
            "path": "Assets",
            "pattern": "*.cs"
        }
    },
    "id": 2
}

# Envia la comanda
missatge = json.dumps(comanda) + "\n"
proc.stdin.write(missatge.encode())
proc.stdin.flush()

# Llegeix la resposta
resposta = proc.stdout.readline()
if resposta:
    print("Resposta:", json.loads(resposta.decode()))
else:
    error = proc.stderr.read()
    print("Error:", error.decode())

proc.terminate()
