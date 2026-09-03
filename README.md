# Minigolf VR

Proyecto de Sebastian Angel Warman y Joaquin Sanchez Laffont para 4to TIC.

## Estado actual

El repositorio incluye la primera base jugable:

- mapa de prueba generado desde Unity;
- pelota con fisicas;
- medidor de fuerza;
- control de escritorio para poder probar sin las gafas;
- entre 1 y 4 jugadores;
- turnos y contador de golpes;
- timer de 60 segundos para el ultimo jugador;
- penalizacion de 5 golpes si termina el tiempo;
- deteccion del hoyo y pantalla de resultados;
- base del palo para conectar despues a los controles VR;
- paquetes OpenXR y XR Interaction Toolkit.

## Crear el prototipo

1. Abrir el proyecto con Unity 6.3.21f1.
2. Esperar que Unity termine de instalar los paquetes.
3. Ir al menu superior MiniGolf VR > Crear prototipo jugable.
4. Aceptar la creacion del mapa.
5. Abrir Assets/Scenes/MinigolfVR.unity y presionar Play.

## Controles de prueba

- A/D o flechas: apuntar.
- Mantener Espacio: cargar fuerza.
- Soltar Espacio: golpear.
- Teclas 1, 2, 3 o 4: reiniciar con esa cantidad de jugadores.
- R: reiniciar la partida.

## VR - primer paso del tutorial

1. Crear primero el mapa con MiniGolf VR > Crear prototipo jugable.
2. En Edit > Project Settings > XR Plug-in Management, activar OpenXR para Windows.
3. Ir a MiniGolf VR > Agregar jugador VR (tutorial).
4. Conectar las gafas al PC y presionar Play.

Esta opcion agrega un XR Origin, la camara que sigue las gafas, los controles izquierdo y derecho y el XR Interaction Manager. El agarre del palo se agrega en el siguiente paso.
