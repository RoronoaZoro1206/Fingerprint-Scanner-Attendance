/**
 * Common Export Utilities
 * Shared export functions for print, CSV, Excel, PDF
 */

/**
 * Print data in a formatted table
 * @param {string} title - Title of the document
 * @param {Array} data - Array of objects to print
 */
function printData(title, data) {
    const printWindow = window.open('', '_blank');
    
    if (data.length === 0) {
        Toast.warning('No data to print');
        printWindow.close();
        return;
    }

    const headers = Object.keys(data[0]);
    
    let html = `
        <!DOCTYPE html>
        <html>
        <head>
            <title>${title}</title>
            <style>
                body { font-family: Arial, sans-serif; padding: 20px; }
                h1 { color: #1a1a1a; margin-bottom: 20px; }
                table { width: 100%; border-collapse: collapse; margin-top: 20px; }
                th, td { padding: 12px; text-align: left; border: 1px solid #ddd; }
                th { background-color: #f9fafb; font-weight: 600; }
                tr:nth-child(even) { background-color: #f9fafb; }
                @media print {
                    button { display: none; }
                }
            </style>
        </head>
        <body>
            <h1>${title}</h1>
            <p>Generated on ${new Date().toLocaleString()}</p>
            <table>
                <thead>
                    <tr>`;
    
    headers.forEach(header => {
        html += `<th>${header}</th>`;
    });
    
    html += `</tr>
                </thead>
                <tbody>`;
    
    data.forEach(row => {
        html += '<tr>';
        headers.forEach(header => {
            html += `<td>${row[header] || ''}</td>`;
        });
        html += '</tr>';
    });
    
    html += `
                </tbody>
            </table>
            <script>
                window.onload = function() { window.print(); }
            </script>
        </body>
        </html>`;
    
    printWindow.document.write(html);
    printWindow.document.close();
}

/**
 * Export data to CSV
 * @param {string} filename - Name of the file (without extension)
 * @param {Array} data - Array of objects to export
 */
function exportToCSVFile(filename, data) {
    if (data.length === 0) {
        Toast.warning('No data to export');
        return;
    }
    
    const headers = Object.keys(data[0]);
    let csv = headers.join(',') + '\n';
    
    data.forEach(row => {
        const values = headers.map(header => {
            const value = row[header] || '';
            return `"${value.toString().replace(/"/g, '""')}"`;
        });
        csv += values.join(',') + '\n';
    });
    
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = `${filename}_${new Date().getTime()}.csv`;
    link.click();
    Toast.success('CSV exported successfully');
}

/**
 * Export data to Excel
 * @param {string} filename - Name of the file (without extension)
 * @param {Array} data - Array of objects to export
 */
function exportToExcelFile(filename, data) {
    if (data.length === 0) {
        Toast.warning('No data to export');
        return;
    }
    
    const headers = Object.keys(data[0]);
    let html = '<table><thead><tr>';
    headers.forEach(header => {
        html += `<th>${header}</th>`;
    });
    html += '</tr></thead><tbody>';
    
    data.forEach(row => {
        html += '<tr>';
        headers.forEach(header => {
            html += `<td>${row[header] || ''}</td>`;
        });
        html += '</tr>';
    });
    html += '</tbody></table>';
    
    const blob = new Blob([html], { type: 'application/vnd.ms-excel' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = `${filename}_${new Date().getTime()}.xls`;
    link.click();
    Toast.success('Excel exported successfully');
}

/**
 * Export data to PDF (fallback to print)
 * @param {string} title - Title of the document
 * @param {Array} data - Array of objects to export
 */
function exportToPDFFile(title, data) {
    Toast.info('PDF export requires additional library. Using print instead.');
    printData(title, data);
}
