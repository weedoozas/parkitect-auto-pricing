using System.Reflection;
using Parkitect.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AutoPrecios
{
	// Junto a cada casilla de precio (ventana de atracción, lista de atracciones y productos de
	// tienda) pone un interruptor «Auto-pricing ON/OFF». Encendido (por defecto) la casilla queda
	// bloqueada y el mod pone el precio; apagado se puede escribir a mano. Su tooltip muestra el
	// precio sugerido y cuántos visitantes rechazan el precio actual.
	internal class Interfaz : MonoBehaviour
	{
		private const BindingFlags PRIVADO = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
		private const float CADA = 0.25f;

		private static readonly Color VERDE = new Color(0.18f, 0.55f, 0.27f, 1f);
		private static readonly Color GRIS = new Color(0.42f, 0.42f, 0.45f, 1f);

		private static readonly FieldInfo feeTab = typeof(AttractionStateTab).GetField("entranceFeeInput", PRIVADO);
		private static readonly MethodInfo objetoTab = typeof(AttractionStateTab).GetMethod("getObject", PRIVADO);
		private static readonly FieldInfo feeLista = typeof(AttractionListEntry).GetField("entranceFeeInput", PRIVADO);
		private static readonly FieldInfo precioProducto = typeof(ShopProductInfoEntry).GetField("priceInput", PRIVADO);
		private static readonly FieldInfo ajustesProducto = typeof(ShopProductInfoEntry).GetField("productSettings", PRIVADO);
		private static readonly FieldInfo tiendaProducto = typeof(ShopProductInfoEntry).GetField("shop", PRIVADO);

		private float siguiente;

		private void Update()
		{
			if (Time.unscaledTime < siguiente)
			{
				return;
			}
			siguiente = Time.unscaledTime + CADA;
			GameController gc = GameController.Instance;
			if (gc == null || gc.isLoadingGame || gc.park == null)
			{
				return;
			}
			try
			{
				bool atraccionesLibres = !Ajustes.atracciones || gc.park.settings.freeRideEntranceFees;
				foreach (AttractionStateTab tab in Object.FindObjectsOfType<AttractionStateTab>())
				{
					Attraction a = objetoTab?.Invoke(tab, null) as Attraction;
					decorar(feeTab?.GetValue(tab) as UIUnitInputField, a != null ? Estado.claveAtraccion(a) : null,
						gc.park.settings.freeRideEntranceFees, atraccionesLibres);
				}
				foreach (AttractionListEntry entrada in Object.FindObjectsOfType<AttractionListEntry>())
				{
					Attraction a = entrada.getObject();
					decorar(feeLista?.GetValue(entrada) as UIUnitInputField, a != null ? Estado.claveAtraccion(a) : null,
						gc.park.settings.freeRideEntranceFees, atraccionesLibres);
				}
				bool tiendasLibres = !Ajustes.tiendas || gc.park.settings.freeShopProducts;
				foreach (ShopProductInfoEntry entrada in Object.FindObjectsOfType<ShopProductInfoEntry>())
				{
					ProductSettings ps = ajustesProducto?.GetValue(entrada) as ProductSettings;
					ProductShop tienda = tiendaProducto?.GetValue(entrada) as ProductShop;
					string clave = ps != null && tienda != null && ps.product != null
						? Estado.claveProducto(tienda, ps, ps.product) : null;
					decorar(precioProducto?.GetValue(entrada) as UIUnitInputField, clave,
						gc.park.settings.freeShopProducts, tiendasLibres);
				}
			}
			catch (System.Exception e)
			{
				Debug.LogWarning("[AutoPrecios] Interfaz: " + e);
				siguiente = Time.unscaledTime + 5f;
			}
		}

		private void OnDestroy()
		{
			foreach (Interruptor interruptor in Object.FindObjectsOfType<Interruptor>())
			{
				UIUnitInputField campo = interruptor.GetComponent<UIUnitInputField>();
				if (campo != null && campo.inputField != null)
				{
					Park parque = GameController.Instance != null ? GameController.Instance.park : null;
					bool gratis = parque != null && (interruptor.clave.StartsWith("A:") ? parque.settings.freeRideEntranceFees : parque.settings.freeShopProducts);
					campo.inputField.interactable = !gratis;
				}
				Object.Destroy(interruptor);
			}
		}

		// `bloqueadoPorJuego`: el escenario no deja cobrar (la casilla ya viene bloqueada).
		// `sinMod`: el mod no gestiona esta categoría; no se muestra el interruptor.
		private static void decorar(UIUnitInputField casilla, string clave, bool bloqueadoPorJuego, bool sinMod)
		{
			if (casilla == null || clave == null || casilla.inputField == null)
			{
				return;
			}
			Interruptor interruptor = casilla.GetComponent<Interruptor>();
			if (interruptor == null)
			{
				interruptor = casilla.gameObject.AddComponent<Interruptor>();
				interruptor.crear(casilla);
			}
			interruptor.clave = clave;
			interruptor.mostrar(!sinMod);
			if (sinMod)
			{
				casilla.inputField.interactable = !bloqueadoPorJuego;
				return;
			}
			bool auto = Estado.esAuto(clave);
			casilla.inputField.interactable = !auto && !bloqueadoPorJuego;
			interruptor.pintar(auto, Estado.resultado(clave));
		}

		// Botón junto a la casilla de precio. Si la fila tiene layout horizontal (productos de tienda,
		// lista de atracciones) entra en la fila justo antes de la casilla; si no (ventana de la
		// atracción), va pegado a la izquierda de la casilla y el texto de info baja una línea.
		// El precio sugerido y los rechazos salen en el tooltip del botón.
		internal class Interruptor : MonoBehaviour
		{
			public string clave;

			private GameObject boton;
			private Image fondo;
			private TextMeshProUGUI etiqueta;
			private UITooltip tooltip;
			private RectTransform infoMovida;
			private Vector2 offsetOriginal;

			public void crear(UIUnitInputField casilla)
			{
				TMP_Text modelo = casilla.inputField.textComponent;
				RectTransform rc = casilla.GetComponent<RectTransform>();
				float alto = rc != null && rc.rect.height > 0f ? rc.rect.height : 14f;
				Transform padre = casilla.transform.parent;
				bool enFila = padre != null && padre.GetComponent<HorizontalLayoutGroup>() != null;

				boton = new GameObject("AutoPrecios.Interruptor", typeof(RectTransform));
				fondo = boton.AddComponent<Image>();
				Button b = boton.AddComponent<Button>();
				b.targetGraphic = fondo;
				b.onClick.AddListener(alternar);
				tooltip = boton.AddComponent<UITooltip>();

				etiqueta = texto(boton.transform, modelo, "Etiqueta", 0.85f);
				RectTransform re = etiqueta.rectTransform;
				re.anchorMin = Vector2.zero;
				re.anchorMax = Vector2.one;
				re.offsetMin = new Vector2(2f, 0f);
				re.offsetMax = new Vector2(-2f, 0f);
				etiqueta.alignment = TextAlignmentOptions.Center;
				etiqueta.color = Color.white;
				float ancho = Mathf.Ceil(Mathf.Max(etiqueta.GetPreferredValues("Auto-pricing OFF").x, etiqueta.GetPreferredValues("Precio auto. NO").x)) + 6f;

				RectTransform rb = boton.GetComponent<RectTransform>();
				if (enFila)
				{
					boton.transform.SetParent(padre, false);
					boton.transform.SetSiblingIndex(casilla.transform.GetSiblingIndex());
					LayoutElement le = boton.AddComponent<LayoutElement>();
					le.minWidth = ancho;
					le.preferredWidth = ancho;
					le.flexibleWidth = 0f;
					le.minHeight = alto;
					le.preferredHeight = alto;
				}
				else
				{
					boton.transform.SetParent(casilla.transform, false);
					boton.AddComponent<LayoutElement>().ignoreLayout = true;
					rb.anchorMin = new Vector2(0f, 0.5f);
					rb.anchorMax = new Vector2(0f, 0.5f);
					rb.pivot = new Vector2(1f, 0.5f);
					rb.anchoredPosition = new Vector2(-3f, 0f);
					rb.sizeDelta = new Vector2(ancho, alto);
					// El texto de info de la atracción ocupa esa línea: bajarlo una línea.
					Transform info = padre != null ? padre.Find("InfoText") : null;
					RectTransform ri = info as RectTransform;
					if (ri != null)
					{
						infoMovida = ri;
						offsetOriginal = ri.offsetMax;
						ri.offsetMax = new Vector2(ri.offsetMax.x, ri.offsetMax.y - alto - 3f);
					}
				}
			}

			private void OnDestroy()
			{
				if (infoMovida != null) infoMovida.offsetMax = offsetOriginal;
				if (boton != null)
				{
					Object.Destroy(boton);
				}
			}

			private static TextMeshProUGUI texto(Transform padre, TMP_Text modelo, string nombre, float escala)
			{
				GameObject go = new GameObject(nombre, typeof(RectTransform));
				go.transform.SetParent(padre, false);
				TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
				if (modelo != null)
				{
					t.font = modelo.font;
					t.fontSize = modelo.fontSize * escala;
				}
				t.raycastTarget = false;
				t.enableWordWrapping = false;
				t.overflowMode = TextOverflowModes.Overflow;
				return t;
			}

			public void mostrar(bool visible)
			{
				if (boton != null && boton.activeSelf != visible)
				{
					boton.SetActive(visible);
				}
			}

			public void pintar(bool auto, Estado.Resultado r)
			{
				if (fondo == null)
				{
					return;
				}
				fondo.color = auto ? VERDE : GRIS;
				etiqueta.text = auto ? Texto.Elegir("Auto-pricing ON", "Precio auto. SÍ") : Texto.Elegir("Auto-pricing OFF", "Precio auto. NO");
				string ayuda;
				if (r == null || r.sugerido <= 0f)
				{
					ayuda = Texto.Elegir("Auto Pricing: not enough guest data yet.", "Precio automático: aún no hay suficientes datos de visitantes.");
				}
				else
				{
					ayuda = Texto.Elegir("Suggested price: ", "Precio sugerido: ") + r.sugerido.ToString("0.00");
					if (r.rechazanAhora >= 0f)
					{
						ayuda += Texto.Elegir("\nAt the current price, ", "\nCon el precio actual no paga el ") + Mathf.RoundToInt(r.rechazanAhora * 100f) +
							Texto.Elegir("% of interested guests would refuse.", "% de los visitantes interesados.");
					}
				}
				ayuda += auto ? Texto.Elegir("\nClick to set the price manually.", "\nClic para fijar el precio a mano.") : Texto.Elegir("\nClick to enable automatic pricing.", "\nClic para activar el precio automático.");
				tooltip.text = ayuda;
			}

			private void alternar()
			{
				if (string.IsNullOrEmpty(clave))
				{
					return;
				}
				Estado.cambiar(clave, !Estado.esAuto(clave));
				Ajustador.ajustarPronto();
			}
		}
	}
}
