using System.Reflection;
using Parkitect.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AutoPrecios
{
	// Junto a cada casilla de precio (ventana de atracción, lista de atracciones y productos de
	// tienda) pone un interruptor Auto-pricing ON/OFF solo para ese precio. El modo de todo el parque
	// (Money, Rating, Off) se elige en un panel abajo a la derecha, que se puede arrastrar y se
	// pone ámbar si el escenario pide una nota sin cumplir y estamos en Money.
	internal class Interfaz : MonoBehaviour
	{
		private const BindingFlags PRIVADO = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
		private const float CADA = 0.25f;

		private static readonly Color VERDE = new Color(0.18f, 0.55f, 0.27f, 1f);
		private static readonly Color AZUL = new Color(0.16f, 0.42f, 0.72f, 1f);
		private static readonly Color GRIS = new Color(0.36f, 0.36f, 0.40f, 1f);
		private static readonly Color AMBAR = new Color(0.85f, 0.55f, 0.1f, 1f);
		private static readonly Color PANEL = new Color(0.22f, 0.24f, 0.3f, 0.95f);
		private static readonly Color APAGADO = new Color(0.8f, 0.8f, 0.83f, 1f);
		private static readonly Color TEXTO_APAGADO = new Color(0.25f, 0.25f, 0.28f, 1f);

		private static string nombreDinero => Texto.Elegir("Money", "Dinero");
		private const string NOMBRE_RATING = "Rating";

		private static string nombreApagado => Texto.Elegir("Off", "Apagado");

		private static string nombreModo(Estado.Modo modo)
		{
			return modo == Estado.Modo.Dinero ? nombreDinero : modo == Estado.Modo.Rating ? NOMBRE_RATING : nombreApagado;
		}

		private static string explicacionModo(Estado.Modo modo)
		{
			if (modo == Estado.Modo.Dinero)
			{
				return Texto.Elegir(
					"This option prioritizes earning money. Prices go up, so guests may find the park expensive.",
					"Esta opción prioriza ganar dinero. Los precios suben, así que a los visitantes el parque les puede parecer caro.");
			}
			if (modo == Estado.Modo.Rating)
			{
				return Texto.Elegir(
					"This option prioritizes park rating over income. Prices go down to improve the guests' experience, so you earn less.",
					"Esta opción prioriza el rating del parque sobre el dinero. Los precios bajan para mejorar la experiencia de los visitantes, así que ganas menos.");
			}
			return Texto.Elegir(
				"This option turns off automatic prices in the whole park. You set every price yourself.",
				"Esta opción apaga los precios automáticos en todo el parque. Tú pones todos los precios.");
		}

		private static readonly FieldInfo feeTab = typeof(AttractionStateTab).GetField("entranceFeeInput", PRIVADO);
		private static readonly MethodInfo objetoTab = typeof(AttractionStateTab).GetMethod("getObject", PRIVADO);
		private static readonly FieldInfo feeLista = typeof(AttractionListEntry).GetField("entranceFeeInput", PRIVADO);
		private static readonly FieldInfo precioProducto = typeof(ShopProductInfoEntry).GetField("priceInput", PRIVADO);
		private static readonly FieldInfo ajustesProducto = typeof(ShopProductInfoEntry).GetField("productSettings", PRIVADO);
		private static readonly FieldInfo tiendaProducto = typeof(ShopProductInfoEntry).GetField("shop", PRIVADO);

		private static PanelParque panelParque;
		private float siguiente;

		public static PanelParque panel => panelParque;

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
				asegurarPanel();
				if (panelParque != null)
				{
					panelParque.pintar();
				}
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

		private static void asegurarPanel()
		{
			if (panelParque != null)
			{
				return;
			}
			GameObject lienzo = GameObject.Find("MenuCanvas");
			if (lienzo == null)
			{
				return;
			}
			GameObject go = new GameObject("AutoPrecios.Panel", typeof(RectTransform));
			go.transform.SetParent(lienzo.transform, false);
			panelParque = go.AddComponent<PanelParque>();
			panelParque.montar(lienzo.GetComponentInChildren<TMP_Text>());
		}

		private void OnDestroy()
		{
			if (panelParque != null)
			{
				Object.Destroy(panelParque.gameObject);
				panelParque = null;
			}
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
			casilla.inputField.interactable = !Estado.esAuto(clave) && !bloqueadoPorJuego;
			interruptor.pintar(Estado.esAuto(clave), Estado.resultado(clave));
		}

		// Interruptor ON/OFF de un solo precio. Si la fila tiene layout horizontal (productos de tienda,
		// lista de atracciones) entra en la fila justo antes de la casilla; si no (ventana de la
		// atracción), va pegado a la izquierda de la casilla. El precio sugerido y los rechazos salen
		// en el tooltip. Con el parque en Off queda gris y sin efecto: lo manda la fila global.
		internal class Interruptor : MonoBehaviour
		{
			public string clave;

			private GameObject boton;
			private Image fondo;
			private TextMeshProUGUI etiqueta;
			private UITooltip tooltip;
			private Button accion;
			private bool compacto;
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
				accion = boton.AddComponent<Button>();
				accion.targetGraphic = fondo;
				accion.onClick.AddListener(alternar);
				tooltip = boton.AddComponent<UITooltip>();

				// Sin este ajuste, al desactivarse se ve lo que hay detrás.
				ColorBlock tonos = accion.colors;
				tonos.disabledColor = new Color(0.8f, 0.8f, 0.8f, 1f);
				accion.colors = tonos;

				etiqueta = crearTexto(boton.transform, modelo, 0.85f);
				etiqueta.color = Color.white;
				// En la lista de atracciones no cabe el texto completo sin tapar el nombre.
				compacto = !enFila && padre != null && padre.GetComponent<VerticalLayoutGroup>() != null;
				float ancho = compacto
					? Mathf.Ceil(etiqueta.GetPreferredValues("OFF").x) + 6f
					: Mathf.Ceil(Mathf.Max(etiqueta.GetPreferredValues("Auto-pricing OFF").x, etiqueta.GetPreferredValues("Precio auto. NO").x)) + 6f;

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
					RectTransform ri = padre != null ? padre.Find("InfoText") as RectTransform : null;
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
				if (infoMovida != null)
				{
					infoMovida.offsetMax = offsetOriginal;
				}
				if (boton != null)
				{
					Object.Destroy(boton);
				}
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
				bool parqueApagado = Estado.modo == Estado.Modo.Apagado;
				accion.interactable = !parqueApagado;
				fondo.color = auto ? VERDE : GRIS;
				etiqueta.text = compacto ? (auto ? "ON" : "OFF") : auto ? Texto.Elegir("Auto-pricing ON", "Precio auto. SÍ") : Texto.Elegir("Auto-pricing OFF", "Precio auto. NO");
				string ayuda = (auto ? Texto.Elegir("This price is automatic.", "Este precio es automático.") : Texto.Elegir("You set this price yourself.", "Este precio lo pones tú.")) + "\n";
				if (r == null || r.sugerido <= 0f)
				{
					ayuda += Texto.Elegir("Not enough guest data yet.", "Aún no hay suficientes datos de visitantes.");
				}
				else
				{
					ayuda += Texto.Elegir("Suggested price: ", "Precio sugerido: ") + r.sugerido.ToString("0.00");
					if (r.rechazanAhora >= 0f)
					{
						ayuda += Texto.Elegir("\nAt the current price, ", "\nCon el precio actual no paga el ") + Mathf.RoundToInt(r.rechazanAhora * 100f) +
							Texto.Elegir("% of interested guests would refuse.", "% de los visitantes interesados.");
					}
				}
				if (parqueApagado)
				{
					ayuda += Texto.Elegir("\nAuto-pricing is off for the whole park. Choose Money or Rating in the Auto-pricing panel.", "\nEl precio automático está apagado en todo el parque. Elige Dinero o Rating en el panel Auto-pricing.");
				}
				else
				{
					ayuda += auto ? Texto.Elegir("\nClick to set only this price by hand.", "\nClic para poner solo este precio a mano.") : Texto.Elegir("\nClick to make this price automatic.", "\nClic para que este precio sea automático.");
				}
				tooltip.text = ayuda;
			}

			public void alternar()
			{
				if (string.IsNullOrEmpty(clave) || Estado.modo == Estado.Modo.Apagado)
				{
					return;
				}
				Estado.cambiar(clave, !Estado.esAuto(clave));
				Ajustador.ajustarPronto();
			}
		}

		// Panel «Auto-pricing: Money | Rating | Off» para todo el parque. Empieza abajo a la derecha,
		// bajo el botón de notificaciones, y se arrastra con el ratón; la posición se recuerda.
		internal class PanelParque : MonoBehaviour, IDragHandler, IEndDragHandler
		{
			private const float ALTO = 22f;
			private const float TAMANO_TEXTO = 11f;
			private const float RELLENO = 10f;
			private const float SEPARACION = 2f;
			private static readonly Vector2 POSICION_INICIAL = new Vector2(-8f, 5f);
			private const string CLAVE_X = "autoprecios.panelX";
			private const string CLAVE_Y = "autoprecios.panelY";

			private static readonly Estado.Modo[] MODOS = { Estado.Modo.Dinero, Estado.Modo.Rating, Estado.Modo.Apagado };

			private readonly Image[] fondos = new Image[MODOS.Length];
			private readonly TextMeshProUGUI[] etiquetas = new TextMeshProUGUI[MODOS.Length];
			private readonly UITooltip[] tooltips = new UITooltip[MODOS.Length];
			private Image fondoPanel;
			private UITooltip tooltipTitulo;
			private RectTransform rect;

			public void montar(TMP_Text modelo)
			{
				rect = GetComponent<RectTransform>();
				fondoPanel = gameObject.AddComponent<Image>();
				tooltipTitulo = gameObject.AddComponent<UITooltip>();

				float x = RELLENO / 2f;
				TextMeshProUGUI titulo = crearTexto(transform, modelo, 1f);
				titulo.fontSize = TAMANO_TEXTO;
				titulo.color = Color.white;
				titulo.text = "Auto-pricing";
				float anchoTitulo = Mathf.Ceil(titulo.GetPreferredValues("Auto-pricing").x) + RELLENO;
				colocar(titulo.rectTransform, x, anchoTitulo, 0f);
				x += anchoTitulo;
				for (int i = 0; i < MODOS.Length; i++)
				{
					x += crearOpcion(i, modelo, x) + SEPARACION;
				}
				float ancho = x - SEPARACION + RELLENO / 2f;

				rect.anchorMin = new Vector2(1f, 0f);
				rect.anchorMax = new Vector2(1f, 0f);
				rect.pivot = new Vector2(1f, 0f);
				rect.sizeDelta = new Vector2(ancho, ALTO);
				rect.anchoredPosition = new Vector2(PlayerPrefs.GetFloat(CLAVE_X, POSICION_INICIAL.x), PlayerPrefs.GetFloat(CLAVE_Y, POSICION_INICIAL.y));
				mantenerEnPantalla();
			}

			private float crearOpcion(int indice, TMP_Text modelo, float x)
			{
				GameObject go = new GameObject("Opcion" + indice, typeof(RectTransform));
				go.transform.SetParent(transform, false);
				fondos[indice] = go.AddComponent<Image>();
				Button b = go.AddComponent<Button>();
				b.targetGraphic = fondos[indice];
				Navigation sinNavegacion = new Navigation();
				sinNavegacion.mode = Navigation.Mode.None;
				b.navigation = sinNavegacion;
				b.onClick.AddListener(() => elegir(MODOS[indice]));
				tooltips[indice] = go.AddComponent<UITooltip>();
				TextMeshProUGUI t = crearTexto(go.transform, modelo, 1f);
				t.fontSize = TAMANO_TEXTO;
				string nombre = nombreModo(MODOS[indice]);
				t.text = nombre;
				etiquetas[indice] = t;
				float ancho = Mathf.Ceil(t.GetPreferredValues(nombre).x) + RELLENO;
				colocar(go.GetComponent<RectTransform>(), x, ancho, 3f);
				return ancho;
			}

			private static void colocar(RectTransform r, float x, float ancho, float margenVertical)
			{
				r.anchorMin = new Vector2(0f, 0f);
				r.anchorMax = new Vector2(0f, 1f);
				r.pivot = new Vector2(0f, 0.5f);
				r.offsetMin = new Vector2(x, margenVertical);
				r.offsetMax = new Vector2(x + ancho, -margenVertical);
			}

			public void OnDrag(PointerEventData datos)
			{
				Canvas lienzo = GetComponentInParent<Canvas>();
				float escala = lienzo != null && lienzo.scaleFactor > 0f ? lienzo.scaleFactor : 1f;
				rect.anchoredPosition += datos.delta / escala;
				mantenerEnPantalla();
			}

			public void OnEndDrag(PointerEventData datos)
			{
				PlayerPrefs.SetFloat(CLAVE_X, rect.anchoredPosition.x);
				PlayerPrefs.SetFloat(CLAVE_Y, rect.anchoredPosition.y);
				PlayerPrefs.Save();
			}

			private void mantenerEnPantalla()
			{
				RectTransform padre = rect.parent as RectTransform;
				if (padre == null)
				{
					return;
				}
				Vector2 p = rect.anchoredPosition;
				p.x = Mathf.Clamp(p.x, rect.sizeDelta.x - padre.rect.width, 0f);
				p.y = Mathf.Clamp(p.y, 0f, padre.rect.height - rect.sizeDelta.y);
				rect.anchoredPosition = p;
			}

			// Solo para las capturas del modo de prueba.
			public void mostrarAyuda(int opcion)
			{
				tooltips[opcion].OnPointerEnter(new PointerEventData(EventSystem.current));
			}

			public void elegir(Estado.Modo modo)
			{
				Estado.cambiarModo(modo);
				Ajustador.ajustarPronto();
				pintar();
			}

			public void pintar()
			{
				if (fondoPanel == null)
				{
					return;
				}
				Estado.Modo actual = Estado.modo;
				string meta = actual == Estado.Modo.Dinero ? Objetivos.metaSinCumplir : null;
				fondoPanel.color = meta != null ? AMBAR : PANEL;
				Color[] colores = { VERDE, AZUL, GRIS };
				for (int i = 0; i < MODOS.Length; i++)
				{
					bool activo = MODOS[i] == actual;
					fondos[i].color = activo ? colores[i] : APAGADO;
					etiquetas[i].color = activo ? Color.white : TEXTO_APAGADO;
					tooltips[i].text = explicacionModo(MODOS[i]);
				}
				string ayuda = Texto.Elegir("Choose what automatic prices prioritize in the whole park. Drag this panel to move it.",
					"Elige qué priorizan los precios automáticos en todo el parque. Arrastra este panel para moverlo.");
				if (meta != null)
				{
					ayuda += Texto.Elegir("\nThis scenario still needs: ", "\nEste escenario todavía pide: ") + meta +
						Texto.Elegir("\nRating can help.", "\nRating puede ayudar.");
				}
				tooltipTitulo.text = ayuda;
			}
		}

		private static TextMeshProUGUI crearTexto(Transform padre, TMP_Text modelo, float escala)
		{
			GameObject go = new GameObject("Etiqueta", typeof(RectTransform));
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
			t.alignment = TextAlignmentOptions.Center;
			RectTransform r = t.rectTransform;
			r.anchorMin = Vector2.zero;
			r.anchorMax = Vector2.one;
			r.offsetMin = Vector2.zero;
			r.offsetMax = Vector2.zero;
			return t;
		}
	}
}
