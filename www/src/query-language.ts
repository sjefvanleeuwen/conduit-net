import './styles/main.scss';
import './components/conduit-nav/conduit-nav';
import './components/conduit-footer/conduit-footer';
import './components/conduit-sidebar/conduit-sidebar';
import './components/conduit-hero';
import mermaid from 'mermaid';

console.log('ConduitNet Query Language Loaded');

mermaid.initialize({
    startOnLoad: true,
    theme: 'dark',
});
