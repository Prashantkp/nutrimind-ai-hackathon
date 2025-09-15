/* ========================================
   🍎 NutriMind Floating Food Animation System 🥕
   ======================================== */

class FloatingFoodManager {
    constructor() {
        this.foodEmojis = [
            // Fruits
            '🍎', '🍊', '🍋', '🍌', '🍇', '🍓', '🫐', '🍑', '🥝', '🍍', 
            '🥭', '🍈', '🍉', '🍒', '🥥', '🍐', '🍑',
            
            // Vegetables  
            '🥕', '🥬', '🥒', '🌽', '🍅', '🥑', '🥦', '🥬', '🌶️', '🫑',
            '🧄', '🧅', '🥔', '🍠', '🥐', '🌰',
            
            // Herbs & Spices
            '🌿', '🍃', '🌱', '🫒',
            
            // Other food items
            '🥯', '🍞', '🥖', '🧀', '🥜', '🌾'
        ];
        
        this.container = null;
        this.isActive = true;
        this.elements = [];
        this.maxElements = 15; // Maximum floating elements at once
        this.spawnRate = 2000; // Spawn new element every 2 seconds
        
        this.init();
    }
    
    init() {
        this.createContainer();
        this.startAnimation();
        this.handleVisibilityChange();
        this.handleResize();
    }
    
    createContainer() {
        // Remove existing container if it exists
        const existing = document.getElementById('floating-food-container');
        if (existing) {
            existing.remove();
        }
        
        this.container = document.createElement('div');
        this.container.id = 'floating-food-container';
        this.container.className = 'floating-elements';
        document.body.appendChild(this.container);
    }
    
    createFloatingElement() {
        if (this.elements.length >= this.maxElements) {
            return;
        }
        
        const element = document.createElement('div');
        const emoji = this.getRandomEmoji();
        const size = this.getRandomSize();
        const startPosition = Math.random() * window.innerWidth;
        const animationDuration = 15 + Math.random() * 20; // 15-35 seconds
        const delay = Math.random() * 2; // 0-2 seconds delay
        
        element.className = `floating-food ${size}`;
        element.innerHTML = emoji;
        element.style.left = `${startPosition}px`;
        element.style.animationDuration = `${animationDuration}s`;
        element.style.animationDelay = `${delay}s`;
        
        // Add random horizontal drift
        const drift = (Math.random() - 0.5) * 100; // -50px to +50px
        element.style.setProperty('--drift', `${drift}px`);
        
        // Custom animation with drift
        element.style.animation = `floatWithDrift ${animationDuration}s linear infinite ${delay}s`;
        
        this.container.appendChild(element);
        this.elements.push(element);
        
        // Remove element after animation completes
        setTimeout(() => {
            this.removeElement(element);
        }, (animationDuration + delay) * 1000);
    }
    
    getRandomEmoji() {
        return this.foodEmojis[Math.floor(Math.random() * this.foodEmojis.length)];
    }
    
    getRandomSize() {
        const rand = Math.random();
        if (rand < 0.3) return 'small';
        if (rand < 0.8) return '';
        return 'large';
    }
    
    removeElement(element) {
        if (element && element.parentNode) {
            element.parentNode.removeChild(element);
        }
        this.elements = this.elements.filter(el => el !== element);
    }
    
    startAnimation() {
        if (!this.isActive) return;
        
        this.createFloatingElement();
        
        // Schedule next element
        const nextSpawn = this.spawnRate + (Math.random() * 1000); // Add some randomness
        setTimeout(() => this.startAnimation(), nextSpawn);
    }
    
    pause() {
        this.isActive = false;
        this.elements.forEach(element => {
            element.style.animationPlayState = 'paused';
        });
    }
    
    resume() {
        this.isActive = true;
        this.elements.forEach(element => {
            element.style.animationPlayState = 'running';
        });
        this.startAnimation();
    }
    
    handleVisibilityChange() {
        document.addEventListener('visibilitychange', () => {
            if (document.hidden) {
                this.pause();
            } else {
                this.resume();
            }
        });
    }
    
    handleResize() {
        let resizeTimeout;
        window.addEventListener('resize', () => {
            clearTimeout(resizeTimeout);
            resizeTimeout = setTimeout(() => {
                // Adjust existing elements positions if needed
                this.elements.forEach(element => {
                    const currentLeft = parseInt(element.style.left);
                    if (currentLeft > window.innerWidth) {
                        element.style.left = `${Math.random() * window.innerWidth}px`;
                    }
                });
            }, 250);
        });
    }
    
    // Special effects for holidays or themes
    addSeasonalElements(season = 'default') {
        const seasonalEmojis = {
            spring: ['🌸', '🌼', '🌷', '🌺', '🌻'],
            summer: ['🍉', '🍓', '🍒', '🥝', '🍍'],
            autumn: ['🍂', '🎃', '🌰', '🍎', '🍊'],
            winter: ['❄️', '⭐', '🎄', '🥥', '🫖'],
            default: this.foodEmojis
        };
        
        if (seasonalEmojis[season]) {
            this.foodEmojis = [...seasonalEmojis[season], ...this.foodEmojis.slice(0, 10)];
        }
    }
}

// Enhanced CSS animations (add to existing styles)
const additionalStyles = `
@keyframes floatWithDrift {
    0% {
        transform: translateY(100vh) translateX(0px) rotate(0deg) scale(1);
        opacity: 0;
    }
    5% {
        opacity: 0.6;
    }
    95% {
        opacity: 0.6;
    }
    100% {
        transform: translateY(-100px) translateX(var(--drift, 0px)) rotate(360deg) scale(1.1);
        opacity: 0;
    }
}

@keyframes gentleBob {
    0%, 100% { transform: translateY(0px) rotate(-1deg); }
    50% { transform: translateY(-10px) rotate(1deg); }
}

.floating-food:nth-child(odd) {
    animation-direction: reverse;
}

.floating-food:hover {
    animation: gentleBob 1s ease-in-out infinite;
    transform: scale(1.2);
    z-index: 1000;
}

/* Particle trail effect */
.floating-food::after {
    content: '';
    position: absolute;
    top: 50%;
    left: 50%;
    width: 4px;
    height: 4px;
    background: radial-gradient(circle, rgba(76,175,80,0.5) 0%, transparent 70%);
    border-radius: 50%;
    transform: translate(-50%, -50%);
    animation: trail 2s linear infinite;
}

@keyframes trail {
    0% {
        opacity: 0;
        transform: translate(-50%, -50%) scale(0);
    }
    50% {
        opacity: 0.8;
        transform: translate(-50%, -50%) scale(1);
    }
    100% {
        opacity: 0;
        transform: translate(-50%, -50%) scale(0);
    }
}

/* Magical sparkle effect */
@keyframes sparkle {
    0%, 100% { opacity: 0; transform: scale(0) rotate(0deg); }
    50% { opacity: 1; transform: scale(1) rotate(180deg); }
}

.magical-sparkle {
    position: fixed;
    pointer-events: none;
    color: #FFD700;
    font-size: 1rem;
    animation: sparkle 1.5s ease-in-out infinite;
    z-index: 1000;
}
`;

// Inject additional styles
const styleSheet = document.createElement('style');
styleSheet.textContent = additionalStyles;
document.head.appendChild(styleSheet);

// Sparkle effect manager
class SparkleManager {
    constructor() {
        this.sparkles = ['✨', '⭐', '💫', '🌟'];
        this.init();
    }
    
    init() {
        document.addEventListener('click', (e) => {
            if (Math.random() < 0.3) { // 30% chance of sparkle on click
                this.createSparkle(e.clientX, e.clientY);
            }
        });
    }
    
    createSparkle(x, y) {
        const sparkle = document.createElement('div');
        sparkle.className = 'magical-sparkle';
        sparkle.innerHTML = this.sparkles[Math.floor(Math.random() * this.sparkles.length)];
        sparkle.style.left = `${x - 10}px`;
        sparkle.style.top = `${y - 10}px`;
        
        document.body.appendChild(sparkle);
        
        setTimeout(() => {
            if (sparkle.parentNode) {
                sparkle.parentNode.removeChild(sparkle);
            }
        }, 1500);
    }
}

// Initialize everything when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    // Small delay to ensure everything is ready
    setTimeout(() => {
        window.floatingFoodManager = new FloatingFoodManager();
        window.sparkleManager = new SparkleManager();
        
        // Add seasonal theme based on current date
        const month = new Date().getMonth();
        if (month >= 2 && month <= 4) {
            window.floatingFoodManager.addSeasonalElements('spring');
        } else if (month >= 5 && month <= 7) {
            window.floatingFoodManager.addSeasonalElements('summer');
        } else if (month >= 8 && month <= 10) {
            window.floatingFoodManager.addSeasonalElements('autumn');
        } else {
            window.floatingFoodManager.addSeasonalElements('winter');
        }
    }, 500);
});

// Export for module usage (if needed)
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { FloatingFoodManager, SparkleManager };
}
