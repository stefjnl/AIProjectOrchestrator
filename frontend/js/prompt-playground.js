// Prompt Playground JavaScript
class PromptPlayground {
    constructor() {
        this.templates = [];
        this.currentTemplate = null;
        this.isEditMode = false;
        this.init();
    }

    async init() {
        this.bindEvents();
        await this.loadTemplates();
        this.loadFirstTemplate();
    }

    bindEvents() {
        document.getElementById('newTemplateBtn').addEventListener('click', () => this.newTemplate());
        document.getElementById('saveTemplateBtn').addEventListener('click', () => this.saveTemplate());
        document.getElementById('cancelBtn').addEventListener('click', () => this.cancelEdit());
        document.getElementById('generateBtn').addEventListener('click', () => this.generateResponse());
        document.getElementById('copyResponseBtn').addEventListener('click', () => this.copyResponse());
        document.getElementById('templateTitle').addEventListener('input', () => this.toggleSaveButton());
        document.getElementById('templateContent').addEventListener('input', () => this.toggleSaveButton());
    }

    async loadTemplates() {
        try {
            this.showLoading('templateList');
            this.templates = await APIClient.getPromptTemplates();
            this.renderTemplateList();
        } catch (error) {
            this.showToast('Failed to load templates', 'error');
            console.error('Load templates error:', error);
        } finally {
            this.hideLoading('templateList');
        }
    }

    renderTemplateList() {
        const list = document.getElementById('templateList');
        list.innerHTML = '';

        this.templates.forEach(template => {
            const li = document.createElement('li');
            li.className = this.currentTemplate?.id === template.id ? 'active' : '';
            li.setAttribute('role', 'listitem');
            li.innerHTML = `
                <div class="template-item">
                    <span class="template-title" onclick="promptPlayground.selectTemplate('${template.id}')">${template.title}</span>
                    <div class="template-actions">
                        <button class="btn-edit" onclick="promptPlayground.editTemplate('${template.id}')" aria-label="Edit template" title="Edit">
                            ✏️
                        </button>
                        <button class="btn-delete" onclick="promptPlayground.deleteTemplate('${template.id}')" aria-label="Delete template" title="Delete">
                            🗑️
                        </button>
                    </div>
                </div>
            `;
            list.appendChild(li);
        });
    }

    async selectTemplate(id) {
        this.currentTemplate = this.templates.find(t => t.id === id);
        this.loadTemplateToEditor();
        this.isEditMode = false;
        this.updateUIState();
    }

    loadTemplateToEditor() {
        if (this.currentTemplate) {
            document.getElementById('templateTitle').value = this.currentTemplate.title;
            document.getElementById('templateContent').value = this.currentTemplate.content;
            document.getElementById('currentTemplateId').value = this.currentTemplate.id;
        }
        this.toggleSaveButton();
    }

    newTemplate() {
        this.currentTemplate = null;
        document.getElementById('templateTitle').value = '';
        document.getElementById('templateContent').value = '';
        document.getElementById('currentTemplateId').value = '';
        this.isEditMode = false;
        this.updateUIState();
        document.getElementById('templateTitle').focus();
    }

    editTemplate(id) {
        this.selectTemplate(id);
        this.isEditMode = true;
        this.updateUIState();
    }

    async saveTemplate() {
        const title = document.getElementById('templateTitle').value.trim();
        const content = document.getElementById('templateContent').value.trim();
        const id = document.getElementById('currentTemplateId').value.trim();

        if (!title) {
            this.showToast('Title is required', 'error');
            return;
        }

        // For new templates, don't send id (let backend generate it)
        // For existing templates, ensure id is a valid GUID
        const templateData = {
            title,
            content
        };

        if (id) {
            templateData.id = id;
        }

        try {
            this.showLoading('saveTemplateBtn');
            let savedTemplate;

            if (id) {
                // Update existing
                savedTemplate = await APIClient.post('/PromptTemplates', templateData);
                this.showToast('Template updated successfully', 'success');
            } else {
                // Create new
                savedTemplate = await APIClient.post('/PromptTemplates', templateData);
                this.showToast('Template created successfully', 'success');
                this.templates.unshift(savedTemplate); // Add to beginning of list
            }

            this.currentTemplate = savedTemplate;
            document.getElementById('currentTemplateId').value = savedTemplate.id;
            this.renderTemplateList();
            this.isEditMode = false;
            this.updateUIState();
        } catch (error) {
            this.showToast('Failed to save template', 'error');
            console.error('Save template error:', error);
        } finally {
            this.hideLoading('saveTemplateBtn');
        }
    }

    async deleteTemplate(id) {
        const template = this.templates.find(t => t.id === id);
        if (!template) return;

        if (!confirm(`Delete template "${template.title}"? This cannot be undone.`)) {
            return;
        }

        try {
            await APIClient.delete(`/PromptTemplates/${id}`);
            this.templates = this.templates.filter(t => t.id !== id);
            this.renderTemplateList();

            if (this.currentTemplate?.id === id) {
                this.newTemplate();
            }

            this.showToast('Template deleted successfully', 'success');
        } catch (error) {
            this.showToast('Failed to delete template', 'error');
            console.error('Delete template error:', error);
        }
    }

    cancelEdit() {
        if (this.currentTemplate) {
            this.loadTemplateToEditor();
        } else {
            this.newTemplate();
        }
        this.isEditMode = false;
        this.updateUIState();
    }

    async generateResponse() {
        const content = document.getElementById('templateContent').value.trim();
        if (!content) {
            this.showToast('Please enter a prompt', 'error');
            return;
        }

        try {
            this.showLoading('generateBtn');
            this.showLoading('responseArea');

            const response = await APIClient.post('/ai/generate', {
                prompt: content,
                // model is optional as per API spec
            });

            // Handle the AI response properly
            let responseText = '';
            if (typeof response === 'string') {
                responseText = response;
            } else if (response && response.content) {
                responseText = response.content;
            } else if (response && response.Content) {
                responseText = response.Content;
            } else {
                responseText = JSON.stringify(response, null, 2);
            }

            document.getElementById('responseContent').textContent = responseText;
            document.getElementById('copyResponseBtn').classList.remove('hidden');
            this.showToast('Response generated successfully', 'success');
        } catch (error) {
            document.getElementById('responseContent').textContent = 'Error generating response: ' + error.message;
            this.showToast('Failed to generate response', 'error');
            console.error('Generate response error:', error);
        } finally {
            this.hideLoading('generateBtn');
            this.hideLoading('responseArea');
        }
    }

    copyResponse() {
        const responseContent = document.getElementById('responseContent').textContent;
        navigator.clipboard.writeText(responseContent).then(() => {
            this.showToast('Response copied to clipboard', 'success');
        }).catch(err => {
            this.showToast('Failed to copy response', 'error');
            console.error('Copy error:', err);
        });
    }

    loadFirstTemplate() {
        if (this.templates.length > 0) {
            this.selectTemplate(this.templates[0].id);
        }
    }

    updateUIState() {
        const saveBtn = document.getElementById('saveTemplateBtn');
        const cancelBtn = document.getElementById('cancelBtn');

        if (this.isEditMode) {
            saveBtn.textContent = 'Update Template';
            cancelBtn.classList.remove('hidden');
        } else {
            saveBtn.textContent = 'Save Template';
            cancelBtn.classList.add('hidden');
        }

        this.toggleSaveButton();
        this.renderTemplateList();
    }

    toggleSaveButton() {
        const title = document.getElementById('templateTitle').value.trim();
        const saveBtn = document.getElementById('saveTemplateBtn');
        saveBtn.disabled = !title;
    }

    showLoading(elementId) {
        const element = document.getElementById(elementId);
        if (element) {
            element.classList.add('loading');
            element.disabled = true;
        }

        if (elementId === 'responseArea') {
            document.getElementById('responseLoading').classList.remove('hidden');
            document.getElementById('responseContent').textContent = '';
            document.getElementById('copyResponseBtn').classList.add('hidden');
        }
    }

    hideLoading(elementId) {
        const element = document.getElementById(elementId);
        if (element) {
            element.classList.remove('loading');
            element.disabled = false;
        }

        if (elementId === 'responseArea') {
            document.getElementById('responseLoading').classList.add('hidden');
        }
    }

    showToast(message, type = 'info') {
        const container = document.getElementById('toastContainer');
        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;
        toast.textContent = message;
        container.appendChild(toast);

        setTimeout(() => {
            toast.classList.add('show');
        }, 100);

        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => {
                container.removeChild(toast);
            }, 300);
        }, 3000);
    }
}

// Global access for event handlers
const promptPlayground = new PromptPlayground();

// Expose to global scope for onclick handlers
window.promptPlayground = promptPlayground;