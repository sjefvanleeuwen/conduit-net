import './styles/main.scss';
import './components/conduit-hero';

console.log('ConduitNet Stats Page Loaded');

interface TestDetail {
    name: string;
    outcome: string;
    duration: string;
    errorMessage?: string;
}

interface TestResults {
    total: number;
    passed: number;
    failed: number;
    skipped: number;
    timestamp: string;
    tests: TestDetail[];
}

document.addEventListener('DOMContentLoaded', async () => {
    try {
        const response = await fetch('/test-results.json');
        if (!response.ok) {
            throw new Error('Failed to load test results');
        }

        const data: TestResults = await response.json();

        // Update Stats
        document.getElementById('total-tests')!.innerText = data.total.toString();
        document.getElementById('passed-tests')!.innerText = data.passed.toString();
        document.getElementById('failed-tests')!.innerText = data.failed.toString();
        
        // Update Timestamp
        document.getElementById('generated-at')!.innerText = new Date(data.timestamp).toLocaleString();

        // Render Test List
        const listContainer = document.getElementById('test-list');
        if (listContainer) {
            listContainer.innerHTML = ''; // Clear loading message

            if (data.tests && data.tests.length > 0) {
                const table = document.createElement('table');
                table.className = 'alt'; // HTML5 UP style
                table.style.width = '100%';
                table.style.tableLayout = 'fixed';
                
                const thead = document.createElement('thead');
                thead.innerHTML = `
                    <tr>
                        <th style="width: 60px; text-align: center;">Status</th>
                        <th>Test Name</th>
                        <th style="width: 100px; text-align: right;">Duration</th>
                    </tr>
                `;
                table.appendChild(thead);

                const tbody = document.createElement('tbody');
                data.tests.forEach(test => {
                    const tr = document.createElement('tr');
                    
                    // Status Icon/Color
                    let statusColor = '#ffffff';
                    let statusIcon = 'fa-question-circle';
                    
                    if (test.outcome === 'Passed') {
                        statusColor = '#39c088'; // Green
                        statusIcon = 'fa-check-circle';
                    } else if (test.outcome === 'Failed') {
                        statusColor = '#e44c65'; // Red
                        statusIcon = 'fa-times-circle';
                    } else {
                        statusColor = '#e4b34c'; // Yellow/Orange
                        statusIcon = 'fa-minus-circle';
                    }

                    // Format duration
                    let durationDisplay = test.duration;
                    const durationMatch = durationDisplay.match(/00:00:(\d+\.\d+)/);
                    if (durationMatch) {
                         durationDisplay = parseFloat(durationMatch[1]).toFixed(3) + 's';
                    }

                    tr.innerHTML = `
                        <td style="color: ${statusColor}; text-align: center; vertical-align: middle;">
                            <i class="icon solid ${statusIcon}"></i>
                        </td>
                        <td style="word-wrap: break-word; overflow-wrap: break-word; white-space: normal;">
                            <strong>${test.name}</strong>
                            ${test.errorMessage ? `<div style="color: #e44c65; font-size: 0.8em; margin-top: 0.5em; word-break: break-word;">${test.errorMessage}</div>` : ''}
                        </td>
                        <td style="text-align: right; vertical-align: middle;">${durationDisplay}</td>
                    `;
                    tbody.appendChild(tr);
                });
                table.appendChild(tbody);
                listContainer.appendChild(table);
            } else {
                listContainer.innerHTML = '<p>No detailed test results available.</p>';
            }
        }

    } catch (error) {
        console.error(error);
        const listContainer = document.getElementById('test-list');
        if (listContainer) listContainer.innerText = 'Error loading test results.';
    }
});
