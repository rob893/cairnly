import type { ReactNode } from 'react';
import { Button, Card, CardContent } from '@heroui/react';

interface EmptyStateAction {
  /** Text shown in the primary empty-state action. */
  label: string;
  /** Runs when the empty-state action is pressed. */
  onPress(): void;
}

interface EmptyStateProps {
  /** Decorative icon that reinforces the empty-state context. */
  icon: ReactNode;
  /** Short headline describing what is missing. */
  title: string;
  /** Supporting copy that tells users what to do next. */
  subtitle: string;
  /** Optional primary action for first-run guidance. */
  cta?: EmptyStateAction;
}

/** Shared card-based empty state used across first-run and zero-result surfaces. */
export function EmptyState({ icon, title, subtitle, cta }: EmptyStateProps) {
  return (
    <Card className="bg-surface border border-border">
      <CardContent className="space-y-4 p-10 text-center">
        <div className="mx-auto flex size-12 items-center justify-center rounded-full bg-accent/10 text-accent">
          {icon}
        </div>
        <div className="space-y-2">
          <p className="text-lg font-semibold text-foreground">{title}</p>
          <p className="mx-auto max-w-md text-sm text-muted">{subtitle}</p>
        </div>
        {cta && (
          <div className="pt-1">
            <Button onPress={cta.onPress}>{cta.label}</Button>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
