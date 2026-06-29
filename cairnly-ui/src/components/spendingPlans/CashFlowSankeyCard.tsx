import { Card, CardContent, CardHeader } from '@heroui/react';
import { Layer, Rectangle, ResponsiveContainer, Sankey, Tooltip } from 'recharts';
import type { SankeyLinkProps, SankeyNodeProps } from 'recharts';
import { formatMoney } from '../../utils/money';
import { ChartModeDropdown } from './ChartModeDropdown';
import {
  getFlowLinkTarget,
  isFlowNodePayload,
  OTHER_COLOR,
  type FlowData,
  type FlowMode,
  type FlowNode
} from './spendingPlanChartUtils';

const FLOW_MODE_OPTIONS: ReadonlyArray<{ id: FlowMode; label: string }> = [
  { id: 'category', label: 'By category' },
  { id: 'group', label: 'By group' },
  { id: 'category-group', label: 'By category & group' }
];

/** A cash-flow Sankey showing income sources, expense destinations, and monthly surplus. */
export function CashFlowSankeyCard({
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
  const nodeCount = flowData?.nodes.length ?? 0;
  const chartHeight = Math.max(380, Math.min(nodeCount * 30, 1100));
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

function SankeyFlowNode(props: SankeyNodeProps & { currency: string; totalIncome: number; maxDepth: number }) {
  const { x, y, width, height, index, payload, currency, totalIncome, maxDepth } = props;
  const node = isFlowNodePayload(payload) ? payload : undefined;

  if (!node) {
    return null;
  }

  const nx = Number(x);
  const ny = Number(y);
  const w = Number(width);
  const h = Number(height);
  const pct = totalIncome > 0 ? Math.round((node.value / totalIncome) * 100) : 0;
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

function SankeyFlowLink(props: Partial<SankeyLinkProps>) {
  const { sourceX, sourceY, sourceControlX, targetControlX, targetX, targetY, linkWidth, index, payload } = props;
  const target = getFlowLinkTarget(payload);
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
