using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AutoPrecios
{
	// Qué atracciones y productos tienen el auto-precio apagado (por defecto todo encendido)
	// y el último resultado del cálculo de cada uno, para mostrarlo junto al precio.
	// Se guarda por parque en la carpeta del mod: estado-<guid del parque>.txt.
	internal static class Estado
	{
		internal class Resultado
		{
			public float sugerido;
			// Parte de los visitantes interesados que no pagarían el precio actual (-1 = sin datos).
			public float rechazanAhora = -1f;
		}

		private static string parque;
		private static readonly HashSet<string> apagados = new HashSet<string>();
		private static readonly Dictionary<string, Resultado> resultados = new Dictionary<string, Resultado>();

		public static string claveAtraccion(Attraction a)
		{
			return "A:" + a.objectID;
		}

		// Los productos con precio sincronizado comparten precio en todo el parque: un único interruptor.
		public static string claveProducto(ProductShop tienda, ProductSettings ps, Product producto)
		{
			if (ps.synchronizedPrice)
			{
				return "P:" + producto.getReferenceName();
			}
			return "S:" + tienda.objectID + ":" + producto.getReferenceName();
		}

		public static bool esAuto(string clave)
		{
			comprobarParque();
			return !apagados.Contains(clave);
		}

		public static void cambiar(string clave, bool auto)
		{
			comprobarParque();
			if (auto ? apagados.Remove(clave) : apagados.Add(clave))
			{
				guardar();
			}
		}

		public static void anotar(string clave, float sugerido, float rechazanAhora)
		{
			Resultado r;
			if (!resultados.TryGetValue(clave, out r))
			{
				r = new Resultado();
				resultados[clave] = r;
			}
			r.sugerido = sugerido;
			r.rechazanAhora = rechazanAhora;
		}

		public static Resultado resultado(string clave)
		{
			Resultado r;
			return resultados.TryGetValue(clave, out r) ? r : null;
		}

		// Al cambiar de parque se recargan los interruptores de ese parque.
		private static void comprobarParque()
		{
			Park park = GameController.Instance != null ? GameController.Instance.park : null;
			string guid = park != null ? park.guid : null;
			if (guid == parque)
			{
				return;
			}
			parque = guid;
			apagados.Clear();
			resultados.Clear();
			string archivo = ruta();
			if (archivo != null && File.Exists(archivo))
			{
				try
				{
					foreach (string linea in File.ReadAllLines(archivo))
					{
						if (linea.Trim().Length > 0) apagados.Add(linea.Trim());
					}
				}
				catch (System.Exception e)
				{
					Debug.LogWarning("[AutoPrecios] Could not read saved state: " + e.Message);
				}
			}
		}

		private static void guardar()
		{
			string archivo = ruta();
			if (archivo == null)
			{
				return;
			}
			try
			{
				File.WriteAllLines(archivo, new List<string>(apagados).ToArray());
			}
			catch (System.Exception e)
			{
				Debug.LogWarning("[AutoPrecios] No se pudo guardar el estado: " + e.Message);
			}
		}

		private static string ruta()
		{
			if (string.IsNullOrEmpty(parque) || Prueba.carpeta == null)
			{
				return null;
			}
			return System.IO.Path.Combine(Prueba.carpeta, "estado-" + parque + ".txt");
		}
	}
}
