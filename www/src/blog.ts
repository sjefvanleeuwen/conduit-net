import './styles/main.scss';
import './components/conduit-footer/conduit-footer';
import mermaid from 'mermaid';

console.log('ConduitNet Blog Loaded');

mermaid.initialize({
    startOnLoad: true,
    theme: 'dark',
});
