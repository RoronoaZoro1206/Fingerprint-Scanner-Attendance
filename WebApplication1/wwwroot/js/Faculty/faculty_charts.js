/**
 * Faculty Dashboard Charts using Chart.js
 * Displays today's attendance, weekly trend, and punctuality analysis
 */

let todayAttendanceChart = null;
let weeklyAttendanceChart = null;
let punctualityChart = null;

document.addEventListener('DOMContentLoaded', function () {
    // Load all charts
    loadTodayAttendanceChart();
    loadWeeklyAttendanceChart();
    loadPunctualityChart();

    // Refresh charts every 5 minutes
    setInterval(loadTodayAttendanceChart, 300000);
    setInterval(loadWeeklyAttendanceChart, 300000);
    setInterval(loadPunctualityChart, 300000);
});

/**
 * Load Today's Attendance Status (Doughnut Chart)
 */
async function loadTodayAttendanceChart() {
  try {
        const response = await fetch('/api/faculty/chart/today-attendance');
   
        if (!response.ok) {
       throw new Error(`HTTP error! status: ${response.status}`);
    }
   
 const data = await response.json();
 
        const ctx = document.getElementById('todayAttendanceChart');
    if (!ctx) return;
     
        // Destroy existing chart if exists
 if (todayAttendanceChart) {
   todayAttendanceChart.destroy();
     }
        
        // Create new chart
        todayAttendanceChart = new Chart(ctx, {
type: 'doughnut',
            data: {
    labels: data.labels,
     datasets: [{
      label: 'Students',
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
    text: "Today's Attendance Status",
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
        
        console.log("Today's attendance chart loaded");
} catch (error) {
console.error("Error loading today's attendance chart:", error);
    const ctx = document.getElementById('todayAttendanceChart');
        if (ctx) {
       ctx.parentElement.innerHTML = '<p class="text-center text-muted">Failed to load chart</p>';
        }
    }
}

/**
 * Load Weekly Attendance Trend (Line Chart)
 */
async function loadWeeklyAttendanceChart() {
    try {
  const response = await fetch('/api/faculty/chart/weekly-attendance');
   
        if (!response.ok) {
throw new Error(`HTTP error! status: ${response.status}`);
      }
        
        const data = await response.json();
    
        const ctx = document.getElementById('weeklyAttendanceChart');
        if (!ctx) return;
        
    // Destroy existing chart if exists
        if (weeklyAttendanceChart) {
      weeklyAttendanceChart.destroy();
        }
 
   // Create new chart
        weeklyAttendanceChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: data.labels,
             datasets: [
      {
             label: 'Present',
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
      text: 'Weekly Attendance Trend (Last 7 Days)',
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
        
   console.log('Weekly attendance chart loaded');
    } catch (error) {
        console.error('Error loading weekly attendance chart:', error);
 const ctx = document.getElementById('weeklyAttendanceChart');
        if (ctx) {
        ctx.parentElement.innerHTML = '<p class="text-center text-muted">Failed to load chart</p>';
        }
    }
}

/**
 * Load Punctuality Analysis (Bar Chart)
 */
async function loadPunctualityChart() {
    try {
        const response = await fetch('/api/faculty/chart/punctuality');
        
        if (!response.ok) {
throw new Error(`HTTP error! status: ${response.status}`);
        }
      
 const data = await response.json();
 
        const ctx = document.getElementById('punctualityChart');
        if (!ctx) return;
        
        // Destroy existing chart if exists
        if (punctualityChart) {
          punctualityChart.destroy();
        }
        
        // Create new chart
        punctualityChart = new Chart(ctx, {
       type: 'bar',
  data: {
      labels: data.labels,
 datasets: [{
       label: 'Students',
       data: data.data,
 backgroundColor: [
      'rgba(34, 197, 94, 0.8)',   // On Time - Green
      'rgba(234, 179, 8, 0.8)'    // Late - Yellow
         ],
       borderColor: [
  'rgb(34, 197, 94)',
             'rgb(234, 179, 8)'
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
     text: 'Student Punctuality (Last 7 Days)',
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
        
        console.log('Punctuality chart loaded');
    } catch (error) {
 console.error('Error loading punctuality chart:', error);
        const ctx = document.getElementById('punctualityChart');
        if (ctx) {
            ctx.parentElement.innerHTML = '<p class="text-center text-muted">Failed to load chart</p>';
        }
 }
}
