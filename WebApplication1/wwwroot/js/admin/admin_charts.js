/**
 * Admin Dashboard Charts using Chart.js
 * Displays user role distribution, attendance trend, and fingerprint status
 */

let userRoleChart = null;
let attendanceTrendChart = null;
let fingerprintStatusChart = null;

document.addEventListener('DOMContentLoaded', function () {
  // Load all charts
    loadUserRoleChart();
 loadAttendanceTrendChart();
    loadFingerprintStatusChart();

    // Refresh charts every 5 minutes
    setInterval(loadUserRoleChart, 300000);
    setInterval(loadAttendanceTrendChart, 300000);
    setInterval(loadFingerprintStatusChart, 300000);
});

/**
 * Load User Role Distribution (Doughnut Chart)
 */
async function loadUserRoleChart() {
    try {
      const response = await fetch('/api/admin/chart/user-roles');
        
     if (!response.ok) {
       throw new Error(`HTTP error! status: ${response.status}`);
        }
    
        const data = await response.json();
   
        const ctx = document.getElementById('userRoleChart');
        if (!ctx) return;
        
        // Destroy existing chart if exists
        if (userRoleChart) {
     userRoleChart.destroy();
        }
        
        // Create new chart
  userRoleChart = new Chart(ctx, {
            type: 'doughnut',
   data: {
     labels: data.labels.map(label => label.charAt(0).toUpperCase() + label.slice(1)),
        datasets: [{
  label: 'User Count',
             data: data.data,
        backgroundColor: [
     'rgba(99, 102, 241, 0.8)',   // Admin - Indigo
         'rgba(34, 197, 94, 0.8)',    // Faculty - Green
            'rgba(234, 179, 8, 0.8)'     // Student - Yellow
   ],
       borderColor: [
      'rgb(99, 102, 241)',
              'rgb(34, 197, 94)',
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
          text: 'User Role Distribution',
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
   const percentage = ((value / total) * 100).toFixed(1);
   return `${label}: ${value} (${percentage}%)`;
    }
               }
   }
    }
    }
        });
      
        console.log('User role chart loaded');
    } catch (error) {
        console.error('Error loading user role chart:', error);
        const ctx = document.getElementById('userRoleChart');
      if (ctx) {
        ctx.parentElement.innerHTML = '<p class="text-center text-muted">Failed to load chart</p>';
     }
    }
}

/**
 * Load Attendance Trend (Line Chart)
 */
async function loadAttendanceTrendChart() {
 try {
  const response = await fetch('/api/admin/chart/attendance-trend?days=30');
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
    }
        
        const data = await response.json();
      
   const ctx = document.getElementById('attendanceTrendChart');
     if (!ctx) return;
        
        // Destroy existing chart if exists
        if (attendanceTrendChart) {
            attendanceTrendChart.destroy();
        }
   
   // Create new chart
        attendanceTrendChart = new Chart(ctx, {
            type: 'line',
  data: {
           labels: data.labels,
              datasets: [{
 label: 'Attendance Records',
  data: data.data,
         backgroundColor: 'rgba(59, 130, 246, 0.1)',
        borderColor: 'rgb(59, 130, 246)',
        borderWidth: 2,
     fill: true,
        tension: 0.4,
         pointRadius: 3,
          pointHoverRadius: 5,
          pointBackgroundColor: 'rgb(59, 130, 246)',
      pointBorderColor: '#fff',
 pointBorderWidth: 2
       }]
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
      display: false
        },
 title: {
    display: true,
  text: 'Daily Attendance Trend (Last 30 Days)',
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
           displayColors: false
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
        
        console.log('Attendance trend chart loaded');
 } catch (error) {
        console.error('Error loading attendance trend chart:', error);
  const ctx = document.getElementById('attendanceTrendChart');
  if (ctx) {
            ctx.parentElement.innerHTML = '<p class="text-center text-muted">Failed to load chart</p>';
     }
    }
}

/**
 * Load Fingerprint Registration Status (Bar Chart)
 */
async function loadFingerprintStatusChart() {
    try {
   const response = await fetch('/api/admin/chart/fingerprint-status');
        
        if (!response.ok) {
throw new Error(`HTTP error! status: ${response.status}`);
  }
        
        const data = await response.json();
        
        const ctx = document.getElementById('fingerprintStatusChart');
        if (!ctx) return;
    
        // Destroy existing chart if exists
        if (fingerprintStatusChart) {
            fingerprintStatusChart.destroy();
 }
        
     // Create new chart
        fingerprintStatusChart = new Chart(ctx, {
       type: 'bar',
            data: {
        labels: data.labels,
       datasets: [{
       label: 'Users',
     data: data.data,
       backgroundColor: [
         'rgba(34, 197, 94, 0.8)',   // Registered - Green
 'rgba(239, 68, 68, 0.8)'    // Not Registered - Red
    ],
      borderColor: [
            'rgb(34, 197, 94)',
 'rgb(239, 68, 68)'
     ],
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
    text: 'Fingerprint Registration Status',
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
              displayColors: false
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
        
        console.log('Fingerprint status chart loaded');
    } catch (error) {
  console.error('Error loading fingerprint status chart:', error);
        const ctx = document.getElementById('fingerprintStatusChart');
 if (ctx) {
   ctx.parentElement.innerHTML = '<p class="text-center text-muted">Failed to load chart</p>';
        }
    }
}
