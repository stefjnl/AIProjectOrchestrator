
### 🧩 AI CODING ASSISTANT PROMPT — FRONT-END NAVIGATION & UX FIXES

> **GOAL**: Fix all broken or incomplete navigation flows, UI states, and UX notifications in the front-end (HTML/CSS/JS) to fully implement the primary user journey:  
> **Project Creation → Workflow (Stages 1–3) → Stories Management → Workflow (Completion View)**

---

## ✅ CONTEXT

You are working on a multi-stage project workflow web app. The user journey breaks after Stage 3 approval because the **Stories Management page (`stories-overview.html`)** is non-functional, and several UX/notification elements are missing. The workflow completion view is also incomplete.

The app uses:
- `workflow.html` — main workflow page with stages 1–4
- `stories-overview.html` — stories management workspace (currently broken)
- JavaScript logic in `WorkflowManager` and `checkStoriesManagementRedirect()`
- URL parameters: `?projectId=X` and should include `&source=workflow` or `&fromStories=true` for context

---

## 🛠️ TASKS TO IMPLEMENT

### 1. ✅ STORIES MANAGEMENT PAGE — MAKE FUNCTIONAL
- Implement `stories-overview.html` as a **minimal but functional workspace**:
  - Display placeholder content: “Stories Management Workspace — Under Construction” (for now)
  - Include a working **“Back to Project Workflow”** button (see label fix below)
  - Ensure it reads `projectId` from URL and preserves it in return navigation
  - Hook up the “Back to Project Workflow” button to redirect to `workflow.html?projectId=X&fromStories=true`

> 💡 *Note: Full stories functionality is out of scope — just make the page navigable and visually consistent.*

---

### 2. ✅ FIX NAVIGATION TRIGGERS & PARAMETERS

#### From `workflow.html` → `stories-overview.html`:
- In `checkStoriesManagementRedirect()`:
  - Append `&source=workflow` to the redirect URL:  
    `stories-overview.html?projectId=X&source=workflow`
  - Show a **brief toast/notification** before redirect:  
    `“Stories approved! Redirecting to prompt management...”`  
    → Display for 2 seconds, then auto-redirect.

> 💡 Use a simple `<div class="notification-toast">` with CSS fade-in/out. No libraries.

#### From `stories-overview.html` → `workflow.html`:
- Ensure the “Back to Project Workflow” button:
  - Redirects to `workflow.html?projectId=X&fromStories=true`
  - Triggers `WorkflowManager.syncWithStoriesPage()` on load (already exists — verify it works)

---

### 3. ✅ WORKFLOW COMPLETION VIEW — ENHANCE DASHBOARD

In `workflow.html`, after Stage 3 approval (Stage 4 view):

- Hide or disable all “Generate” buttons (already done ✅)
- Replace the disabled button **“Continue to Code Generation”** with:
  - Primary button: **“View All Prompts”** (links to a placeholder or scrolls to prompt section)
  - Secondary button: **“Download Results”** (triggers `alert(“Download feature coming soon.”)` for now)
- Add a **summary dashboard section** below Stage 4 with:
  - Placeholder stats:  
    `“✅ 5 Stories Approved | 📥 3 Prompts Generated | 📤 Export Ready”`
  - Simple styled cards or `<div>` blocks (no backend needed — static for now)

---

### 4. ✅ UX & LABEL FIXES

#### Button Labels:
- In `stories-overview.html`:  
  ❌ Current: `“← Back to Workflow”`  
  ✅ Change to: `“← Back to Project Workflow”`

- In `workflow.html` (Stage 4):  
  ❌ Current: `“Continue to Code Generation”` (disabled)  
  ✅ Replace with:  
    - `“View All Prompts”` (primary action)  
    - `“Download Results”` (secondary action)

#### Notifications:
- Implement a reusable toast notification system (simple, CSS-only fade):
  - Class: `.notification-toast`
  - Appears at top center, fixed position
  - Auto-hides after 2s
  - Triggered before redirect from Stage 3 → Stories page

---

### 5. ✅ CLEANUP & SIMPLIFICATION

- Remove or comment out any unused **cross-page selection persistence** logic in `WorkflowManager` unless it’s actively used.
- Ensure all navigation uses **only `projectId`, `source`, and `fromStories`** — no intent-based params.
- Confirm each page manages its own state independently (✅ already true — no changes needed).

---

## 📁 FILES TO MODIFY

- `workflow.html`
- `stories-overview.html`
- `workflow.js` (or wherever `checkStoriesManagementRedirect()` and `WorkflowManager` live)
- `styles.css` (add `.notification-toast` styles + dashboard section styles)

---

## ✅ DELIVERABLES

After implementation, the following must be true:

1. User completes Stage 3 → sees notification → auto-redirects to `stories-overview.html?projectId=X&source=workflow`
2. `stories-overview.html` displays placeholder content + working “Back to Project Workflow” button
3. Clicking “Back to Project Workflow” returns user to `workflow.html?projectId=X&fromStories=true` and triggers sync
4. Workflow Stage 4 shows:
   - Summary stats
   - “View All Prompts” and “Download Results” buttons
   - No misleading “Continue to Code Generation” button
5. All button labels match recommendations
6. Toast notification appears and auto-dismisses before redirect

---

## 🧪 TESTING SCENARIO

> 1. Start at `workflow.html?projectId=123`  
> 2. Simulate Stage 3 approval → should show toast → redirect to `stories-overview.html?projectId=123&source=workflow`  
> 3. Click “Back to Project Workflow” → returns to `workflow.html?projectId=123&fromStories=true`  
> 4. Stage 4 view shows summary stats + correct buttons  
> 5. No JS errors, broken links, or missing params

---

✅ **DO NOT** implement backend logic, story editing, or prompt generation — this is purely front-end navigation, labeling, and UI state fixes.
