/**
 * Faculty Dashboard Charts using Chart.js
 * Displays today's attendance, weekly trend, and punctuality analysis
 * ? Always renders charts, even with empty data
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
 * ✅ UPDATED: Now includes 'Excused' status with blue color
 * ? Always renders chart, even with empty data
 */
async function loadTodayAttendanceChart() {
    try {
        const response = await fetch('/api/faculty/chart/today-attendance');
   
        let data = { labels: [], data: [] };
        
        if (response.ok) {
            data = await response.json();
        } else {
            console.warn(`Today's attendance API returned status: ${response.status}`);
            // Use default empty structure
            data = { 
                labels: ['Present', 'Absent', 'Late', 'Excused'], 
                data: [0, 0, 0, 0] 
            };
        }
 
        const ctx = document.getElementById('todayAttendanceChart');
        if (!ctx) return;
     
        // Destroy existing chart if exists
        if (todayAttendanceChart) {
            todayAttendanceChart.destroy();
        }
        
        // ✅ Always create chart, even with empty data
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
                        'rgba(234, 179, 8, 0.8)',   // Late - Yellow
                        'rgba(59, 130, 246, 0.8)'   // Excused - Blue
                    ],
                    borderColor: [
                        'rgb(34, 197, 94)',
                        'rgb(239, 68, 68)',
                        'rgb(234, 179, 8)',
                        'rgb(59, 130, 246)'
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
     
        // ? Still create chart with empty data on error
        const ctx = document.getElementById('todayAttendanceChart');
        if (ctx && !todayAttendanceChart) {
            todayAttendanceChart = new Chart(ctx, {
                type: 'doughnut',
                data: {
                    labels: ['Present', 'Absent', 'Late', 'Excused'],
                    datasets: [{
                        label: 'Students',
                        data: [0, 0, 0, 0],
                        backgroundColor: [
                            'rgba(34, 197, 94, 0.8)',
                            'rgba(239, 68, 68, 0.8)',
                            'rgba(234, 179, 8, 0.8)',
                            'rgba(59, 130, 246, 0.8)'
                        ]
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        title: {
                            display: true,
                            text: "Today's Attendance Status (No Data)",
                            font: { size: 16, weight: 'bold' }
                        }
                    }
                }
            });
        }
    }
}

/**
 * Load Weekly Attendance Trend (Line Chart)
 * ✅ UPDATED: Now includes 'Excused' status with blue color
 * ✅ FIXED: Correctly maps datasets by label instead of index
 * ? Always renders chart, even with empty data
 */
async function loadWeeklyAttendanceChart() {
    try {
        const response = await fetch('/api/faculty/chart/weekly-attendance');
   
        let data = { labels: [], datasets: [] };
        
        if (response.ok) {
            data = await response.json();
            console.log('✅ Weekly Attendance API Response:', data); // Debug log
        } else {
            console.warn(`Weekly attendance API returned status: ${response.status}`);
            // Use default empty structure
            data = {
                labels: [],
                datasets: [
                    { label: 'Present', data: [] },
                    { label: 'Late', data: [] },
                    { label: 'Absent', data: [] },
                    { label: 'Excused', data: [] }
                ]
            };
        }
    
        const ctx = document.getElementById('weeklyAttendanceChart');
        if (!ctx) return;
        
        // Destroy existing chart if exists
        if (weeklyAttendanceChart) {
            weeklyAttendanceChart.destroy();
        }
 
        // ✅ FIX: Extract data by finding the correct dataset by label
        const findDataset = (label) => {
            const dataset = data.datasets?.find(d => d.label === label);
            return dataset?.data || [];
        };

        const presentData = findDataset('Present');
        const lateData = findDataset('Late');
        const absentData = findDataset('Absent');
        const excusedData = findDataset('Excused');

        console.log('✅ Mapped Data:', { presentData, lateData, absentData, excusedData }); // Debug log

        // ✅ Always create chart, even with empty data
        weeklyAttendanceChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: data.labels,
                datasets: [
                    {
                        label: 'Present',
                        data: presentData,
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
                        data: lateData,
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
                        data: absentData,
                        backgroundColor: 'rgba(239, 68, 68, 0.1)',
                        borderColor: 'rgb(239, 68, 68)',
                        borderWidth: 2,
                        fill: true,
                        tension: 0.4,
                        pointRadius: 4,
                        pointHoverRadius: 6
                    },
                    {
                        label: 'Excused',
                        data: excusedData,
                        backgroundColor: 'rgba(59, 130, 246, 0.1)',
                        borderColor: 'rgb(59, 130, 246)',
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
        
        // ? Still create chart with empty data on error
        const ctx = document.getElementById('weeklyAttendanceChart');
        if (ctx && !weeklyAttendanceChart) {
            weeklyAttendanceChart = new Chart(ctx, {
                type: 'line',
                data: {
                    labels: [],
                    datasets: [
                        { label: 'Present', data: [], borderColor: 'rgb(34, 197, 94)', fill: false },
                        { label: 'Late', data: [], borderColor: 'rgb(234, 179, 8)', fill: false },
                        { label: 'Absent', data: [], borderColor: 'rgb(239, 68, 68)', fill: false },
                        { label: 'Excused', data: [], borderColor: 'rgb(59, 130, 246)', fill: false }
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        title: {
                            display: true,
                            text: 'Weekly Attendance Trend (No Data)',
                            font: { size: 16, weight: 'bold' }
                        }
                    },
                    scales: {
                        y: { beginAtZero: true },
                        x: { }
                    }
                }
            });
        }
    }
}

/**
 * Load Punctuality Analysis (Bar Chart)
 * ? Always renders chart, even with empty data
 */
async function loadPunctualityChart() {
    try {
        const response = await fetch('/api/faculty/chart/punctuality');
        
   let data = { labels: [], data: [] };
        
if (response.ok) {
            data = await response.json();
        } else {
       console.warn(`Punctuality API returned status: ${response.status}`);
            // Use default empty structure
  data = { 
         labels: ['On Time', 'Late'], 
    data: [0, 0] 
            };
    }
 
        const ctx = document.getElementById('punctualityChart');
        if (!ctx) return;

        // Destroy existing chart if exists
        if (punctualityChart) {
            punctualityChart.destroy();
        }
 
        // ? Always create chart, even with empty data
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
        
        // ? Still create chart with empty data on error
     const ctx = document.getElementById('punctualityChart');
  if (ctx && !punctualityChart) {
        punctualityChart = new Chart(ctx, {
           type: 'bar',
        data: {
            labels: ['On Time', 'Late'],
            datasets: [{
        label: 'Students',
       data: [0, 0],
         backgroundColor: [
               'rgba(34, 197, 94, 0.8)',
              'rgba(234, 179, 8, 0.8)'
            ]
          }]
              },
    options: {
        responsive: true,
         maintainAspectRatio: false,
      plugins: {
          title: {
        display: true,
             text: 'Student Punctuality (No Data)',
       font: { size: 16, weight: 'bold' }
      }
        },
        scales: {
   y: { beginAtZero: true }
        }
      }
            });
      }
    }
}
