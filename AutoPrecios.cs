using System.Collections.Generic;
using UnityEngine;

namespace AutoPrecios
{
	internal static class Texto
	{
		public static string Elegir(string ingles, string espanol) =>
			Settings.Instance.localeIsoCode != null && Settings.Instance.localeIsoCode.StartsWith("es", System.StringComparison.OrdinalIgnoreCase) ? espanol : ingles;
	}

	// Punto de entrada del mod: Parkitect crea esta clase al cargar la DLL.
	public class AutoPreciosMod : AbstractMod, IModSettings
	{
		private const string NOMBRE_HOST = "AutoPrecios";

		private GameObject host;

		public AutoPreciosMod()
		{
			// Se crea al arrancar el juego (menú principal): sitio para el modo de prueba.
			Prueba.iniciarSiHayArchivo();
		}

#if BETA
		public override string getName() => "Auto Pricing BETA";

		public override string getIdentifier() => "wezzz.autoprecios.beta";
#else
		public override string getName() => "Auto Pricing";

		public override string getIdentifier() => "wezzz.autoprecios";
#endif

		public override string getDescription() =>
			Texto.Elegir("Automatically adjusts ride and shop prices based on guest valuations.", "Ajusta automáticamente los precios de atracciones y tiendas según la valoración de los visitantes.");

#if BETA
		public override string getVersionNumber() => "1.3.0-beta.1";
#else
		public override string getVersionNumber() => "1.3.0";
#endif

		public override void onEnabled()
		{
			// El mod puede estar instalado dos veces (Workshop y carpeta Mods): solo actúa una copia.
			if (GameObject.Find(NOMBRE_HOST) != null)
			{
				Debug.Log("[AutoPrecios] Another copy of the mod is already active; this one stays idle.");
				return;
			}
			Ajustes.cargar();
			host = new GameObject(NOMBRE_HOST);
			Object.DontDestroyOnLoad(host);
			host.AddComponent<Ajustador>();
			host.AddComponent<Interfaz>();
		}

		public override void onDisabled()
		{
			if (host != null)
			{
				Object.Destroy(host);
				host = null;
			}
		}

		public void onSettingsOpened()
		{
		}

		public void onSettingsClosed()
		{
			Ajustes.guardar();
		}

		public void onDrawSettingsUI()
		{
			Ajustes.atracciones = GUILayout.Toggle(Ajustes.atracciones, Texto.Elegir(" Adjust ride prices", " Ajustar atracciones"));
			Ajustes.tiendas = GUILayout.Toggle(Ajustes.tiendas, Texto.Elegir(" Adjust shop product prices", " Ajustar productos de tiendas"));
			Ajustes.noPerder = GUILayout.Toggle(Ajustes.noPerder, Texto.Elegir(" Never sell below cost", " Nunca vender por debajo del coste"));
			Ajustes.moderarConEntrada = GUILayout.Toggle(Ajustes.moderarConEntrada,
				Texto.Elegir(" Moderate ride prices when the park charges admission", " Moderar precios si el parque cobra entrada"));
			Ajustes.guardarInforme = GUILayout.Toggle(Ajustes.guardarInforme,
				Texto.Elegir(" Save report (informe.txt and historial.csv in the mod data folder)", " Guardar informe (informe.txt e historial.csv en la carpeta de datos del mod)"));
			GUILayout.Space(8f);
			GUILayout.Label(Texto.Elegir("Price level: ", "Nivel de precio: ") + Ajustes.nivel.ToString("0.00") + Texto.Elegir(" × the typical guest valuation (", " × el valor del visitante típico (") +
				describirNivel(Ajustes.nivel) + ")");
			Ajustes.nivel = Mathf.Round(GUILayout.HorizontalSlider(Ajustes.nivel, 0.5f, 1.5f) * 20f) / 20f;
			GUILayout.Label(Texto.Elegir("This is the level used by Money. Level now: ", "Este es el nivel que usa Dinero. Nivel ahora: ") + Ajustador.nivelActual.ToString("0.00"));
			GUILayout.Space(8f);
			GUILayout.Label(Texto.Elegir("Target maximum share of interested guests priced out: ", "Porcentaje máximo deseado de visitantes interesados que no pagarían: ") + Mathf.RoundToInt(Ajustes.rechazoMax * 100f) +
				"%");
			Ajustes.rechazoMax = Mathf.Round(GUILayout.HorizontalSlider(Ajustes.rechazoMax, 0f, 0.3f) * 20f) / 20f;
			GUILayout.Space(8f);
			GUILayout.Label(Texto.Elegir("Adjust every ", "Ajustar cada ") + Mathf.RoundToInt(Ajustes.intervalo) + " s");
			Ajustes.intervalo = Mathf.Round(GUILayout.HorizontalSlider(Ajustes.intervalo, 5f, 120f));
		}

		private static string describirNivel(float nivel)
		{
			if (nivel < 0.95f)
			{
				return Texto.Elegir("low", "bajo");
			}
			if (nivel < 1.15f)
			{
				return Texto.Elegir("fair", "justo");
			}
			if (nivel < 1.35f)
			{
				return Texto.Elegir("medium-high", "medio-alto");
			}
			return Texto.Elegir("high", "alto");
		}
	}

	// Preferencias guardadas en PlayerPrefs del juego.
	internal static class Ajustes
	{
		public static bool atracciones = true;
		public static bool tiendas = true;
		public static bool noPerder = true;
		public static bool moderarConEntrada = true;
		public static bool guardarInforme = false;
		public static float nivel = 1.25f;
		public static float rechazoMax = 0.1f;
		public static float intervalo = 20f;

		public static void cargar()
		{
			atracciones = PlayerPrefs.GetInt("autoprecios.atracciones", 1) == 1;
			tiendas = PlayerPrefs.GetInt("autoprecios.tiendas", 1) == 1;
			noPerder = PlayerPrefs.GetInt("autoprecios.noPerder", 1) == 1;
			moderarConEntrada = PlayerPrefs.GetInt("autoprecios.moderarConEntrada", 1) == 1;
			guardarInforme = PlayerPrefs.GetInt("autoprecios.guardarInforme", 0) == 1;
			nivel = PlayerPrefs.GetFloat("autoprecios.nivel", 1.25f);
			rechazoMax = PlayerPrefs.GetFloat("autoprecios.rechazoMax", 0.1f);
			intervalo = PlayerPrefs.GetFloat("autoprecios.intervalo", 20f);
			if (Prueba.nivelForzado > 0f)
			{
				nivel = Prueba.nivelForzado;
			}
		}

		public static void guardar()
		{
			PlayerPrefs.SetInt("autoprecios.atracciones", atracciones ? 1 : 0);
			PlayerPrefs.SetInt("autoprecios.tiendas", tiendas ? 1 : 0);
			PlayerPrefs.SetInt("autoprecios.noPerder", noPerder ? 1 : 0);
			PlayerPrefs.SetInt("autoprecios.moderarConEntrada", moderarConEntrada ? 1 : 0);
			PlayerPrefs.SetInt("autoprecios.guardarInforme", guardarInforme ? 1 : 0);
			PlayerPrefs.SetFloat("autoprecios.nivel", nivel);
			PlayerPrefs.SetFloat("autoprecios.rechazoMax", rechazoMax);
			PlayerPrefs.SetFloat("autoprecios.intervalo", intervalo);
			PlayerPrefs.Save();
		}
	}

	// Recalcula los precios cada pocos segundos mientras hay un parque cargado.
	//
	// Fórmula del juego (Person.calculatePriceStatisfaction), con v = valor para ese visitante:
	//   precio <  v → satisfacción 1 - 0.5·(precio/v)^1.6   (contento)
	//   precio >= v → satisfacción 1 - precio/(2·v)          (0.5 en precio == v)
	//   satisfacción < 0.2 (precio > 1.6·v) → no se sube / no compra.
	//   satisfacción < 0.25 (precio > 1.5·v) en productos → piensa que es caro.
	// Valor de una atracción = emoción × 16 × gusto por la intensidad (hasta ×1.25) × generosidad
	// (×1 a ×1.2): para quien le gusta la intensidad es emoción × 20, la regla «emoción × 2 / 10»
	// de los foros (emoción en escala 0-100). Los foros ponen las montañas rusas hasta un 25 %
	// por encima, y por eso el nivel por defecto es 1.25 × la mediana (satisfacción ≈ 0.38).
	// Tope: como mucho `rechazoMax` de los interesados queda por encima de 1.6 × su valor.
	internal class Ajustador : MonoBehaviour
	{
		private const int MIN_MUESTRAS = 5;
		private const float CAMBIO_MINIMO = 0.1f;
		// Por encima de 1.6 × valor el visitante se niega; dejamos un pequeño margen.
		private const float LIMITE_RECHAZO = 1.55f;
		// En productos, por encima de 1.5 × valor piensan que es caro y baja la nota de la tienda.
		private const float LIMITE_QUEJA_PRODUCTO = 1.45f;
		// Si el parque cobra entrada, cada atracción cara resta valor a la entrada
		// (Park.calculateValueFor resta 1.5 × precio de cada atracción): bajamos el nivel.
		private const float FACTOR_CON_ENTRADA = 0.85f;
		// Espera tras cargar el parque antes del primer ajuste.
		private const float ESPERA_INICIAL = 10f;

		private float siguiente;
		private readonly List<float> valores = new List<float>();
		private static bool pronto;

		// Nivel de precio en uso, según el modo del parque.
		public static float nivelActual = 1.25f;

		// Tras encender un interruptor: ajustar en el siguiente fotograma, sin esperar el intervalo.
		public static void ajustarPronto()
		{
			pronto = true;
		}

		private void Start()
		{
			siguiente = Time.unscaledTime + ESPERA_INICIAL;
		}

		private void Update()
		{
			if (pronto)
			{
				pronto = false;
				siguiente = 0f;
			}
			if (Time.unscaledTime < siguiente)
			{
				return;
			}
			GameController gc = GameController.Instance;
			if (gc == null || gc.isLoadingGame || gc.park == null || CommandController.Instance == null)
			{
				// No tocar nada mientras el juego carga: reintentar con margen.
				siguiente = Time.unscaledTime + ESPERA_INICIAL;
				return;
			}
			siguiente = Time.unscaledTime + Mathf.Max(5f, Ajustes.intervalo);
			try
			{
				nivelActual = Objetivos.nivelEfectivo(gc.park);
				if (Informe.activo)
				{
					Informe.empezar(gc.park);
				}
				if (Ajustes.atracciones && !gc.park.settings.freeRideEntranceFees)
				{
					ajustarAtracciones(gc.park);
				}
				if (Informe.activo)
				{
					Informe.cabeceraTiendas();
				}
				if (Ajustes.tiendas && !gc.park.settings.freeShopProducts)
				{
					ajustarTiendas(gc.park);
				}
				if (Informe.activo)
				{
					Informe.terminar(gc.park);
				}
			}
			catch (System.Exception e)
			{
				Debug.LogWarning("[AutoPrecios] " + e);
			}
		}

		private void ajustarAtracciones(Park park)
		{
			float nivel = nivelActual;
			if (Ajustes.moderarConEntrada && park.parkInfo != null && park.parkInfo.parkEntranceFee > 0f)
			{
				nivel *= FACTOR_CON_ENTRADA;
			}
			IList<Guest> visitantes = park.getGuests();
			foreach (Attraction atraccion in park.getAttractions())
			{
				if (atraccion == null)
				{
					continue;
				}
				valores.Clear();
				foreach (Guest g in visitantes)
				{
					// Solo cuentan los que soportan esa intensidad: los demás no se suben igual.
					if (g != null && atraccion.gaugeInterest(g) >= 0.5f)
					{
						valores.Add(atraccion.calculateValueFor(g));
					}
				}
				float precio;
				if (valores.Count >= MIN_MUESTRAS)
				{
					precio = precioObjetivo(valores, nivel, LIMITE_RECHAZO);
				}
				else
				{
					// Parque vacío: visitante al que le gusta la intensidad, generosidad media (×1.1).
					float emocion = atraccion.getExcitementRating();
					if (atraccion.statsAreOutdated)
					{
						emocion = Mathf.Max(0.3f, emocion);
					}
					precio = emocion * 16f * 1.25f * 1.1f * nivel;
				}
				string clave = Estado.claveAtraccion(atraccion);
				bool automatico = Estado.esAuto(clave);
				Estado.anotar(clave, redondear(precio), valores.Count >= MIN_MUESTRAS
					? Informe.fraccion(valores, atraccion.entranceFee, 1.6f) : -1f);
				precio = redondear(suavizar(atraccion.entranceFee, precio));
				if (Prueba.soloInforme || !automatico)
				{
					precio = atraccion.entranceFee;
				}
				if (Informe.activo)
				{
					valores.Sort();
					Informe.atraccion(atraccion, valores, atraccion.entranceFee, precio);
				}
				if (Mathf.Abs(precio - atraccion.entranceFee) >= CAMBIO_MINIMO)
				{
					CommandController.Instance.addCommand(new AttractionChangeEntranceFeeCommand(atraccion, precio));
				}
			}
		}

		private void ajustarTiendas(Park park)
		{
			IList<Guest> visitantes = park.getGuests();
			// Precio objetivo por producto (los sincronizados comparten precio en todo el parque).
			Dictionary<Product, float> objetivos = new Dictionary<Product, float>();
			Dictionary<Product, List<float>> muestras = new Dictionary<Product, List<float>>();
			HashSet<Product> sincronizadosHechos = new HashSet<Product>();
			foreach (Shop shop in park.getShops())
			{
				ProductShop tienda = shop as ProductShop;
				if (tienda == null)
				{
					continue;
				}
				ProductShopSettings ajustesTienda = tienda.getSettings() as ProductShopSettings;
				if (ajustesTienda == null)
				{
					continue;
				}
				foreach (Product producto in tienda.selectedProducts)
				{
					if (producto == null)
					{
						continue;
					}
					ProductSettings ps = ajustesTienda.getProductSettings(producto);
					if (ps == null || (ps.synchronizedPrice && sincronizadosHechos.Contains(producto)))
					{
						continue;
					}
					float objetivo;
					if (!objetivos.TryGetValue(producto, out objetivo))
					{
						objetivo = valorProducto(producto, visitantes);
						objetivos[producto] = objetivo;
						muestras[producto] = new List<float>(valores);
					}
					string clave = Estado.claveProducto(tienda, ps, producto);
					bool automatico = Estado.esAuto(clave);
					if (objetivo <= 0f)
					{
						Estado.anotar(clave, 0f, -1f);
						continue;
					}
					float precio = objetivo;
					float costeProducto = coste(producto, ps);
					if (Ajustes.noPerder)
					{
						precio = Mathf.Max(precio, costeProducto + 0.1f);
					}
					Estado.anotar(clave, redondear(precio), Informe.fraccion(muestrasDe(muestras, producto), ps.price, 1.6f));
					precio = redondear(suavizar(ps.price, precio));
					if (Prueba.soloInforme || !automatico)
					{
						precio = ps.price;
					}
					if (Informe.activo)
					{
						Informe.producto(producto, tienda, muestras[producto], ps.price, precio, costeProducto);
					}
					if (ps.synchronizedPrice)
					{
						sincronizadosHechos.Add(producto);
					}
					if (Mathf.Abs(precio - ps.price) >= CAMBIO_MINIMO)
					{
						CommandController.Instance.addCommand(new ShopChangeProductPriceCommand(tienda, producto, precio));
					}
				}
			}
		}

		private static List<float> muestrasDe(Dictionary<Product, List<float>> muestras, Product producto)
		{
			List<float> lista;
			return muestras.TryGetValue(producto, out lista) ? lista : new List<float>();
		}

		// Precio objetivo para los visitantes que ahora mismo lo querrían comprar.
		// Devuelve 0 si no hay suficientes para estimarlo (se deja el precio como está).
		private float valorProducto(Product producto, IList<Guest> visitantes)
		{
			valores.Clear();
			foreach (Guest g in visitantes)
			{
				if (g != null && producto.calculateInterestFor(g) >= 0.25f)
				{
					valores.Add(producto.calculateValueFor(g));
				}
			}
			if (valores.Count < MIN_MUESTRAS)
			{
				return 0f;
			}
			return precioObjetivo(valores, Mathf.Min(nivelActual, LIMITE_QUEJA_PRODUCTO), LIMITE_QUEJA_PRODUCTO);
		}

		// nivel × mediana del valor, sin pasar del precio que dejaría fuera a más de
		// `rechazoMax` de los visitantes (limite × valor del percentil rechazoMax).
		private static float precioObjetivo(List<float> lista, float nivel, float limite)
		{
			lista.Sort();
			float mediana = percentil(lista, 0.5f);
			float tope = percentil(lista, Ajustes.rechazoMax) * limite;
			return Mathf.Min(mediana * nivel, tope);
		}

		private static float coste(Product producto, ProductSettings ps)
		{
			float total = 0f;
			if (producto.ingredients == null)
			{
				return total;
			}
			foreach (Ingredient ing in producto.ingredients)
			{
				if (ing == null || ing.resource == null)
				{
					continue;
				}
				ResourceSettings rs = ps.getResourceSetting(ing.resource);
				total += ing.resource.getCosts() * (rs != null ? rs.percentage : 1f);
			}
			return total;
		}

		// La lista debe venir ordenada.
		private static float percentil(List<float> lista, float p)
		{
			int i = Mathf.Clamp(Mathf.FloorToInt(p * (lista.Count - 1)), 0, lista.Count - 1);
			return lista[i];
		}

		// Los valores de los visitantes cambian de un ajuste a otro (entran y salen, tienen más o
		// menos hambre): para que el precio no dé saltos, se acerca a medias al objetivo. Si está
		// muy lejos (primer ajuste, atracción nueva) salta directamente.
		private static float suavizar(float actual, float objetivo)
		{
			if (objetivo <= 0f || Mathf.Abs(objetivo - actual) > objetivo * 0.3f)
			{
				return objetivo;
			}
			return actual + (objetivo - actual) * 0.5f;
		}

		// Múltiplos de 0.10 y nunca negativo.
		private static float redondear(float precio)
		{
			return Mathf.Max(0f, Mathf.Round(precio * 10f) / 10f);
		}
	}
}
