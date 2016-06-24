
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Holoville.HOTween;
using Holoville.HOTween.Path;

public class ObstacleGeneration : MonoBehaviour {

    //Used for editing within the Inspector
    public GameObject obstacleClump;
    public float obstacleSize = 3.0f;
    public int jemIndexPosition = -1;
    public float[] gateDistanceBuffers = new float[9];
    public float[] gatePositions = new float[0];
    public int[] clumpsBetweenGates = new int[9];
    public ObstacleClump.DifficultyMode[] gateSectionDifficulty = new ObstacleClump.DifficultyMode[9];

    private int pooledAmount = 0;
    private Tweener runTween;
    private float laneWidth = 3.5f;
    private float percentDistanceOne = 0;

    private ObstacleClump.PatternType currentPatternType;
    private ObstacleClump.PatternType previousPatternType;
    private ObstacleClump.ObstacleType currentObstacleType;
    private ObstacleClump.ObstacleType previousObstacleType;

#if UNITY_EDITOR
	void OnGUI ()
	{
		if(GUI.Button(new Rect(100, 5, 100, 30), "Re-Generate"))
		{
			PooledObjects.objectPool.ResetPooledObjects();
			DetermineClumpSettings();
			PositionObstacles();
		}
	}
#endif

    void Start() {
        StartCoroutine(Initialize());
    }

    IEnumerator Initialize() {
        laneWidth = GameObject.Find("PathLanes").GetComponent<PathLaneSettings>().pathNodeDistance;
        for (int index = 0; index < clumpsBetweenGates.Length; index++)
            pooledAmount += clumpsBetweenGates[index];
        PooledObjects.objectPool.PopulateGameObjectPool("clump", obstacleClump, pooledAmount);

        yield return new WaitForSeconds(0.25f);

        runTween = GameObject.Find("PathController").GetComponent<PathController>().runTween;
        percentDistanceOne = Vector3.Distance(runTween.GetPointOnPath(0.01f), runTween.GetPointOnPath(0.0f));
        DetermineClumpSettings();
        PositionObstacles();
    }

    public void RegenerateObstacles() {
        PooledObjects.objectPool.ResetPooledObjects();
        DetermineClumpSettings();
        PositionObstacles();
    }

    private void DetermineClumpSettings() {
        int obstacleIndex = 0;
        int betweenGateIndex = 0;
        int pooledObjectIndex = 0;

        do {
            jemIndexPosition = Random.Range(pooledAmount / 8, pooledAmount - 1);
        } while (jemIndexPosition >= pooledAmount);
        //	jemIndexPosition = pooledAmount - 1;

        while (pooledObjectIndex < pooledAmount) {
            while (obstacleIndex < clumpsBetweenGates[betweenGateIndex]) {
                RandomizeClumpSettings(pooledObjectIndex, betweenGateIndex);
                if (pooledObjectIndex == jemIndexPosition) {
                    ObstacleClump obstacleClumpScript = PooledObjects.objectPool.GetObject("clump", pooledObjectIndex).GetComponent<ObstacleClump>();
                    obstacleClumpScript.SetJemClumpType();
                }
                pooledObjectIndex++;
                obstacleIndex++;
            }
            if (obstacleIndex == clumpsBetweenGates[betweenGateIndex]) {
                betweenGateIndex++;
                obstacleIndex = 0;
            }
        }
    }

    //	Will stay the same for the most part objects will still be positioned in this fashion
    private void PositionObstacles() {
        int obstacleIndex = 0; //Index of the current obstacle with a section between gates
        int betweenGateIndex = 0; //Index of the current section between gates
        int pooledObjectIndex = 0; //Index to access a specific obstacle in pooledObjects

        float pathPercent = 0; //current percent of the generator on the path 
        float sectionSize = 0; //The percent size of the area between two gates
        float obstacleOffset = 0; //The offest between each obstacle relative to the sectionSize


        while (betweenGateIndex < clumpsBetweenGates.Length) {
            sectionSize = CalculateSectionSize(betweenGateIndex);

            while (obstacleIndex < clumpsBetweenGates[betweenGateIndex]) {
                obstacleOffset = CalculateObstacleOffset(sectionSize, betweenGateIndex, obstacleIndex);
                pathPercent += obstacleOffset;

                ActivateObstacle(pooledObjectIndex, betweenGateIndex, pathPercent);

                pooledObjectIndex++;
                obstacleIndex++;
            }
            if (obstacleIndex == clumpsBetweenGates[betweenGateIndex]) {
                float percentDistanceBuffer = ((gateDistanceBuffers[betweenGateIndex] / 2) / percentDistanceOne) / 100;
                betweenGateIndex++;
                pathPercent = gatePositions[betweenGateIndex] + percentDistanceBuffer;
                obstacleIndex = 0;
            }
        }
    }

    //	Calculates the percent size of area between two gates
    float CalculateSectionSize(int gateIndex) {
        float calculatedSectionSize = 0;
        float gateBufferPercent = ((gateDistanceBuffers[gateIndex] * 1.5f) / percentDistanceOne) / 100;

        calculatedSectionSize = gatePositions[gateIndex + 1] - gatePositions[gateIndex] - gateBufferPercent;
        if (calculatedSectionSize < 0)
            calculatedSectionSize = 0;
        return calculatedSectionSize;
    }

    float CalculateObstacleOffset(float sectionSize, int betweenGateIndex, int obstacleIndex) {
        float obstacleOffset = 0.0f;

        if (sectionSize == 0)
            return obstacleOffset;

        float obstaclePercentSize = obstacleSize / (percentDistanceOne * 100);
        float maxObstacleOffset = (sectionSize / clumpsBetweenGates[betweenGateIndex]) - obstaclePercentSize;
        obstacleOffset = obstaclePercentSize + Random.Range(maxObstacleOffset * 0.75f, maxObstacleOffset);

        return obstacleOffset;
    }

    //	This is where the obstacle clump's pattern type and obstacle type will be randomized
    //	Then the obstacle clump will be instantiated with the models it needs.
    //	All models that are childed to the obstacle clump will be pooled based on their type

    void RandomizeClumpSettings(int pooledObjectIndex, int betweenGateindex) {
        ObstacleClump obstacleClumpScript;

        //		randomize the clump pattern and obstacle type
        obstacleClumpScript = PooledObjects.objectPool.GetObject("clump", pooledObjectIndex).GetComponent<ObstacleClump>();
        obstacleClumpScript.currentDifficulty = gateSectionDifficulty[betweenGateindex];

        do {
            currentPatternType = obstacleClumpScript.RandomizePattern();
        } while (currentPatternType == previousPatternType);

        do {
            currentObstacleType = obstacleClumpScript.RandomizeObstacleType();
        } while (currentObstacleType == previousObstacleType);

        previousPatternType = currentPatternType;
        previousObstacleType = currentObstacleType;
    }

    void ActivateObstacle(int pooledObjectIndex, int betweenGateIndex, float pathPercent) {
        ObstacleClump obstacleClumpScript;

        //obstacleClumpScript.currentDifficulty = gateSectionDifficulty[betweenGateIndex];
        PooledObjects.objectPool.GetObject("clump", pooledObjectIndex).SetActive(true);
        PooledObjects.objectPool.GetObject("clump", pooledObjectIndex).transform.position = runTween.GetPointOnPath(pathPercent);
        obstacleClumpScript = PooledObjects.objectPool.GetObject("clump", pooledObjectIndex).GetComponent<ObstacleClump>();
        obstacleClumpScript.BuildClump(laneWidth, runTween.GetPointOnPath(pathPercent + 0.001f));
    }
}
