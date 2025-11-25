/**
 * Expo Demo Task Tracker
 * Tracks user progress through pilot test tasks
 */

class ExpoTaskTracker {
    constructor() {
        this.storageKey = 'expoTaskProgress';
        this.tasks = [
            {
                id: 'register',
                title: 'Opprett ny brukerkonto',
                description: 'Klikk "Sign Up" og registrer deg som ny bruker',
                completed: false
            },
            {
                id: 'login',
                title: 'Logg inn',
                description: 'Logg inn med din nye brukerkonto',
                completed: false
            },
            {
                id: 'view-register-type',
                title: 'Se RegisterType',
                description: 'Naviger til registreringsvalg-siden',
                completed: false
            },
            {
                id: 'create-quick-register',
                title: 'Opprett Quick Register',
                description: 'Lag en ny Quick Register oppføring',
                completed: false
            },
            {
                id: 'complete-quick-register',
                title: 'Fullfør Quick Register',
                description: 'Gå til My Registrations og fullfør Quick Register',
                completed: false
            },
            {
                id: 'create-full-register',
                title: 'Opprett Full Register',
                description: 'Lag en ny Full Register fra hjemmesiden',
                completed: false
            },
            {
                id: 'view-full-register',
                title: 'Sjekk Full Register',
                description: 'Se din nye Full Register i My Registrations',
                completed: false
            }
        ];

        this.loadProgress();
        this.initEventListeners();
    }

    /**
     * Load progress from localStorage
     */
    loadProgress() {
        try {
            const saved = localStorage.getItem(this.storageKey);
            if (saved) {
                const savedTasks = JSON.parse(saved);
                this.tasks = this.tasks.map(task => {
                    const savedTask = savedTasks.find(t => t.id === task.id);
                    return savedTask ? { ...task, completed: savedTask.completed } : task;
                });
            }
        } catch (error) {
            console.error('Failed to load task progress:', error);
        }
    }

    /**
     * Save progress to localStorage
     */
    saveProgress() {
        try {
            localStorage.setItem(this.storageKey, JSON.stringify(this.tasks));
        } catch (error) {
            console.error('Failed to save task progress:', error);
        }
    }

    /**
     * Mark a task as completed
     */
    completeTask(taskId) {
        const task = this.tasks.find(t => t.id === taskId);
        if (task && !task.completed) {
            task.completed = true;
            this.saveProgress();
            this.updateUI();
            this.checkCompletion();

            // Dispatch custom event
            window.dispatchEvent(new CustomEvent('expoTaskCompleted', {
                detail: { taskId, task }
            }));
        }
    }

    /**
     * Check if all tasks are completed
     */
    checkCompletion() {
        const allCompleted = this.tasks.every(task => task.completed);
        if (allCompleted) {
            // Wait a bit to show the last checkmark
            setTimeout(() => {
                window.location.href = '/Pilot/ExpoCompletion';
            }, 1500);
        }
    }

    /**
     * Get completion percentage
     */
    getProgress() {
        const completed = this.tasks.filter(t => t.completed).length;
        const total = this.tasks.length;
        return Math.round((completed / total) * 100);
    }

    /**
     * Update UI with current progress
     */
    updateUI() {
        const taskList = document.getElementById('expo-task-list');
        const progressBar = document.getElementById('expo-progress-bar');
        const progressText = document.getElementById('expo-progress-text');

        if (!taskList) return;

        // Update task list
        taskList.innerHTML = this.tasks.map((task, index) => `
            <div class="expo-task-item ${task.completed ? 'completed' : ''}" data-task-id="${task.id}">
                <div class="expo-task-number">${index + 1}</div>
                <div class="expo-task-content">
                    <div class="expo-task-title">${task.title}</div>
                    <div class="expo-task-description">${task.description}</div>
                </div>
                <div class="expo-task-check">
                    ${task.completed ? '<svg class="checkmark" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"></polyline></svg>' : '<div class="unchecked-circle"></div>'}
                </div>
            </div>
        `).join('');

        // Update progress bar
        const progress = this.getProgress();
        if (progressBar) {
            progressBar.style.width = `${progress}%`;
        }
        if (progressText) {
            progressText.textContent = `${progress}% fullført`;
        }
    }

    /**
     * Initialize event listeners for automatic task detection
     */
    initEventListeners() {
        // Detect current page and mark relevant tasks
        this.detectCurrentPage();

        // Listen for form submissions
        document.addEventListener('submit', (e) => {
            this.handleFormSubmit(e);
        });

        // Listen for navigation
        window.addEventListener('popstate', () => {
            this.detectCurrentPage();
        });
    }

    /**
     * Detect current page and mark tasks as completed
     */
    detectCurrentPage() {
        const path = window.location.pathname.toLowerCase();

        // Check if user is logged in (has specific navigation elements)
        const isLoggedIn = document.querySelector('[data-user-authenticated="true"]');

        if (isLoggedIn) {
            this.completeTask('login');
        }

        // Register Type page
        if (path.includes('/pilot/registertype')) {
            this.completeTask('view-register-type');

            // Check URL parameters for success messages
            const urlParams = new URLSearchParams(window.location.search);
            if (urlParams.get('fullRegisterSuccess') === 'true') {
                this.completeTask('create-full-register');
            }
        }

        // Quick Register page - detect if opened
        if (path.includes('/pilot/quickregister')) {
            // This will be completed when form is submitted successfully
        }

        // Complete Quick Register page - detect when visiting
        if (path.includes('/pilot/completequickregister')) {
            this.completeTask('create-quick-register');
        }

        // Full Register page
        if (path.includes('/pilot/fullregister')) {
            // This will be completed when form is submitted successfully
        }

        // My Registrations page
        if (path.includes('/pilot/myregistrations')) {
            // Check if we just completed a quick register
            const urlParams = new URLSearchParams(window.location.search);
            if (urlParams.get('quickRegisterCompleted') === 'true') {
                this.completeTask('complete-quick-register');
            }

            // Check if we're viewing after full register
            if (urlParams.get('viewingFullRegister') === 'true') {
                this.completeTask('view-full-register');
            }

            // Auto-detect if user has completed obstacles
            setTimeout(() => {
                this.detectCompletedObstacles();
            }, 500);
        }
    }

    /**
     * Detect completed obstacles on My Registrations page
     */
    detectCompletedObstacles() {
        const path = window.location.pathname.toLowerCase();
        if (!path.includes('/pilot/myregistrations')) return;

        // Check if there are no incomplete obstacles (yellow box empty or missing)
        const incompleteSection = document.querySelector('.bg-yellow-100');
        const incompleteCount = incompleteSection ?
            incompleteSection.querySelectorAll('tbody tr').length : 0;

        // Check if there are pending obstacles (completed quick register)
        const pendingSection = document.querySelectorAll('.bg-blue-100, .bg-green-100');
        const hasPendingOrApproved = pendingSection.length > 0;

        // If quick register was created and now no incomplete items, mark as completed
        if (this.tasks.find(t => t.id === 'create-quick-register')?.completed &&
            incompleteCount === 0 && hasPendingOrApproved) {
            this.completeTask('complete-quick-register');
        }

        // Check for full register by looking at pending/approved obstacles
        const allObstacles = document.querySelectorAll('[data-obstacle-id]');
        if (allObstacles.length >= 2) { // Has both quick and full register
            this.completeTask('view-full-register');
        }
    }

    /**
     * Handle form submissions
     */
    handleFormSubmit(event) {
        const form = event.target;
        const action = form.action?.toLowerCase() || '';

        // Registration form
        if (action.includes('/account/register')) {
            // Mark as completed after successful registration
            setTimeout(() => {
                if (!document.querySelector('.text-red-500')) {
                    this.completeTask('register');
                }
            }, 500);
        }

        // Quick Register form
        if (action.includes('/pilot/quickregister')) {
            // Will be detected by success state on page
        }

        // Full Register form
        if (action.includes('/pilot/fullregister')) {
            // Will be detected by success state on page
        }
    }

    /**
     * Reset progress (for testing)
     */
    reset() {
        localStorage.removeItem(this.storageKey);
        this.tasks.forEach(task => task.completed = false);
        this.updateUI();
    }

    /**
     * Toggle tracker visibility
     */
    toggleTracker() {
        const tracker = document.getElementById('expo-task-tracker');
        if (tracker) {
            tracker.classList.toggle('collapsed');
        }
    }
}

// Initialize tracker when DOM is ready
let expoTracker;
document.addEventListener('DOMContentLoaded', () => {
    expoTracker = new ExpoTaskTracker();
    expoTracker.updateUI();

    // Make it globally accessible for manual task completion
    window.expoTracker = expoTracker;
});
