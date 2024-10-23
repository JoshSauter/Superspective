using System;
using System.Collections;
using SuperspectiveUtils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TheEntity {
    // Common behavior for TheEntity
    public static class TheEntity {
        private const float BLINK_TIME = .125f;
        private const float MIN_TIME_BETWEEN_BLINKS = 2.5f;
        private const float MAX_TIME_BETWEEN_BLINKS = 9f;
        
        // Eye blinking behavior
        public static IEnumerator BlinkController(MonoBehaviour monoBehavior, Transform eyeTransform, Func<float> blinkTimeMultiplier = null) {
            if (blinkTimeMultiplier == null) {
                blinkTimeMultiplier = () => 1f;
            }
            
            while (true) {
                monoBehavior.StartCoroutine(BlinkCoroutine(eyeTransform));
                
                yield return new WaitForSeconds(Random.Range(MIN_TIME_BETWEEN_BLINKS * blinkTimeMultiplier.Invoke(), MAX_TIME_BETWEEN_BLINKS * blinkTimeMultiplier.Invoke()));
            }
        }

        private static IEnumerator BlinkCoroutine(Transform eyeTransform) {
            float time = 0;

            // Blink closed
            while (time < BLINK_TIME) {
                time += Time.deltaTime;

                eyeTransform.localScale = eyeTransform.localScale.WithY(1 - Easing.EaseInOut(time / BLINK_TIME));
                
                yield return null;
            }
            eyeTransform.localScale = eyeTransform.localScale.WithY(0);
            
            // Blink open
            time = 0f;
            while (time < BLINK_TIME) {
                time += Time.deltaTime;

                eyeTransform.localScale = eyeTransform.localScale.WithY(Easing.EaseInOut(time / BLINK_TIME));
                
                yield return null;
            }
            eyeTransform.localScale = eyeTransform.localScale.WithY(1);
        }
    }
}