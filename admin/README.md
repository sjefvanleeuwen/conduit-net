# Conduit Admin

This is the admin interface for Conduit.

## Setup

1. Install dependencies:
   ```bash
   npm install
   ```

2. Run in development mode:
   ```bash
   npm run dev
   ```

3. Build for production:
   ```bash
   npm run build
   ```

## Architecture

- **Vite**: Build tool.
- **TypeScript**: Language.
- **Web Components**: Custom Elements for UI components (`admin-layout`, `admin-sidebar`, etc.).
- **Conduit Client**: Uses `conduit-ts-client` to communicate with the backend.

## Styling

Styles are defined in `src/style.css` using CSS variables for theming. The theme matches the main website's dark mode.
