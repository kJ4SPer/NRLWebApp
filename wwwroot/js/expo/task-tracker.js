// Expo Demo Task Tracker - Base Class

class ExpoTaskTracker {
    constructor(role) {
        this.role = role; // 'pilot' or 'registerforer'
        this.storageKey = `expoTaskProgress_${role}`;
        this.tasks = [];
        this.initialized = false;

        // Load tasks from role-specific definitions
        this.loadTaskDefinitions(role);

        // Load saved progress
        this.loadProgress();

        // Initialize UI and event listeners
        this.initUI();
        this.initEventListeners();

        // Auto-detect current page and complete relevant tasks
        this.detectCurrentPage();

        // Check if all tasks are complete
        this.checkCompletion();

        this.initialized = true;
    }

    loadTaskDefinitions(role) {
        // This will be overridden by role-specific scripts
        // that define window.pilotTasks or window.registerforerTasks
        if (role === 'pilot' && window.pilotTasks) {
            this.tasks = window.pilotTasks.map((task, index) => ({
                ...task,
                completed: false
            }));
        } else if (role === 'registerforer' && window.registerforerTasks) {
            this.tasks = window.registerforerTasks.map((task, index) => ({
                ...task,
                completed: false
            }));
        }
    }

    loadProgress() {
        try {
            const saved = localStorage.getItem(this.storageKey);
            if (saved) {
                const progress = JSON.parse(saved);
                // Merge saved progress with task definitions
                progress.forEach(savedTask => {
                    const task = this.tasks.find(t => t.id === savedTask.id);
                    if (task) {
                        task.completed = savedTask.completed;
                    }
                });
            }
        } catch (error) {
            console.error('Error loading task progress:', error);
        }
    }

    saveProgress() {
        try {
            const progress = this.tasks.map(task => ({
                id: task.id,
                completed: task.completed
            }));
            localStorage.setItem(this.storageKey, JSON.stringify(progress));
        } catch (error) {
            console.error('Error saving task progress:', error);
        }
    }

    initUI() {
        this.renderTasks();
        this.updateProgress();
    }

    renderTasks() {
        const container = document.getElementById('tracker-task-list');
        if (!container) return;

        container.innerHTML = '';

        this.tasks.forEach((task, index) => {
            const taskElement = document.createElement('div');
            taskElement.className = `task-item ${task.completed ? 'completed' : ''}`;
            taskElement.setAttribute('data-task-id', task.id);

            taskElement.innerHTML = `
                <div class="task-number">${index + 1}</div>
                <div class="task-content">
                    <h4 class="task-title">${task.title}</h4>
                    <p class="task-description">${task.description}</p>
                </div>
                <div class="task-status">
                    <div class="status-icon ${task.completed ? 'completed' : 'pending'}">
                        ${task.completed ? '✓' : ''}
                    </div>
                </div>
            `;

            container.appendChild(taskElement);
        });
    }

    updateProgress() {
        const completedCount = this.tasks.filter(t => t.completed).length;
        const totalCount = this.tasks.length;
        const percentage = totalCount > 0 ? Math.round((completedCount / totalCount) * 100) : 0;

        const progressBar = document.querySelector('.progress-bar-fill');
        const progressText = document.querySelector('.progress-text');

        if (progressBar) {
            progressBar.style.width = `${percentage}%`;
        }

        if (progressText) {
            progressText.textContent = `${percentage}% fullført`;
        }
    }

    initEventListeners() {
        // Toggle button
        const toggleBtn = document.getElementById('tracker-toggle-btn');
        const tracker = document.getElementById('expo-task-tracker');

        if (toggleBtn && tracker) {
            toggleBtn.addEventListener('click', () => {
                tracker.classList.toggle('collapsed');

                // Update button icon
                const isCollapsed = tracker.classList.contains('collapsed');
                toggleBtn.textContent = isCollapsed ? '→' : '←';
            });
        }
    }

    completeTask(taskId) {
        const taskIndex = this.tasks.findIndex(t => t.id === taskId);
        if (taskIndex === -1) {
            console.warn(`Task not found: ${taskId}`);
            return;
        }

        const task = this.tasks[taskIndex];

        // CRITICAL: Enforce sequential completion
        // Check all previous tasks are completed
        for (let i = 0; i < taskIndex; i++) {
            if (!this.tasks[i].completed) {
                console.log(`Cannot skip to task ${taskIndex + 1}. Please complete task ${i + 1} first.`);
                return;
            }
        }

        // Mark task as complete
        if (!task.completed) {
            task.completed = true;
            this.saveProgress();
            this.renderTasks();
            this.updateProgress();

            console.log(`Task completed: ${task.title}`);

            // Check if all tasks are now complete
            this.checkCompletion();
        }
    }

    detectCurrentPage() {
        const path = window.location.pathname.toLowerCase();
        const params = new URLSearchParams(window.location.search);

        // Check for URL parameters that indicate task completion
        if (params.get('newUser') === 'true') {
            this.completeTask('register');
        }

        if (params.get('quickRegisterCompleted') === 'true') {
            this.completeTask('complete-quick-register');
        }

        if (params.get('fullRegisterSuccess') === 'true') {
            this.completeTask('create-full-register');
        }

        if (params.get('obstacleApproved') === 'true') {
            this.completeTask('approve-obstacle');
        }

        if (params.get('obstacleDenied') === 'true') {
            this.completeTask('deny-obstacle');
        }

        // Check if user is authenticated
        const isAuthenticated = document.querySelector('[data-user-authenticated="true"]');
        if (isAuthenticated) {
            this.completeTask('login');
        }

        // Role-specific page detection
        if (this.role === 'pilot') {
            this.detectPilotPages(path);
        } else if (this.role === 'registerforer') {
            this.detectRegisterforerPages(path);
        }

        // Check DOM for specific elements (for final tasks)
        this.detectDOMElements();
    }

    detectPilotPages(path) {
        if (path.includes('/pilot/completequickregister')) {
            this.completeTask('create-quick-register');
        }

        if (path.includes('/pilot/myregistrations')) {
            // The final task will be completed by DOM detection
            setTimeout(() => this.detectDOMElements(), 1000);
        }
    }

    detectRegisterforerPages(path) {
        if (path.includes('/registerforer/mapview')) {
            this.completeTask('check-map-point');
        }

        if (path.includes('/registerforer/allobstacles')) {
            this.completeTask('go-to-all-obstacles');
        }
    }

    detectDOMElements() {
        if (this.role === 'pilot') {
            // Check for 2+ obstacles in MyRegistrations
            const obstacles = document.querySelectorAll('[data-obstacle-id]');
            if (obstacles.length >= 2) {
                this.completeTask('check-full-register');
            }
        }
    }

    checkCompletion() {
        const allCompleted = this.tasks.every(task => task.completed);

        if (allCompleted && this.initialized) {
            console.log('All tasks completed! Redirecting to completion page...');

            // Redirect to completion page after a short delay
            setTimeout(() => {
                const completionUrl = this.role === 'pilot'
                    ? '/Pilot/ExpoCompletion'
                    : '/Registerforer/ExpoCompletion';
                window.location.href = completionUrl;
            }, 1500);
        }
    }

    clearProgress() {
        localStorage.removeItem(this.storageKey);
        this.tasks.forEach(task => task.completed = false);
        this.renderTasks();
        this.updateProgress();
    }
}

// Initialize tracker when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    // Check if we're on a page that should have the tracker
    const tracker = document.getElementById('expo-task-tracker');
    if (!tracker) return;

    // Get role from URL parameter or data attribute
    const params = new URLSearchParams(window.location.search);
    const role = params.get('role') || tracker.getAttribute('data-role');

    if (role) {
        // Wait for role-specific task definitions to load
        setTimeout(() => {
            window.expoTracker = new ExpoTaskTracker(role);
        }, 100);
    }
});
