import { useEffect, useMemo, useState } from 'react';
import {
  Bar,
  BarChart,
  Cell,
  Layer,
  Pie,
  PieChart,
  Rectangle,
  ResponsiveContainer,
  Sankey,
  Tooltip,
  XAxis,
  YAxis
} from 'recharts';
import type { SankeyLinkProps, SankeyNodeProps } from 'recharts';
import { Card, CardContent, CardHeader, Dropdown, Spinner } from '@heroui/react';
import { ChevronDown } from 'lucide-react';
import { useSpendingPlanExpenses, useSpendingPlanIncomes } from '../../hooks/spendingPlans';
import { useCategories } from '../../hooks/categories';
import { cadenceBreakdown } from '../../utils/cadence';
import { formatMoney, minorToMajor } from '../../utils/money';
import type { Category } from '../../types/categories';
import type { SpendingPlanExpense, SpendingPlanIncome, SpendingPlanSummary } from '../../types/spendingPlans';

/** Categorical palette for composition slices (works on dark + light). */
const PALETTE = ['var(--accent)', '#38bdf8', '#a78bfa', '#fbbf24', '#fb7185', '#34d399', '#f472b6', '#60a5fa'];
const OTHER_COLOR = '#94a3b8';

/** Teal/green family used for the income side of the cash-flow Sankey. */
const INCOME_COLOR = '#2dd4bf';
const INCOME_SOURCE_COLOR = '#5eead4';
const SAVINGS_COLOR = '#4ade80';

/** Per-group expense colors for the cash-flow Sankey (leaves inherit their group's color). */
const FLOW_PALETTE = [
  '#fb923c',
  '#f472b6',
  '#c084fc',
  '#fbbf24',
  '#f87171',
  '#60a5fa',
  '#a78bfa',
  '#22d3ee',
  '#facc15',
  '#fb7185',
  '#34d399',
  '#818cf8',
  '#e879f9',
  '#fdba74',
  '#a3e635',
  '#38bdf8'
];

/** How the cash-flow Sankey groups the expense side. */
type FlowMode = 'category' | 'group' | 'category-group';

const FLOW_MODE_OPTIONS: ReadonlyArray<{ id: FlowMode; label: string }> = [
  { id: 'category', label: 'By category' },
  { id: 'group', label: 'By group' },
  { id: 'category-group', label: 'By category & group' }
];

/** A node in the cash-flow Sankey, carrying display metadata recharts preserves. */
interface FlowNode {
  name: string;
  color: string;
  icon?: string;
}

interface FlowData {
  nodes: FlowNode[];
  links: { source: number; target: number; value: number }[];
  /** Total monthly income (minor units), used for node percentage labels. */
  totalIncome: number;
}

/** Recharts enriches each node payload with the laid-out geometry plus our {@link FlowNode} fields. */
type FlowNodePayload = FlowNode & { value: number; depth: number };

/** A single composition slice (monthly amount in minor units). */
interface Slice {
  name: string;
  value: number;
  color: string;
}

interface SpendingPlanChartsProps {
  spendingPlanId: number;
  currency: string;
  summary: SpendingPlanSummary | undefined;
  summaryLoading: boolean;
}

/** How a composition donut groups its line items. */
type DonutMode = 'source' | 'group' | 'category';

const DONUT_MODE_OPTIONS: ReadonlyArray<{ id: DonutMode; label: string }> = [
  { id: 'source', label: 'By source' },
  { id: 'group', label: 'By group' },
  { id: 'category', label: 'By category' }
];

/** Reduces named monthly entries to a top-N (+ "Other") palette-colored slice list. */
function toSlices(entries: ReadonlyArray<{ name: string; value: number }>): Slice[] {
  const sorted = entries.filter(entry => entry.value > 0).sort((a, b) => b.value - a.value);

  const TOP = 6;
  const top = sorted.slice(0, TOP);
  const rest = sorted.slice(TOP);

  const slices: Slice[] = top.map((entry, index) => ({ ...entry, color: PALETTE[index % PALETTE.length] }));

  if (rest.length > 0) {
    slices.push({
      name: `Other (${rest.length})`,
      value: rest.reduce((sum, entry) => sum + entry.value, 0),
      color: OTHER_COLOR
    });
  }

  return slices;
}

/**
 * Builds a top-N (+ "Other") monthly composition from line items, normalized to
 * monthly minor units and sorted by size. By default ("source") each line item
 * is its own slice; "category"/"group" aggregate by the item's category or its
 * parent group.
 */
function buildComposition(
  items: ReadonlyArray<SpendingPlanIncome | SpendingPlanExpense>,
  categoriesById?: Map<number, Category>,
  mode: DonutMode = 'source'
): Slice[] {
  const monthlyOf = (item: SpendingPlanIncome | SpendingPlanExpense): number =>
    Math.round(cadenceBreakdown(item.amount, item.cadence).monthly);

  if (mode === 'source' || !categoriesById) {
    return toSlices(items.map(item => ({ name: item.name, value: monthlyOf(item) })));
  }

  const buckets = new Map<string, { name: string; value: number }>();
  for (const item of items) {
    const value = monthlyOf(item);
    if (value <= 0) {
      continue;
    }
    const category = categoriesById.get(item.categoryId);
    const target =
      mode === 'category' ? category : category?.parentId != null ? categoriesById.get(category.parentId) : category;

    const key = target ? `cat-${target.id}` : `item-${item.id}`;
    const label = target ? (target.icon ? `${target.icon} ${target.name}` : target.name) : item.name;

    const existing = buckets.get(key);
    if (existing) {
      existing.value += value;
    } else {
      buckets.set(key, { name: label, value });
    }
  }

  return toSlices([...buckets.values()]);
}

/** Aggregates monthly minor-unit totals keyed by an id, preserving a display name and icon. */
interface Bucket {
  name: string;
  icon?: string;
  value: number;
}

/**
 * Builds the income → (Income) → expenses cash-flow graph for the Sankey chart.
 * Income line items fan in by their category; the surplus (income − expenses)
 * flows to a "Savings" node. Expenses fan out per the selected {@link FlowMode}:
 * by leaf category, by parent group, or grouped then split into categories. All
 * figures are normalized to a monthly cadence (minor units).
 */
function buildFlow(
  incomes: ReadonlyArray<SpendingPlanIncome>,
  expenses: ReadonlyArray<SpendingPlanExpense>,
  categoriesById: Map<number, Category>,
  mode: FlowMode
): FlowData | null {
  const monthlyOf = (item: SpendingPlanIncome | SpendingPlanExpense): number =>
    Math.round(cadenceBreakdown(item.amount, item.cadence).monthly);

  // Income sources, aggregated by category (falling back to the item name).
  const incomeBuckets = new Map<string, Bucket>();
  for (const item of incomes) {
    const value = monthlyOf(item);
    if (value <= 0) {
      continue;
    }
    const category = categoriesById.get(item.categoryId);
    const key = category ? `cat-${category.id}` : `item-${item.id}`;
    const existing = incomeBuckets.get(key);
    if (existing) {
      existing.value += value;
    } else {
      incomeBuckets.set(key, { name: category?.name ?? item.name, icon: category?.icon ?? undefined, value });
    }
  }

  const totalIncome = [...incomeBuckets.values()].reduce((sum, b) => sum + b.value, 0);
  if (totalIncome <= 0) {
    return null;
  }

  // Expenses, aggregated by parent group then leaf category.
  interface Group extends Bucket {
    leaves: Map<string, Bucket>;
  }
  const groups = new Map<string, Group>();
  for (const item of expenses) {
    const value = monthlyOf(item);
    if (value <= 0) {
      continue;
    }
    const category = categoriesById.get(item.categoryId);
    const parent = category?.parentId != null ? categoriesById.get(category.parentId) : undefined;
    const groupKey = parent ? `grp-${parent.id}` : category ? `cat-${category.id}` : `item-${item.id}`;
    const groupName = parent?.name ?? category?.name ?? 'Other';
    const groupIcon = parent?.icon ?? (parent ? undefined : (category?.icon ?? undefined));
    const leafKey = category ? `cat-${category.id}` : `item-${item.id}`;

    let group = groups.get(groupKey);
    if (!group) {
      group = { name: groupName, icon: groupIcon ?? undefined, value: 0, leaves: new Map() };
      groups.set(groupKey, group);
    }
    group.value += value;

    const leaf = group.leaves.get(leafKey);
    if (leaf) {
      leaf.value += value;
    } else {
      group.leaves.set(leafKey, { name: category?.name ?? item.name, icon: category?.icon ?? undefined, value });
    }
  }

  const nodes: FlowNode[] = [];
  const links: FlowData['links'] = [];
  const addNode = (node: FlowNode): number => nodes.push(node) - 1;

  // Income column → central Income node.
  const incomeIdx = addNode({ name: 'Income', color: INCOME_COLOR });
  for (const bucket of [...incomeBuckets.values()].sort((a, b) => b.value - a.value)) {
    const idx = addNode({ name: bucket.name, color: INCOME_SOURCE_COLOR, icon: bucket.icon });
    links.push({ source: idx, target: incomeIdx, value: bucket.value });
  }

  // Surplus → Savings.
  const totalExpenses = [...groups.values()].reduce((sum, g) => sum + g.value, 0);
  const savings = totalIncome - totalExpenses;
  if (savings > 0) {
    const savingsIdx = addNode({ name: 'Savings', color: SAVINGS_COLOR, icon: '💰' });
    links.push({ source: incomeIdx, target: savingsIdx, value: savings });
  }

  // Expense side, ordered largest-first so the heaviest flows sit on top.
  const sortedGroups = [...groups.values()].sort((a, b) => b.value - a.value);
  sortedGroups.forEach((group, index) => {
    const color = FLOW_PALETTE[index % FLOW_PALETTE.length];
    const leaves = [...group.leaves.values()].sort((a, b) => b.value - a.value);

    if (mode === 'group') {
      const groupIdx = addNode({ name: group.name, color, icon: group.icon });
      links.push({ source: incomeIdx, target: groupIdx, value: group.value });
    } else if (mode === 'category') {
      for (const leaf of leaves) {
        const leafIdx = addNode({ name: leaf.name, color, icon: leaf.icon });
        links.push({ source: incomeIdx, target: leafIdx, value: leaf.value });
      }
    } else {
      const groupIdx = addNode({ name: group.name, color, icon: group.icon });
      links.push({ source: incomeIdx, target: groupIdx, value: group.value });
      for (const leaf of leaves) {
        const leafIdx = addNode({ name: leaf.name, color, icon: leaf.icon });
        links.push({ source: groupIdx, target: leafIdx, value: leaf.value });
      }
    }
  });

  return { nodes, links, totalIncome };
}

function compactMoney(minorUnits: number, currency: string): string {
  const major = minorToMajor(minorUnits, currency);
  const abs = Math.abs(major);
  const sign = major < 0 ? '-' : '';

  if (abs >= 1000) {
    return `${sign}$${Math.round(abs / 1000)}k`;
  }

  return `${sign}$${Math.round(abs)}`;
}

/**
 * Visualizes a spending plan: monthly income vs expenses vs remaining, plus
 * income-source and expense-breakdown composition donuts. All figures are
 * normalized to a monthly cadence. Line items are loaded client-side (the set is
 * small).
 */
export function SpendingPlanCharts({ spendingPlanId, currency, summary, summaryLoading }: SpendingPlanChartsProps) {
  const incomesQuery = useSpendingPlanIncomes(spendingPlanId);
  const expensesQuery = useSpendingPlanExpenses(spendingPlanId);
  const { categoriesById, isLoading: categoriesLoading } = useCategories();
  const [flowMode, setFlowMode] = useState<FlowMode>('category-group');
  const [incomeMode, setIncomeMode] = useState<DonutMode>('source');
  const [expenseMode, setExpenseMode] = useState<DonutMode>('source');

  // Eagerly load every page so the composition reflects all line items.
  useEffect(() => {
    if (incomesQuery.hasNextPage && !incomesQuery.isFetchingNextPage) {
      void incomesQuery.fetchNextPage();
    }
  }, [incomesQuery.hasNextPage, incomesQuery.isFetchingNextPage, incomesQuery]);

  useEffect(() => {
    if (expensesQuery.hasNextPage && !expensesQuery.isFetchingNextPage) {
      void expensesQuery.fetchNextPage();
    }
  }, [expensesQuery.hasNextPage, expensesQuery.isFetchingNextPage, expensesQuery]);

  const incomes = useMemo(() => incomesQuery.data?.pages.flatMap(page => page.nodes ?? []) ?? [], [incomesQuery.data]);
  const expenses = useMemo(
    () => expensesQuery.data?.pages.flatMap(page => page.nodes ?? []) ?? [],
    [expensesQuery.data]
  );

  const incomeSlices = useMemo(
    () => buildComposition(incomes, categoriesById, incomeMode),
    [incomes, categoriesById, incomeMode]
  );
  const expenseSlices = useMemo(
    () => buildComposition(expenses, categoriesById, expenseMode),
    [expenses, categoriesById, expenseMode]
  );

  const flowData = useMemo(
    () => buildFlow(incomes, expenses, categoriesById, flowMode),
    [incomes, expenses, categoriesById, flowMode]
  );

  const cashFlow = useMemo(() => {
    if (!summary) {
      return [];
    }
    return [
      { label: 'Income', value: Math.round(summary.income.monthly), fill: 'var(--success)' },
      { label: 'Expenses', value: Math.round(summary.expenses.monthly), fill: 'var(--danger)' },
      { label: 'Remaining', value: Math.round(summary.remaining.monthly), fill: 'var(--accent)' }
    ];
  }, [summary]);

  const isLoading = incomesQuery.isLoading || expensesQuery.isLoading || summaryLoading || categoriesLoading;

  if (isLoading) {
    return (
      <Card className="bg-surface border border-border">
        <CardContent className="flex justify-center py-16">
          <Spinner size="lg" color="accent" />
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <CashFlowSankeyCard flowData={flowData} currency={currency} mode={flowMode} onModeChange={setFlowMode} />

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        <Card className="bg-surface border border-border">
          <CardHeader className="px-6 pt-6">
            <h2 className="text-lg font-semibold">Monthly cash flow</h2>
            <p className="text-sm text-muted">Income, expenses, and what's left over.</p>
          </CardHeader>
          <CardContent className="px-2 pb-4 sm:px-4">
            <div className="h-64 w-full">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={cashFlow} margin={{ top: 8, right: 8, left: 8, bottom: 0 }}>
                  <XAxis
                    dataKey="label"
                    tick={{ fill: 'var(--muted)', fontSize: 12 }}
                    tickLine={false}
                    axisLine={{ stroke: 'var(--border)' }}
                  />
                  <YAxis
                    tick={{ fill: 'var(--muted)', fontSize: 12 }}
                    tickLine={false}
                    axisLine={false}
                    width={48}
                    tickFormatter={(value: number) => compactMoney(value, currency)}
                  />
                  <Tooltip
                    cursor={{ fill: 'var(--surface-secondary)', opacity: 0.5 }}
                    formatter={value => [formatMoney(Number(value), currency), '']}
                    contentStyle={{
                      background: 'var(--overlay)',
                      border: '1px solid var(--border)',
                      borderRadius: 8,
                      color: 'var(--foreground)'
                    }}
                    labelStyle={{ color: 'var(--foreground)' }}
                  />
                  <Bar dataKey="value" radius={[4, 4, 0, 0]} maxBarSize={64}>
                    {cashFlow.map(entry => (
                      <Cell key={entry.label} fill={entry.fill} />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>

        <DonutCard
          title="Income sources"
          slices={incomeSlices}
          currency={currency}
          emptyText="No income to chart yet."
          mode={incomeMode}
          onModeChange={setIncomeMode}
          modeAriaLabel="Change income grouping"
        />
        <DonutCard
          title="Expense breakdown"
          slices={expenseSlices}
          currency={currency}
          emptyText="No expenses to chart yet."
          mode={expenseMode}
          onModeChange={setExpenseMode}
          modeAriaLabel="Change expense grouping"
        />
      </div>
    </div>
  );
}

/** A composition donut with a centered monthly total, a labeled legend, and a grouping-mode selector. */
function DonutCard({
  title,
  slices,
  currency,
  emptyText,
  mode,
  onModeChange,
  modeAriaLabel
}: {
  title: string;
  slices: Slice[];
  currency: string;
  emptyText: string;
  mode: DonutMode;
  onModeChange(mode: DonutMode): void;
  modeAriaLabel: string;
}) {
  const total = slices.reduce((sum, slice) => sum + slice.value, 0);

  return (
    <Card className="bg-surface border border-border">
      <CardHeader className="px-6 pt-6 flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h2 className="text-lg font-semibold">{title}</h2>
          <p className="text-sm text-muted">Normalized to a monthly cadence.</p>
        </div>
        <ChartModeDropdown
          options={DONUT_MODE_OPTIONS}
          value={mode}
          onChange={onModeChange}
          ariaLabel={modeAriaLabel}
        />
      </CardHeader>
      <CardContent className="px-6 pb-6">
        {slices.length === 0 || total === 0 ? (
          <p className="py-10 text-center text-sm text-muted">{emptyText}</p>
        ) : (
          <>
            <div className="relative h-48">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={slices}
                    dataKey="value"
                    nameKey="name"
                    innerRadius={58}
                    outerRadius={80}
                    paddingAngle={2}
                    stroke="none"
                  >
                    {slices.map(slice => (
                      <Cell key={slice.name} fill={slice.color} />
                    ))}
                  </Pie>
                  <Tooltip content={<DonutTooltip currency={currency} total={total} />} />
                </PieChart>
              </ResponsiveContainer>
              <div className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center">
                <span className="text-xs text-muted">Monthly</span>
                <span className="text-lg font-bold tabular-nums">{formatMoney(total, currency)}</span>
              </div>
            </div>

            <ul className="mt-4 space-y-1.5">
              {slices.map(slice => (
                <li key={slice.name} className="flex items-center gap-2 text-sm">
                  <span
                    className="size-2.5 shrink-0 rounded-full"
                    style={{ backgroundColor: slice.color }}
                    aria-hidden="true"
                  />
                  <span className="min-w-0 flex-1 truncate text-foreground">{slice.name}</span>
                  <span className="shrink-0 tabular-nums text-muted">{Math.round((slice.value / total) * 100)}%</span>
                  <span className="w-24 shrink-0 text-right tabular-nums text-foreground">
                    {formatMoney(slice.value, currency)}
                  </span>
                </li>
              ))}
            </ul>
          </>
        )}
      </CardContent>
    </Card>
  );
}

interface DonutTooltipPayloadItem {
  name: string;
  value: number;
}

/** Tooltip showing a slice's monthly amount and share of the total. */
function DonutTooltip({
  active,
  payload,
  currency,
  total
}: {
  active?: boolean;
  payload?: DonutTooltipPayloadItem[];
  currency: string;
  total: number;
}) {
  if (!active || !payload?.length) {
    return null;
  }

  const slice = payload[0];
  const pct = total > 0 ? Math.round((slice.value / total) * 100) : 0;

  return (
    <div className="rounded-lg border border-border bg-overlay px-3 py-2 text-sm shadow-lg">
      <p className="font-medium text-foreground">{slice.name}</p>
      <p className="text-muted">
        <span className="tabular-nums text-foreground">{formatMoney(slice.value, currency)}</span> · {pct}%
      </p>
    </div>
  );
}

/**
 * A small Monarch-style dropdown for switching a chart's grouping mode. Generic
 * over the mode id type so it works for both the Sankey and the donut charts.
 */
function ChartModeDropdown<T extends string>({
  options,
  value,
  onChange,
  ariaLabel
}: {
  options: ReadonlyArray<{ id: T; label: string }>;
  value: T;
  onChange(value: T): void;
  ariaLabel: string;
}) {
  const activeLabel = options.find(option => option.id === value)?.label ?? options[0]?.label ?? '';

  return (
    <Dropdown>
      <Dropdown.Trigger
        aria-label={ariaLabel}
        className="inline-flex items-center gap-2 rounded-lg border border-border bg-surface-secondary px-3 py-1.5 text-sm font-medium text-foreground outline-none transition-colors hover:bg-surface focus-visible:ring-2 focus-visible:ring-focus"
      >
        {activeLabel}
        <ChevronDown className="size-4 text-muted" />
      </Dropdown.Trigger>
      <Dropdown.Popover placement="bottom end" className="min-w-48">
        <Dropdown.Menu
          aria-label={ariaLabel}
          selectionMode="single"
          selectedKeys={new Set([value])}
          onAction={key => onChange(key as T)}
        >
          {options.map(option => (
            <Dropdown.Item key={option.id} id={option.id}>
              {option.label}
            </Dropdown.Item>
          ))}
        </Dropdown.Menu>
      </Dropdown.Popover>
    </Dropdown>
  );
}

/**
 * A Monarch-style cash-flow Sankey: income sources flow into a central Income
 * node, which fans out to expense groups/categories (per the selected mode) plus
 * a Savings node for the surplus. Includes a mode selector to switch the expense
 * grouping. All amounts are normalized to a monthly cadence.
 */
function CashFlowSankeyCard({
  flowData,
  currency,
  mode,
  onModeChange
}: {
  flowData: FlowData | null;
  currency: string;
  mode: FlowMode;
  onModeChange(mode: FlowMode): void;
}) {
  // Size the chart so dense graphs (many categories) keep readable row spacing.
  const nodeCount = flowData?.nodes.length ?? 0;
  const chartHeight = Math.max(380, Math.min(nodeCount * 30, 1100));
  // Income sources sit at depth 0; the deepest column is leaves (3) or direct expense targets (2).
  const maxDepth = mode === 'category-group' ? 3 : 2;

  return (
    <Card className="bg-surface border border-border">
      <CardHeader className="px-6 pt-6 flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h2 className="text-lg font-semibold">Cash flow</h2>
          <p className="text-sm text-muted">Where your monthly income flows.</p>
        </div>
        <ChartModeDropdown
          options={FLOW_MODE_OPTIONS}
          value={mode}
          onChange={onModeChange}
          ariaLabel="Change cash-flow grouping"
        />
      </CardHeader>
      <CardContent className="px-2 pb-6 sm:px-4">
        {!flowData || flowData.links.length === 0 ? (
          <p className="py-16 text-center text-sm text-muted">Add income and expenses to see how your money flows.</p>
        ) : (
          <div className="w-full overflow-x-auto">
            <div style={{ height: chartHeight, minWidth: 640 }}>
              <ResponsiveContainer width="100%" height="100%">
                <Sankey
                  data={flowData}
                  nodeWidth={14}
                  nodePadding={22}
                  linkCurvature={0.5}
                  iterations={64}
                  margin={{ top: 16, right: 16, bottom: 16, left: 16 }}
                  node={nodeProps => (
                    <SankeyFlowNode
                      {...nodeProps}
                      currency={currency}
                      totalIncome={flowData.totalIncome}
                      maxDepth={maxDepth}
                    />
                  )}
                  link={<SankeyFlowLink />}
                >
                  <Tooltip content={<SankeyFlowTooltip currency={currency} />} />
                </Sankey>
              </ResponsiveContainer>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

/** Renders a Sankey node bar with its label (icon + name, amount, and % of income); the label sits on the outward side of the node so the chart can fill the container width. */
function SankeyFlowNode(props: SankeyNodeProps & { currency: string; totalIncome: number; maxDepth: number }) {
  const { x, y, width, height, index, payload, currency, totalIncome, maxDepth } = props;
  const node = payload as unknown as FlowNodePayload;
  const nx = Number(x);
  const ny = Number(y);
  const w = Number(width);
  const h = Number(height);
  const pct = totalIncome > 0 ? Math.round((node.value / totalIncome) * 100) : 0;

  // Labels point inward: right of the node on the left half, left of the node on
  // the right half — keeping all text inside the chart so it can span full width.
  const isRightHalf = node.depth > maxDepth / 2;
  const anchor = isRightHalf ? 'end' : 'start';
  const labelX = isRightHalf ? nx - 10 : nx + w + 10;

  return (
    <Layer key={`flow-node-${index}`}>
      <Rectangle x={nx} y={ny} width={w} height={h} fill={node.color} fillOpacity={1} radius={2} />
      {h >= 5 && (
        <text x={labelX} y={ny + h / 2} textAnchor={anchor}>
          <tspan x={labelX} dy="-0.3em" fill="var(--foreground)" fontSize={15} fontWeight={600}>
            {node.icon ? `${node.icon} ${node.name}` : node.name}
          </tspan>
          <tspan x={labelX} dy="1.3em" fill="var(--muted)" fontSize={13}>
            {`${formatMoney(node.value, currency)} (${pct}%)`}
          </tspan>
        </text>
      )}
    </Layer>
  );
}

/** Renders a Sankey link as a bezier ribbon tinted by the destination node's color. */
function SankeyFlowLink(props: Partial<SankeyLinkProps>) {
  const { sourceX, sourceY, sourceControlX, targetControlX, targetX, targetY, linkWidth, index, payload } = props;
  const target = payload?.target as unknown as FlowNode | undefined;
  const stroke = target?.color ?? OTHER_COLOR;

  return (
    <path
      key={`flow-link-${index}`}
      d={`M${sourceX},${sourceY}C${sourceControlX},${sourceY} ${targetControlX},${targetY} ${targetX},${targetY}`}
      fill="none"
      stroke={stroke}
      strokeWidth={Math.max(1, Number(linkWidth))}
      strokeOpacity={0.4}
    />
  );
}

interface SankeyFlowTooltipPayloadItem {
  payload?: {
    name?: string;
    value?: number;
    source?: FlowNode;
    target?: FlowNode;
  };
}

/** Tooltip for the cash-flow Sankey, handling both node hovers and link hovers. */
function SankeyFlowTooltip({
  active,
  payload,
  currency
}: {
  active?: boolean;
  payload?: SankeyFlowTooltipPayloadItem[];
  currency: string;
}) {
  if (!active || !payload?.length) {
    return null;
  }

  const data = payload[0].payload;
  if (!data) {
    return null;
  }

  const isLink = Boolean(data.source && data.target);
  const title = isLink ? `${data.source?.name} → ${data.target?.name}` : (data.name ?? '');

  return (
    <div className="rounded-lg border border-border bg-overlay px-3 py-2 text-sm shadow-lg">
      <p className="font-medium text-foreground">{title}</p>
      <p className="tabular-nums text-muted">{formatMoney(Number(data.value ?? 0), currency)}</p>
    </div>
  );
}
