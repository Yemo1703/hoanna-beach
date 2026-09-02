# XR18 Bar Control

Aplicación de escritorio WPF para controlar únicamente las zonas y volúmenes permitidos de una Behringer XR18. La pantalla de personal está en español y no expone configuración de audio.

## Compilar

Requiere Windows 10/11 y el SDK de .NET 8:

```powershell
dotnet build .\XR18BarControl.sln -c Release
dotnet publish .\src\XR18BarControl\XR18BarControl.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

El ejecutable publicado queda en `src\XR18BarControl\bin\Release\net8.0-windows\win-x64\publish`.

## Primer uso

1. Conecta el PC y la XR18 a la misma red.
2. Abre la aplicación. Mantén pulsado `AJUSTES` durante dos segundos (o pulsa F12).
3. El PIN inicial es `1234`. Cámbialo durante la instalación.
4. Configura la IP, prueba la conexión y guarda.
5. Verifica los límites máximos con el instalador de sonido.

La configuración se guarda en `%LOCALAPPDATA%\XR18BarControl\config.json`; el PIN se almacena con PBKDF2-SHA256 y sal aleatoria. Los logs rotados están en la subcarpeta `logs`.

## Garantías de seguridad

- Al arrancar o reconectar solo se consulta el estado real: nunca se envían valores predeterminados.
- La API pública de control acepta zonas, no rutas OSC arbitrarias.
- La whitelist contiene exclusivamente `/lr/mix/fader`, `/lr/mix/on`, `/bus/1/mix/fader`, `/bus/1/mix/on`, `/bus/2/mix/fader` y `/bus/2/mix/on`.
- Los límites en dB se vuelven a aplicar en el cliente XR18, independientemente del slider.
- El 0% baja el fader a menos infinito y desactiva la salida. El 100% equivale al máximo configurado.
- La aplicación no sustituye los limitadores/DSP de protección configurados en el mezclador.

## Protocolo

La implementación sigue el documento oficial [X AIR Remote Control Protocol](https://mediadl.musictribe.com/download/software/behringer/XAIR/X%20AIR%20Remote%20Control%20Protocol.pdf): OSC sobre UDP, puerto 10024, valores big-endian alineados a 4 bytes y renovación de `/xremote` cada cinco segundos (su timeout es de diez). Las rutas concretas están aisladas en `XR18Commands.cs` para que una actualización futura del protocolo pueda auditarse en un único lugar.

Bus 1 y Bus 2 deben quedar previamente configurados/enlazados en la XR18. Esta aplicación no modifica enlace, routing, DSP, ganancia ni ningún otro parámetro técnico.

La pantalla administrativa también permite buscar automáticamente mesas X AIR en la red local. La búsqueda envía únicamente `/xinfo` por broadcast UDP, muestra las respuestas recibidas y rellena la IP; no conecta ni cambia ningún parámetro de audio.

## Simulador local

`XR18Simulator` permite probar la aplicación sin hardware. Escucha en UDP 10024, responde a `/xinfo`, mantiene `/xremote` y simula exclusivamente los faders/mutes permitidos:

```bash
dotnet run --project src/XR18Simulator
```

En la aplicación pulsa `BUSCAR XR18`; aparecerá como `XR18-SIM`. El terminal del simulador muestra todos los paquetes recibidos y permite simular cambios externos con `main 50`, `terrace 30`, `main off` o `terrace on`.

## Batería automática

Antes de publicar o lanzar una versión se ejecuta el comprobador sin dependencias externas:

```bash
dotnet run --project tests/XR18BarControl.SelfTests -c Release
```

Valida serialización OSC, ley de fader, límites backend, whitelist, Main/Aux 1–6, parejas enlazadas y configuración dinámica de zonas.
