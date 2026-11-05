using System.Collections.Generic;
using UnityEngine;

namespace AutoPrecios
{
	// Modo Rating. El precio pesa en la nota: entra directo en la nota de precios y, en cada
	// atracción, un visitante solo piensa «¡fue genial!» si su nota supera 0.75, y la nota incluye
	// la satisfacción con el precio. En este modo el nivel empieza en 0.75 (en las pruebas, buena
	// nota sin dejar de ganar dinero). Si el escenario tiene una meta de nota pendiente, baja poco
	// a poco hasta cumplirla y sube otra vez cuando sobra margen, sin pasar del nivel de los ajustes.
	internal static class Objetivos
	{
		private const float NIVEL_MINIMO = 0.5f;
		private const float NIVEL_INICIAL = 0.75f;
		// Las notas son medias lentas: entre pasos hay que dar tiempo a que reaccionen.
		private const int SEGUNDOS_ENTRE_PASOS = 300;
		private const float MARGEN = 0.02f;
		private const float HOLGURA_PARA_SUBIR = 0.06f;
		private const float DEFICIT_GRANDE = 0.1f;
		private const float PASO = 0.05f;
		private const float PASO_GRANDE = 0.15f;

		private static string parque;
		private static float nivel = -1f;
		private static int siguientePaso;

		// Meta de nota del escenario que aún no se cumple (para avisar en modo Money); null si no hay.
		public static string metaSinCumplir { get; private set; }

		// Nivel de precio que hay que usar ahora mismo.
		public static float nivelEfectivo(Park park)
		{
			float elegido = Ajustes.nivel;
			float deficit;
			float holgura;
			bool hayMeta = medirMetas(park, out deficit, out holgura);
			if (Estado.modo != Estado.Modo.Rating)
			{
				nivel = -1f;
				return elegido;
			}
			float inicial = Mathf.Min(elegido, NIVEL_INICIAL);
			if (!hayMeta)
			{
				nivel = -1f;
				return inicial;
			}
			if (park.guid != parque || nivel < 0f)
			{
				parque = park.guid;
				nivel = inicial;
				siguientePaso = ParkInfo.ParkTime + SEGUNDOS_ENTRE_PASOS;
			}
			else if (ParkInfo.ParkTime >= siguientePaso)
			{
				siguientePaso = ParkInfo.ParkTime + SEGUNDOS_ENTRE_PASOS;
				if (deficit > 0f)
				{
					nivel -= deficit > DEFICIT_GRANDE ? PASO_GRANDE : PASO;
				}
				else if (holgura > HOLGURA_PARA_SUBIR)
				{
					nivel += PASO;
				}
			}
			nivel = Mathf.Clamp(nivel, NIVEL_MINIMO, Mathf.Max(elegido, NIVEL_MINIMO));
			return nivel;
		}

		// true si hay metas de nota sin ganar. `deficit`: lo que le falta a la peor (con margen);
		// `holgura`: lo que le sobra a la más justa.
		private static bool medirMetas(Park park, out float deficit, out float holgura)
		{
			deficit = 0f;
			holgura = float.MaxValue;
			metaSinCumplir = null;
			bool hay = false;
			if (park.scenario == null || park.scenario.goals == null || !park.settings.goalsEnabled)
			{
				return false;
			}
			IList<IScenarioGoal> metas = park.scenario.goals.getGoals();
			foreach (IScenarioGoal meta in metas)
			{
				float nota;
				PercentageScenarioGoal porcentaje = meta as PercentageScenarioGoal;
				if (porcentaje == null || meta.hasBeenWon || meta.hasReceivedRewards || !notaDe(park, meta, out nota))
				{
					continue;
				}
				hay = true;
				deficit = Mathf.Max(deficit, porcentaje.value + MARGEN - nota);
				holgura = Mathf.Min(holgura, nota - porcentaje.value);
				if (nota < porcentaje.value && metaSinCumplir == null)
				{
					metaSinCumplir = meta.description + " (" + meta.progressText + ")";
				}
			}
			return hay;
		}

		private static bool notaDe(Park park, IScenarioGoal meta, out float nota)
		{
			ParkInfo info = park.parkInfo;
			nota = 0f;
			if (meta is ParkOverallRatingGoal)
			{
				nota = info.getOverallRating();
			}
			else if (meta is ParkPricesGoal)
			{
				nota = info.RatingPriceSatisfaction;
			}
			else if (meta is ParkExperiencesGoal)
			{
				nota = info.RatingExperiences;
			}
			else if (meta is ParkHappinessGoal)
			{
				nota = info.RatingHappiness;
			}
			else
			{
				return false;
			}
			return true;
		}
	}
}
