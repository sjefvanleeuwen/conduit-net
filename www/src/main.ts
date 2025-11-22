import './styles/main.scss';
import './components/conduit-nav/conduit-nav';
import './components/conduit-footer/conduit-footer';

console.log('ConduitNet Website Loaded');

document.addEventListener('DOMContentLoaded', () => {
    const img = document.querySelector('#banner .image img') as HTMLImageElement;
    if (img && img.src.includes('conduit-anim.gif')) {
        // Preload the static image
        const staticImage = new Image();
        staticImage.src = '/images/conduit.png';
        
        // Swap after 5 seconds (duration of animation) to stop looping
        // Assuming conduit.png is the final frame (the logo)
        setTimeout(() => {
            img.src = staticImage.src;
        }, 5000);
    }
});

