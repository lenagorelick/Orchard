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

    public class TreeManager : MonoBehaviour, FruitContainer
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
                Debug.Log("Not enough fruits on tree ");
                // check if it is time to spawn a fruit
                if (fruitTimer > nextFruitTime)
                {
                    // spawn a fruit                    
                    ((FruitContainer)this).SpawnFruit();
                    
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
        /// Updates fruit's tree ref
        /// Passes floor to fruit
        /// Finds random position for the fruit on the tree
        /// Adds fruit to its list of fruits
        /// Asks fruit to grow
        /// 
        /// </summary>
        public void SpawnFruit()
        {
            
            // create prefab
            GameObject newFruitGameObject = Instantiate(fruitPrefab, this.transform);
            // get fruit component
            Fruit newFruit = newFruitGameObject.GetComponent<Fruit>();
            // updates fruit's tree
            newFruit.myContainer = this;
            // updates fruits floor
            newFruit.floorTransform = this.floorTransform;
            // gets random spawn position on the crown
            var randomPos = RandomPointInCrown(fruitSpawnCenter.position);
            newFruit.transform.position = randomPos;
            // keeps the fruit in the list of fruits
            fruits.Add(newFruit);
            // grow fruit
            newFruit.Grow(fruitSprite, fruitSplashColor);
        }



        /// <summary>
       /// The tree reaches after a fruit that is being pulled by a mouse 
       /// </summary>
       /// <param name="target"></param>
        void FruitContainer.Reach(Vector3 target)
        {
            // if the tree was resting - switch off resting
            if (resting)
            {
                // crown location offset with respect to the mouse
                // when started reaching
                reachOffset = target - crownSpawnPosition;
                resting = false;
            }
            // interpolate the crown location towards the offset target using the reach scale property
            treeCrownTransform.position = Vector3.Lerp(crownSpawnPosition, target - reachOffset, treeCrownReachScale);
        }

       /// <summary>
       /// Stop reaching:
       /// return to resting state
       /// move to initial spawn location
       /// if detached - play leaves particle system
       /// </summary>
       /// <param name="detachHappened"></param>
        void FruitContainer.Unreach(bool detachHappened)
        {
            
            // return to initial spawn location of the tree crown
            treeCrownTransform.DOMove(crownSpawnPosition, 0.2f);
            // switch to resting
            resting = true;
            // if fruit detached
            if (detachHappened)
            {
                // play leaves particle system
                leavesParticleSystem.Play();
            }
            
        }
        
        
        /// <summary>
        /// Find a random spot for a new fruit but also make sure it doesn't overlap with existing fruits
        /// </summary>
        /// <param name="crownPosition"></param>
        /// <returns></returns>
        public Vector3 RandomPointInCrown(Vector3 crownPosition)
        {
            // how many attempts are allowed until the right location is found
            int attempts = 0;
            Vector3 randomPointInCrown = Vector3.zero;
            
            // while there are still attempts
            while (attempts < 10)
            {
                attempts++;
                // get random location in a unit circle scaled by radius of the crown and local scale of the tree
                var p2 = Random.insideUnitCircle * treeCrownRadius * transform.localScale.x;
                randomPointInCrown = new Vector3(p2.x,p2.y,0) + crownPosition;

                // if not fruits on the tree - no need to check the position
                if (fruits.Count == 0) 
                    return randomPointInCrown;
                
                // check the distance to the closest fruit on the tree
                var closestFruitTransform = FindClosestFruit(randomPointInCrown);
                
                // if it is far enough
                if (Vector3.Distance(randomPointInCrown, closestFruitTransform.position) > 1)
                    return randomPointInCrown;
            }

            // This is very unlikely to happen but if we got here just return the last position
            // there will be an overlap
            return randomPointInCrown;
        }

        /// <summary>
        /// Find the closest fruit to a given position
        /// Linear search over the fruits in the list
        /// Assumes the list is non-empty
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public Transform FindClosestFruit(Vector3 fruitPosition)
        {
            // if there are not fruits throw exception
            if (fruits.Count == 0)
            {
                throw new Exception("No fruits on the tree");
            }
            
            // min distance so far
            float minD = float.MaxValue;
            Transform closestFruitTransform = fruits[0].transform;
            
            // go over the fruits
            foreach (var fruit in fruits)
            {
                // calc distance to given fruit position
                float d = Vector3.Distance(fruit.transform.position, fruitPosition);
                
                // check if that is the closest dist
                if (d<minD)
                {
                    // keep the dist and the transform
                    minD = d;
                    closestFruitTransform = fruit.transform;
                }
            }
            
            return closestFruitTransform;
        }

        /// <summary>
        /// Remove the fruit from the tree
        /// </summary>
        /// <param name="currFruit"></param>
        void FruitContainer.RemoveFruit(Fruit currFruit)
        {
            Debug.Log("Remove from container");
            fruits.Remove(currFruit);
        }
        
    }

}

