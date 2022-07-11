using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Orchard
{

    public class TreeManager : MonoBehaviour
    {
        [SerializeField] private int maxNumFruits = 5;
        [SerializeField] private float minTimeBetweenSpawn = 1.0f;
        [SerializeField] private float maxTimeBetweenSpawn = 3.0f;
        [SerializeField] private GameObject fruitPrefab;
        [SerializeField] private Transform treeCrownCenter;
        [SerializeField] private float treeCrownRadius;
        [SerializeField] private Transform treeCrownTransform;
        [SerializeField] private Transform floorTransform;
        [SerializeField] private float treeCrownReachScale;
        [SerializeField] private ParticleSystem leavesParticleSystem;
        
        private float fruitTimer = 0.0f;
        private float nextFruitTime = 0;
        private List<Fruit> fruits = new List<Fruit>();
        private Vector3 crownSpawnPosition;

        private bool resting = true;

        private Vector3 reachOffset;
        // Start is called before the first frame update
        void Start()
        {
            nextFruitTime = Random.Range(1f, 3f);
            crownSpawnPosition = treeCrownTransform.position;
        }

        // Update is called once per frame
        void Update()
        {
            if (fruits.Count < maxNumFruits)
            {
                if (fruitTimer > nextFruitTime)
                {
                    
                    Shake();
                    SpawnFruit();
                    fruitTimer = 0;
                    nextFruitTime = Random.Range(minTimeBetweenSpawn, maxTimeBetweenSpawn);
                }

                fruitTimer += Time.deltaTime;

            }

        }

        
        private void SpawnFruit()
        {
            
            GameObject newFruitGameObject = Instantiate(fruitPrefab, this.transform);
            Fruit newFruit = newFruitGameObject.GetComponent<Fruit>();
            newFruit.MyTree = this;
            newFruit.floorTransform = this.floorTransform;
            var randomPos = RandomPointInCrown(treeCrownCenter.position);
            newFruit.transform.position = randomPos;
            fruits.Add(newFruit);
            // grow in size
            newFruit.Grow();
        }
        
        
        private void Shake()
        {
            // sounds for shake
        }

        public void Reach(Vector3 target)
        {
            if (resting)
            {
                reachOffset = target - crownSpawnPosition;
                resting = false;
            }
            treeCrownTransform.position = Vector3.Lerp(crownSpawnPosition, target - reachOffset, treeCrownReachScale);
        }

        public void Unreach(bool detachHappened)
        {
            treeCrownTransform.DOMove(crownSpawnPosition, 0.2f);
            resting = true;
            if (detachHappened)
            {
                leavesParticleSystem.Play();
            }
        }
        
        // Find a random spot for a new fruit but also make sure it doesn't overlap with existing fruits 
        public Vector3 RandomPointInCrown(Vector3 crownPosition)
        {
            int attempts = 0;
            Vector3 p = Vector3.zero;
            while (attempts<10)
            {
                attempts++;
                var p2 = Random.insideUnitCircle * treeCrownRadius * transform.localScale.x;
                p = new Vector3(p2.x,p2.y,0) + crownPosition;

                //if (fruits.Count == 0) 
                    return p;
                
                var closest = FindClosestFruit(p);
                if (Vector3.Distance(p, closest.position) > 1)
                    return p;
            }

            // This is very unlikely to happen but if we got here just return the last position
            return p;
        }

        public Transform FindClosestFruit(Vector3 p)
        {
            float minD = float.MaxValue;
            Transform t = fruits[0].transform;
            foreach (var fruit in fruits)
            {
                float d = Vector3.Distance(fruit.transform.position, p);
                if (d<minD)
                {
                    minD = d;
                    t = fruit.transform;
                }
            }
            
            return t;
        }

        public void Dispawn(Fruit currFruit)
        {
            fruits.Remove(currFruit);
        }
    }

}

