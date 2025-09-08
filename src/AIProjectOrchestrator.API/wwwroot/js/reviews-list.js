/**
 * Reviews List Handler
 * Handles loading and displaying reviews
 */

document.addEventListener('DOMContentLoaded', function () {
    loadReviews();
});

/**
 * Load and display reviews
 */
async function loadReviews() {
    const reviewsContainer = document.getElementById('reviews-container');
    const loadingElement = document.getElementById('loading');

    try {
        loadingElement.style.display = 'block';

        const reviews = await window.APIClient.getPendingReviews();

        loadingElement.style.display = 'none';

        if (reviews.length === 0) {
            reviewsContainer.innerHTML = `
                <div class="alert alert-info">
                    <h3 class="alert-heading">No Reviews Found</h3>
                    <p>There are no reviews available at the moment. Reviews will appear here as they are created during the project workflow.</p>
                </div>
            `;
            return;
        }

        reviewsContainer.innerHTML = reviews.map(review => createReviewCard(review)).join('');

    } catch (error) {
        loadingElement.style.display = 'none';
        console.error('Error loading reviews:', error);
        reviewsContainer.innerHTML = `
            <div class="alert alert-danger">
                <h3 class="alert-heading">Error Loading Reviews</h3>
                <p>Failed to load reviews: ${escapeHtml(error.message)}</p>
            </div>
        `;
    }
}

/**
 * Create a review card HTML
 */
function createReviewCard(review) {
    const createdDate = new Date(review.createdAt).toLocaleDateString();
    const statusClass = `status-${review.status.toLowerCase()}`;

    return `
        <div class="review-card">
            <div class="review-header">
                <h3 class="review-title">
                    <a href="/reviews/details.html?id=${review.id}" class="review-link">
                        ${escapeHtml(review.title)}
                    </a>
                </h3>
                <span class="review-status ${statusClass}">${escapeHtml(review.status)}</span>
            </div>
            
            <div class="review-meta">
                <div class="meta-item">
                    <strong>Project:</strong> ${escapeHtml(review.projectName || 'N/A')}
                </div>
                <div class="meta-item">
                    <strong>Type:</strong> ${escapeHtml(review.reviewType)}
                </div>
                <div class="meta-item">
                    <strong>Created:</strong> ${createdDate}
                </div>
            </div>
            
            <div class="review-content">
                <h4>Review Content</h4>
                <div class="markdown-content">
                    ${renderMarkdown(review.content)}
                </div>
            </div>
            
            <div class="review-actions">
                <button class="btn btn-primary" onclick="viewReviewDetails('${review.id}')">
                    View Details
                </button>
                ${review.status === 'Pending' ? `
                    <button class="btn btn-success" onclick="approveReview('${review.id}')">
                        Approve
                    </button>
                    <button class="btn btn-danger" onclick="rejectReview('${review.id}')">
                        Reject
                    </button>
                ` : ''}
            </div>
        </div>
    `;
}

/**
 * View review details
 */
function viewReviewDetails(reviewId) {
    window.location.href = `/reviews/details.html?id=${reviewId}`;
}

/**
 * Approve a review
 */
async function approveReview(reviewId) {
    if (!confirm('Are you sure you want to approve this review?')) {
        return;
    }

    try {
        await window.APIClient.approveReview(reviewId);

        // Reload the reviews list
        loadReviews();

    } catch (error) {
        console.error('Error approving review:', error);
        alert(`Error approving review: ${error.message}`);
    }
}

/**
 * Reject a review
 */
async function rejectReview(reviewId) {
    if (!confirm('Are you sure you want to reject this review?')) {
        return;
    }

    try {
        await window.APIClient.rejectReview(reviewId);

        // Reload the reviews list
        loadReviews();

    } catch (error) {
        console.error('Error rejecting review:', error);
        alert(`Error rejecting review: ${error.message}`);
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