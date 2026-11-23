// BharatBank - Frontend Interactions & 3D Visual Engine
document.addEventListener("DOMContentLoaded", function () {
    // 1. Auto-dismiss flash alerts after 6 seconds
    const alerts = document.querySelectorAll('.alert-dismissible');
    alerts.forEach(alert => {
        setTimeout(() => {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            if (bsAlert) bsAlert.close();
        }, 6000);
    });

    // 2. Copy to clipboard helper
    const copyBtns = document.querySelectorAll('.btn-copy');
    copyBtns.forEach(btn => {
        btn.addEventListener('click', function () {
            const textToCopy = this.getAttribute('data-clipboard-text') || this.innerText;
            navigator.clipboard.writeText(textToCopy).then(() => {
                const originalHtml = this.innerHTML;
                this.innerHTML = '<i class="bi bi-check2"></i> Copied!';
                setTimeout(() => { this.innerHTML = originalHtml; }, 2000);
            });
        });
    });

    // 3. Initialize 3D Visual Design Elements
    const prefersReducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    if (!prefersReducedMotion) {
        init3DTilt();
        init3DFlipCard();
        initHero3DScene();
        init3DParticleBackground();
        initCinematicGalaxyBackground();
    }
});

// 4. 3D Tilt Effect on Cards
function init3DTilt() {
    const tiltCards = document.querySelectorAll('.tilt-card');
    tiltCards.forEach(card => {
        card.addEventListener('mousemove', function (e) {
            const rect = card.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;
            
            const centerX = rect.width / 2;
            const centerY = rect.height / 2;
            
            const rotateX = ((y - centerY) / centerY) * -10;
            const rotateY = ((x - centerX) / centerX) * 10;
            
            card.style.transform = `perspective(1000px) rotateX(${rotateX}deg) rotateY(${rotateY}deg) scale3d(1.02, 1.02, 1.02)`;
        });
        
        card.addEventListener('mouseleave', function () {
            card.style.transform = 'perspective(1000px) rotateX(0deg) rotateY(0deg) scale3d(1, 1, 1)';
        });
    });
}

// 5. 3D Interactive Banking Card Flipper
function init3DFlipCard() {
    const cardContainers = document.querySelectorAll('.card-3d-container');
    cardContainers.forEach(container => {
        container.addEventListener('click', function () {
            this.classList.toggle('is-flipped');
        });
    });
}

// 6. 3D Interactive Hero Canvas (Welcome Page Centerpiece)
function initHero3DScene() {
    const canvas = document.getElementById('hero3dCanvas');
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    let width = canvas.width = canvas.parentElement.clientWidth || 380;
    let height = canvas.height = 300;

    let angleY = 0;
    let angleX = 0.2;
    let mouseX = 0, mouseY = 0;
    let isHovered = false;

    window.addEventListener('resize', () => {
        if (canvas.parentElement) {
            width = canvas.width = canvas.parentElement.clientWidth;
            height = canvas.height = 300;
        }
    });

    canvas.addEventListener('mousemove', (e) => {
        const rect = canvas.getBoundingClientRect();
        mouseX = (e.clientX - rect.left - width / 2) / (width / 2);
        mouseY = (e.clientY - rect.top - height / 2) / (height / 2);
        isHovered = true;
    });

    canvas.addEventListener('mouseleave', () => {
        isHovered = false;
    });

    function drawScene() {
        ctx.clearRect(0, 0, width, height);

        // Smooth rotation
        if (isHovered) {
            angleY += (mouseX * 0.8 - angleY) * 0.05;
            angleX += (mouseY * 0.4 - angleX) * 0.05;
        } else {
            angleY += 0.012;
            angleX = Math.sin(Date.now() * 0.001) * 0.15;
        }

        const cx = width / 2;
        const cy = height / 2;
        const cardW = 180;
        const cardH = 110;

        ctx.save();
        ctx.translate(cx, cy);

        // Simulated 3D Card Projection
        const cosY = Math.cos(angleY);
        const sinY = Math.sin(angleY);
        const cosX = Math.cos(angleX);

        // Shadow
        ctx.beginPath();
        ctx.ellipse(0, 75, cardW * 0.7 * Math.abs(cosY) + 20, 18, 0, 0, Math.PI * 2);
        ctx.fillStyle = "rgba(0, 0, 0, 0.4)";
        ctx.fill();

        // 3D Card Body
        ctx.save();
        ctx.transform(cosY, sinY * 0.2, 0, cosX, 0, Math.sin(Date.now() * 0.002) * 8);

        // Card Face Gradient
        const grad = ctx.createLinearGradient(-cardW / 2, -cardH / 2, cardW / 2, cardH / 2);
        if (cosY >= 0) {
            grad.addColorStop(0, '#0b2545');
            grad.addColorStop(0.5, '#134074');
            grad.addColorStop(1, '#d97706');
        } else {
            grad.addColorStop(0, '#091a30');
            grad.addColorStop(1, '#020617');
        }

        ctx.fillStyle = grad;
        ctx.beginPath();
        ctx.roundRect(-cardW / 2, -cardH / 2, cardW, cardH, 12);
        ctx.fill();
        ctx.strokeStyle = "rgba(255, 255, 255, 0.4)";
        ctx.lineWidth = 1.5;
        ctx.stroke();

        // Card Details (Front vs Back)
        if (cosY >= 0) {
            // Front: Chip & Logo
            ctx.fillStyle = "#fcd34d";
            ctx.fillRect(-cardW / 2 + 16, -cardH / 2 + 16, 26, 20);

            ctx.fillStyle = "#ffffff";
            ctx.font = "bold 11px Outfit, sans-serif";
            ctx.fillText("BharatBank", cardW / 2 - 75, -cardH / 2 + 25);

            ctx.font = "10px monospace";
            ctx.fillText("**** **** **** 4892", -cardW / 2 + 16, cardH / 2 - 25);

            ctx.font = "8px sans-serif";
            ctx.fillStyle = "#cbd5e1";
            ctx.fillText("VALUED CUSTOMER", -cardW / 2 + 16, cardH / 2 - 12);
            ctx.fillText("12/29", cardW / 2 - 35, cardH / 2 - 12);
        } else {
            // Back: Magnetic Strip & CVV
            ctx.fillStyle = "#000000";
            ctx.fillRect(-cardW / 2, -cardH / 2 + 18, cardW, 22);

            ctx.fillStyle = "#ffffff";
            ctx.fillRect(-cardW / 2 + 16, cardH / 2 - 35, cardW - 60, 16);
            ctx.fillStyle = "#000000";
            ctx.font = "italic bold 9px monospace";
            ctx.fillText("CVV: 892", cardW / 2 - 85, cardH / 2 - 23);
        }

        ctx.restore();

        // Floating Gold Coin Stack
        const coinX = 90;
        const coinY = 40 + Math.sin(Date.now() * 0.003) * 6;
        for (let i = 0; i < 4; i++) {
            ctx.beginPath();
            ctx.ellipse(coinX, coinY - i * 6, 18, 8, 0, 0, Math.PI * 2);
            ctx.fillStyle = i === 3 ? "#f59e0b" : "#d97706";
            ctx.fill();
            ctx.strokeStyle = "#b45309";
            ctx.lineWidth = 1;
            ctx.stroke();
        }

        ctx.restore();
        requestAnimationFrame(drawScene);
    }

    drawScene();
}

// 7. Full-Page Three.js Cosmic Galaxy & Starfield Background (All Frontend Pages)
function initCinematicGalaxyBackground() {
    const canvas = document.getElementById('bg-canvas');
    if (!canvas || typeof THREE === 'undefined') return;

    try {
        const scene = new THREE.Scene();
        scene.fog = new THREE.FogExp2(0x000000, 0.0008);

        const camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.1, 2000);
        camera.position.z = 400;

        const renderer = new THREE.WebGLRenderer({ canvas: canvas, alpha: true, antialias: true });
        renderer.setSize(window.innerWidth, window.innerHeight);
        renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));

        const galaxyGroup = new THREE.Group();
        scene.add(galaxyGroup);

        // 1. Star field
        const starGeometry = new THREE.BufferGeometry();
        const starCount = 6000;
        const starPositions = new Float32Array(starCount * 3);
        const starColors = new Float32Array(starCount * 3);
        const starSizes = new Float32Array(starCount);

        const colorPalette = [
            new THREE.Color(0xffffff), // Crisp White
            new THREE.Color(0x41eec2), // Luminous Emerald/Teal
            new THREE.Color(0xffd700), // Pale Gold
            new THREE.Color(0x60a5fa)  // Soft Blue
        ];

        for (let i = 0; i < starCount; i++) {
            starPositions[i * 3] = (Math.random() - 0.5) * 2000;
            starPositions[i * 3 + 1] = (Math.random() - 0.5) * 2000;
            starPositions[i * 3 + 2] = (Math.random() - 0.5) * 2000;

            const color = colorPalette[Math.floor(Math.random() * colorPalette.length)];
            starColors[i * 3] = color.r;
            starColors[i * 3 + 1] = color.g;
            starColors[i * 3 + 2] = color.b;

            starSizes[i] = Math.random() * 2 + 0.5;
        }

        starGeometry.setAttribute('position', new THREE.BufferAttribute(starPositions, 3));
        starGeometry.setAttribute('color', new THREE.BufferAttribute(starColors, 3));
        starGeometry.setAttribute('size', new THREE.BufferAttribute(starSizes, 1));

        const starMaterial = new THREE.PointsMaterial({
            size: 1.8,
            vertexColors: true,
            transparent: true,
            opacity: 0.85,
            sizeAttenuation: true
        });
        const stars = new THREE.Points(starGeometry, starMaterial);
        galaxyGroup.add(stars);

        // 2. Cosmic Nebula Clouds
        const createNebulaTexture = () => {
            const cvs = document.createElement('canvas');
            cvs.width = 64;
            cvs.height = 64;
            const ctx = cvs.getContext('2d');
            const gradient = ctx.createRadialGradient(32, 32, 0, 32, 32, 32);
            gradient.addColorStop(0, 'rgba(255, 255, 255, 1)');
            gradient.addColorStop(0.2, 'rgba(255, 255, 255, 0.7)');
            gradient.addColorStop(0.5, 'rgba(255, 255, 255, 0.2)');
            gradient.addColorStop(1, 'rgba(0, 0, 0, 0)');
            ctx.fillStyle = gradient;
            ctx.fillRect(0, 0, 64, 64);
            return new THREE.CanvasTexture(cvs);
        };

        const nebulaTexture = createNebulaTexture();
        const nebulaGeometry = new THREE.BufferGeometry();
        const nebulaCount = 400;
        const nebulaPositions = new Float32Array(nebulaCount * 3);
        const nebulaColors = new Float32Array(nebulaCount * 3);

        const nebulaColorsList = [
            new THREE.Color(0x041322), // Midnight navy
            new THREE.Color(0x00d4aa), // Deep teal
            new THREE.Color(0x1a0f30), // Cosmic violet
            new THREE.Color(0x4a3b10)  // Golden dust
        ];

        for (let i = 0; i < nebulaCount; i++) {
            const spread = Math.random() * 800 - 400;
            nebulaPositions[i * 3] = (Math.random() - 0.5) * 1600;
            nebulaPositions[i * 3 + 1] = nebulaPositions[i * 3] * 0.5 + spread;
            nebulaPositions[i * 3 + 2] = (Math.random() - 0.5) * 600 - 150;

            const color = nebulaColorsList[Math.floor(Math.random() * nebulaColorsList.length)];
            nebulaColors[i * 3] = color.r;
            nebulaColors[i * 3 + 1] = color.g;
            nebulaColors[i * 3 + 2] = color.b;
        }

        nebulaGeometry.setAttribute('position', new THREE.BufferAttribute(nebulaPositions, 3));
        nebulaGeometry.setAttribute('color', new THREE.BufferAttribute(nebulaColors, 3));

        const nebulaMaterial = new THREE.PointsMaterial({
            size: 260,
            map: nebulaTexture,
            transparent: true,
            opacity: 0.18,
            vertexColors: true,
            depthWrite: false,
            blending: THREE.AdditiveBlending
        });
        const nebula = new THREE.Points(nebulaGeometry, nebulaMaterial);
        galaxyGroup.add(nebula);

        // 3. Floating Gold Rupee Coins Arc (Realism from code.html)
        const createRupeeFaceTexture = () => {
            const cvs = document.createElement('canvas');
            cvs.width = 256;
            cvs.height = 256;
            const ctx = cvs.getContext('2d');
            ctx.fillStyle = '#ffcf40';
            ctx.fillRect(0, 0, 256, 256);
            ctx.beginPath();
            ctx.arc(128, 128, 118, 0, Math.PI * 2);
            ctx.lineWidth = 8;
            ctx.strokeStyle = '#e6b800';
            ctx.stroke();
            ctx.fillStyle = 'rgba(153, 115, 0, 0.7)';
            ctx.font = 'bold 130px Inter, sans-serif';
            ctx.textAlign = 'center';
            ctx.textBaseline = 'middle';
            ctx.fillText('₹', 128, 142);
            ctx.fillStyle = '#fff0b3';
            ctx.fillText('₹', 128, 138);
            return new THREE.CanvasTexture(cvs);
        };

        const coinFaceTex = createRupeeFaceTexture();
        const coinGeometry = new THREE.CylinderGeometry(18, 18, 2.5, 32);
        const edgeMaterial = new THREE.MeshBasicMaterial({ color: 0xffd700 });
        const faceMaterial = new THREE.MeshBasicMaterial({ map: coinFaceTex });
        const coinMaterials = [edgeMaterial, faceMaterial, faceMaterial];

        const coinGroup = new THREE.Group();
        scene.add(coinGroup);

        for (let i = 0; i < 6; i++) {
            const coin = new THREE.Mesh(coinGeometry, coinMaterials);
            const angle = (i / 6) * Math.PI - Math.PI / 2;
            const radius = 380;
            coin.position.x = Math.cos(angle) * radius;
            coin.position.y = Math.sin(angle) * radius * 0.45;
            coin.position.z = -180 + i * 20;
            coin.rotation.x = Math.random() * Math.PI;
            coin.rotation.y = Math.random() * Math.PI;
            coinGroup.add(coin);
        }

        // Mouse Parallax
        let mouseX = 0, mouseY = 0;
        let targetX = 0, targetY = 0;
        const windowHalfX = window.innerWidth / 2;
        const windowHalfY = window.innerHeight / 2;

        document.addEventListener('mousemove', (event) => {
            mouseX = (event.clientX - windowHalfX) * 0.08;
            mouseY = (event.clientY - windowHalfY) * 0.08;
        });

        // Animation Loop
        let time = 0;
        const animate = () => {
            requestAnimationFrame(animate);

            targetX = mouseX;
            targetY = mouseY;
            camera.position.x += (targetX - camera.position.x) * 0.04;
            camera.position.y += (-targetY - camera.position.y) * 0.04;
            camera.lookAt(scene.position);

            galaxyGroup.rotation.y += 0.0003;
            galaxyGroup.rotation.z += 0.0001;

            time += 0.01;
            coinGroup.children.forEach((coin, i) => {
                coin.rotation.x += 0.006;
                coin.rotation.y += 0.01;
                coin.position.y += Math.sin(time + i) * 0.25;
            });

            renderer.render(scene, camera);
        };
        animate();

        window.addEventListener('resize', () => {
            camera.aspect = window.innerWidth / window.innerHeight;
            camera.updateProjectionMatrix();
            renderer.setSize(window.innerWidth, window.innerHeight);
        });
    } catch (err) {
        console.warn("Galaxy background init:", err);
    }
}

// 8. Subtle 3D Particle Mesh Background (Login / Register Screens)
function init3DParticleBackground() {
    const canvas = document.getElementById('particle3dCanvas');
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    let width = canvas.width = canvas.offsetWidth;
    let height = canvas.height = canvas.offsetHeight;

    window.addEventListener('resize', () => {
        if (canvas) {
            width = canvas.width = canvas.offsetWidth;
            height = canvas.height = canvas.offsetHeight;
        }
    });

    const particles = [];
    const particleCount = Math.min(width > 600 ? 40 : 20, 50);

    for (let i = 0; i < particleCount; i++) {
        particles.push({
            x: Math.random() * width,
            y: Math.random() * height,
            z: Math.random() * 2 + 1,
            vx: (Math.random() - 0.5) * 0.6,
            vy: (Math.random() - 0.5) * 0.6
        });
    }

    function animateParticles() {
        ctx.clearRect(0, 0, width, height);

        ctx.fillStyle = "rgba(0, 212, 170, 0.5)";
        ctx.strokeStyle = "rgba(255, 255, 255, 0.08)";

        for (let i = 0; i < particles.length; i++) {
            const p = particles[i];
            p.x += p.vx;
            p.y += p.vy;

            if (p.x < 0) p.x = width;
            if (p.x > width) p.x = 0;
            if (p.y < 0) p.y = height;
            if (p.y > height) p.y = 0;

            ctx.beginPath();
            ctx.arc(p.x, p.y, p.z * 1.5, 0, Math.PI * 2);
            ctx.fill();

            // Connect nearby nodes
            for (let j = i + 1; j < particles.length; j++) {
                const p2 = particles[j];
                const dx = p.x - p2.x;
                const dy = p.y - p2.y;
                const dist = Math.sqrt(dx * dx + dy * dy);

                if (dist < 90) {
                    ctx.beginPath();
                    ctx.moveTo(p.x, p.y);
                    ctx.lineTo(p2.x, p2.y);
                    ctx.stroke();
                }
            }
        }

        requestAnimationFrame(animateParticles);
    }

    animateParticles();
}

// 9. Print helper for Receipts & Passbook
function printSection(elementId) {
    const printContent = document.getElementById(elementId).innerHTML;
    const originalContent = document.body.innerHTML;
    document.body.innerHTML = printContent;
    window.print();
    document.body.innerHTML = originalContent;
    location.reload();
}
