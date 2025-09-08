# Markdown Support for Project Descriptions

## Overview
This feature enables users to enter Markdown-formatted text in the Project Description field during creation and have it rendered as formatted HTML across project pages. This enhances readability for complex descriptions with headers, lists, bold/italic text, etc.

Implemented using Clean Architecture principles on the frontend (shared utilities) and leveraging existing backend string storage.

## Frontend Implementation

### Libraries Used
- **marked.js**: Markdown parser supporting GitHub Flavored Markdown (GFM).
  - CDN: `https://cdn.jsdelivr.net/npm/marked/marked.min.js`
  - Options: `{ breaks: true, gfm: true, headerIds: false }` for line breaks, GFM support, and security.
- **DOMPurify**: HTML sanitization to prevent XSS.
  - CDN: `https://cdn.jsdelivr.net/npm/dompurify@3.1.7/dist/purify.min.js`
  - Allowed tags: p, br, strong, em, u, h1-h6, ul, ol, li, blockquote, code, pre, a, img.
  - Allowed attributes: href, src, alt, title.

### Shared Utility
- File: `frontend/js/markdown-utils.js`
- Key Function: [`renderMarkdownToHTML(markdownText)`](frontend/js/markdown-utils.js:6)
  - Parses Markdown to HTML.
  - Sanitizes output.
  - Falls back to escaped plain text on errors or missing libraries.
- Includes [`escapeHTML(str)`](frontend/js/markdown-utils.js:28) for compatibility.

### Changes by File
- **create.html**:
  - Added placeholder with Markdown examples.
  - Real-time preview div below textarea.
  - Event listener on input: Updates preview with rendered HTML.
- **list.html**, **workflow.html**, **stories-overview.html**:
  - Added CDN scripts in `<head>`.
  - Updated description display: `innerHTML = renderMarkdownToHTML(description)` instead of `textContent`.
  - Added CSS class `project-description` for styling (add to styles.css: `line-height: 1.5;`).

## Backend Compatibility
- **Entity**: [`Project.Description`](src/AIProjectOrchestrator.Domain/Entities/Project.cs:7) is `string` – stores raw Markdown.
- **API**: [`ProjectsController`](src/AIProjectOrchestrator.API/Controllers/ProjectsController.cs:1) returns raw string via JSON serialization.
- No changes needed; supports Markdown out-of-the-box.

## Security Considerations
- **XSS Prevention**: DOMPurify strips dangerous tags/attributes (e.g., script, onclick).
- **Restricted Features**: No auto-header IDs; limited tags to essentials.
- **Fallbacks**: Errors render as escaped text.
- **Testing**: Verified with malicious input (e.g., `<script>alert(1)</script>`) – stripped safely.

## Usage Examples
- Input: `# My Project\n**Bold** text with a [link](https://example.com).`
- Rendered: `<h1>My Project</h1><p><strong>Bold</strong> text with a <a href="https://example.com">link</a>.</p>`

## Testing
- Create project with Markdown.
- Verify rendering on all pages.
- Test sanitization with invalid HTML.

## Maintenance
- Update CDNs for library versions.
- Monitor bundle size (~50KB added).
- Extend allowed tags if needed via DOMPurify config.