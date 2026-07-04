// ===========================
// API Configuration
// ===========================
const API_BASE = 'https://localhost:7105/api';

function getAuthHeaders() {
    const authToken = document.getElementById('authToken')?.value;
    return {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${authToken}`
    };
}

function getUserData() {
    return {
        userId: document.getElementById('userId')?.value,
        userName: document.getElementById('userName')?.value,
        userEmail: document.getElementById('userEmail')?.value,
        userRole: document.getElementById('userRole')?.value,
        authToken: document.getElementById('authToken')?.value
    };
}

// ===========================
// Notification Toast Functions
// ===========================
function showSuccess(message) {
    const toast = document.createElement('div');
    toast.className = 'fixed top-20 right-4 bg-green-500 text-white px-6 py-3 rounded-lg shadow-lg z-50 flex items-center gap-2';
    toast.innerHTML = `<i class="fas fa-check-circle"></i><span>${message}</span>`;
    document.body.appendChild(toast);
    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transition = 'opacity 0.3s';
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

function showError(message) {
    const toast = document.createElement('div');
    toast.className = 'fixed top-20 right-4 bg-red-500 text-white px-6 py-3 rounded-lg shadow-lg z-50 flex items-center gap-2';
    toast.innerHTML = `<i class="fas fa-exclamation-circle"></i><span>${message}</span>`;
    document.body.appendChild(toast);
    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transition = 'opacity 0.3s';
        setTimeout(() => toast.remove(), 300);
    }, 5000);
}

// ===========================
// Modal Functions
// ===========================
function openModal() {
    console.log('Opening add task modal');
    document.getElementById("taskModal").classList.add("active");

    const today = new Date();
    const tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);

    document.getElementById("taskDueDate").min = today.toISOString().split('T')[0];
    document.getElementById("taskDueDate").value = tomorrow.toISOString().split('T')[0];

    const voiceStatus = document.getElementById('voiceStatus');
    voiceStatus.textContent = "Click microphone to use voice";
    voiceStatus.className = "text-sm text-gray-600";

    stopVoiceRecognition();
}

function closeModal() {
    console.log('Closing add task modal');
    document.getElementById("taskModal").classList.remove("active");
    document.getElementById("taskName").value = "";
    document.getElementById("taskDescription").value = "";
    document.getElementById("taskPriority").value = "1";
    document.getElementById("taskStatus").value = "2";
    document.getElementById("taskDueDate").value = "";

    const voiceStatus = document.getElementById('voiceStatus');
    voiceStatus.textContent = "";

    stopVoiceRecognition();
}

// ===========================
// Task Handler Functions
// ===========================
async function handleAddTask(event) {
    event.preventDefault();
    console.log('=== ADDING NEW TASK ===');

    const addButton = document.getElementById('addTaskButton');
    const originalText = addButton.innerHTML;

    try {
        addButton.disabled = true;
        addButton.innerHTML = '<i class="fas fa-spinner fa-spin mr-2"></i>Adding...';

        const taskName = document.getElementById("taskName").value.trim();
        const taskDescription = document.getElementById("taskDescription").value.trim();
        const taskPriority = parseInt(document.getElementById("taskPriority").value);
        const taskStatus = parseInt(document.getElementById("taskStatus").value);
        const taskDueDate = document.getElementById("taskDueDate").value;
        const { userId } = getUserData();

        if (!taskName) {
            showError('Task name is required');
            return;
        }

        if (!taskDueDate) {
            showError('Due date is required');
            return;
        }

        const newTask = {
            Title: taskName,
            Description: taskDescription || null,
            DueDate: taskDueDate,
            Priority: taskPriority,
            Status: taskStatus,
            UserId: parseInt(userId)
        };

        console.log('Sending task:', newTask);

        const response = await fetch(`${API_BASE}/tasks`, {
            method: 'POST',
            headers: getAuthHeaders(),
            body: JSON.stringify(newTask)
        });

        console.log('Response status:', response.status);

        if (response.ok) {
            const result = await response.json();
            console.log('Task created:', result);
            showSuccess('Task added successfully!');
            closeModal();

            if (typeof loadTasks === 'function') {
                setTimeout(() => loadTasks(), 500);
            } else {
                setTimeout(() => window.location.reload(), 1000);
            }
        } else {
            const errorText = await response.text();
            console.error('Error response:', errorText);
            showError('Failed to add task. Please try again.');
        }
    } catch (error) {
        console.error('Error adding task:', error);
        showError('Network error: ' + error.message);
    } finally {
        addButton.disabled = false;
        addButton.innerHTML = originalText;
    }
}

// ===========================
// Voice Recognition Functions
// ===========================
let recognition = null;
let isListening = false;

function startVoiceInput() {
    // Voice recognition code here (keeping original)
    // Add your voice recognition implementation
}

function stopVoiceRecognition() {
    if (recognition && isListening) {
        recognition.stop();
        const voiceButton = document.getElementById('voiceButton');
        const voiceStatus = document.getElementById('voiceStatus');
        voiceButton.classList.remove('listening-animation');
        voiceStatus.textContent = "Voice input stopped";
        voiceStatus.className = "text-sm text-gray-600";
        isListening = false;
    }
}

// ===========================
// DOM Initialization
// ===========================
document.addEventListener('DOMContentLoaded', function () {
    console.log('Layout DOM loaded');

    const { userId, authToken } = getUserData();
    console.log('=== LAYOUT INITIALIZED ===');
    console.log('UserId:', userId);
    console.log('AuthToken exists:', !!authToken);

    if (!userId || !authToken) {
        console.error('No authentication, redirecting to login');
        window.location.href = '/Account/Login';
        return;
    }

    // Modal click outside to close
    document.getElementById('taskModal')?.addEventListener('click', function (e) {
        if (e.target.id === 'taskModal') {
            closeModal();
        }
    });

    // Notification panel toggle
    const notificationBell = document.getElementById('notificationBell');
    const notificationPanel = document.getElementById('notificationPanel');

    if (notificationBell && notificationPanel) {
        notificationBell.addEventListener('click', function (e) {
            e.stopPropagation();
            notificationPanel.classList.toggle('hidden');
        });
    }

    // Close notification panel when clicking outside
    document.addEventListener('click', function (e) {
        if (notificationPanel && !notificationPanel.contains(e.target) &&
            notificationBell && !notificationBell.contains(e.target)) {
            notificationPanel.classList.add('hidden');
        }
    });
});