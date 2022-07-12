using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Orchard
{

    public class TreeManager : MonoBehaviour
    {
        [Tooltip("Max number of fruits on a tree.")]
        [SerializeField] private int maxNumFruits = 5;
        
        [Tooltip("Min time between fruit spawning.")]
        [SerializeField] private float minTimeBetweenSpawn = 1.0f;
        
        [Tooltip("Min time between fruit spawning.")]
        [SerializeField] private float maxTimeBetweenSpawn = 3.0f;
        
        [Tooltip("Ref to the Fruit prefab.")]
        [SerializeField] private GameObject fruitPrefab;
        
        [FormerlySerializedAs("treeCrownCenter")]
        [Tooltip("Ref to the center of the region where we want to spawn fruits.")]
        [SerializeField] private Transform fruitSpawnCenter;
        
        [Tooltip("Radius of the tree crown for fruit spawning.")]
        [SerializeField] private float treeCrownRadius;
        
        [Tooltip("Ref to the transform of the tree crown sprite.")]
        [SerializeField] private Transform treeCrownTransform;
        
        [Tooltip("Ref to the transform of the floor in the scene.")]
        [SerializeField] private Transform floorTransform;
        
        [Tooltip("Extent of reaching motion after fruits picked from a tree.")]
        [SerializeField] private float treeCrownReachScale;
        
        [Tooltip("Ref to shake leaves particle system.")]
        [SerializeField] private ParticleSystem leavesParticleSystem;
        
        [Tooltip("Ref to image of the fruit.")]
        [SerializeField] private Sprite fruitSprite;
        
        [Tooltip("Color of splashing when fruit falls.")]
        [SerializeField] private Color fruitSplashColor;
        
        
        
        // stores timer since the last spawn
        private float fruitTimer = 0.0f;
        // stored the time for the next spawn
        private float nextFruitTime = 0;
        // list of all fruits that belong to the tree
        private List<Fruit> fruits = new List<Fruit>();
        // stores the initial position of the crown
        private Vector3 crownSpawnPosition;
        // state of the tree, can be either resting or not
        private bool resting = true;
        // offset of the tree crown position with respect to the mouse click
        // when clicked on fruit
        private Vector3 reachOffset;
        
        // Start is called before the first frame update
        /// <summary>
        /// Starts timer and keeps tree crown position
        /// </summary>
        void Start()
        {
            // when to spawn a fruit for the first time 
            nextFruitTime = Random.Range(1f, 3f);
            // keep the position of the crown
            crownSpawnPosition = treeCrownTransform.position;
        }

        // Update is called once per frame
        /// <summary>
        /// Spawns fruits when necessary
        /// Resets Timer
        /// </summary>
        void Update()
        {
            // check the number of fruits on the tree
            if (fruits.Count < maxNumFruits)
            {
                // check if it is time to spawn a fruit
                if (fruitTimer > nextFruitTime)
                {
                    // spawn a fruit                    
                    SpawnFruit();
                    
                    // reset the timer for the next spawn
                    fruitTimer = 0;
                    
                    // set the next time for spawning to be random
                    nextFruitTime = Random.Range(minTimeBetweenSpawn, maxTimeBetweenSpawn);
                }

                // accumulate the timer
                fruitTimer += Time.deltaTime;

            }

        }

        /// <summary>
        /// Spawns a new fruit:
        /// Instantiates a fruit prefab
        /// Keeps ref to its "Fruit" component
        /// Updates fruit's tree to be this
        /// Passes floor to fruit
        /// Finds random position for the fruit on the tree
        /// Adds fruit to its list of fruits
        /// Asks fruit to grow
        /// 
        /// </summary>
        
        private void SpawnFruit()
        {
            
            GameObject newFruitGameObject = Instantiate(fruitPrefab, this.transform);
            Fruit newFruit = newFruitGameObject.GetComponent<Fruit>();
            newFruit.MyTree = this;
            newFruit.floorTransform = this.floorTransform;
            var randomPos = RandomPointInCrown(fruitSpawnCenter.position);
            newFruit.transform.position = randomPos;
            fruits.Add(newFruit);
            // grow in size
            newFruit.Grow(fruitSprite, fruitSplashColor);
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

                if (fruits.Count == 0) 
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

