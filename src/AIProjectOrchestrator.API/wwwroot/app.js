// API Base Configuration
const API_BASE = window.location.origin;

// Dashboard Functions
async function loadDashboardData() {
    try {
        const response = await fetch(`${API_BASE}/api/review/dashboard-data`);
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        
        const data = await response.json();
        
        // Update pending reviews
        document.getElementById('pendingCount').textContent = data.pendingReviews.length;
        renderPendingReviews(data.pendingReviews);
        
        // Update active workflows
        document.getElementById('workflowCount').textContent = data.activeWorkflows.length;
        renderActiveWorkflows(data.activeWorkflows);
        
    } catch (error) {
        console.error('Error loading dashboard:', error);
        showError('Failed to load dashboard data. Please refresh the page.');
    }
}

function renderPendingReviews(reviews) {
    const container = document.getElementById('pendingReviews');
    
    if (reviews.length === 0) {
        container.innerHTML = '<p>No pending reviews at this time.</p>';
        return;
    }
    
    container.innerHTML = reviews.map(review => `
        <div class="review-item">
            <h3><span class="status-indicator status-warning"></span>${review.serviceType} Review</h3>
            <p><strong>Title:</strong> ${review.title}</p>
            <p><strong>Submitted:</strong> ${new Date(review.submittedAt).toLocaleString()}</p>
            
            <div class="review-content">
                <h4>Generated Content:</h4>
                <pre>${review.content}</pre>
                
                <h4>Original Request:</h4>
                <pre>${review.originalRequest}</pre>
            </div>
            
            <div class="button-group">
                <button class="btn btn-approve" onclick="approveReview('${review.reviewId}')">
                    Approve
                </button>
                <button class="btn btn-reject" onclick="rejectReview('${review.reviewId}')">
                    Reject
                </button>
            </div>
        </div>
    `).join('');
}

function renderActiveWorkflows(workflows) {
    const container = document.getElementById('activeWorkflows');
    
    if (workflows.length === 0) {
        container.innerHTML = '<p>No active workflows at this time.</p>';
        return;
    }
    
    container.innerHTML = workflows.map(workflow => `
        <div class="review-item">
            <h3><span class="status-indicator status-success"></span>${workflow.projectTitle}</h3>
            <p><strong>Current Stage:</strong> ${workflow.currentStage}</p>
            <p><strong>Next Action:</strong> ${workflow.nextAction}</p>
            
            <div style="margin: 10px 0;">
                ${workflow.stageStatuses.map(stage => `
                    <span class="workflow-stage stage-${stage.status.toLowerCase()}">
                        ${stage.stageName}: ${stage.status}
                    </span>
                `).join('')}
            </div>
            
            <button class="btn btn-primary" onclick="viewWorkflowDetails('${workflow.projectId}')">
                View Details
            </button>
        </div>
    `).join('');
}

// Review Actions
async function approveReview(reviewId) {
    if (!confirm('Are you sure you want to approve this review?')) return;
    
    try {
        const response = await fetch(`${API_BASE}/api/review/${reviewId}/approve`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' }
        });
        
        if (response.ok) {
            showSuccess('Review approved successfully!');
            loadDashboardData(); // Refresh
        } else {
            throw new Error(`HTTP ${response.status}`);
        }
    } catch (error) {
        console.error('Error approving review:', error);
        showError('Failed to approve review. Please try again.');
    }
}

async function rejectReview(reviewId) {
    const feedback = prompt('Enter rejection feedback (optional):');
    if (feedback === null) return; // User cancelled
    
    try {
        const response = await fetch(`${API_BASE}/api/review/${reviewId}/reject`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ feedback: feedback || 'No feedback provided' })
        });
        
        if (response.ok) {
            showSuccess('Review rejected successfully!');
            loadDashboardData(); // Refresh
        } else {
            throw new Error(`HTTP ${response.status}`);
        }
    } catch (error) {
        console.error('Error rejecting review:', error);
        showError('Failed to reject review. Please try again.');
    }
}

// Test Scenarios
function loadPredefinedScenarios() {
    const scenarios = [
        {
            name: 'E-commerce Platform',
            description: 'Build an e-commerce platform for small businesses selling handmade goods',
            context: 'Focus on inventory management, order processing, and payment integration'
        },
        {
            name: 'University Course Management',
            description: 'Create a course management system for university professors',
            context: 'Integration with Canvas LMS, student grade tracking, assignment management'
        },
        {
            name: 'Task Management System',
            description: 'Build a task management application for software development teams',
            context: 'Agile workflow support, time tracking, team collaboration features'
        },
        {
            name: 'Healthcare Patient Portal',
            description: 'Develop a patient portal for medical practices',
            context: 'HIPAA compliance, appointment scheduling, medical record access'
        }
    ];
    
    const container = document.getElementById('predefinedScenarios');
    container.innerHTML = scenarios.map((scenario, index) => `
        <div class="review-item">
            <h3>${scenario.name}</h3>
            <p><strong>Description:</strong> ${scenario.description}</p>
            <p><strong>Context:</strong> ${scenario.context}</p>
            <button class="btn btn-primary" onclick="submitPredefinedScenario(${index})">
                Submit This Scenario
            </button>
        </div>
    `).join('');
}

async function submitPredefinedScenario(index) {
    const scenarios = [
        {
            name: 'E-commerce Platform',
            description: 'Build an e-commerce platform for small businesses selling handmade goods',
            context: 'Focus on inventory management, order processing, and payment integration'
        },
        {
            name: 'University Course Management',
            description: 'Create a course management system for university professors',
            context: 'Integration with Canvas LMS, student grade tracking, assignment management'
        },
        {
            name: 'Task Management System',
            description: 'Build a task management application for software development teams',
            context: 'Agile workflow support, time tracking, team collaboration features'
        },
        {
            name: 'Healthcare Patient Portal',
            description: 'Develop a patient portal for medical practices',
            context: 'HIPAA compliance, appointment scheduling, medical record access'
        }
    ];
    
    const scenario = scenarios[index];
    await submitTestScenario(scenario.name, scenario.description, scenario.context);
}

async function submitTestScenario(title, description, context) {
    try {
        const response = await fetch(`${API_BASE}/api/review/test-scenario`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                scenarioName: title,
                projectDescription: description,
                additionalContext: context,
                constraints: ''
            })
        });
        
        if (response.ok) {
            const result = await response.json();
            showSuccess(`Test scenario submitted! Project ID: ${result.projectId}`);
            loadRecentSubmissions();
        } else {
            throw new Error(`HTTP ${response.status}`);
        }
    } catch (error) {
        console.error('Error submitting scenario:', error);
        showError('Failed to submit test scenario. Please try again.');
    }
}

// Utility Functions
function refreshDashboard() {
    showSuccess('Refreshing dashboard...');
    loadDashboardData();
}

function showSuccess(message) {
    // Simple alert for now - can be enhanced with better UI
    alert(`Success: ${message}`);
}

function showError(message) {
    // Simple alert for now - can be enhanced with better UI
    alert(`Error: ${message}`);
}

function viewWorkflowDetails(projectId) {
    // Open detailed workflow view
    window.open(`${API_BASE}/project-status.html?id=${projectId}`, '_blank');
}