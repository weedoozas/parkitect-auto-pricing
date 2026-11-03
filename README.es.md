# Auto Pricing para Parkitect

El mod ajusta automáticamente los precios de las atracciones y los productos según la valoración de los visitantes. Usa español cuando Parkitect está en español e inglés para los demás idiomas. [English](README.md).

## Un día en el parque

**Visitante de una atracción:** «¿$8.60?» El mod estima el precio de entrada según los visitantes interesados en esa atracción.

<img src="assets/illustrations/ride-guest.png" width="320" alt="Visitante preguntando por un precio de $8.60">

**Visitante de una tienda:** «¿$4.40?» También puede estimar precios de productos si está activada esa opción.

<img src="assets/illustrations/shop-guest.png" width="300" alt="Visitante preguntando por un producto de $4.40">

**Administrador:** «Hora de descansar». Auto Pricing revisa los precios con el intervalo elegido; puedes pasar cualquier precio individual a control manual.

<img src="assets/illustrations/sleeping-manager.png" width="420" alt="Administrador dormido junto a una computadora que calcula precios">

El diálogo y los precios son ilustrativos: no son precios fijos ni una conversación dentro del juego. Consulta los detalles y límites más abajo.

## Instalación y uso

[Suscríbete en Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3832850553) y activa **Auto Pricing** en los ajustes de mods del juego. Como alternativa, descarga la [versión publicada](https://github.com/weedoozas/parkitect-auto-pricing/releases/latest) y copia `AutoPrecios.dll` a una carpeta propia dentro de `Mods` y actívalo. En los ajustes puedes elegir atracciones y tiendas, nivel de precio, porcentaje máximo deseado de visitantes interesados que no pagarían e intervalo de ajuste.

Pulsa **Precio auto. SÍ** junto al precio de una atracción o producto para fijarlo manualmente; vuelve a pulsarlo para automatizarlo. El texto de ayuda muestra el precio sugerido y una estimación de rechazos. Los productos con precio sincronizado comparten el interruptor. Los ajustes individuales se guardan por parque en la carpeta de datos persistentes del juego, dentro de `AutoPricing`. Desactivar el mod no restaura los precios anteriores; puedes guardar una copia antes de probarlo.

## Capturas

![Atracción con precio automático activado](assets/screenshots/ride-es.jpg)

![Producto con precio automático desactivado](assets/screenshots/shop-manual-es.jpg)

Las capturas son del juego; la portada fue creada con IA.

## Cálculo y límites

Por defecto calcula cada 20 segundos reales. Usa la mediana del valor de los visitantes interesados y un límite basado en un percentil inferior; redondea a 0,10 y suaviza cambios pequeños. Si hay menos de cinco muestras, estima atracciones por su emoción y deja sin cambiar los productos. Respeta los escenarios con atracciones o productos gratis. El coste mínimo de un producto puede superar el límite estimado, por lo que el porcentaje de rechazo es orientativo. Puede moderar precios de atracciones cuando se cobra entrada. No se ha comprobado la compatibilidad con otros mods que modifiquen precios ni con multijugador.

Los informes opcionales `informe.txt` e `historial.csv` quedan en `AutoPricing` y no se envían a ningún servidor. Para compilar, reportar errores o contribuir consulta el [README en inglés](README.md). El código se ofrece bajo la licencia permisiva [0BSD](LICENSE); Parkitect y sus recursos pertenecen a sus respectivos titulares. Este es un mod no oficial.
