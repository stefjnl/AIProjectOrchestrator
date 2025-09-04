// Dashboard initialization
async function initializeDashboard() {
    try {
        await loadRecentProjects();
        await loadPendingReviews();
        await loadSystemStatus();
    } catch (error) {
        console.error('Dashboard initialization failed:', error);
        showError('Failed to load dashboard data');
    }
}

async function loadRecentProjects() {
    try {
        const projects = await getProjects();
        const container = document.getElementById('recent-projects');
        
        if (projects.length === 0) {
            container.innerHTML = '<p>No projects yet. <a href="/projects/create.html">Create your first project</a></p>';
            return;
        }
        
        const recentProjects = projects.slice(0, 5);
        container.innerHTML = recentProjects.map(project => `
            <div class="project-summary">
                <h4><a href="/projects/workflow.html?id=${project.id}">${project.name}</a></h4>
                <p>${project.description}</p>
                <small>Created: ${formatDate(project.createdAt)}</small>
            </div>
        `).join('');
    } catch (error) {
        document.getElementById('recent-projects').innerHTML = '<p class="error">Failed to load projects</p>';
        console.error('Error loading recent projects:', error);
    }
}

async function loadPendingReviews() {
    try {
        const reviews = await getPendingReviews();
        const container = document.getElementById('pending-reviews');
        
        if (reviews.length === 0) {
            container.innerHTML = '<p>No pending reviews</p>';
            return;
        }
        
        container.innerHTML = `
            <p>${reviews.length} pending review${reviews.length > 1 ? 's' : ''}</p>
            <a href="/reviews/queue.html" class="btn btn-small">Review Now</a>
        `;
    } catch (error) {
        document.getElementById('pending-reviews').innerHTML = '<p class="error">Failed to load reviews</p>';
        console.error('Error loading pending reviews:', error);
    }
}

async function loadSystemStatus() {
    try {
        const health = await getHealthStatus();
        const container = document.getElementById('system-status');
        container.innerHTML = '<p class="status-healthy">System Operational</p>';
    } catch (error) {
        document.getElementById('system-status').innerHTML = '<p class="status-error">API Unavailable</p>';
        console.error('Error checking system status:', error);
    }
}

// Projects list page
async function loadProjectsList() {
    const loading = document.getElementById('loading');
    const error = document.getElementById('error');
    const grid = document.getElementById('projects-grid');
    const noProjects = document.getElementById('no-projects');
    
    try {
        const projects = await getProjects();
        loading.style.display = 'none';
        
        if (projects.length === 0) {
            noProjects.style.display = 'block';
            return;
        }
        
        grid.innerHTML = projects.map(project => `
            <div class="project-card" onclick="openProject('${project.id}')">
                <h3>${project.name}</h3>
                <p>${project.description}</p>
                <div class="project-meta">
                    <span class="date">Created: ${formatDate(project.createdAt)}</span>
                </div>
                <div class="project-actions">
                    <button class="btn btn-small" onclick="event.stopPropagation(); startWorkflow('${project.id}')">
                        <i class="fas fa-play"></i> Start Workflow
                    </button>
                </div>
            </div>
        `).join('');
        
        grid.style.display = 'grid';
    } catch (err) {
        loading.style.display = 'none';
        error.textContent = `Error loading projects: ${err.message}`;
        error.style.display = 'block';
        console.error('Error loading projects:', err);
    }
}

// Project creation
async function handleCreateProject(event) {
    event.preventDefault();
    
    const form = event.target;
    const formData = new FormData(form);
    const statusDiv = document.getElementById('create-status');
    
    const projectData = {
        name: formData.get('name'),
        description: formData.get('description')
    };
    
    try {
        statusDiv.innerHTML = '<p class="status-loading">Creating project...</p>';
        statusDiv.style.display = 'block';
        
        const result = await createProject(projectData);
        
        statusDiv.innerHTML = '<p class="status-success">Project created successfully!</p>';
        
        // Redirect after brief delay
        setTimeout(() => {
            window.location.href = `/projects/workflow.html?id=${result.id}`;
        }, 1500);
        
    } catch (error) {
        statusDiv.innerHTML = `<p class="status-error">Error creating project: ${error.message}</p>`;
        console.error('Error creating project:', error);
    }
}

// Utility functions
function formatDate(dateString) {
    return new Date(dateString).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    });
}

function openProject(projectId) {
    window.location.href = `/projects/workflow.html?id=${projectId}`;
}

function startWorkflow(projectId) {
    window.location.href = `/projects/workflow.html?id=${projectId}`;
}

function showError(message) {
    alert(message); // Simple error display - can be enhanced later
}