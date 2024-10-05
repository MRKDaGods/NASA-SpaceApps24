using UnityEngine;

namespace MRK
{
    public class DeviateElement : MRKBehaviour
    {
        // Variables to control the oscillation
        public float speed = 2.0f;  // Speed of oscillation
        public float amplitude = 1.0f;
        private Vector2 startPosition;

        void Start()
        {
            startPosition = rectTransform.anchoredPosition;
            //startPosition.y = transform.parent.GetChild(0).position.y;
        }

        void Update()
        {
            Vector3 newPosition = startPosition;
            newPosition.x += Mathf.Sin(Time.time * speed) * amplitude; // Oscillate along the X axis
            rectTransform.anchoredPosition = newPosition;
        }
    }
}