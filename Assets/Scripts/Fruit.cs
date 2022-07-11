using System;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Orchard
{
    
    public class Fruit : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip growSound;
        [SerializeField] private float growDuration = 0.3f;
        
        

        [SerializeField] private float dragSpeed = 20;
        [SerializeField] private float detachSpeed = 50;
        [SerializeField] private float detachThreshold = 1;
        [SerializeField] private float shakeRadius = 0.02f;
        

        private Vector3 dragOffset;
        private Camera cam;
        
        enum State
        {
            Grow,
            Rest,
            Shake,
            Detach,
            Drag,
            Fall,
            Die
        }

        private State myState;
        private Vector3 spawnPosition;
        
        public void Grow()
        {

            myState = State.Grow;
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, growDuration).SetEase(Ease.OutBack).OnComplete(delegate { myState = State.Rest; });
            PlayGrowSound();
            spawnPosition = transform.position;



        }

        private void PlayGrowSound()
        {
            // sound for spawn
            audioSource.clip = growSound;
            audioSource.pitch = Random.Range(0.8f, 1.2f);
            float fromSeconds = Random.Range(0, growSound.length - growDuration);
            PlaySoundInterval(fromSeconds);

        }
        
        private void PlaySoundInterval(float fromSeconds)
        {
            audioSource.time = fromSeconds;
            audioSource.Play();
            audioSource.SetScheduledEndTime( AudioSettings.dspTime + growDuration);
            
        }
        
        
        void Awake() 
        {
            cam = Camera.main;
        }

        void OnMouseDown()
        {
            myState = State.Shake;
            dragOffset = transform.position - GetMousePos();
        }

        private void OnMouseUp()
        {
            if (myState == State.Shake)
            {
                myState = State.Rest;
            }
            if (myState == State.Drag)
            {
                myState = State.Fall;
            }
            
        }

        void OnMouseDrag() 
        {
            if (myState == State.Shake)
            {
                if (Vector3.Distance(GetMousePos(), spawnPosition) > detachThreshold)
                {
                    myState = State.Detach;
                }
            }

            if (myState == State.Drag)
            {
                transform.position = Vector3.MoveTowards(transform.position, GetMousePos() + dragOffset,
                    dragSpeed * Time.deltaTime);
            }
            if (myState == State.Detach)
            {
                transform.position = Vector3.MoveTowards(transform.position, GetMousePos() + dragOffset,
                    detachSpeed * Time.deltaTime);
                if (Vector3.Distance(GetMousePos(), spawnPosition) < 0.01f)
                {
                    myState = State.Drag;
                }
            }
        }

        Vector3 GetMousePos() 
        {
            var mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            return mousePos;
        }

        void Update()
        {
            if (myState == State.Shake)
            {
                Shake();
                
            }
        }

        void Shake()
        {
            var p2 = Random.insideUnitCircle * shakeRadius;
            transform.position = new Vector3(p2.x,p2.y,0)+ spawnPosition;
        }

    }
}