# The September 22 Editor crash stack ends in NVIDIA AI HDR during a DX12 Present.
# Launch this project with D3D11 to avoid that driver path while editing.
$unityEditor = 'D:\Unit\6000.5.9f1\Editor\Unity.exe'
$projectDirectory = $PSScriptRoot
if (-not (Test-Path -LiteralPath $unityEditor)) { throw "Unity Editor not found: $unityEditor" }
Start-Process -FilePath $unityEditor -ArgumentList @('-projectPath', $projectDirectory, '-force-d3d11')
