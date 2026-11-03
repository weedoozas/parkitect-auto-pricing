using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace AutoPrecios
{
	// Modo de prueba: si existe `prueba.txt` en la carpeta del mod, al arrancar el juego abre
	// sola una partida, acelera el tiempo y cierra el juego pasado un rato. Así se puede
	// comprobar el mod sin tocar el ratón. Formato de prueba.txt (una clave por línea):
	//   partida=/ruta/a/la/partida.park
	//   velocidad=3
	//   segundos=300        (tiempo real con el parque abierto antes de cerrar el juego)
	internal class Prueba : MonoBehaviour
	{
		public static string carpeta;
		public static bool activa;
		// ajustar=0: no cambia precios, solo escribe el informe (partida de referencia).
		public static bool soloInforme;
		// ventanas=1: abre la ventana de una atracción y de una tienda para hacer capturas.
		private bool abrirVentanas;

		private string partida;
		private float velocidad = 3f;
		private float segundos = 300f;

		private int fase;
		private float momento;

		public static void iniciarSiHayArchivo()
		{
			string carpetaMod = System.IO.Path.GetDirectoryName(typeof(Prueba).Assembly.Location);
			carpeta = System.IO.Path.Combine(Application.persistentDataPath, "AutoPricing");
			try { Directory.CreateDirectory(carpeta); }
			catch (System.Exception e) { Debug.LogWarning("[AutoPrecios] Cannot create data directory: " + e.Message); carpeta = null; }
			string archivo = System.IO.Path.Combine(carpetaMod ?? "", "prueba.txt");
			if (string.IsNullOrEmpty(carpetaMod) || !File.Exists(archivo))
			{
				return;
			}
			activa = true;
			GameObject go = new GameObject("AutoPrecios.Prueba");
			Object.DontDestroyOnLoad(go);
			go.AddComponent<Prueba>().leer(archivo);
		}

		private void leer(string archivo)
		{
			foreach (string linea in File.ReadAllLines(archivo))
			{
				int igual = linea.IndexOf('=');
				if (igual <= 0)
				{
					continue;
				}
				string clave = linea.Substring(0, igual).Trim();
				string valor = linea.Substring(igual + 1).Trim();
				float numero;
				if (clave == "partida")
				{
					partida = valor;
				}
				else if (clave == "ventanas")
				{
					abrirVentanas = valor == "1";
				}
				else if (clave == "ajustar")
				{
					soloInforme = valor == "0";
				}
				else if (clave == "velocidad" && float.TryParse(valor, NumberStyles.Float, CultureInfo.InvariantCulture, out numero))
				{
					velocidad = numero;
				}
				else if (clave == "segundos" && float.TryParse(valor, NumberStyles.Float, CultureInfo.InvariantCulture, out numero))
				{
					segundos = numero;
				}
			}
			Debug.Log("[AutoPrecios] Modo prueba: partida=" + partida + " velocidad=" + velocidad + " segundos=" + segundos +
				(soloInforme ? " (solo informe)" : ""));
		}

		// Capturas en la carpeta del mod: 1 ventana de atracción, 2 productos de una tienda con
		// el auto-precio encendido, 3 la misma tienda con el primer producto apagado.
		private System.Collections.IEnumerator capturas(Park park)
		{
			yield return new WaitForSecondsRealtime(12f);
			foreach (Attraction a in park.getAttractions())
			{
				if (a != null && a.onOpenInfoWindow())
				{
					break;
				}
			}
			yield return new WaitForSecondsRealtime(2f);
			volcarJerarquia();
			capturar(1);
			yield return new WaitForSecondsRealtime(1f);
			cerrarVentanas();
			foreach (Shop s in park.getShops())
			{
				if (s is ProductShop && ((ProductShop)s).selectedProducts.Length > 0 && s.onOpenInfoWindow())
				{
					break;
				}
			}
			yield return null;
			abrirPestanaDeProductos();
			yield return new WaitForSecondsRealtime(2f);
			volcarJerarquia();
			capturar(2);
			yield return new WaitForSecondsRealtime(1f);
			foreach (Parkitect.UI.ShopProductInfoEntry e in Object.FindObjectsOfType<Parkitect.UI.ShopProductInfoEntry>())
			{
				Interfaz.Interruptor i = e.GetComponentInChildren<Interfaz.Interruptor>();
				if (i != null && i.clave != null)
				{
					Estado.cambiar(i.clave, false);
					break;
				}
			}
			yield return new WaitForSecondsRealtime(1.5f);
			capturar(3);
		}

		// Para colocar el interruptor: escribe en el log la cadena de padres de cada casilla de
		// precio decorada y los hermanos de cada nivel, con sus tamaños y layouts.
		private static void volcarJerarquia()
		{
			StringBuilder sb = new StringBuilder("[AutoPrecios] Jerarquía:\n");
			foreach (Interfaz.Interruptor i in Object.FindObjectsOfType<Interfaz.Interruptor>())
			{
				sb.Append("== casilla ").Append(i.clave).Append('\n');
				Transform t = i.transform;
				for (int nivel = 0; nivel < 5 && t != null; nivel++, t = t.parent)
				{
					sb.Append("  nivel ").Append(nivel).Append(": ").Append(describir(t)).Append('\n');
					if (t.parent != null)
					{
						foreach (Transform h in t.parent)
						{
							sb.Append("      hermano: ").Append(describir(h)).Append('\n');
						}
					}
				}
			}
			Debug.Log(sb.ToString());
		}

		private static string describir(Transform t)
		{
			RectTransform r = t as RectTransform;
			StringBuilder sb = new StringBuilder(t.name);
			if (r != null)
			{
				sb.Append(" rect=").Append(r.rect.width.ToString("0")).Append('x').Append(r.rect.height.ToString("0"))
					.Append(" anchors=").Append(r.anchorMin).Append(r.anchorMax).Append(" pos=").Append(r.anchoredPosition);
			}
			sb.Append(t.gameObject.activeSelf ? "" : " (inactivo)").Append(" [");
			foreach (Component c in t.GetComponents<Component>())
			{
				if (c != null && !(c is Transform))
				{
					sb.Append(c.GetType().Name).Append(' ');
				}
			}
			return sb.Append(']').ToString();
		}

		private static void capturar(int n)
		{
			ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(carpeta, "captura" + n + ".png"));
		}

		private static void cerrarVentanas()
		{
			foreach (Parkitect.UI.UIWindowFrame w in Object.FindObjectsOfType<Parkitect.UI.UIWindowFrame>())
			{
				w.close();
			}
		}

		private static void abrirPestanaDeProductos()
		{
			System.Reflection.FieldInfo paneles = typeof(Parkitect.UI.UITabGroup).GetField("tabPanels",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			foreach (Parkitect.UI.UITabGroup g in Object.FindObjectsOfType<Parkitect.UI.UITabGroup>())
			{
				Transform[] lista = paneles?.GetValue(g) as Transform[];
				if (lista == null)
				{
					continue;
				}
				foreach (Transform panel in lista)
				{
					if (panel != null && panel.GetComponentInChildren<Parkitect.UI.ShopProductInfoEntry>(true) != null)
					{
						g.openTab(panel);
						return;
					}
				}
			}
		}

		private void Update()
		{
			GameController gc = GameController.Instance;
			switch (fase)
			{
				case 0:
					// Esperar al menú principal ya cargado.
					if (Time.realtimeSinceStartup > 15f && gc == null && !string.IsNullOrEmpty(partida))
					{
						Debug.Log("[AutoPrecios] Modo prueba: abriendo " + partida);
						Loader.Instance.loadSavegame(partida, GameController.GameMode.NO_OVERRIDE, null,
							rememberFilePath: false, newPark: false, showNewParkUI: false);
						fase = 1;
					}
					break;
				case 1:
					if (gc != null && gc.park != null && !gc.isLoadingGame)
					{
						momento = Time.unscaledTime;
						fase = 2;
					}
					break;
				case 2:
					// Unos segundos para que termine de montarse la escena antes de acelerar.
					if (Time.unscaledTime - momento > 5f)
					{
						gc.triggerChangeGameSpeed(velocidad);
						if (abrirVentanas)
						{
							StartCoroutine(capturas(gc.park));
						}
						Debug.Log("[AutoPrecios] Modo prueba: speed " + velocidad);
						momento = Time.unscaledTime;
						fase = 3;
					}
					break;
				case 3:
					if (Time.unscaledTime - momento > segundos)
					{
						Debug.Log("[AutoPrecios] Modo prueba: fin, cerrando el juego");
						fase = 4;
						Application.Quit();
					}
					break;
			}
		}
	}

	// Escribe en la carpeta del mod lo que el mod calcula en cada ajuste:
	// informe.txt (último ajuste, legible) e historial.csv (una línea por ajuste).
	internal static class Informe
	{
		private static readonly StringBuilder texto = new StringBuilder();
		private static readonly CultureInfo inv = CultureInfo.InvariantCulture;
		private static int ajustes;

		public static bool activo => Prueba.activa || Ajustes.guardarInforme;

		public static void empezar(Park park)
		{
			texto.Length = 0;
			ParkInfo info = park.parkInfo;
			texto.AppendLine("Auto Pricing — adjustment " + (++ajustes) + ", park time " + ParkInfo.ParkTime +
				", speed " + Time.timeScale.ToString("0.#", inv));
			texto.AppendLine("Guests " + park.getGuests().Count + " · money " + info.money.ToString("0", inv) +
				" · park rating " + pct(info.getOverallRating()) + " · price satisfaction " +
				pct(info.RatingPriceSatisfaction) + " · admission " + info.parkEntranceFee.ToString("0.00", inv) +
				" · level " + Ajustes.nivel.ToString("0.00", inv));
			texto.AppendLine();
			texto.AppendLine("RIDES (value = valuation by an interested guest)");
			texto.AppendLine("name | excitement | interested guests | value p10/median/p90 | price before → after | refuse | mean satisfaction | customers");
		}

		public static void cabeceraTiendas()
		{
			texto.AppendLine();
			texto.AppendLine("PRODUCTS");
			texto.AppendLine("product (shop) | interested guests | value p10/median/p90 | price before → after | cost | consider expensive | refuse | mean satisfaction");
		}

		// `valores` debe venir ordenada.
		public static void atraccion(Attraction a, List<float> valores, float antes, float ahora)
		{
			texto.Append(a.getCustomizedName()).Append(" | ").Append((a.getExcitementRating() * 100f).ToString("0", inv))
				.Append(" | ").Append(valores.Count).Append(" | ").Append(rango(valores))
				.Append(" | ").Append(antes.ToString("0.00", inv)).Append(" → ").Append(ahora.ToString("0.00", inv))
				.Append(" | ").Append(pct(fraccion(valores, ahora, 1.6f)))
				.Append(" | ").Append(pct(satisfaccionMedia(valores, ahora)))
				.Append(" | ").Append(a.customersCount).AppendLine();
		}

		public static void producto(Product p, ProductShop tienda, List<float> valores, float antes, float ahora, float coste)
		{
			texto.Append(p.getUnlocalizedName()).Append(" (").Append(tienda.getCustomizedName()).Append(") | ")
				.Append(valores.Count).Append(" | ").Append(rango(valores))
				.Append(" | ").Append(antes.ToString("0.00", inv)).Append(" → ").Append(ahora.ToString("0.00", inv))
				.Append(" | ").Append(coste.ToString("0.00", inv))
				.Append(" | ").Append(pct(fraccion(valores, ahora, 1.5f)))
				.Append(" | ").Append(pct(fraccion(valores, ahora, 1.6f)))
				.Append(" | ").Append(pct(satisfaccionMedia(valores, ahora))).AppendLine();
		}

		public static void terminar(Park park)
		{
			if (Prueba.carpeta == null)
			{
				return;
			}
			File.WriteAllText(System.IO.Path.Combine(Prueba.carpeta, "informe.txt"), texto.ToString());
			string csv = System.IO.Path.Combine(Prueba.carpeta, "historial.csv");
			if (!File.Exists(csv))
			{
				File.WriteAllText(csv, "ajuste,tiempo,visitantes,dinero,nota,satisfaccion_precios\n");
			}
			ParkInfo info = park.parkInfo;
			File.AppendAllText(csv, string.Format(inv, "{0},{1},{2},{3:0},{4:0.000},{5:0.000}\n", ajustes, ParkInfo.ParkTime,
				park.getGuests().Count, info.money, info.getOverallRating(), info.RatingPriceSatisfaction));
		}

		private static string rango(List<float> v)
		{
			if (v.Count == 0)
			{
				return "-";
			}
			return en(v, 0.1f).ToString("0.00", inv) + "/" + en(v, 0.5f).ToString("0.00", inv) + "/" + en(v, 0.9f).ToString("0.00", inv);
		}

		private static float en(List<float> v, float p)
		{
			return v[Mathf.Clamp(Mathf.FloorToInt(p * (v.Count - 1)), 0, v.Count - 1)];
		}

		// Parte de los visitantes con precio > limite × su valor.
		internal static float fraccion(List<float> v, float precio, float limite)
		{
			if (v.Count == 0)
			{
				return 0f;
			}
			int n = 0;
			foreach (float x in v)
			{
				if (precio > limite * x)
				{
					n++;
				}
			}
			return (float)n / v.Count;
		}

		// Misma fórmula que Person.calculatePriceStatisfaction.
		private static float satisfaccionMedia(List<float> v, float precio)
		{
			if (v.Count == 0)
			{
				return 0f;
			}
			float suma = 0f;
			foreach (float w in v)
			{
				float s;
				if (Mathf.Approximately(w, 0f))
				{
					s = Mathf.Approximately(precio, 0f) ? 1f : 0f;
				}
				else
				{
					s = precio < w ? 1f - Mathf.Pow(precio / w, 1.6f) * 0.5f : 1f - precio / (w * 2f);
				}
				suma += Mathf.Clamp01(s);
			}
			return suma / v.Count;
		}

		private static string pct(float x)
		{
			return Mathf.RoundToInt(x * 100f) + "%";
		}
	}
}
