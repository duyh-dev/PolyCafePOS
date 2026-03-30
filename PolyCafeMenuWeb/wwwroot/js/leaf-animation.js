/**
 * PolyCafe — Falling Leaves Background Animation
 * Creates lightweight SVG leaves falling gently with wind sway.
 * Performance-optimized: uses CSS animations, will-change, and limits leaf count.
 */
(function () {
    'use strict';

    // ===== CONFIGURATION =====
    const CONFIG = {
        maxLeaves: 12,           // 8–15 range, 12 is a sweet spot
        minFallDuration: 10,     // seconds (slowest leaf)
        maxFallDuration: 20,     // seconds (fastest leaf)
        minSwayDuration: 3,      // sway cycle speed
        maxSwayDuration: 6,
        minScale: 0.5,
        maxScale: 1.2,
        minOpacity: 0.1,
        maxOpacity: 0.28,
        minSwayAmount: 20,       // px sway left-right
        maxSwayAmount: 60,
        spawnInterval: 2500,     // ms between new leaf spawns
    };

    // ===== LEAF SVG TEMPLATES =====
    // Three slightly different leaf shapes for visual variety
    const LEAF_SVGS = [
        // Leaf 1 — Simple rounded leaf
        (color, size) => `<svg width="${size}" height="${size}" viewBox="0 0 40 40" xmlns="http://www.w3.org/2000/svg">
            <path d="M20 2 C8 8, 2 20, 20 38 C38 20, 32 8, 20 2Z" fill="${color}" />
            <line x1="20" y1="6" x2="20" y2="34" stroke="${color}" stroke-width="0.8" opacity="0.4" />
        </svg>`,
        // Leaf 2 — Elongated leaf
        (color, size) => `<svg width="${size}" height="${Math.round(size * 1.4)}" viewBox="0 0 30 42" xmlns="http://www.w3.org/2000/svg">
            <path d="M15 2 C5 10, 2 22, 15 40 C28 22, 25 10, 15 2Z" fill="${color}" />
            <path d="M15 6 Q13 20, 15 36" stroke="${color}" stroke-width="0.6" fill="none" opacity="0.35" />
        </svg>`,
        // Leaf 3 — Small round leaf
        (color, size) => `<svg width="${size}" height="${size}" viewBox="0 0 32 32" xmlns="http://www.w3.org/2000/svg">
            <path d="M16 2 C6 6, 2 16, 16 30 C30 16, 26 6, 16 2Z" fill="${color}" />
            <path d="M16 5 L16 26" stroke="${color}" stroke-width="0.5" opacity="0.3" />
            <path d="M16 14 Q10 10, 8 8" stroke="${color}" stroke-width="0.4" fill="none" opacity="0.25" />
            <path d="M16 18 Q22 14, 24 12" stroke="${color}" stroke-width="0.4" fill="none" opacity="0.25" />
        </svg>`
    ];

    // Leaf colors matching the brand palette
    const LEAF_COLORS = [
        '#048C73',
        '#0FAF8C',
        'rgba(4,140,115,0.45)',
        '#0FAF8C',
        '#048C73',
    ];

    // ===== HELPER FUNCTIONS =====
    function rand(min, max) {
        return Math.random() * (max - min) + min;
    }

    function randInt(min, max) {
        return Math.floor(rand(min, max + 1));
    }

    function pickRandom(arr) {
        return arr[randInt(0, arr.length - 1)];
    }

    // ===== CREATE A LEAF ELEMENT =====
    function createLeaf(container) {
        const leaf = document.createElement('div');
        leaf.className = 'leaf';

        // Random properties
        const size = randInt(16, 32);
        const color = pickRandom(LEAF_COLORS);
        const svgFn = pickRandom(LEAF_SVGS);
        const fallDuration = rand(CONFIG.minFallDuration, CONFIG.maxFallDuration);
        const swayDuration = rand(CONFIG.minSwayDuration, CONFIG.maxSwayDuration);
        const scale = rand(CONFIG.minScale, CONFIG.maxScale);
        const opacity = rand(CONFIG.minOpacity, CONFIG.maxOpacity);
        const swayAmount = rand(CONFIG.minSwayAmount, CONFIG.maxSwayAmount) * (Math.random() > 0.5 ? 1 : -1);
        const startRotate = randInt(-30, 30);
        const endRotate = startRotate + randInt(180, 540) * (Math.random() > 0.5 ? 1 : -1);
        const leftPos = rand(0, 100);
        const fallDelay = rand(0, 3);
        const swayDelay = rand(0, 2);

        // Set CSS custom properties for the animation
        leaf.style.setProperty('--fall-duration', `${fallDuration}s`);
        leaf.style.setProperty('--sway-duration', `${swayDuration}s`);
        leaf.style.setProperty('--leaf-scale', scale);
        leaf.style.setProperty('--leaf-opacity', opacity);
        leaf.style.setProperty('--sway-amount', `${swayAmount}px`);
        leaf.style.setProperty('--start-rotate', `${startRotate}deg`);
        leaf.style.setProperty('--end-rotate', `${endRotate}deg`);
        leaf.style.setProperty('--fall-delay', `${fallDelay}s`);
        leaf.style.setProperty('--sway-delay', `${swayDelay}s`);
        leaf.style.left = `${leftPos}%`;

        // Set the SVG content
        leaf.innerHTML = svgFn(color, size);

        // Remove leaf after animation ends to prevent DOM buildup
        leaf.addEventListener('animationiteration', function handler(e) {
            if (e.animationName === 'leafFall') {
                // Only remove if we're over the max count
                const currentLeaves = container.querySelectorAll('.leaf');
                if (currentLeaves.length > CONFIG.maxLeaves) {
                    leaf.removeEventListener('animationiteration', handler);
                    leaf.remove();
                }
            }
        });

        container.appendChild(leaf);
    }

    // ===== INITIALIZE =====
    function init() {
        // Check for reduced motion preference
        if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
            return;
        }

        const container = document.querySelector('.leaf-background');
        if (!container) return;

        // Create initial batch of leaves with staggered delays
        for (let i = 0; i < CONFIG.maxLeaves; i++) {
            setTimeout(() => createLeaf(container), i * 600);
        }

        // Continuously spawn new leaves
        setInterval(() => {
            const currentLeaves = container.querySelectorAll('.leaf');
            if (currentLeaves.length < CONFIG.maxLeaves) {
                createLeaf(container);
            }
        }, CONFIG.spawnInterval);

        // Pause animation when tab is not visible (performance)
        document.addEventListener('visibilitychange', () => {
            container.style.animationPlayState = document.hidden ? 'paused' : 'running';
            container.querySelectorAll('.leaf').forEach(leaf => {
                leaf.style.animationPlayState = document.hidden ? 'paused' : 'running';
            });
        });
    }

    // Start when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
