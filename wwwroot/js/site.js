/* wwwroot/js/site.js */
// Site-wide JavaScript functions (Enhanced minimal UI)

// Format currency
function formatCurrency(amount) {
    return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD',
        minimumFractionDigits: 2
    }).format(amount);
}

// Format date
function formatDate(date) {
    return new Intl.DateTimeFormat('en-US', {
        year: 'numeric',
        month: 'short',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
    }).format(new Date(date));
}

// Show loading spinner
function showLoading() {
    return `
        <div class="text-center my-5">
            <div class="spinner"></div>
            <p class="mt-3 text-muted">Loading...</p>
        </div>
    `;
}

// Show toast notification
function showToast(message, type = 'success') {
    const toast = document.createElement('div');
    toast.className = 'position-fixed bottom-0 end-0 p-3';
    toast.style.zIndex = '1100';
    toast.innerHTML = `
        <div class="toast show" role="alert">
            <div class="toast-header bg-${type} text-white">
                <strong class="me-auto">StockMaster</strong>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast"></button>
            </div>
            <div class="toast-body">${message}</div>
        </div>
    `;
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 3500);
}

// Confirm action
function confirmAction(message, callback) {
    if (confirm(message)) callback();
}

// Export to CSV
function exportToCSV(data, filename) {
    const csv = data.join('\n');
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
}

// Print receipt
function printReceipt(elementId) {
    const printContent = document.getElementById(elementId)?.innerHTML;
    if (!printContent) return;
    const originalContent = document.body.innerHTML;
    document.body.innerHTML = printContent;
    window.print();
    document.body.innerHTML = originalContent;
    location.reload();
}

// Auto-hide alerts after 4 seconds
document.addEventListener('DOMContentLoaded', () => {
    setTimeout(() => {
        document.querySelectorAll('.alert').forEach(alert => {
            const bsAlert = bootstrap.Alert?.getInstance(alert);
            if (bsAlert) bsAlert.close();
            else alert.remove();
        });
    }, 4000);
});