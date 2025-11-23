// Conduit Query Protocol (CQP) - Fluent Client

// --- Enums ---
export enum FilterOperator {
    Eq = 'Eq',
    Neq = 'Neq',
    Gt = 'Gt',
    Lt = 'Lt',
    Gte = 'Gte',
    Lte = 'Lte',
    Contains = 'Contains',
    StartsWith = 'StartsWith',
    EndsWith = 'EndsWith',
    In = 'In',
    Any = 'Any',
    All = 'All'
}

export enum LogicOperator {
    And = 'And',
    Or = 'Or'
}

export enum AggregateType {
    Count = 'Count',
    Sum = 'Sum',
    Min = 'Min',
    Max = 'Max',
    Avg = 'Avg'
}

// --- DTOs ---
export interface QueryFilter {
    fieldName: string;
    operator: FilterOperator;
    value: any;
    logic: LogicOperator;
    group: QueryFilter[];
}

export interface QuerySort {
    fieldName: string;
    isDescending: boolean;
}

export interface QueryInclude {
    path: string;
    filter: ConduitQuery;
}

export interface QueryAggregate {
    type: AggregateType;
    fieldName: string;
    alias: string;
}

export interface ConduitQuery {
    filters: QueryFilter[];
    sorts: QuerySort[];
    skip?: number;
    take?: number;
    selectFields: string[];
    includes: QueryInclude[];
    groupBy: string[];
    aggregates: QueryAggregate[];
    customProperties: Record<string, string>;
}

// --- Fluent Builder Types ---

// Represents a field on an entity that can be filtered
export interface FieldProxy<T> {
    eq(value: T): void;
    neq(value: T): void;
    gt(value: T): void;
    lt(value: T): void;
    gte(value: T): void;
    lte(value: T): void;
    in(values: T[]): void;
}

export interface StringFieldProxy extends FieldProxy<string> {
    contains(value: string): void;
    startsWith(value: string): void;
    endsWith(value: string): void;
}

export interface NumberFieldProxy extends FieldProxy<number> {
    // Number specific if needed
}

export interface BooleanFieldProxy extends FieldProxy<boolean> {
    isTrue(): void;
    isFalse(): void;
}

export interface ArrayFieldProxy<T> {
    any(predicate: (item: EntityProxy<T>) => void): void;
    all(predicate: (item: EntityProxy<T>) => void): void;
}

// Maps an Entity type to a Proxy type where every field is a FieldProxy
export type EntityProxy<T> = {
    [P in keyof T]: T[P] extends string ? StringFieldProxy :
                    T[P] extends number ? NumberFieldProxy :
                    T[P] extends boolean ? BooleanFieldProxy :
                    T[P] extends Array<infer U> ? ArrayFieldProxy<U> :
                    FieldProxy<T[P]>;
};

// --- Implementation ---

export class ConduitContext {
    constructor(private transport: (query: ConduitQuery) => Promise<any>) {}

    public set<T>(tableName: string): DbSet<T> {
        return new DbSet<T>(tableName, this.transport);
    }
}

export class DbSet<T> {
    private query: ConduitQuery = {
        filters: [],
        sorts: [],
        selectFields: [],
        includes: [],
        groupBy: [],
        aggregates: [],
        customProperties: {}
    };

    constructor(private tableName: string, private transport: (query: ConduitQuery) => Promise<any>) {}

    private clone(): DbSet<T> {
        const newSet = new DbSet<T>(this.tableName, this.transport);
        newSet.query = JSON.parse(JSON.stringify(this.query));
        return newSet;
    }

    public where(predicate: (entity: EntityProxy<T>) => void): DbSet<T> {
        const next = this.clone();
        // Create a proxy that captures the operations into the new instance
        const proxy = next.createProxy();
        predicate(proxy);
        return next;
    }

    public orderBy(selector: (entity: EntityProxy<T>) => any): DbSet<T> {
        const next = this.clone();
        next.addSort(selector, false);
        return next;
    }

    public orderByDescending(selector: (entity: EntityProxy<T>) => any): DbSet<T> {
        const next = this.clone();
        next.addSort(selector, true);
        return next;
    }

    public skip(count: number): DbSet<T> {
        const next = this.clone();
        next.query.skip = count;
        return next;
    }

    public take(count: number): DbSet<T> {
        const next = this.clone();
        next.query.take = count;
        return next;
    }

    public async toListAsync(): Promise<T[]> {
        // In a real app, we would wrap the query in an envelope with the table name
        // For now, we just send the query
        console.log(`Executing query on table: ${this.tableName}`);
        return await this.transport(this.query);
    }

    // --- Internal Helpers ---

    private createProxy(pathPrefix: string = ''): EntityProxy<T> {
        return new Proxy({}, {
            get: (_target, prop: string) => {
                const fieldName = pathPrefix ? `${pathPrefix}.${prop}` : prop;
                return this.createFieldHandler(fieldName);
            }
        }) as EntityProxy<T>;
    }

    private createFieldHandler(fieldName: string) {
        const addFilter = (op: FilterOperator, val: any) => {
            this.query.filters.push({
                fieldName: fieldName,
                operator: op,
                value: val,
                logic: LogicOperator.And,
                group: []
            });
        };

        return {
            eq: (v: any) => addFilter(FilterOperator.Eq, v),
            neq: (v: any) => addFilter(FilterOperator.Neq, v),
            gt: (v: any) => addFilter(FilterOperator.Gt, v),
            lt: (v: any) => addFilter(FilterOperator.Lt, v),
            gte: (v: any) => addFilter(FilterOperator.Gte, v),
            lte: (v: any) => addFilter(FilterOperator.Lte, v),
            contains: (v: any) => addFilter(FilterOperator.Contains, v),
            startsWith: (v: any) => addFilter(FilterOperator.StartsWith, v),
            endsWith: (v: any) => addFilter(FilterOperator.EndsWith, v),
            in: (v: any) => addFilter(FilterOperator.In, v),
            isTrue: () => addFilter(FilterOperator.Eq, true),
            isFalse: () => addFilter(FilterOperator.Eq, false),
            
            // Nested collections (Any/All)
            any: (_predicate: (item: any) => void) => {
                // This is tricky. We need to capture the filters generated by the predicate
                // and nest them inside the 'value' of the Any filter.
                // For this MVP, we'll simplify.
                // Real implementation requires a temporary context to capture sub-filters.
            }
        };
    }

    private addSort(selector: (entity: EntityProxy<T>) => any, descending: boolean) {
        // We need to know WHICH field was accessed.
        // We can pass a proxy that records the access.
        let accessedField = '';
        const proxy = new Proxy({}, {
            get: (_target, prop: string) => {
                accessedField = prop;
                return {}; // Return dummy
            }
        });
        
        selector(proxy as EntityProxy<T>);
        
        if (accessedField) {
            this.query.sorts.push({
                fieldName: accessedField,
                isDescending: descending
            });
        }
    }
}
