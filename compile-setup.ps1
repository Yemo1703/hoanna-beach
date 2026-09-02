$issPath = "C:\Users\gburgosh\Downloads\hoanna-beach-main\hoanna-beach-main\hoanna-beach.iss"
$isccPaths = @(
    "C:\Users\gburgosh\AppData\Local\Programs\Inno Setup 6\iscc.exe",
    "C:\Program Files (x86)\Inno Setup 6\iscc.exe",
    "C:\Program Files\Inno Setup 6\iscc.exe",
    "C:\Program Files (x86)\Inno Setup 5\iscc.exe",
    "C:\Program Files\Inno Setup 5\iscc.exe"
)

$isccExe = $isccPaths | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $isccExe) {
    Write-Output "ERROR: No se encontró iscc.exe en las rutas estándar."
    Write-Output "Rutas buscadas:"
    $isccPaths | ForEach-Object { Write-Output "  $_" }
    Write-Output ""
    Write-Output "¿Dónde instalaste Inno Setup?"
    exit 1
}

Write-Output "Encontrado: $isccExe"
Write-Output "Compilando $issPath ..."
& $isccExe $issPath
