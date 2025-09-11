/**
 * Export Service
 * Handles data export functionality for stories and prompts
 */
class ExportService {
    constructor() {
        this.defaultFileName = 'export';
    }

    /**
     * Export stories data as JSON
     * @param {Array} stories - Array of story objects
     * @param {string} generationId - Generation ID
     * @param {string} projectId - Project ID
     * @param {string} customFileName - Optional custom filename
     */
    exportStories(stories, generationId, projectId, customFileName = null) {
        if (!stories || stories.length === 0) {
            throw new Error('No stories to export');
        }

        const data = {
            generationId: generationId,
            projectId: projectId,
            exportDate: new Date().toISOString(),
            exportVersion: '1.0',
            storyCount: stories.length,
            stories: stories
        };

        const fileName = customFileName || `stories-overview-${generationId}`;
        this.downloadJSON(data, `${fileName}.json`);
    }

    /**
     * Export a single prompt as JSON
     * @param {Object} promptData - Prompt data object
     * @param {string} customFileName - Optional custom filename
     */
    exportPrompt(promptData, customFileName = null) {
        if (!promptData || !promptData.generatedPrompt) {
            throw new Error('No prompt to export');
        }

        const data = {
            promptId: promptData.promptId,
            storyTitle: promptData.storyTitle,
            generatedPrompt: promptData.generatedPrompt,
            createdAt: promptData.createdAt,
            exportedAt: new Date().toISOString(),
            exportVersion: '1.0'
        };

        const fileName = customFileName || `prompt-${promptData.promptId}`;
        this.downloadJSON(data, `${fileName}.json`);
    }

    /**
     * Export stories as CSV
     * @param {Array} stories - Array of story objects
     * @param {string} fileName - Output filename
     */
    exportStoriesAsCSV(stories, fileName = 'stories-export.csv') {
        if (!stories || stories.length === 0) {
            throw new Error('No stories to export');
        }

        const headers = ['ID', 'Title', 'Description', 'Status', 'Priority', 'Story Points', 'Has Prompt', 'Prompt ID'];
        const rows = stories.map(story => [
            story.id || '',
            story.title || 'Untitled Story',
            this.escapeCSV(story.description || ''),
            story.status || 'Draft',
            story.priority || 'Medium',
            story.storyPoints || 'N/A',
            story.hasPrompt ? 'Yes' : 'No',
            story.promptId || 'N/A'
        ]);

        const csvContent = [headers, ...rows].map(row => row.join(',')).join('\n');
        this.downloadFile(csvContent, fileName, 'text/csv');
    }

    /**
     * Export prompts as plain text
     * @param {Array} prompts - Array of prompt objects
     * @param {string} fileName - Output filename
     */
    exportPromptsAsText(prompts, fileName = 'prompts-export.txt') {
        if (!prompts || prompts.length === 0) {
            throw new Error('No prompts to export');
        }

        let textContent = `Prompt Export - ${new Date().toLocaleString()}\n`;
        textContent += '='.repeat(50) + '\n\n';

        prompts.forEach((prompt, index) => {
            textContent += `Prompt ${index + 1}: ${prompt.storyTitle || 'Untitled Story'}\n`;
            textContent += `Story ID: ${prompt.storyId || 'N/A'}\n`;
            textContent += `Created: ${new Date(prompt.createdAt).toLocaleString()}\n`;
            textContent += `Quality Score: ${prompt.qualityScore || 'N/A'}\n\n`;
            textContent += 'Generated Prompt:\n';
            textContent += '-'.repeat(30) + '\n';
            textContent += prompt.generatedPrompt + '\n\n';
            textContent += '='.repeat(50) + '\n\n';
        });

        this.downloadFile(textContent, fileName, 'text/plain');
    }

    /**
     * Download data as JSON file
     * @param {Object} data - Data to export
     * @param {string} fileName - Output filename
     */
    downloadJSON(data, fileName) {
        const jsonContent = JSON.stringify(data, null, 2);
        this.downloadFile(jsonContent, fileName, 'application/json');
    }

    /**
     * Download file with specified content and MIME type
     * @param {string} content - File content
     * @param {string} fileName - Output filename
     * @param {string} mimeType - MIME type
     */
    downloadFile(content, fileName, mimeType) {
        try {
            const blob = new Blob([content], { type: mimeType });
            const url = URL.createObjectURL(blob);

            const link = document.createElement('a');
            link.href = url;
            link.download = fileName;
            link.style.display = 'none';

            document.body.appendChild(link);
            link.click();

            // Cleanup
            setTimeout(() => {
                document.body.removeChild(link);
                URL.revokeObjectURL(url);
            }, 100);

            console.log(`File exported successfully: ${fileName}`);

        } catch (error) {
            console.error('Failed to export file:', error);
            throw new Error(`Failed to export file: ${error.message}`);
        }
    }

    /**
     * Escape CSV special characters
     * @param {string} text - Text to escape
     * @returns {string} Escaped text
     */
    escapeCSV(text) {
        if (!text) return '';

        // Escape quotes and wrap in quotes if contains comma, newline, or quotes
        if (text.includes(',') || text.includes('\n') || text.includes('"')) {
            return `"${text.replace(/"/g, '""')}"`;
        }

        return text;
    }

    /**
     * Generate a summary report for stories
     * @param {Array} stories - Array of story objects
     * @returns {Object} Summary report
     */
    generateStoriesSummary(stories) {
        if (!stories || stories.length === 0) {
            return {
                totalStories: 0,
                approvedStories: 0,
                pendingStories: 0,
                rejectedStories: 0,
                storiesWithPrompts: 0,
                approvalRate: 0
            };
        }

        const totalStories = stories.length;
        const approvedStories = stories.filter(s => s.status === 'approved').length;
        const rejectedStories = stories.filter(s => s.status === 'rejected').length;
        const pendingStories = totalStories - approvedStories - rejectedStories;
        const storiesWithPrompts = stories.filter(s => s.hasPrompt).length;
        const approvalRate = totalStories > 0 ? Math.round((approvedStories / totalStories) * 100) : 0;

        return {
            totalStories,
            approvedStories,
            pendingStories,
            rejectedStories,
            storiesWithPrompts,
            approvalRate,
            exportDate: new Date().toISOString()
        };
    }

    /**
     * Show notification for export success
     * @param {string} fileName - Exported filename
     * @param {string} type - Export type
     */
    showExportSuccess(fileName, type) {
        if (window.App && window.App.showNotification) {
            window.App.showNotification(`${type} exported successfully: ${fileName}`, 'success');
        } else {
            console.log(`${type} exported successfully: ${fileName}`);
        }
    }

    /**
     * Show notification for export error
     * @param {Error} error - Export error
     * @param {string} type - Export type
     */
    showExportError(error, type) {
        if (window.App && window.App.showNotification) {
            window.App.showNotification(`Failed to export ${type}: ${error.message}`, 'error');
        } else {
            console.error(`Failed to export ${type}:`, error);
        }
    }
}

// Export for module usage if needed
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { ExportService };
}

// Make available globally for backward compatibility
window.ExportService = ExportService;