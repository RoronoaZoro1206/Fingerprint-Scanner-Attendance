/**
 * Student Dashboard Charts using Chart.js
 * Displays personal attendance overview, punctuality trend, and course-wise attendance
 * ✅ ENHANCED: Now includes SignalR real-time connection status
 * ? Always renders charts, even with empty data
 */

let attendanceOverviewChart = null;
let punctualityTrendChart = null;
let courseAttendanceChart = null;

document.addEventListener('DOMContentLoaded', async function () {
    // Load all charts
    loadAttendanceOverviewChart();
    loadPunctualityTrendChart();
    loadCourseAttendanceChart();

    // ✅ NEW: Initialize SignalR connection for real-time updates
    try {
        await signalRConnection.initialize();
        console.log('✅ SignalR connected - Student Dashboard ready for real-time updates');

        // ✅ Listen for real-time attendance updates
        signalRConnection.onAttendanceUpdate((data) => {
            console.log('📡 Real-time attendance notification received:', data);
            
            // Show toast notification
            const actionText = data.action === 'time_in' ? 'Time In' : 'Time Out';
            Toast.success(`${data.studentName} - ${actionText} (${data.courseCode})`);
            
            // Refresh charts (attendance data updated)
            setTimeout(() => {
                loadAttendanceOverviewChart();
                loadPunctualityTrendChart();
                loadCourseAttendanceChart();
            }, 1000);
        });

        console.log('✅ SignalR attendance listener registered');
    } catch (error) {
        console.error('❌ SignalR connection failed (non-critical):', error);
        // Dashboard will still work without real-time updates
    }

    // Refresh charts every 5 minutes
    setInterval(loadAttendanceOverviewChart, 300000);
    setInterval(loadPunctualityTrendChart, 300000);
    setInterval(loadCourseAttendanceChart, 300000);
});

/**
 * Load Personal Attendance Overview (Doughnut Chart)
 */
async function loadAttendanceOverviewChart() {
    try {
        const response = await fetch('/api/student/chart/attendance-overview');
   
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        
        // ✅ DEBUG: Log the actual data received from API
        console.log('Attendance Overview API Response:', data);
        console.log('Labels:', data.labels);
        console.log('Data:', data.data);
        
        const ctx = document.getElementById('attendanceOverviewChart');
        if (!ctx) return;
        
        // Destroy existing chart if exists
        if (attendanceOverviewChart) {
            attendanceOverviewChart.destroy();
        }
   
        // Create new chart
        attendanceOverviewChart = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: data.labels,
                datasets: [{
                    label: 'Sessions',
                    data: data.data,
                    backgroundColor: [
                        'rgba(34, 197, 94, 0.8)',   // Present - Green
                        'rgba(239, 68, 68, 0.8)',   // Absent - Red
                        'rgba(234, 179, 8, 0.8)'    // Late - Yellow
                    ],
                    borderColor: [
                        'rgb(34, 197, 94)',
                        'rgb(239, 68, 68)',
                        'rgb(234, 179, 8)'
                    ],
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            padding: 15,
                            font: {
                                size: 12
                            }
                        }
                    },
                    title: {
                        display: true,
                        text: 'My Attendance Overview',
                        font: {
                            size: 16,
                            weight: 'bold'
                        },
                        padding: {
                            bottom: 20
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                const label = context.label || '';
                                const value = context.parsed || 0;
                                const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : 0;
                                return `${label}: ${value} (${percentage}%)`;
                            }
                        }
                    }
                }
            }
        });
        
        console.log('Attendance overview chart loaded');
    } catch (error) {
        console.error('Error loading attendance overview chart:', error);
        const ctx = document.getElementById('attendanceOverviewChart');
        if (ctx) {
            ctx.parentElement.innerHTML = '<p class="text-center text-muted">Failed to load chart</p>';
        }
    }
}

/**
 * Load Punctuality Trend (Line Chart)
 */
async function loadPunctualityTrendChart() {
    try {
        const response = await fetch('/api/student/chart/punctuality-trend');
   
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
      
        const data = await response.json();
        
        // ✅ DEBUG: Log the actual data received from API
        console.log('Punctuality Trend API Response:', data);
        console.log('Labels:', data.labels);
        console.log('Datasets:', data.datasets);
        
        const ctx = document.getElementById('punctualityTrendChart');
        if (!ctx) return;
     
        // Destroy existing chart if exists
        if (punctualityTrendChart) {
            punctualityTrendChart.destroy();
        }
        
        // Create new chart
        punctualityTrendChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: data.labels,
                datasets: [
                    {
                        label: 'On Time',
                        data: data.datasets[0].data,
                        backgroundColor: 'rgba(34, 197, 94, 0.1)',
                        borderColor: 'rgb(34, 197, 94)',
                        borderWidth: 2,
                        fill: true,
                        tension: 0.4,
                        pointRadius: 4,
                        pointHoverRadius: 6
                    },
                    {
                        label: 'Late',
                        data: data.datasets[1].data,
                        backgroundColor: 'rgba(234, 179, 8, 0.1)',
                        borderColor: 'rgb(234, 179, 8)',
                        borderWidth: 2,
                        fill: true,
                        tension: 0.4,
                        pointRadius: 4,
                        pointHoverRadius: 6
                    },
                    {
                        label: 'Absent',
                        data: data.datasets[2].data,
                        backgroundColor: 'rgba(239, 68, 68, 0.1)',
                        borderColor: 'rgb(239, 68, 68)',
                        borderWidth: 2,
                        fill: true,
                        tension: 0.4,
                        pointRadius: 4,
                        pointHoverRadius: 6
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: {
                    intersect: false,
                    mode: 'index'
                },
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            padding: 15,
                            font: {
                                size: 12
                            }
                        }
                    },
                    title: {
                        display: true,
                        text: 'My Punctuality Trend (Last 14 Days)',
                        font: {
                            size: 16,
                            weight: 'bold'
                        },
                        padding: {
                            bottom: 20
                        }
                    },
                    tooltip: {
                        backgroundColor: 'rgba(0, 0, 0, 0.8)',
                        padding: 12
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            precision: 0
                        },
                        grid: {
                            color: 'rgba(0, 0, 0, 0.05)'
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        }
                    }
                }
            }
        });
        
        console.log('Punctuality trend chart loaded');
    } catch (error) {
        console.error('Error loading punctuality trend chart:', error);
        const ctx = document.getElementById('punctualityTrendChart');
        if (ctx) {
            ctx.parentElement.innerHTML = '<p class="text-center text-muted">Failed to load chart</p>';
        }
    }
}

/**
 * Load Course-wise Attendance Rate (Bar Chart)
 */
async function loadCourseAttendanceChart() {
    try {
        const response = await fetch('/api/student/chart/course-attendance');
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        
        // ✅ DEBUG: Log the actual data received from API
        console.log('Course Attendance API Response:', data);
        console.log('Labels:', data.labels);
        console.log('Data:', data.data);
        
        const ctx = document.getElementById('courseAttendanceChart');
        if (!ctx) return;
 
        // Destroy existing chart if exists
        if (courseAttendanceChart) {
            courseAttendanceChart.destroy();
        }
     
        // Create new chart
        courseAttendanceChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: data.labels,
                datasets: [{
                    label: 'Attendance Rate (%)',
                    data: data.data,
                    backgroundColor: data.data.map(value => {
                        if (value >= 90) return 'rgba(34, 197, 94, 0.8)';   // Green
                        if (value >= 75) return 'rgba(234, 179, 8, 0.8)';  // Yellow
                        return 'rgba(239, 68, 68, 0.8)';   // Red
                    }),
                    borderColor: data.data.map(value => {
                        if (value >= 90) return 'rgb(34, 197, 94)';
                        if (value >= 75) return 'rgb(234, 179, 8)';
                        return 'rgb(239, 68, 68)';
                    }),
                    borderWidth: 2,
                    borderRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    },
                    title: {
                        display: true,
                        text: 'Course-wise Attendance Rate',
                        font: {
                            size: 16,
                            weight: 'bold'
                        },
                        padding: {
                            bottom: 20
                        }
                    },
                    tooltip: {
                        backgroundColor: 'rgba(0, 0, 0, 0.8)',
                        padding: 12,
                        displayColors: false,
                        callbacks: {
                            label: function(context) {
                                return `Attendance: ${context.parsed.y}%`;
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        max: 100,
                        ticks: {
                            callback: function(value) {
                                return value + '%';
                            }
                        },
                        grid: {
                            color: 'rgba(0, 0, 0, 0.05)'
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        }
                    }
                }
            }
        });
        
        console.log('Course attendance chart loaded');
    } catch (error) {
        console.error('Error loading course attendance chart:', error);
        const ctx = document.getElementById('courseAttendanceChart');
        if (ctx) {
            ctx.parentElement.innerHTML = '<p class="text-center text-muted">Failed to load chart</p>';
        }
    }
}
