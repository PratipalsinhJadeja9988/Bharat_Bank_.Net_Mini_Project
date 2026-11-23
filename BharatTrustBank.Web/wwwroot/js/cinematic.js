// Custom Cursor Logic
const cursorDot = document.getElementById('cursor-dot');
const cursorRing = document.getElementById('cursor-ring');

if (cursorDot && cursorRing) {
    document.addEventListener('mousemove', (e) => {
        requestAnimationFrame(() => {
            cursorDot.style.left = e.clientX + 'px';
            cursorDot.style.top = e.clientY + 'px';
            // Slight delay for ring to create trailing effect
            setTimeout(() => {
                cursorRing.style.left = e.clientX + 'px';
                cursorRing.style.top = e.clientY + 'px';
            }, 50);
        });
    });

    // Hover effects on cursor
    document.querySelectorAll('a, button, .cursor-pointer').forEach(el => {
        el.addEventListener('mouseenter', () => {
            cursorRing.style.width = '48px';
            cursorRing.style.height = '48px';
            cursorRing.style.borderColor = 'rgba(235, 225, 207, 0.8)'; // tertiary color
            cursorDot.style.backgroundColor = '#ebe1cf';
        });
        el.addEventListener('mouseleave', () => {
            cursorRing.style.width = '32px';
            cursorRing.style.height = '32px';
            cursorRing.style.borderColor = 'rgba(65, 238, 194, 0.5)'; // secondary color
            cursorDot.style.backgroundColor = '#41eec2';
        });
    });
}

// Three.js Background
const initThreeJS = () => {
    const canvas = document.getElementById('bg-canvas');
    if (!canvas) return;
    
    const scene = new THREE.Scene();
    scene.fog = new THREE.FogExp2(0x000000, 0.001);

    const camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.1, 2000);
    camera.position.z = 400;

    const renderer = new THREE.WebGLRenderer({ canvas, alpha: true, antialias: true });
    renderer.setSize(window.innerWidth, window.innerHeight);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));

    const galaxyGroup = new THREE.Group();
    scene.add(galaxyGroup);

    // 1. Enhance the star field
    const starGeometry = new THREE.BufferGeometry();
    const starCount = 8000;
    const starPositions = new Float32Array(starCount * 3);
    const starColors = new Float32Array(starCount * 3);
    const starSizes = new Float32Array(starCount);

    const colorPalette = [
        new THREE.Color(0xffffff), // White
        new THREE.Color(0x88ccff), // Soft blue
        new THREE.Color(0xffddaa)  // Pale gold
    ];

    for(let i=0; i<starCount; i++) {
        starPositions[i*3] = (Math.random() - 0.5) * 2000;
        starPositions[i*3+1] = (Math.random() - 0.5) * 2000;
        starPositions[i*3+2] = (Math.random() - 0.5) * 2000;

        const color = colorPalette[Math.floor(Math.random() * colorPalette.length)];
        starColors[i*3] = color.r;
        starColors[i*3+1] = color.g;
        starColors[i*3+2] = color.b;

        starSizes[i] = Math.random() * 1.5;
    }

    starGeometry.setAttribute('position', new THREE.BufferAttribute(starPositions, 3));
    starGeometry.setAttribute('color', new THREE.BufferAttribute(starColors, 3));
    starGeometry.setAttribute('size', new THREE.BufferAttribute(starSizes, 1));

    const starMaterial = new THREE.PointsMaterial({
        size: 1.5,
        vertexColors: true,
        transparent: true,
        opacity: 0.8,
        sizeAttenuation: true
    });
    const stars = new THREE.Points(starGeometry, starMaterial);
    galaxyGroup.add(stars);

    // 2. Add 'Milky Way' nebula
    const createNebulaTexture = () => {
        const canvas = document.createElement('canvas');
        canvas.width = 64;
        canvas.height = 64;
        const context = canvas.getContext('2d');
        const gradient = context.createRadialGradient(32, 32, 0, 32, 32, 32);
        gradient.addColorStop(0, 'rgba(255,255,255,1)');
        gradient.addColorStop(0.2, 'rgba(255,255,255,0.8)');
        gradient.addColorStop(0.5, 'rgba(255,255,255,0.2)');
        gradient.addColorStop(1, 'rgba(0,0,0,0)');
        context.fillStyle = gradient;
        context.fillRect(0, 0, 64, 64);
        return new THREE.CanvasTexture(canvas);
    };

    const nebulaTexture = createNebulaTexture();
    const nebulaGeometry = new THREE.BufferGeometry();
    const nebulaCount = 500;
    const nebulaPositions = new Float32Array(nebulaCount * 3);
    const nebulaColors = new Float32Array(nebulaCount * 3);

    const nebulaColorsList = [
        new THREE.Color(0x0a1930), // Midnight blue
        new THREE.Color(0x3a1050), // Subtle purple
        new THREE.Color(0x4a3b2c)  // Cosmic dust brown
    ];

    for(let i=0; i<nebulaCount; i++) {
        // Span diagonally
        const spread = Math.random() * 800 - 400;
        nebulaPositions[i*3] = (Math.random() - 0.5) * 1500;
        nebulaPositions[i*3+1] = nebulaPositions[i*3] * 0.5 + spread;
        nebulaPositions[i*3+2] = (Math.random() - 0.5) * 500 - 200;

        const color = nebulaColorsList[Math.floor(Math.random() * nebulaColorsList.length)];
        nebulaColors[i*3] = color.r;
        nebulaColors[i*3+1] = color.g;
        nebulaColors[i*3+2] = color.b;
    }

    nebulaGeometry.setAttribute('position', new THREE.BufferAttribute(nebulaPositions, 3));
    nebulaGeometry.setAttribute('color', new THREE.BufferAttribute(nebulaColors, 3));

    const nebulaMaterial = new THREE.PointsMaterial({
        size: 250,
        map: nebulaTexture,
        transparent: true,
        opacity: 0.15,
        vertexColors: true,
        depthWrite: false,
        blending: THREE.AdditiveBlending
    });
    const nebula = new THREE.Points(nebulaGeometry, nebulaMaterial);
    galaxyGroup.add(nebula);

    // 3. Shining stars
    const createFlareTexture = () => {
        const canvas = document.createElement('canvas');
        canvas.width = 64;
        canvas.height = 64;
        const context = canvas.getContext('2d');
        context.fillStyle = 'black';
        context.fillRect(0,0,64,64);
        const gradient = context.createRadialGradient(32, 32, 0, 32, 32, 32);
        gradient.addColorStop(0, 'rgba(255, 255, 255, 1)');
        gradient.addColorStop(0.2, 'rgba(255, 255, 255, 0.5)');
        gradient.addColorStop(1, 'rgba(0, 0, 0, 0)');
        context.fillStyle = gradient;
        context.fillRect(0, 0, 64, 64);
        
        // Cross shape
        context.fillStyle = 'rgba(255,255,255,0.8)';
        context.fillRect(31, 10, 2, 44);
        context.fillRect(10, 31, 44, 2);
        return new THREE.CanvasTexture(canvas);
    };

    const flareTexture = createFlareTexture();
    const shiningCount = 15;
    for(let i=0; i<shiningCount; i++) {
        const spriteMaterial = new THREE.SpriteMaterial({
            map: flareTexture,
            color: 0xffffff,
            transparent: true,
            blending: THREE.AdditiveBlending
        });
        const sprite = new THREE.Sprite(spriteMaterial);
        sprite.position.set(
            (Math.random() - 0.5) * 1000,
            (Math.random() - 0.5) * 1000,
            (Math.random() - 0.5) * 500
        );
        const size = Math.random() * 20 + 10;
        sprite.scale.set(size, size, 1);
        galaxyGroup.add(sprite);
    }

    // 4. Gold coin arc with enhanced realism
    const ambientLight = new THREE.AmbientLight(0xffffff, 0.4);
    scene.add(ambientLight);

    const dirLight = new THREE.DirectionalLight(0xfff5e6, 2.5); // Warm key light
    dirLight.position.set(300, 500, 200);
    scene.add(dirLight);

    const fillLight = new THREE.PointLight(0x41eec2, 1, 1000); // Cool fill light matching brand
    fillLight.position.set(-300, -100, 100);
    scene.add(fillLight);

    // High-quality Rupee Texture
    const createRupeeFaceTexture = () => {
        const cvs = document.createElement('canvas');
        cvs.width = 512;
        cvs.height = 512;
        const ctx = cvs.getContext('2d');
        
        // Base Gold Background
        ctx.fillStyle = '#ffcf40';
        ctx.fillRect(0, 0, 512, 512);
        
        // Outer Ring Ridge
        ctx.beginPath();
        ctx.arc(256, 256, 240, 0, Math.PI * 2);
        ctx.lineWidth = 15;
        ctx.strokeStyle = '#e6b800';
        ctx.stroke();

        // Inner Detail Ring
        ctx.beginPath();
        ctx.arc(256, 256, 220, 0, Math.PI * 2);
        ctx.lineWidth = 4;
        ctx.strokeStyle = '#cca300';
        ctx.setLineDash([10, 10]);
        ctx.stroke();
        ctx.setLineDash([]);

        // Rupee Symbol Shadow (Emboss effect)
        ctx.fillStyle = 'rgba(153, 115, 0, 0.6)';
        ctx.font = 'bold 260px Inter, sans-serif';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillText('₹', 256, 286); // Offset shadow

        // Rupee Symbol
        ctx.fillStyle = '#fff0b3'; // Bright gold highlight
        ctx.fillText('₹', 256, 276);
        
        const texture = new THREE.CanvasTexture(cvs);
        texture.anisotropy = renderer.capabilities.getMaxAnisotropy();
        return texture;
    };

    const coinFaceTex = createRupeeFaceTexture();
    const coinGeometry = new THREE.CylinderGeometry(20, 20, 3, 64);
    
    // Refined Materials
    const edgeMaterial = new THREE.MeshStandardMaterial({
        color: 0xffd700,
        metalness: 1.0,
        roughness: 0.15,
    });
    const faceMaterial = new THREE.MeshStandardMaterial({
        color: 0xffe066,
        metalness: 0.9,
        roughness: 0.2,
        map: coinFaceTex,
    });

    const materials = [edgeMaterial, faceMaterial, faceMaterial];
    const coinGroup = new THREE.Group();
    scene.add(coinGroup);

    for(let i=0; i<8; i++) {
        const coin = new THREE.Mesh(coinGeometry, materials);
        const angle = (i / 8) * Math.PI - Math.PI / 2;
        const radius = 350;
        coin.position.x = Math.cos(angle) * radius;
        coin.position.y = Math.sin(angle) * radius * 0.5;
        coin.position.z = -150 + i * 25;
        
        coin.rotation.x = Math.random() * Math.PI;
        coin.rotation.y = Math.random() * Math.PI;
        
        coinGroup.add(coin);
    }

    // Interactive Parallax
    let mouseX = 0;
    let mouseY = 0;
    let targetX = 0;
    let targetY = 0;

    const windowHalfX = window.innerWidth / 2;
    const windowHalfY = window.innerHeight / 2;

    document.addEventListener('mousemove', (event) => {
        mouseX = (event.clientX - windowHalfX) * 0.1;
        mouseY = (event.clientY - windowHalfY) * 0.1;
    });

    // Animation Loop
    let time = 0;
    const animate = () => {
        requestAnimationFrame(animate);
        
        // Parallax easing
        targetX = mouseX;
        targetY = mouseY;
        camera.position.x += (targetX - camera.position.x) * 0.05;
        camera.position.y += (-targetY - camera.position.y) * 0.05;
        camera.lookAt(scene.position);

        // Rotate galaxy slowly
        galaxyGroup.rotation.y += 0.0003;
        galaxyGroup.rotation.z += 0.0001;

        // Animate coins
        time += 0.01;
        coinGroup.children.forEach((coin, i) => {
            coin.rotation.x += 0.005;
            coin.rotation.y += 0.01;
            coin.position.y += Math.sin(time + i) * 0.3;
        });

        // Twinkle shining stars
        galaxyGroup.children.forEach(child => {
            if (child.type === 'Sprite') {
                child.material.opacity = 0.5 + Math.sin(time * 2 + child.position.x) * 0.5;
            }
        });

        renderer.render(scene, camera);
    };
    animate();

    // Resize handler
    window.addEventListener('resize', () => {
        camera.aspect = window.innerWidth / window.innerHeight;
        camera.updateProjectionMatrix();
        renderer.setSize(window.innerWidth, window.innerHeight);
    });
};

document.addEventListener('DOMContentLoaded', () => {
    setTimeout(initThreeJS, 100);
});
