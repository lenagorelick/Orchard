using DG.Tweening;
using UnityEngine;

namespace Orchard
{
    
    public class Fruit : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip growSound;
        [SerializeField] private float growDuration = 0.5f;
        
        public void Grow()
        {
 
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, growDuration).SetEase(Ease.OutBack);
            PlayGrowSound();
            
           
        }

        private void PlayGrowSound()
        {
            // sound for spawn
            audioSource.clip = growSound;
            //audioSource.pitch = Random.Range(0.5f, 1.5f);
            float fromSeconds = Random.Range(0, growSound.length - growDuration);
            PlaySoundInterval(fromSeconds);

        }
        
        private void PlaySoundInterval(float fromSeconds)
        {
            audioSource.time = fromSeconds;
            audioSource.Play();
            audioSource.SetScheduledEndTime( AudioSettings.dspTime + growDuration);
            
        }
    }
}