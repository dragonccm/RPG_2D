using UnityEngine;

namespace AI
{
    public class EnemyPauseController : MonoBehaviour, IPauseListener
    {
        [Header("Pause Settings")]
        [SerializeField] private bool pauseMovement = true;
        [SerializeField] private bool pauseAnimation = true;
        [SerializeField] private bool pauseAI = true;
        
        private EnemyMovementController movementController;
        private EnemyAnimatorController animatorController;
        private Enemy enemy;
        private Animator animator;
        private bool wasPaused = false;
        
        private void Awake()
        {
            movementController = GetComponent<EnemyMovementController>();
            animatorController = GetComponent<EnemyAnimatorController>();
            enemy = GetComponent<Enemy>();
            animator = GetComponent<Animator>();
        }
        
        private void Start()
        {
            // Tự động đăng ký với PauseMenu
            PauseMenu.AddListener(this);
        }
        
        public void OnPause()
        {
            if (wasPaused) return;
            wasPaused = true;
            
            // Pause movement
            if (pauseMovement && movementController != null)
            {
                movementController.Stop();
            }
            
            // Pause animation
            if (pauseAnimation && animator != null)
            {
                animator.speed = 0f;
            }
            
            // Pause AI behavior - don't disable enemy completely, just pause movement and attacks
            if (pauseAI && enemy != null)
            {
                // Instead of disabling the entire enemy, just pause specific behaviors
                // This allows damage dealing to still work when enemies are paused
                if (enemy.TryGetComponent<EnemyMovementController>(out var movement))
                    movement.Stop();
                
                if (enemy.TryGetComponent<EnemyAnimatorController>(out var animatorCtrl))
                    animatorCtrl.enabled = false;
            }
            
            // Pause animator controller
            if (pauseAnimation && animatorController != null)
            {
                // Có thể thêm logic pause cho animator controller nếu cần
            }
        }
        
        public void OnResume()
        {
            if (!wasPaused) return;
            wasPaused = false;
            
            // Resume movement
            if (pauseMovement && movementController != null)
            {
                movementController.Resume();
            }
            
            // Resume animation
            if (pauseAnimation && animator != null)
            {
                animator.speed = 1f;
            }
            
            // Resume AI behavior - restore specific components
            if (pauseAI && enemy != null)
            {
                if (enemy.TryGetComponent<EnemyMovementController>(out var movement))
                    movement.Resume();
                
                if (enemy.TryGetComponent<EnemyAnimatorController>(out var animatorCtrl))
                    animatorCtrl.enabled = true;
            }
            
            // Resume animator controller
            if (pauseAnimation && animatorController != null)
            {
                // Có thể thêm logic resume cho animator controller nếu cần
            }
        }
        
        private void OnDestroy()
        {
            // Đảm bảo resume khi object bị destroy
            if (wasPaused)
            {
                OnResume();
            }
            
            // Hủy đăng ký khỏi PauseMenu
            PauseMenu.RemoveListener(this);
        }
    }
}