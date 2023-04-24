import os
import subprocess

dotnet_path = "/usr/bin/dotnet"
project_path = os.path.join(os.getcwd(), "MikuMemories.dll")

subprocess.call([dotnet_path, project_path])
