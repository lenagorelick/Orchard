using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;
using Sequence = DG.Tweening.Sequence;
using Vector3 = UnityEngine.Vector3;


namespace Orchard
{
    
    public class Fruit : MonoBehaviour
    {
        // serialized properties
        [Tooltip("Ref to audio source for playing sound effects.")]
        [SerializeField] private AudioSource audioSource;
        
        [Tooltip("Ref to grow sound effect.")]
        [SerializeField] private AudioClip growSound;
        
        [Tooltip("Ref to detach-from-tree sound effect.")]
        [SerializeField] private AudioClip detachSound;
        
        [Tooltip("Ref to fall sound effect.")]
        [SerializeField] private AudioClip fallSound;
        
        [Tooltip("Duration of fruit grow phase.")]
        [SerializeField] private float growDuration = 0.3f;
        
        [Tooltip("How fast fruit detaches from tree.")]
        [SerializeField] private float detachSpeed = 50;
       
        [Tooltip("How fast fruit follows the mouse when dragged.")]
        [SerializeField] private float dragSpeed = 20;
        
        [Tooltip("How fast fruit falls.")]
        [SerializeField] private float fallSpeed = 30;
        
        [Tooltip("Distance between mouse and fruit that causes detachment.")]
        [SerializeField] private float detachThreshold = 1;
        
        [Tooltip("Strength of shake when detaching from tree.")]
        [SerializeField] private float shakeRadius = 0.02f;
        
        [Tooltip("Reference to splash particle system prefab")]
        [SerializeField] private GameObject splashParticleSystemPrefab;
        
        
        

        // ref to the tree that holds the fruit
        public FruitContainer myContainer;
        // ref to the location of the floor
        public Transform floorTransform;
        
        // fruit offset with respect to mouse click
        private Vector3 dragOffset;
        // ref to camera
        private Camera cam;
        // ref to fruit spawning position
        private Vector3 restPosition;
        
        // ref to the splash color 
        private Color splashColor;

        
        
        
        


        // states of a fruit - fruit life cycle
        enum State
        {
            None, 
            Grow,
            Rest,
            Shake,
            Detach,
            Drag,
            Fall,
            Bouncing,
            OnFloor
        }
        // fruit state
        private State myState = State.None;

       
        void setState(State newState)
        {
            //Debug.Log("Moving from " + myState + " to " + newState);
            myState = newState;
        }
        
        /// <summary>
        /// Grows the fruit gradually:
        /// Sets the sprite for the fruit
        /// Plays grow sound
        /// Assigns color to splash
        /// 
        /// </summary>
        /// <param name="fruitSprite"></param>
        /// <param name="fruitSplashColor"></param>
        public void Grow(Sprite fruitSprite, Color fruitSplashColor)
        {

            //Debug.Log("Grow");
            // change state
            setState(State.Grow);
           
            // assign sprite
            this.GetComponent<SpriteRenderer>().sprite = fruitSprite;
            
            // gradually change the scale of fruit
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, growDuration).SetEase(Ease.OutBack).OnComplete(delegate { setState(State.Rest); });
            
            // play grow sound
            PlaySound(growSound);
            
            // remember spawn position
            restPosition = transform.position;

            splashColor = fruitSplashColor;
            
        }

        /// <summary>
        /// Plays a given sound clip
        /// Randomizes the pitch
        /// </summary>
        /// <param name="soundClip"></param>
        private void PlaySound(AudioClip soundClip)
        {
            
            audioSource.clip = soundClip;
            audioSource.pitch = Random.Range(0.8f, 1.2f);
            audioSource.Play();

        }
        
        /// <summary>
        /// Awake: keeps camera position
        /// </summary>
        void Awake() 
        {
            
            cam = Camera.main;
            
        }

        /// <summary>
        /// What happens when mouse clicks on the fruit
        /// If the fruit is resting on the tree:
        /// sets the dragOffset and changes the state to shaking
        /// </summary>
        void OnMouseDown()
        {
            //Debug.Log("Mouse Down " + myState);
            // if fruit is resting on the tree
            if (myState == State.Rest)
            {
                setState(State.Shake);
                dragOffset = transform.position - GetMousePos();
            }

            if (myState == State.OnFloor)
            {
                setState(State.Drag);
            }
            
        }

        /// <summary>
        /// What happens when mouse lets go of the fruit
        /// If the state is shaking, then the fruit did not detach and it goes back to rest on the tree
        /// If the state is drag or detach, the fruit will fall
        /// </summary>
        private void OnMouseUp()
        {
            
            // is the state is shaking, go back to resting on the tree
            if (myState == State.Shake)
            {
                Rest();    
            }
            
            // if the state is drag or detach, start falling
            if (myState == State.Drag || myState == State.Detach)
            {
                setState(State.Fall);
            }
            
        }

        /// <summary>
        /// Fruit goes back to resting on the tree at its original spawn location
        /// The parent tree stops reaching after the fruit and goes back to its original position 
        /// </summary>
        void Rest()
        {
            
            // gradually move fruit back to its original location
            transform.DOMove(restPosition, 0.2f);
            
            // change the state to rest
            setState(State.Rest);
            
            // tell the tree to stop reaching
            myContainer.Unreach(false);
            
        }
        
        /// <summary>
        /// What happens when the fruit is dragged
        /// </summary>
        void OnMouseDrag()
        {
            
            // this is where the fruit needs to be with respect to the mouse
            var fruitTargetPosition = GetMousePos() + dragOffset;
            // if the fruit is shaking
            if (myState == State.Shake)
            {
                // check the distance whether it is time to detach
                if (Vector3.Distance(fruitTargetPosition, restPosition) > detachThreshold)
                {
                    Detach();
                }
            }

            // if the fruit is dragging
            if (myState == State.Drag)
            {
                // keep dragging after the mouse gradually with drag speed
                transform.position = Vector3.MoveTowards(transform.position, fruitTargetPosition,dragSpeed * Time.deltaTime);

            }
            
            
            // if the fruit is detaching
            if (myState == State.Detach)
            {
                // play detach sound
                PlaySound(detachSound);
                
                // keep dragging after the mouse gradually with fast spring like detach speed
                transform.position = Vector3.MoveTowards(transform.position, fruitTargetPosition,
                    detachSpeed * Time.deltaTime);
                
                // if close enough to the mouse 
                if (Vector3.Distance(fruitTargetPosition, transform.position) < 0.01f)
                {
                    // change the state to drag (that will affect its speed)
                    setState(State.Drag);
                }
            }
        }

        /// <summary>
        /// Detaches fruit from the tree
        /// Moves fruit into interactive layer, so it is in front of everything else
        /// Tell tree to stop reaching after the fruit
        /// </summary>
        private void Detach()
        {
            
            // change the state to detach
            setState(State.Detach);
           
            // change sprite layer
            this.GetComponent<SpriteRenderer>().sortingLayerName = "Interactive";
            
            // each new interaction is put in front of everything else
            this.GetComponent<SpriteRenderer>().sortingOrder = (int)Time.time;
           
            // stop tree from reaching after the fruit
            myContainer.Unreach(true);
            
        }
        
        /// <summary>
        /// Get mouse position in world coord
        /// </summary>
        /// <returns></returns>
        Vector3 GetMousePos() 
        {
            
            var mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            return mousePos;
            
        }

        /// <summary>
        /// Every frame do:
        /// if shake state - fruit keeps shaking and tree keeps reaching
        /// if fall state - fruit keeps falling
        /// </summary>
        void Update()
        {
      
            
            // shake
            if (myState == State.Shake)
            {
                Shake();
                myContainer.Reach(GetMousePos());
                
            }

            // fall
            if (myState == State.Fall)
            {
                Fall();

            }

        }

        private void Drag()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// fruit simulates shaking
        /// </summary>
        void Shake()
        {
           
            // shake radius is larger as the distance between the spawn position and the mouse grows
            // normalized by detach threshold
            float currShakeRadius = shakeRadius * Vector3.Distance(GetMousePos() + dragOffset, restPosition)/ detachThreshold;
            
            // shake randomly around within shake radius
            var p2 = Random.insideUnitCircle * currShakeRadius;
            
            // shake closer to the mouse - this is vecotr in the direction of the mouse
            var d = Vector3.Lerp(restPosition, GetMousePos(), 0.2f);
            
            // shake closer to the mouse
            transform.position = new Vector3(p2.x,p2.y,0) + d;
            
        }

        /// <summary>
        /// Fruit falls down until it hits abstract floor location
        /// </summary>
        void Fall()
        {
            
            // fall with falling speed
            transform.position += fallSpeed * Vector3.down * Time.deltaTime;
            //Vector3 avgMomentumVector = momentumQueue.GetAverage();
            //Debug.DrawLine(transform.position , transform.position + avgMomentumVector * 10f);

            //transform.position += avgMomentumVector*10f;
            //momentumVector = Vector3.Lerp(avgMomentumVector,Vector3.down, gravityScale);//throwDampening * momentum); 
            

            // check if we reached the height of the floor
            if (transform.position.y < floorTransform.position.y)
            {
                // fruit "dies"
                Bounce();
            }
            
        }

        /// <summary>
        /// Fruit dies after falling:
        /// disconnect the particle effect from the fruit, so the effect stays while fruit bounces
        /// play particle effect
        /// change state to die
        /// tell tree to forget about the fruit
        /// bounce the fruit
        /// play fall sound
        /// move the fruit into "fruits" sorting layer
        /// </summary>
        private void Bounce()
        {
            // disconnect the particle system from the parent

            var splash = Instantiate(splashParticleSystemPrefab).GetComponent<ParticleSystem>();
            splash.transform.position = this.transform.position;
            // assign color to the splash particle system
            var main = splash.main;
            main.startColor = splashColor;
            
            
            // change the state of the fruit
            setState(State.Bouncing);
            
            // play splash effect
            splash.Play();
            
            // remove fruit from the tree
            //MyTree.Dispawn(this);
            
            // perform bouncing effect
            float y = transform.position.y;
            // a sequence of motion in y direction
            Sequence sequence =  DOTween.Sequence();
            // first bounce up
            sequence.Append(transform.DOMoveY(y+0.7f, 0.2f).SetEase(Ease.OutQuad));
            // then bounce down
            sequence.Append(transform.DOMoveY(y, 0.2f).SetEase(Ease.InQuad));
            
            // bounce sideways randomly in x direction
            float direction = Mathf.Sign(Random.Range(-1,1));
            transform.DOMoveX(transform.position.x + 0.7f*direction, 0.4f)
                .OnComplete(
                    delegate 
                    { 
                        // change the state of the fruit
                        setState(State.OnFloor); 
                        // update spawn position
                        restPosition = transform.position;

                    });
            
            // play sound
            PlaySound(fallSound);
            
            // this fruit goes back to "Fruits" sorting level
            this.GetComponent<SpriteRenderer>().sortingLayerName = "Fruits";
            
        }
    }
}

class MomentumQueue{
    private Queue<Vector3> _queue;
    private int _maxSize;
    public int Count {
        get {
            return _queue.Count;
        }
    }
    public MomentumQueue(int maxSize){
        _maxSize = maxSize;
        _queue = new Queue<Vector3>();
    }
    public void Enqueue(Vector3 v){
        if(_queue.Count>=_maxSize){
            _queue.Dequeue();
        }
        _queue.Enqueue(v);
    }
    public Vector3 Peek(){
        return _queue.Peek();
    }
    public void Clear(){
        _queue.Clear();
    }

    public Vector3 GetAverage()
    {
        Vector3 avg = Vector3.zero;
        foreach (var vec in _queue)
        {
            avg += vec;
        }

        Vector3 returnAvg = new Vector3(avg.x / _queue.Count, avg.y / _queue.Count, avg.z / _queue.Count);
        return avg / (float)_queue.Count;
    }
}