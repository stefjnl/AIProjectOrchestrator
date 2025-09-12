# CSS Architecture Guidelines

## Naming Conventions

### BEM Methodology
We use a modified BEM (Block Element Modifier) approach:

- **Block**: Component name (`.card`, `.button`, `.modal`)
- **Element**: Component part (`.card__header`, `.button__icon`)
- **Modifier**: Component variant (`.card--highlighted`, `.button--primary`)

### Examples:
```css
/* Block */
.btn { }

/* Element */
.btn__icon { }

/* Modifier */
.btn--primary { }
.btn--large { }
```

### Utility Classes
Follow a functional naming pattern:

```css
/* Spacing */
.mt-sm { margin-top: var(--spacing-sm); }
.mb-lg { margin-bottom: var(--spacing-lg); }

/* Display */
.d-flex { display: flex; }
.d-block { display: block; }

/* Text Alignment */
.text-center { text-align: center; }
.text-left { text-align: left; }
```

## File Organization

### Core Files
- `variables.css`: CSS custom properties only
- `reset.css`: Base HTML element styles
- `typography.css`: Font styles and heading hierarchy

### Component Files
- `buttons.css`: All button variants and states
- `cards.css`: Card components and variations
- `forms.css`: Form inputs, labels, and validation
- `modals.css`: Modal dialogs and overlays
- `navigation.css`: Header, nav items, and menus
- `loading.css`: Spinners and loading states

### Layout Files
- `containers.css`: Page layout containers
- `footer.css`: Footer component
- `responsive.css`: All media queries

### Page-Specific Files
- `playground.css`: Prompt playground interface
- `stories.css`: Stories overview page
- `workflow.css`: Workflow pipeline visualization

### Utility Files
- `animations.css`: Keyframes and transitions
- `helpers.css`: Utility classes

## CSS Custom Properties (Variables)

### Color System
```css
:root {
  /* Primary Brand Colors */
  --color-primary-50: #eff6ff;
  --color-primary-100: #dbeafe;
  --color-primary-500: #3b82f6;
  --color-primary-600: #2563eb;
  --color-primary-700: #1d4ed8;
  --color-primary-900: #1e3a8a;

  /* Semantic Colors */
  --color-success-50: #ecfdf5;
  --color-success-500: #10b981;
  --color-success-600: #059669;
  --color-warning-500: #f59e0b;
  --color-danger-500: #ef4444;

  /* Neutral Grays */
  --color-gray-25: #fcfcfd;
  --color-gray-50: #f9fafb;
  --color-gray-100: #f3f4f6;
  --color-gray-200: #e5e7eb;
  --color-gray-300: #d1d5db;
  --color-gray-400: #9ca3af;
  --color-gray-500: #6b7280;
  --color-gray-600: #4b5563;
  --color-gray-700: #374151;
  --color-gray-800: #1f2937;
  --color-gray-900: #111827;
}
```

### Typography System
```css
:root {
  /* Font Families */
  --font-sans: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  --font-mono: 'JetBrains Mono', 'Fira Code', Consolas, 'Courier New', monospace;

  /* Font Sizes */
  --text-xs: 0.75rem;
  --text-sm: 0.875rem;
  --text-base: 1rem;
  --text-lg: 1.125rem;
  --text-xl: 1.25rem;
  --text-2xl: 1.5rem;
  --text-3xl: 1.875rem;
  --text-4xl: 2.25rem;

  /* Font Weights */
  --font-light: 300;
  --font-normal: 400;
  --font-medium: 500;
  --font-semibold: 600;
  --font-bold: 700;
}
```

### Spacing System
```css
:root {
  --spacing-xs: 0.25rem;
  --spacing-sm: 0.5rem;
  --spacing-md: 1rem;
  --spacing-lg: 1.5rem;
  --spacing-xl: 2rem;
  --spacing-2xl: 3rem;
}
```

## Best Practices

### 1. Use CSS Variables
Always use CSS custom properties for colors, spacing, and typography:
```css
/* Good */
.card {
  padding: var(--spacing-md);
  background: var(--color-white);
  border: 1px solid var(--color-gray-200);
}

/* Bad */
.card {
  padding: 16px;
  background: #ffffff;
  border: 1px solid #e5e7eb;
}
```

### 2. Mobile-First Approach
Write base styles for mobile, then enhance for larger screens:
```css
.component {
  /* Mobile styles */
  padding: var(--spacing-sm);
}

@media (min-width: 768px) {
  .component {
    /* Tablet and up */
    padding: var(--spacing-md);
  }
}
```

### 3. Avoid Over-Specificity
Keep selectors simple and avoid deep nesting:
```css
/* Good */
.btn--primary:hover { }

/* Bad */
.card .card__actions .btn.btn--primary:hover { }
```

### 4. Consistent Transitions
Use consistent timing and easing:
```css
.component {
  transition: all 0.2s ease;
}
```

### 5. Component Isolation
Each component should be self-contained:
```css
/* Button component should work anywhere */
.btn {
  /* Only button-specific styles */
}

/* Not dependent on parent context */
.card .btn { /* Avoid this */ }
```

## File Import Order

The main CSS file should import files in this order:

1. Core variables and reset
2. Base typography
3. Layout components
4. UI components
5. Page-specific styles
6. Utility classes
7. Animations

```css
/* main.css */
@import url('core/variables.css');
@import url('core/reset.css');
@import url('core/typography.css');
@import url('layouts/containers.css');
@import url('components/buttons.css');
@import url('components/cards.css');
@import url('components/forms.css');
@import url('components/navigation.css');
@import url('layouts/footer.css');
@import url('pages/playground.css');
@import url('pages/stories.css');
@import url('pages/workflow.css');
@import url('utilities/helpers.css');
@import url('utilities/animations.css');
@import url('layouts/responsive.css');
```

## Migration Strategy

### Phase 1: Create New Structure
1. Create directory structure
2. Extract clean components (no duplicates)
3. Create individual files

### Phase 2: Eliminate Duplicates
1. Remove redundant `.story-status` and `.story-priority` rules
2. Consolidate button hover effects
3. Merge duplicate responsive styles

### Phase 3: Testing & Validation
1. Visual regression testing
2. Cross-browser compatibility
3. Performance validation

### Phase 4: Documentation
1. Update component documentation
2. Create usage examples
3. Establish maintenance guidelines