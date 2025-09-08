/**
 * Projects List Handler
 * Handles loading and displaying projects list
 */

document.addEventListener('DOMContentLoaded', function () {
    loadProjects();
});

/**
 * Load and display projects
 */
async function loadProjects() {
    const projectsContainer = document.getElementById('projects-container');
    const loadingElement = document.getElementById('loading');

    try {
        loadingElement.style.display = 'block';

        const projects = await window.APIClient.getProjects();

        if (projects.length === 0) {
            projectsContainer.innerHTML = `
                <div class="alert alert-info" role="alert">
                    <h4 class="alert-heading">No Projects Found</h4>
                    <p>You haven't created any projects yet.</p>
                    <hr>
                    <a href="/projects/create.html" class="btn btn-primary">Create Your First Project</a>
                </div>
            `;
            return;
        }

        const projectsList = document.createElement('div');
        projectsList.className = 'projects-list';

        projects.forEach(project => {
            const projectCard = createProjectCard(project);
            projectsList.appendChild(projectCard);
        });

        projectsContainer.innerHTML = '';
        projectsContainer.appendChild(projectsList);

    } catch (error) {
        console.error('Error loading projects:', error);

        projectsContainer.innerHTML = `
            <div class="alert alert-danger" role="alert">
                <h4 class="alert-heading">Error Loading Projects</h4>
                <p>There was an error loading your projects: ${error.message}</p>
                <hr>
                <button onclick="loadProjects()" class="btn btn-primary">Retry</button>
            </div>
        `;

    } finally {
        loadingElement.style.display = 'none';
    }
}

/**
 * Create a project card element
 */
function createProjectCard(project) {
    const card = document.createElement('div');
    card.className = 'project-card';

    const createdDate = new Date(project.createdAt).toLocaleDateString();
    const updatedDate = new Date(project.updatedAt).toLocaleDateString();

    card.innerHTML = `
        <div class="project-header">
            <h3 class="project-title">${escapeHtml(project.name)}</h3>
            <span class="project-status status-${project.status.toLowerCase()}">${project.status}</span>
        </div>
        <div class="project-body">
            <p class="project-description">${escapeHtml(project.description)}</p>
            <div class="project-meta">
                <span class="meta-item">
                    <strong>AI Model:</strong> ${project.aiModel || 'Auto-select'}
                </span>
                <span class="meta-item">
                    <strong>Created:</strong> ${createdDate}
                </span>
                <span class="meta-item">
                    <strong>Updated:</strong> ${updatedDate}
                </span>
            </div>
        </div>
        <div class="project-actions">
            <a href="/projects/details.html?id=${project.id}" class="btn btn-primary btn-sm">View Details</a>
            <button onclick="deleteProject('${project.id}', '${escapeHtml(project.name)}')" 
                    class="btn btn-danger btn-sm">Delete</button>
        </div>
    `;

    return card;
}

/**
 * Delete a project
 */
async function deleteProject(projectId, projectName) {
    if (!confirm(`Are you sure you want to delete the project "${projectName}"? This action cannot be undone.`)) {
        return;
    }

    try {
        await window.APIClient.deleteProject(projectId);

        // Reload the projects list
        loadProjects();

    } catch (error) {
        console.error('Error deleting project:', error);
        alert(`Error deleting project: ${error.message}`);
    }
}

/**
 * Escape HTML to prevent XSS
 */
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}