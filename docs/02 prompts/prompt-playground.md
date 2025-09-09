
# **Create “Prompt Playground” UI**

> I want you to create a **new standalone razor page** called `PromptPlayground` that allows users to:
> - Generate prompts independently (not tied to projects),
> - **Create, Read, Update, and Delete prompt templates** stored in **PostgreSQL** (no localStorage or JS arrays),
> - Send prompts to the LLM and view responses.

---

## 🎯 Core Features

### 1. **Left Sidebar — Template Manager (CRUD from DB)**
- Title: “Prompt Templates”
- Fetch templates from `GET /api/prompt-templates` on load.
- Display as a list with:
  - Template title (clickable → loads into editor)
  - ✏️ Edit icon (inline or on hover)
  - 🗑️ Delete icon (with confirmation)
- “+ New Template” button at top → clears editor + sets “Create Mode”.

### 2. **Main Editor Area**
- Input: `Template Title` (text field, required)
- Input: `Prompt Content` (large textarea, monospace font)
- Buttons:
  - “Generate” → sends `content` to LLM via `APIClient.generatePrompt()`
  - “Save Template” → creates new or updates existing (based on hidden `templateId`)
  - “Cancel” → reverts to last saved or selected template
- Response area (read-only) below editor → LLM output
- “Copy” button next to response
- Loading states + toasts for success/error

### 3. **Edit & Delete Behavior**
- **Edit**:
  - Click ✏️ → enables editing of that template in the editor.
  - “Save Template” updates the existing record.
- **Delete**:
  - Click 🗑️ → show confirmation: “Delete template ‘X’? This cannot be undone.”
  - On confirm → `DELETE /api/prompt-templates/{id}`
  - Remove from UI + show success toast.
  - If currently loaded in editor → clear editor or load next template.

### 4. **Backend (PostgreSQL + .NET)** ALREADY IN PLACE
- Entity: `PromptTemplate`
  - `Id: Guid`
  - `Title: string`
  - `Content: string`
  - `CreatedAt: DateTime`
  - `UpdatedAt: DateTime?`
- API Endpoints:
  - `GET /api/prompt-templates` → returns all
  - `POST /api/prompt-templates` → create or update (if `id` provided)
  - `DELETE /api/prompt-templates/{id}` → delete by ID
- EF Core + Clean Architecture:
  - Domain → `PromptTemplate.cs`, `IPromptTemplateRepository.cs`
  - Application → `PromptTemplateService.cs`, `IPromptTemplateService.cs`
  - Infrastructure → `PromptTemplateRepository.cs`
  - API → `PromptTemplatesController.cs`
- Add migration to create `PromptTemplates` table.

### 5. **Frontend Files**
- `frontend/prompt-playground.html`
- `frontend/js/prompt-playground.js` — handles:
  - Fetching, displaying, selecting templates
  - Edit/delete UI + API calls
  - Saving (create/update)
  - LLM generation + response handling
- Reuse `APIClient.js` and existing CSS.

### 6. **UX & Responsiveness**
- On load → fetch templates → auto-select first.
- Highlight currently loaded template in sidebar.
- Disable “Save” if title is empty.
- Show loading spinners during API/LLM calls.
- Mobile: sidebar collapses to top dropdown or hamburger menu.
- Accessible: `aria-labels`, `role=button`, focus management.

---

## 📁 Deliverables

Please generate:

### ➤ Backend (.NET)
- `PromptTemplate.cs` (Domain)
- `PromptTemplateDto.cs` (optional, for API)
- `IPromptTemplateRepository.cs` + `PromptTemplateRepository.cs`
- `IPromptTemplateService.cs` + `PromptTemplateService.cs`
- `PromptTemplatesController.cs` (with GET, POST, DELETE)
- Migration to create `PromptTemplates` table
- Example seed data (optional)


### ➤ API Contract Examples
```http
GET /api/prompt-templates
→ [{ "id": "...", "title": "Email Draft", "content": "Write a professional email..." }]

POST /api/prompt-templates
→ { "id": null, "title": "New Template", "content": "..." } → creates
→ { "id": "guid", "title": "...", "content": "..." } → updates

DELETE /api/prompt-templates/{id}
→ 204 No Content on success
```

---

## ✅ Key Principles
- **Follow existing architecture** — reuse patterns from Reviews/Projects.
- **Optimistic UI updates** — update UI immediately on success, rollback on error.
- **Clean separation** — backend handles data, frontend handles presentation.