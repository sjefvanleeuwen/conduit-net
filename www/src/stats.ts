import './styles/main.scss';
import './components/conduit-hero';

console.log('ConduitNet Stats Page Loaded');

interface TestDetail {
    name: string;
    outcome: string;
    duration: string;
    errorMessage?: string;
}

interface BuildInfo {
    runNumber: string;
    commitHash: string;
    branch: string;
    actor: string;
    workflow: string;
}

interface TestResults {
    total: number;
    passed: number;
    failed: number;
    skipped: number;
    timestamp: string;
    buildInfo?: BuildInfo;
    buildLog?: string;
    tests: TestDetail[];
}

document.addEventListener('DOMContentLoaded', async () => {
    try {
        const response = await fetch('/test-results.json');
        if (!response.ok) {
            throw new Error('Failed to load test results');
        }

        const data: TestResults = await response.json();

        // Filter tests
        const allUnitTests = data.tests ? data.tests.filter(t => t.name.startsWith('Unit - ')) : [];
        const allE2eTests = data.tests ? data.tests.filter(t => t.name.startsWith('E2E - ')) : [];

        // Update Stats Tiles
        const updateTile = (id: string, total: number, unit: number, e2e: number) => {
            const el = document.getElementById(id);
            if (el) {
                el.style.display = 'flex';
                el.style.alignItems = 'center';
                el.style.justifyContent = 'center';
                el.style.gap = '0.5em';

                el.innerHTML = `
                    <span>${total}</span>
                    <div style="font-size: 0.4em; line-height: 1.2em; text-align: left; opacity: 0.7; font-weight: normal; display: grid; grid-template-columns: auto auto; gap: 0 0.5em;">
                        <span>Unit:</span> <span style="text-align: right; font-variant-numeric: tabular-nums;">${unit}</span>
                        <span>E2E:</span> <span style="text-align: right; font-variant-numeric: tabular-nums;">${e2e}</span>
                    </div>
                `;
            }
        };

        updateTile('total-tests', data.total, allUnitTests.length, allE2eTests.length);
        
        const passedUnit = allUnitTests.filter(t => t.outcome === 'Passed').length;
        const passedE2E = allE2eTests.filter(t => t.outcome === 'Passed').length;
        updateTile('passed-tests', data.passed, passedUnit, passedE2E);

        const failedUnit = allUnitTests.filter(t => t.outcome === 'Failed').length;
        const failedE2E = allE2eTests.filter(t => t.outcome === 'Failed').length;
        updateTile('failed-tests', data.failed, failedUnit, failedE2E);
        
        // Update Timestamp
        document.getElementById('generated-at')!.innerText = new Date(data.timestamp).toLocaleString();

        // Render Build Info
        if (data.buildInfo) {
            const buildInfoContainer = document.getElementById('build-info');
            if (buildInfoContainer) {
                buildInfoContainer.innerHTML = `
                    <header class="major">
                        <h2>Build Information</h2>
                    </header>
                    <div class="row">
                        <div class="col-3 col-6-medium col-12-small">
                            <section class="box" style="text-align: center;">
                                <header><h3>Run</h3></header>
                                <p>#${data.buildInfo.runNumber}</p>
                            </section>
                        </div>
                        <div class="col-3 col-6-medium col-12-small">
                            <section class="box" style="text-align: center;">
                                <header><h3>Commit</h3></header>
                                <p>${data.buildInfo.commitHash}</p>
                            </section>
                        </div>
                        <div class="col-3 col-6-medium col-12-small">
                            <section class="box" style="text-align: center;">
                                <header><h3>Branch</h3></header>
                                <p>${data.buildInfo.branch}</p>
                            </section>
                        </div>
                        <div class="col-3 col-6-medium col-12-small">
                            <section class="box" style="text-align: center;">
                                <header><h3>Actor</h3></header>
                                <p>${data.buildInfo.actor}</p>
                            </section>
                        </div>
                    </div>
                `;
            }
        }

        // Render Test List
        const listContainer = document.getElementById('test-list');
        if (listContainer) {
            listContainer.innerHTML = ''; // Clear loading message

            if (data.tests && data.tests.length > 0) {
                const unitTests = data.tests.filter(t => t.name.startsWith('Unit - '));
                const e2eTests = data.tests.filter(t => t.name.startsWith('E2E - '));

                const renderTable = (tests: TestDetail[], title: string) => {
                    if (tests.length === 0) return;

                    const header = document.createElement('h3');
                    header.innerText = title;
                    header.style.marginTop = '2em';
                    listContainer.appendChild(header);

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
                    tests.forEach(test => {
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

                        // Clean name
                        const displayName = test.name.replace(/^(Unit|E2E) - /, '');

                        tr.innerHTML = `
                            <td style="color: ${statusColor}; text-align: center; vertical-align: middle;">
                                <i class="icon solid ${statusIcon}"></i>
                            </td>
                            <td style="word-wrap: break-word; overflow-wrap: break-word; white-space: normal;">
                                <strong>${displayName}</strong>
                                ${test.errorMessage ? `<div style="color: #e44c65; font-size: 0.8em; margin-top: 0.5em; word-break: break-word;">${test.errorMessage}</div>` : ''}
                            </td>
                            <td style="text-align: right; vertical-align: middle;">${durationDisplay}</td>
                        `;
                        tbody.appendChild(tr);
                    });
                    table.appendChild(tbody);
                    listContainer.appendChild(table);
                };

                renderTable(unitTests, 'Unit Tests');
                renderTable(e2eTests, 'End-to-End Tests');

                // Render Build Log
                const logContainer = document.getElementById('build-log-container');
                if (logContainer) {
                    if (data.buildLog) {
                        logContainer.innerText = data.buildLog;
                    } else {
                        logContainer.innerHTML = '<em>No build log available.</em>';
                    }
                }

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
