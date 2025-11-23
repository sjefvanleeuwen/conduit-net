import { FilterOperator, LogicOperator, ConduitQuery } from 'conduit-ts-client';

export class QueryBuilder extends HTMLElement {
    private _fields: string[] = [];
    private _query: ConduitQuery = {
        filters: [],
        sorts: [],
        selectFields: [],
        includes: [],
        groupBy: [],
        aggregates: [],
        customProperties: {}
    };

    static get observedAttributes() {
        return ['fields'];
    }

    set fields(value: string[]) {
        this._fields = value;
        this.render();
    }

    get value(): ConduitQuery {
        return this._query;
    }

    connectedCallback() {
        this.render();
        this.setupEventListeners();
    }

    attributeChangedCallback(name: string, oldValue: string, newValue: string) {
        if (name === 'fields' && oldValue !== newValue) {
            try {
                this._fields = JSON.parse(newValue);
                this.render();
            } catch (e) {
                console.error('Invalid fields attribute', e);
            }
        }
    }

    private setupEventListeners() {
        this.addEventListener('click', (e) => {
            const target = e.target as HTMLElement;
            if (target.matches('.add-filter-btn')) {
                this.addFilter();
            } else if (target.matches('.remove-filter-btn')) {
                const index = parseInt(target.dataset.index || '0');
                this.removeFilter(index);
            } else if (target.matches('.add-sort-btn')) {
                this.addSort();
            } else if (target.matches('.remove-sort-btn')) {
                const index = parseInt(target.dataset.index || '0');
                this.removeSort(index);
            }
        });

        this.addEventListener('change', (e) => {
            const target = e.target as HTMLInputElement | HTMLSelectElement;
            if (target.matches('.filter-field')) {
                const index = parseInt(target.dataset.index || '0');
                this._query.filters[index].fieldName = target.value;
            } else if (target.matches('.filter-operator')) {
                const index = parseInt(target.dataset.index || '0');
                this._query.filters[index].operator = target.value as FilterOperator;
            } else if (target.matches('.filter-value')) {
                const index = parseInt(target.dataset.index || '0');
                this._query.filters[index].value = target.value;
            } else if (target.matches('.sort-field')) {
                const index = parseInt(target.dataset.index || '0');
                this._query.sorts[index].fieldName = target.value;
            } else if (target.matches('.sort-dir')) {
                const index = parseInt(target.dataset.index || '0');
                this._query.sorts[index].isDescending = target.value === 'desc';
            }
            
            this.dispatchEvent(new CustomEvent('query-change', { detail: this._query }));
        });
    }

    private addFilter() {
        this._query.filters.push({
            fieldName: this._fields[0] || '',
            operator: FilterOperator.Eq,
            value: '',
            logic: LogicOperator.And,
            group: []
        });
        this.render();
        this.dispatchEvent(new CustomEvent('query-change', { detail: this._query }));
    }

    private removeFilter(index: number) {
        this._query.filters.splice(index, 1);
        this.render();
        this.dispatchEvent(new CustomEvent('query-change', { detail: this._query }));
    }

    private addSort() {
        this._query.sorts.push({
            fieldName: this._fields[0] || '',
            isDescending: false
        });
        this.render();
        this.dispatchEvent(new CustomEvent('query-change', { detail: this._query }));
    }

    private removeSort(index: number) {
        this._query.sorts.splice(index, 1);
        this.render();
        this.dispatchEvent(new CustomEvent('query-change', { detail: this._query }));
    }

    render() {
        this.innerHTML = `
            <div class="query-builder">
                <div class="qb-section">
                    <h4>Filters</h4>
                    <div class="qb-filters">
                        ${this._query.filters.map((f, i) => `
                            <div class="qb-row filter-row">
                                <select class="filter-field" data-index="${i}">
                                    ${this._fields.map(field => `<option value="${field}" ${field === f.fieldName ? 'selected' : ''}>${field}</option>`).join('')}
                                </select>
                                <select class="filter-operator" data-index="${i}">
                                    ${Object.values(FilterOperator).map(op => `<option value="${op}" ${op === f.operator ? 'selected' : ''}>${op}</option>`).join('')}
                                </select>
                                <input type="text" class="filter-value" data-index="${i}" value="${f.value}" placeholder="Value">
                                <button class="btn-sm btn-danger remove-filter-btn" data-index="${i}">&times;</button>
                            </div>
                        `).join('')}
                    </div>
                    <button class="btn-sm add-filter-btn">+ Add Filter</button>
                </div>

                <div class="qb-section" style="margin-top: 1rem;">
                    <h4>Sorting</h4>
                    <div class="qb-sorts">
                        ${this._query.sorts.map((s, i) => `
                            <div class="qb-row sort-row">
                                <select class="sort-field" data-index="${i}">
                                    ${this._fields.map(field => `<option value="${field}" ${field === s.fieldName ? 'selected' : ''}>${field}</option>`).join('')}
                                </select>
                                <select class="sort-dir" data-index="${i}">
                                    <option value="asc" ${!s.isDescending ? 'selected' : ''}>Ascending</option>
                                    <option value="desc" ${s.isDescending ? 'selected' : ''}>Descending</option>
                                </select>
                                <button class="btn-sm btn-danger remove-sort-btn" data-index="${i}">&times;</button>
                            </div>
                        `).join('')}
                    </div>
                    <button class="btn-sm add-sort-btn">+ Add Sort</button>
                </div>
            </div>
            <style>
                .query-builder {
                    background: var(--bg-panel);
                    padding: 1rem;
                    border-radius: 4px;
                    border: 1px solid var(--border);
                }
                .qb-row {
                    display: flex;
                    gap: 0.5rem;
                    margin-bottom: 0.5rem;
                    align-items: center;
                }
                .qb-row select, .qb-row input {
                    padding: 0.4rem;
                    border: 1px solid var(--border);
                    border-radius: 3px;
                    background: var(--bg-input);
                    color: var(--fg);
                }
                .qb-row input {
                    flex: 1;
                }
            </style>
        `;
    }
}

customElements.define('query-builder', QueryBuilder);
