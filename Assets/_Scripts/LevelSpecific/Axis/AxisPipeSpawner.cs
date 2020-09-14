using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LevelSpecific.Axis {
	public class AxisPipeSpawner : MonoBehaviour {
		AxisPipe smallPipePrefab;
		AxisPipe largePipePrefab;

		const float lengthOfAxis_c = 448;
		public Vector3 smallPipeSpawnPosition = Vector3.zero;
		public Vector3 largePipeSpawnPosition = new Vector3(0, 0, lengthOfAxis_c);

		float smallPipeSpawnTimeSeconds = 5;
		float largePipeSpawnTimeSeconds = 15;

		// Use this for initialization
		void Start() {
			string pathToPrefabs = "Prefabs/LevelSpecific/_Axis/";
			smallPipePrefab = Resources.Load<AxisPipe>(pathToPrefabs + "PipeSmall");
			largePipePrefab = Resources.Load<AxisPipe>(pathToPrefabs + "PipeLarge");

			StartCoroutine(SpawnSmallPipesCoroutine());
			StartCoroutine(SpawnLargePipesCoroutine());
		}

		IEnumerator SpawnSmallPipesCoroutine() {
			while (true) {
				SpawnSmallPipe();
				yield return new WaitForSeconds(smallPipeSpawnTimeSeconds);
			}
		}

		IEnumerator SpawnLargePipesCoroutine() {
			while (true) {
				SpawnLargePipe();
				yield return new WaitForSeconds(largePipeSpawnTimeSeconds);
			}
		}

		public void SpawnSmallPipe() {
			AxisPipe newPipe = Instantiate(smallPipePrefab, transform);
			newPipe.distanceToTravelBeforeDespawn = lengthOfAxis_c;
			newPipe.transform.localPosition = smallPipeSpawnPosition;
		}

		public void SpawnLargePipe() {
			AxisPipe newPipe = Instantiate(largePipePrefab, transform);
			newPipe.distanceToTravelBeforeDespawn = lengthOfAxis_c;
			newPipe.transform.localPosition = largePipeSpawnPosition;
		}
	}
}