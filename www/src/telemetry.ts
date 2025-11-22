import './styles/main.scss';
import './components/conduit-footer/conduit-footer';
import './components/conduit-hero';
import mermaid from 'mermaid';

console.log('ConduitNet Telemetry Page Loaded');

mermaid.initialize({
    startOnLoad: true,
    theme: 'dark',
});
