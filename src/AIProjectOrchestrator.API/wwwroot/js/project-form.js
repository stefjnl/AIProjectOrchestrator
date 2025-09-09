/**
 * Project Form Handler
 * Handles project creation form submissions and interactions
 */

document.addEventListener('DOMContentLoaded', function () {
    const projectForm = document.getElementById('project-form');
    const resultContainer = document.getElementById('result-container');

    if (projectForm) {
        projectForm.addEventListener('submit', async function (event) {
            event.preventDefault();

            const formData = new FormData(projectForm);
            const projectData = {
                name: formData.get('projectName'),
                description: formData.get('projectDescription'),
                aiModel: formData.get('aiModel'),
                requirements: formData.get('requirements')
            };

            try {
                // Show loading state
                const submitButton = projectForm.querySelector('button[type="submit"]');
                submitButton.disabled = true;
                submitButton.textContent = 'Creating Project...';

                // Call the API to create the project
                const result = await window.APIClient.createProject(projectData);

                // Show success message
                resultContainer.innerHTML = `
                    <div class="alert alert-success" role="alert">
                        <h4 class="alert-heading">Project Created Successfully!</h4>
                        <p>Your project "${result.name}" has been created with ID: ${result.id}</p>
                        <hr>
                        <p class="mb-0">
                            <a href="/projects/list.html" class="btn btn-primary">View Projects</a>
                            <a href="/" class="btn btn-secondary">Go Home</a>
                        </p>
                    </div>
                `;

                // Clear the form
                projectForm.reset();

            } catch (error) {
                console.error('Error creating project:', error);

                // Show error message
                resultContainer.innerHTML = `
                    <div class="alert alert-danger" role="alert">
                        <h4 class="alert-heading">Error Creating Project</h4>
                        <p>There was an error creating your project: ${error.message}</p>
                        <hr>
                        <p class="mb-0">Please try again or contact support if the problem persists.</p>
                    </div>
                `;

            } finally {
                // Reset button state
                const submitButton = projectForm.querySelector('button[type="submit"]');
                submitButton.disabled = false;
                submitButton.textContent = 'Create Project';
            }
        });
    }
});

/**
 * Load project templates
 */
async function loadProjectTemplates() {
    try {
        const templates = await window.APIClient.get('/projects/templates');
        const templateSelect = document.getElementById('project-template');

        if (templateSelect && templates.length > 0) {
            templates.forEach(template => {
                const option = document.createElement('option');
                option.value = template.id;
                option.textContent = template.name;
                templateSelect.appendChild(option);
            });

            templateSelect.addEventListener('change', function () {
                const selectedTemplate = templates.find(t => t.id === this.value);
                if (selectedTemplate) {
                    document.getElementById('project-description').value = selectedTemplate.description;
                    document.getElementById('requirements').value = selectedTemplate.defaultRequirements || '';
                }
            });
        }
    } catch (error) {
        console.error('Error loading project templates:', error);
    }
}

// Load templates when the page loads
document.addEventListener('DOMContentLoaded', function () {
    loadProjectTemplates();
});