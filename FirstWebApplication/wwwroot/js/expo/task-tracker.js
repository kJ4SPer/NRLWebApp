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
        console.log('[Task Tracker] Loading tasks for role:', role);
        console.log('[Task Tracker] window.pilotTasks:', window.pilotTasks ? 'defined' : 'undefined');
        console.log('[Task Tracker] window.registerforerTasks:', window.registerforerTasks ? 'defined' : 'undefined');

        if (role === 'pilot' && window.pilotTasks) {
            this.tasks = window.pilotTasks.map((task, index) => ({
                ...task,
                completed: false
            }));
            console.log('[Task Tracker] Loaded', this.tasks.length, 'pilot tasks');
        } else if (role === 'registerforer' && window.registerforerTasks) {
            this.tasks = window.registerforerTasks.map((task, index) => ({
                ...task,
                completed: false
            }));
            console.log('[Task Tracker] Loaded', this.tasks.length, 'registerforer tasks');
        } else {
            console.warn('[Task Tracker] No tasks loaded! Role:', role);
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
        const container = document.getElementById('expo-task-list');
        if (!container) return;

        container.innerHTML = '';

        this.tasks.forEach((task, index) => {
            const taskElement = document.createElement('div');
            taskElement.className = `expo-task-item ${task.completed ? 'completed' : ''}`;
            taskElement.setAttribute('data-task-id', task.id);

            taskElement.innerHTML = `
                <div class="expo-task-number">${index + 1}</div>
                <div class="expo-task-content">
                    <h4 class="expo-task-title">${task.title}</h4>
                    <p class="expo-task-description">${task.description}</p>
                </div>
                <div class="expo-task-status">
                    <div class="expo-status-icon ${task.completed ? 'completed' : 'pending'}">
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

        const progressBar = document.querySelector('.expo-progress-bar-fill');
        const progressText = document.querySelector('.expo-progress-text');

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
            // Check for Full Register and Pending status
            setTimeout(() => this.detectDOMElements(), 1000);
        }

        if (path.includes('/pilot/overview')) {
            // Check if viewing a pending review obstacle
            setTimeout(() => this.detectOverviewPage(), 1000);
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
            // Don't complete task here - wait until user views the obstacle
        }
    }

    detectOverviewPage() {
        if (this.role === 'pilot') {
            // When viewing obstacle overview, complete the final task
            const obstacleDetails = document.querySelector('.obstacle-details, [data-obstacle-id]');
            if (obstacleDetails) {
                // Complete final task (check-full-register)
                this.completeTask('check-full-register');
            }
        }
    }

    checkCompletion() {
        const allCompleted = this.tasks.every(task => task.completed);

        if (allCompleted && this.initialized) {
            console.log('All tasks completed! Redirecting to completion page...');

            // For pilot: if on Overview page, wait 5 seconds before redirect
            // Otherwise use shorter delay
            const path = window.location.pathname.toLowerCase();
            const isOnOverview = path.includes('/pilot/overview');
            const delay = (this.role === 'pilot' && isOnOverview) ? 5000 : 1500;

            // Redirect to completion page after delay
            setTimeout(() => {
                const completionUrl = this.role === 'pilot'
                    ? '/Pilot/ExpoCompletion'
                    : '/Registerforer/ExpoCompletion';
                window.location.href = completionUrl;
            }, delay);
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
    if (!tracker) {
        console.log('[Task Tracker] No tracker element found');
        return;
    }

    // Get role from URL parameter or data attribute
    const params = new URLSearchParams(window.location.search);
    const role = params.get('role') || tracker.getAttribute('data-role');

    console.log('[Task Tracker] Detected role:', role);
    console.log('[Task Tracker] URL params role:', params.get('role'));
    console.log('[Task Tracker] data-role attribute:', tracker.getAttribute('data-role'));

    if (role) {
        // Wait for role-specific task definitions to load
        setTimeout(() => {
            window.expoTracker = new ExpoTaskTracker(role);
        }, 100);
    } else {
        console.warn('[Task Tracker] No role detected!');
    }
});
