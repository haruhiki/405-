using UnityEngine;

public class GridLineCon : MonoBehaviour
{
        private float targetTime;
        private float preSpawnTime;
        private Vector3 spawnPos;
        private Vector3 targetPos;
        private bool isInitialized = false;

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Init(float targetTime, float preSpawnTime, Vector3 spawnPos, Vector3 targetPos, Color color)
        {
            this.targetTime = targetTime;
            this.preSpawnTime = preSpawnTime;
            this.spawnPos = spawnPos;
            this.targetPos = targetPos;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = color;
            }

            transform.position = spawnPos;
            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized || AudioManager.Instance == null) return;

            float currentTime = AudioManager.Instance.GetCurrentTime();
            float remainingTime = targetTime - currentTime;
            float progress = 1.0f - (remainingTime / preSpawnTime);

            transform.position = Vector3.LerpUnclamped(spawnPos, targetPos, progress);

            // ”»’èƒ‰ƒCƒ“‚ð’Ê‚è‰ß‚¬‚½‚ç”jŠü
            if (progress >= 1.1f)
            {
                Destroy(gameObject);
            }
        }


}
